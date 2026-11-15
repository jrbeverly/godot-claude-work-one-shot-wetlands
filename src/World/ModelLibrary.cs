using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Loads the imported glTF kit models (Quaternius nature + village, KayKit landmarks) by bare name,
/// caches them, and hands out meshes ready for MultiMesh rendering: leaf/grass surfaces swapped to the
/// tinted <c>foliage.gdshader</c>, everything else kept but flattened to roughness 1 / specular 0
/// so nothing in the world reads as glossy PBR.
/// </summary>
public static class ModelLibrary
{
    private static readonly string[] PackDirs =
    {
        "res://assets/third_party/quaternius_nature/",
        "res://assets/third_party/quaternius_village/",
        "res://assets/third_party/kaykit_landmarks/",
    };

    /// <summary>One MeshInstance3D of a kit model: its mesh and its transform relative to the scene root.</summary>
    public sealed record Part(Mesh Mesh, Transform3D Local);

    /// <summary>A loaded kit model: original meshes plus the combined local-space bounds.</summary>
    public sealed class Model
    {
        public string Name = "";
        public List<Part> Parts = new();
        public Aabb Bounds;
        /// <summary>First part's mesh; the nature kit is one mesh per model, which is what MultiMesh needs.</summary>
        public Mesh Mesh => Parts[0].Mesh;
    }

    private static readonly Dictionary<string, Model?> Cache = new();
    private static readonly Dictionary<string, Mesh> StyledCache = new();
    private static Shader? _foliageShader;

    /// <summary>Load (once) a kit model by bare name, e.g. "CommonTree_1". Null with an error print if missing.</summary>
    public static Model? Get(string name)
    {
        if (Cache.TryGetValue(name, out var cached)) return cached;
        Model? model = ProceduralFoliage.Build(name);
        if (model != null) { Cache[name] = model; return model; }
        foreach (var dir in PackDirs)
        {
            var path = dir + name + ".gltf";
            if (!ResourceLoader.Exists(path)) continue;
            var scene = GD.Load<PackedScene>(path);
            if (scene == null) continue;
            var root = scene.Instantiate<Node3D>();
            model = new Model { Name = name };
            Collect(root, root, Transform3D.Identity, model);
            root.QueueFree();
            if (model.Parts.Count == 0) { GD.PrintErr($"ModelLibrary: {path} has no MeshInstance3D"); model = null; }
            break;
        }
        if (model == null) GD.PrintErr($"ModelLibrary: model '{name}' not found in any pack");
        Cache[name] = model;
        return model;
    }

    private static void Collect(Node node, Node3D root, Transform3D parentXf, Model model)
    {
        var xf = parentXf;
        if (node is Node3D n3 && node != root) xf = parentXf * n3.Transform;
        if (node is MeshInstance3D mi && mi.Mesh != null)
        {
            // Per-instance material overrides from the importer would be lost in a MultiMesh; bake them onto a copy.
            var mesh = (Mesh)mi.Mesh.Duplicate();
            if (mesh is ArrayMesh am)
                for (int s = 0; s < am.GetSurfaceCount(); s++)
                    if (mi.GetSurfaceOverrideMaterial(s) is { } ov) am.SurfaceSetMaterial(s, ov);
            var part = new Part(mesh, xf);
            var b = xf * mesh.GetAabb();
            model.Bounds = model.Parts.Count == 0 ? b : model.Bounds.Merge(b);
            model.Parts.Add(part);
        }
        foreach (var child in node.GetChildren()) Collect(child, root, xf, model);
    }

    /// <summary>Material names from the kits that are painted alpha-cut cards (leaves, grass, petals) rather than solid surfaces.</summary>
    public static bool IsFoliageMaterial(Material? m)
    {
        var n = m?.ResourceName ?? "";
        return n.Contains("Leaves") || n.Contains("Leaf") || n.Contains("Grass") || n.Contains("Flowers") || n.Contains("Clover");
    }

    /// <summary>
    /// A copy of the model's mesh with foliage surfaces on the tinted wind shader and other surfaces matte.
    /// Cached per (model, tint, sway) so MultiMeshes of the same look share one mesh (and its LODs).
    /// </summary>
    public static Mesh GetStyledMesh(Model model, Color tint, float swayAmplitude, float swayHeight)
    {
        string key = $"{model.Name}|{tint.ToHtml(false)}|{swayAmplitude:F3}|{swayHeight:F2}";
        if (StyledCache.TryGetValue(key, out var hit)) return hit;

        var mesh = (Mesh)model.Mesh.Duplicate();
        if (mesh is ArrayMesh am)
        {
            for (int s = 0; s < am.GetSurfaceCount(); s++)
            {
                var src = am.SurfaceGetMaterial(s);
                am.SurfaceSetMaterial(s, IsFoliageMaterial(src)
                    ? MakeLeafMaterial(src as StandardMaterial3D, tint, swayAmplitude, swayHeight)
                    : MakeMatteMaterial(src));
            }
        }
        StyledCache[key] = mesh;
        return mesh;
    }

