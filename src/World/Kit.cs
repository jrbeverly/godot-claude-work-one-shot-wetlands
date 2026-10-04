using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Tiny glTF model cache for the site kits (Quaternius village + nature, KayKit landmarks).
/// Loads each model once, records its meshes with their root-relative transforms and its merged AABB,
/// and hands out cheap instances that share "muted" copies of the imported materials
/// (roughness 1, specular 0, no normal maps; terracotta roof tiles multiplied toward slate-red;
/// KayKit palette atlas swapped for the muted one). Independent from the foliage subsystem on purpose.
/// </summary>
public sealed class Kit
{
    public sealed record Part(Mesh Mesh, Transform3D Xform, Material[] Materials);

    public sealed class Model
    {
        public string Name = "";
        public List<Part> Parts = new();
        /// <summary>Merged bounds in model-local space (root node at the origin).</summary>
        public Aabb Aabb;
    }

    private const string Root = "res://assets/third_party/";
    private static readonly string[] Folders = { "quaternius_village", "quaternius_nature", "kaykit_landmarks" };
    private static readonly Color RoofMultiply = new("#A8887A");

    private readonly Dictionary<string, Model?> _models = new();
    private readonly Dictionary<string, Material> _muted = new();
    private readonly Dictionary<(string, Color, bool), Material> _tinted = new();
    private readonly Dictionary<(ulong, float, float), Texture2D> _desaturated = new();
    private Texture2D? _kaykitMuted;
    private bool _kaykitTried;

    public Model? Get(string name)
    {
        if (_models.TryGetValue(name, out var cached)) return cached;
        var m = Load(name);
        _models[name] = m;
        return m;
    }

    /// <summary>
    /// A node holding one MeshInstance3D per mesh in the model, with muted materials.
    /// <paramref name="tint"/> multiplies the albedo of every surface whose material name passes <paramref name="tintFilter"/> (all when null).
    /// With <paramref name="recolour"/> the surface's texture is first turned greyscale so the tint sets the hue outright
    /// (needed to turn the kit's red TwistedTree leaves amber; a plain multiply can only darken).
    /// </summary>
    public Node3D Instance(string name, Color? tint = null, Func<string, bool>? tintFilter = null, bool recolour = false)
    {
        var node = new Node3D { Name = name };
        var model = Get(name);
        if (model == null) return node;
        foreach (var part in model.Parts)
        {
            var mi = new MeshInstance3D { Mesh = part.Mesh, Transform = part.Xform };
            for (int s = 0; s < part.Materials.Length; s++)
            {
                var mat = part.Materials[s];
                if (tint is { } t && (tintFilter == null || tintFilter(mat.ResourceName))) mat = Tinted(mat, t, recolour);
                mi.SetSurfaceOverrideMaterial(s, mat);
            }
            node.AddChild(mi);
        }
        return node;
    }

    /// <summary>Muted material multiplied by a colour (cached per material + colour, so instances still batch).</summary>
    public Material Tinted(Material muted, Color tint, bool recolour = false)
    {
        var key = (muted.ResourceName, tint, recolour);
        if (_tinted.TryGetValue(key, out var hit)) return hit;
        var copy = (Material)muted.Duplicate();
        if (copy is StandardMaterial3D std)
        {
            if (recolour && std.AlbedoTexture != null) std.AlbedoTexture = Desaturated(std.AlbedoTexture, 0f, 1.45f);
            std.AlbedoColor *= tint;
        }
        _tinted[key] = copy;
        return copy;
    }

    /// <summary>CPU copy of a texture with its saturation/brightness adjusted (done once per texture, then cached).</summary>
    private Texture2D Desaturated(Texture2D src, float saturation, float brightness)
    {
        var key = (src.GetInstanceId(), saturation, brightness);
        if (_desaturated.TryGetValue(key, out var hit)) return hit;
        var img = src.GetImage();
        if (img == null) return src;
        if (img.IsCompressed()) img.Decompress();
        img.Convert(Image.Format.Rgba8);
        img.AdjustBcs(brightness, 1f, saturation);
        if (!img.HasMipmaps()) img.GenerateMipmaps();
        Texture2D tex = ImageTexture.CreateFromImage(img);
        _desaturated[key] = tex;
        return tex;
    }

    /// <summary>Layout short names (schema: church|tavern|...|tower, wagon|crate) to kit file names.</summary>
    private static string Alias(string name) => name switch
    {
        "church" or "tavern" or "blacksmith" or "windmill" or "lumbermill" or "mine" or "well" => $"building_{name}_blue",
        "tower" => "building_tower_A_blue",
        "wagon" => "Prop_Wagon",
        "crate" => "Prop_Crate",
        "chimney" => "Prop_Chimney2",
        _ => name,
    };