    /// <summary>Tinted, swaying, alpha-scissor leaf material using the albedo texture of the imported StandardMaterial3D.</summary>
    public static ShaderMaterial MakeLeafMaterial(StandardMaterial3D? src, Color tint, float swayAmplitude, float swayHeight)
    {
        _foliageShader ??= GD.Load<Shader>("res://shaders/foliage.gdshader");
        var mat = new ShaderMaterial { Shader = _foliageShader };
        var tex = src?.AlbedoTexture;
        if (tex != null) mat.SetShaderParameter("albedo_tex", tex);
        // Fold the kit's own albedo colour (if any) into the tint so recoloured kits keep their intent.
        var baseCol = src?.AlbedoColor ?? Colors.White;
        string name = src?.ResourceName ?? "";
        if (name.Contains("TwistedTree"))
        {
            // The kit's only red leaf sheet; paint its luminance a leaf green so Bush_Common/TwistedTree fit the zone.
            mat.SetShaderParameter("recolor", 1f);
            mat.SetShaderParameter("recolor_ref", 0.26f);
            baseCol = new Color(0.40f, 0.56f, 0.26f);
        }
        else if (name.Contains("Grass"))
        {
            // Neon stripe sheet: mute it towards the ground palette so tufts read as accents, not highlighter.
            mat.SetShaderParameter("saturation", 0.6f);
            baseCol = new Color(0.82f, 0.86f, 0.66f);
        }
        else if (name.Contains("Flowers"))
        {
            mat.SetShaderParameter("saturation", 0.85f);
            // A non-white tint repaints the kit's multicolour petal sheet in that colour (bog-blue accents).
            if (tint != Colors.White) { mat.SetShaderParameter("recolor", 1f); mat.SetShaderParameter("recolor_ref", 0.5f); }
        }
        // Procedural Wetlands foliage: canopies go dark against the sky (little backlight), reeds and moss curtains
        // take a straw rim from the low sun. Thin painted strips need a lower scissor or they erode in the mips.
        if (name.Contains("MossClump"))
            mat.SetShaderParameter("backlight_strength", 0.16f);
        else if (name.Contains("BogOak"))
        {
            mat.SetShaderParameter("backlight_strength", 0.2f);
            mat.SetShaderParameter("backlight_color", new Color(0.85f, 0.80f, 0.60f));
        }
        else if (name.Contains("MossCurtain"))
        {
            mat.SetShaderParameter("backlight_strength", 0.12f);
            mat.SetShaderParameter("backlight_color", new Color(0.95f, 0.88f, 0.62f));
            mat.SetShaderParameter("alpha_scissor", 0.3f);
            // The curtain swings as a sheet: full amplitude from a few metres up, instead of pinned at the hem.
            mat.SetShaderParameter("sway_amplitude", 0.18f);
            mat.SetShaderParameter("sway_height", MathF.Max(swayHeight * 0.3f, 1f));
        }
        else if (name.Contains("Reeds"))
        {
            mat.SetShaderParameter("backlight_strength", 0.18f);
            mat.SetShaderParameter("backlight_color", new Color(0.95f, 0.88f, 0.62f));
            mat.SetShaderParameter("alpha_scissor", 0.26f);
        }
        mat.SetShaderParameter("tint", new Color(baseCol.R * tint.R, baseCol.G * tint.G, baseCol.B * tint.B, 1f));
        if (!(name.Contains("MossCurtain") || name.Contains("Reeds"))) mat.SetShaderParameter("alpha_scissor", 0.4f);
        mat.SetShaderParameter("sway_amplitude", swayAmplitude);
        mat.SetShaderParameter("sway_height", swayHeight);
        mat.ResourceName = (src?.ResourceName ?? "Leaves") + "_tinted";
        return mat;
    }

    /// <summary>Bark, rock, wood, plaster: keep the painted albedo but kill PBR response.</summary>
    public static Material? MakeMatteMaterial(Material? src)
    {
        if (src is not StandardMaterial3D std) return src;
        var m = (StandardMaterial3D)std.Duplicate();
        m.Roughness = 1f;
        m.Metallic = 0f;
        m.MetallicSpecular = 0f;
        m.NormalEnabled = false;      // normal maps add micro-detail the painted look does not want
        m.VertexColorUseAsAlbedo = true; // kit AO in COLOR_0 plus the MultiMesh per-instance tint
        // Pale blue-white kit rock -> dark mossy bog boulders.
        if ((std.ResourceName ?? "").Contains("Rock", StringComparison.OrdinalIgnoreCase)) m.AlbedoColor *= new Color(0.50f, 0.48f, 0.42f);
        // Salmon-pink kit bark -> warm brown trunks.
        if ((std.ResourceName ?? "").Contains("Bark", StringComparison.OrdinalIgnoreCase)) m.AlbedoColor *= new Color(0.52f, 0.44f, 0.38f);
        m.RoughnessTexture = null;
        m.MetallicTexture = null;
        if (m.Transparency == BaseMaterial3D.TransparencyEnum.AlphaScissor) m.AlphaScissorThreshold = 0.4f;
        return m;
    }
}