    private Model? Load(string name)
    {
        name = Alias(name);
        string? path = null;
        foreach (var folder in Folders)
        {
            var candidate = $"{Root}{folder}/{name}.gltf";
            if (ResourceLoader.Exists(candidate)) { path = candidate; break; }
        }
        if (path == null) { GD.PushWarning($"Kit: no model named '{name}' in {string.Join(", ", Folders)}"); return null; }

        var scene = GD.Load<PackedScene>(path);
        if (scene == null) { GD.PushWarning($"Kit: failed to load {path}"); return null; }
        var root = scene.Instantiate<Node3D>();
        var model = new Model { Name = name };
        bool first = true;
        Collect(root, root, model, ref first);
        root.Free();
        // Printed once per model: the kit's origins are not where you would guess (roof eaves hang below y=0 etc.).
        GD.Print($"Kit: {name} aabb min {Fmt(model.Aabb.Position)} max {Fmt(model.Aabb.End)} ({model.Parts.Count} mesh)");
        return model;
    }

    private void Collect(Node node, Node3D root, Model model, ref bool first)
    {
        if (node is MeshInstance3D mi && mi.Mesh != null)
        {
            // Root-relative transform without entering the tree.
            var xf = Transform3D.Identity;
            for (Node? n = mi; n != null && n != root; n = n.GetParent())
                if (n is Node3D n3) xf = n3.Transform * xf;

            int count = mi.Mesh.GetSurfaceCount();
            var mats = new Material[count];
            for (int s = 0; s < count; s++)
            {
                var src = mi.GetActiveMaterial(s);
                mats[s] = src != null ? Muted(src) : new StandardMaterial3D { Roughness = 1f, MetallicSpecular = 0f };
            }
            model.Parts.Add(new Part(mi.Mesh, xf, mats));
            var box = xf * mi.Mesh.GetAabb();
            model.Aabb = first ? box : model.Aabb.Merge(box);
            first = false;
        }
        foreach (var child in node.GetChildren()) Collect(child, root, model, ref first);
    }

    /// <summary>Flat, painterly version of an imported material; shared between every model that uses the same material name.</summary>
    private Material Muted(Material src)
    {
        var key = string.IsNullOrEmpty(src.ResourceName) ? src.GetInstanceId().ToString() : src.ResourceName;
        if (_muted.TryGetValue(key, out var hit)) return hit;
        Material result = src;
        if (src is StandardMaterial3D s)
        {
            var m = (StandardMaterial3D)s.Duplicate();
            m.ResourceName = key;
            m.Roughness = 1f;
            m.RoughnessTexture = null;
            m.Metallic = 0f;
            m.MetallicTexture = null;
            m.MetallicSpecular = 0f;
            m.NormalEnabled = false;
            m.NormalTexture = null;
            m.ClearcoatEnabled = false;
            // Terracotta tiles are near-neon orange in the kit: pull most of the saturation out, then warm/darken toward slate-red.
            if (key.Contains("RoundTiles", StringComparison.OrdinalIgnoreCase))
            {
                if (m.AlbedoTexture != null) m.AlbedoTexture = Desaturated(m.AlbedoTexture, 0.38f, 0.92f);
                m.AlbedoColor *= RoofMultiply;
            }
            // The kit's bark is salmon-pink; pull it toward a warm brown so trunks stop reading as candy.
            if (key.Contains("Bark", StringComparison.OrdinalIgnoreCase)) m.AlbedoColor *= new Color(0.86f, 0.78f, 0.72f);
            if (key.Contains("hexagons_medieval", StringComparison.OrdinalIgnoreCase))
            {
                var tex = KayKitMutedAtlas();
                if (tex != null) m.AlbedoTexture = tex;
            }
            // Vegetation cards from the nature kit: keep alpha scissor but light both sides.
            if (m.Transparency == BaseMaterial3D.TransparencyEnum.AlphaScissor) m.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            result = m;
        }
        _muted[key] = result;
        return result;
    }

    private Texture2D? KayKitMutedAtlas()
    {
        if (_kaykitTried) return _kaykitMuted;
        _kaykitTried = true;
        const string p = Root + "kaykit_landmarks/hexagons_medieval_muted.png";
        if (ResourceLoader.Exists(p)) _kaykitMuted = GD.Load<Texture2D>(p);
        else GD.PushWarning($"Kit: {p} missing (run tools/retint_kaykit.py); landmarks keep the saturated palette");
        return _kaykitMuted;
    }

    private static string Fmt(Vector3 v) => $"({v.X:0.00}, {v.Y:0.00}, {v.Z:0.00})";
}
