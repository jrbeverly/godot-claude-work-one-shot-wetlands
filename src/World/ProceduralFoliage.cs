using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Hand-built meshes for the things no kit provides and the Wetlands look needs most: the giant weeping
/// moss tree (thick pale trunk, a few heavy branches, a broad flat canopy of painted leaf cards and long
/// hanging moss curtains), its dark twisted cousin (a DeadTree trunk under a dome of dark leaf cards), and
/// reed clumps (crossed painted cards). Built once per name and handed to <see cref="ModelLibrary"/> like a
/// kit model, so biomes in layout.json use them by name ("MossTree_1".."MossTree_3", "BogOak_1".."BogOak_3",
/// "Reeds_1".."Reeds_3") and FoliagePlacer instances them through MultiMesh as usual.
/// Classic trick, 2004 budget: a canopy is ~40 quads, a trunk a few hundred triangles; silhouette beats detail.
/// </summary>
public static class ProceduralFoliage
{
    private const string TexDir = "res://assets/textures/foliage/";

    public static bool IsProcedural(string name)
        => name.StartsWith("MossTree_", StringComparison.Ordinal) || name.StartsWith("BogOak_", StringComparison.Ordinal) || name.StartsWith("Reeds_", StringComparison.Ordinal);

    /// <summary>Build the named model, or null when the name is not one of ours.</summary>
    public static ModelLibrary.Model? Build(string name)
    {
        if (!IsProcedural(name)) return null;
        int variant = name[^1] - '0';
        var rng = new RandomNumberGenerator { Seed = 9001UL + (ulong)variant * 131UL + (ulong)name.Length };
        Mesh mesh = name.StartsWith("MossTree_") ? BuildMossTree(variant, rng)
                  : name.StartsWith("BogOak_") ? BuildBogOak(variant, rng)
                  : BuildReeds(variant, rng);
        var model = new ModelLibrary.Model { Name = name };
        model.Parts.Add(new ModelLibrary.Part(mesh, Transform3D.Identity));
        model.Bounds = mesh.GetAabb();
        return model;
    }

    // ---- moss tree ----------------------------------------------------------------------------------

    private static ArrayMesh BuildMossTree(int variant, RandomNumberGenerator rng)
    {
        // Proportions from the references: trunk 1.6-2.2 m thick at chest height flaring to ~3 m at the root,
        // canopy top 15-19 m, canopy spread 14-18 m, curtains hanging to roughly half height.
        float height = 15f + variant * 1.5f + rng.RandfRange(-1f, 1f);
        float trunkR = 0.9f + 0.12f * variant;
        float spread = 7.5f + 0.6f * variant;          // canopy radius
        float canopyY = height * 0.78f;                // where the umbrella sits
        var trunk = new Surface();
        var leaves = new Surface();
        var moss = new Surface();

        // Trunk: a tapered, gently bent tube with a root flare.
        var spine = new List<(Vector3 p, float r)>();
        float lean = rng.RandfRange(-0.08f, 0.08f), bend = rng.RandfRange(-0.12f, 0.12f);
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f;
            float y = t * canopyY;
            float flare = MathF.Pow(1f - t, 5f) * 0.5f;                      // modest root flare
            float r = trunkR * (1.05f - 0.42f * t) + flare * trunkR;
            var p = new Vector3(lean * y + bend * MathF.Sin(t * MathF.PI) * 2.5f, y - 0.4f, bend * 0.5f * MathF.Sin(t * MathF.PI * 1.3f) * 2.0f);
            spine.Add((p, r));
        }
        Tube(trunk, spine, 12, uvPerMetre: 0.25f, rng);

        // Branches: 4-6 heavy limbs from the top third of the trunk fanning out to the canopy rim.
        int branches = 4 + variant % 3;
        var rimPoints = new List<Vector3>();
        var top = spine[^1].p;
        for (int b = 0; b < branches; b++)
        {
            float a = b * MathF.Tau / branches + rng.RandfRange(-0.3f, 0.3f);
            float startT = rng.RandfRange(0.55f, 0.85f);
            var start = spine[(int)MathF.Round(startT * 8)].p;
            float reach = spread * rng.RandfRange(0.75f, 1.0f);
            var end = new Vector3(start.X + MathF.Cos(a) * reach, canopyY + rng.RandfRange(-1.5f, 1.0f), start.Z + MathF.Sin(a) * reach);
            var mid = start.Lerp(end, 0.5f) + new Vector3(0, rng.RandfRange(0.5f, 2.0f), 0);
            var bs = new List<(Vector3, float)> { (start, trunkR * 0.55f), (mid, trunkR * 0.36f), (end, trunkR * 0.16f) };
            Tube(trunk, bs, 7, uvPerMetre: 0.3f, rng);
            rimPoints.Add(end);
        }

        // Crown: three big overlapping cards, each tilted a little in its own direction, so the top reads as a
        // broad lumpy plate (the references' willows are flat-topped but never razor-straight).
        for (int k = 0; k < 3; k++)
        {
            float a = rng.RandfRange(0f, MathF.Tau), tilt = rng.RandfRange(0.12f, 0.3f);
            var n = (Vector3.Up + new Vector3(MathF.Cos(a), 0, MathF.Sin(a)) * tilt).Normalized();
            var c = new Vector3(top.X + rng.RandfRange(-1.5f, 1.5f), canopyY + 0.8f + k * 0.7f, top.Z + rng.RandfRange(-1.5f, 1.5f));
            AddCard(leaves, c, n, rng.RandfRange(0, MathF.Tau), spread * rng.RandfRange(0.78f, 0.95f), spread * rng.RandfRange(0.7f, 0.9f), 0f);
        }
        // Top-knots: a few small upright cards on the crown so the silhouette has bumps instead of a line.
        for (int k = 0; k < 3; k++)
        {
            float a = k * MathF.Tau / 3f + rng.RandfRange(-0.4f, 0.4f);
            var c = new Vector3(top.X + MathF.Cos(a) * spread * 0.3f, canopyY + 2.2f, top.Z + MathF.Sin(a) * spread * 0.3f);
            AddCard(leaves, c, new Vector3(MathF.Cos(a + 1.3f), 0.25f, MathF.Sin(a + 1.3f)), 0f, spread * 0.42f, spread * 0.26f, 0f, upVector: Vector3.Up);
        }
        // Petals: the dome's shoulders, tilted well outward-down, at uneven heights.
        int petals = 6 + variant % 2;
        for (int k = 0; k < petals; k++)
        {
            float a = k * MathF.Tau / petals + rng.RandfRange(-0.25f, 0.25f);
            var c = new Vector3(top.X + MathF.Cos(a) * spread * 0.55f, canopyY + rng.RandfRange(-0.9f, 0.9f), top.Z + MathF.Sin(a) * spread * 0.55f);
            var outward = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
            var n = (Vector3.Up * 0.75f + outward * rng.RandfRange(0.6f, 0.9f)).Normalized();   // ~40-50 deg
            AddCard(leaves, c, n, a + rng.RandfRange(-0.5f, 0.5f), spread * rng.RandfRange(0.55f, 0.75f), spread * rng.RandfRange(0.45f, 0.6f), 0f);
        }
        // Skirt: outward-facing cards around the rim so the canopy keeps its mass seen from eye level; irregular sizes.
        int skirt = 8;
        for (int k = 0; k < skirt; k++)
        {
            float a = k * MathF.Tau / skirt + rng.RandfRange(-0.2f, 0.2f);
            var outward = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
            var c = new Vector3(top.X, canopyY - 0.9f + rng.RandfRange(-0.8f, 0.8f), top.Z) + outward * spread * rng.RandfRange(0.7f, 0.85f);
            var n = (outward * 0.9f + Vector3.Up * rng.RandfRange(0.25f, 0.55f)).Normalized();
            AddCard(leaves, c, n, 0f, spread * rng.RandfRange(0.42f, 0.62f), spread * rng.RandfRange(0.28f, 0.42f), 0f, upVector: Vector3.Up);
        }
        // Curtains: moss strips hanging from the rim, each a V pair so it reads from every side.
        int curtains = 12 + variant;
        float curtainH = height * rng.RandfRange(0.42f, 0.5f);
        for (int k = 0; k < curtains; k++)
        {
            float a = k * MathF.Tau / curtains + rng.RandfRange(-0.12f, 0.12f);
            float rr = spread * rng.RandfRange(0.72f, 0.98f);
            var c = new Vector3(top.X + MathF.Cos(a) * rr, canopyY - 0.8f, top.Z + MathF.Sin(a) * rr);
            float w = rng.RandfRange(3.2f, 4.6f), h = curtainH * rng.RandfRange(0.8f, 1.1f);
            AddHanging(moss, c, a + rng.RandfRange(-0.4f, 0.4f), w, h);
            AddHanging(moss, c, a + MathF.PI * 0.55f, w * 0.8f, h * 0.9f);
        }
        // A few inner curtains under the umbrella so the underside is not hollow.
        for (int k = 0; k < 4; k++)
        {
            var c = new Vector3(top.X + rng.RandfRange(-spread * 0.4f, spread * 0.4f), canopyY - 0.5f, top.Z + rng.RandfRange(-spread * 0.4f, spread * 0.4f));
            AddHanging(moss, c, rng.RandfRange(0, MathF.Tau), rng.RandfRange(3f, 4f), curtainH * 0.7f);
        }

        var mesh = new ArrayMesh();
        trunk.Commit(mesh, BarkMaterial());
        leaves.Commit(mesh, LeafMaterial("Leaves_MossClump", "leaf_clump.png"));
        moss.Commit(mesh, LeafMaterial("Leaves_MossCurtain", "moss_curtain.png"));
        return mesh;
    }

    // ---- bog oak: the giant dark twisted silhouette tree ---------------------------------------------

    private static ArrayMesh BuildBogOak(int variant, RandomNumberGenerator rng)
    {
        // Trunk and limbs from the kit's dead trees (they already have the twisted reach), scaled up to 20+ m.
        string[] trunks = { "DeadTree_5", "DeadTree_3", "DeadTree_1" };
        float[] trunkH = { 16.1f, 12.9f, 9.2f };
        int pick = (variant - 1) % trunks.Length;
        float scale = (21f + variant) / trunkH[pick];
        var mesh = new ArrayMesh();
        var kit = ModelLibrary.Get(trunks[pick]);
        var leaves = new Surface();
        float topY = trunkH[pick] * scale;
        if (kit?.Mesh is ArrayMesh src && src.GetSurfaceCount() > 0)
        {
            var arrays = src.SurfaceGetArrays(0);
            var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            for (int i = 0; i < verts.Length; i++) verts[i] *= scale;
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(0, BarkMaterial(dark: true));
        }
        // Crown: a lumpy dark mass in two tiers of tilted cards (no flat plates: edge-on they read as lines), with a few
        // upright cards so the silhouette is a mass with bumps, and the kit's bare twigs poking out above it.
        float spread = 9f + variant;
        float canopyY = topY * 0.7f;
        for (int tier = 0; tier < 2; tier++)
        {
            int n = tier == 0 ? 7 : 5;
            float rr = tier == 0 ? spread * 0.55f : spread * 0.3f;
            float yy = canopyY + tier * 3.2f;
            for (int k = 0; k < n; k++)
            {
                float a = k * MathF.Tau / n + rng.RandfRange(-0.3f, 0.3f);
                var outward = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
                var c = outward * rr + new Vector3(rng.RandfRange(-1f, 1f), yy + rng.RandfRange(-1.2f, 1.2f), rng.RandfRange(-1f, 1f));
                var nrm = (Vector3.Up * rng.RandfRange(0.5f, 0.9f) + outward * rng.RandfRange(0.4f, 0.9f)).Normalized();
                AddCard(leaves, c, nrm, rng.RandfRange(0, MathF.Tau), spread * rng.RandfRange(0.5f, 0.7f), spread * rng.RandfRange(0.4f, 0.55f), 0f);
            }
        }
        for (int k = 0; k < 6; k++)
        {
            float a = k * MathF.Tau / 6 + rng.RandfRange(-0.3f, 0.3f);
            var outward = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
            var c = outward * spread * 0.7f + new Vector3(0, canopyY + rng.RandfRange(-0.5f, 2.0f), 0);
            AddCard(leaves, c, (outward * 0.9f + Vector3.Up * 0.3f).Normalized(), 0f, spread * rng.RandfRange(0.45f, 0.6f), spread * rng.RandfRange(0.35f, 0.5f), 0f, upVector: Vector3.Up);
        }
        leaves.Commit(mesh, LeafMaterial("Leaves_BogOak", "leaf_clump.png"));
        return mesh;
    }

    // ---- reeds ----------------------------------------------------------------------------------------

    private static ArrayMesh BuildReeds(int variant, RandomNumberGenerator rng)
    {
        var s = new Surface();
        int cards = 2 + variant % 2;                       // 2 or 3 crossed cards
        float h = 1.3f + 0.25f * variant, w = h * 0.55f;
        for (int k = 0; k < cards; k++)
        {
            float a = k * MathF.PI / cards + rng.RandfRange(-0.15f, 0.15f);
            AddUpright(s, new Vector3(rng.RandfRange(-0.1f, 0.1f), 0, rng.RandfRange(-0.1f, 0.1f)), a, w, h);
        }
        var mesh = new ArrayMesh();
        s.Commit(mesh, LeafMaterial("Leaves_Reeds", "reeds.png"));
        return mesh;
    }

    // ---- materials -----------------------------------------------------------------------------------

    private static Texture2D? Tex(string file) => ResourceLoader.Exists(TexDir + file) ? GD.Load<Texture2D>(TexDir + file) : null;

    /// <summary>Names contain "Leaves"/"Grass" on purpose: ModelLibrary swaps those surfaces to the foliage shader.</summary>
    private static StandardMaterial3D LeafMaterial(string name, string file)
    {
        var m = new StandardMaterial3D { ResourceName = name, AlbedoTexture = Tex(file), Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor, AlphaScissorThreshold = 0.4f, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 1f, MetallicSpecular = 0f };
        return m;
    }

    private static StandardMaterial3D BarkMaterial(bool dark = false)
        => new()
        {
            ResourceName = dark ? "Trunk_BogOak" : "Trunk_Pale",
            AlbedoTexture = Tex("bark_pale.png"),
            // The dark oak keeps the pale bark texture but multiplied down to a near-silhouette brown-grey.
            AlbedoColor = dark ? new Color(0.40f, 0.36f, 0.34f) : new Color(0.80f, 0.74f, 0.66f),
            Roughness = 1f,
            MetallicSpecular = 0f,
            VertexColorUseAsAlbedo = true,
        };

    // ---- geometry helpers --------------------------------------------------------------------------

    /// <summary>Accumulates one surface's triangles with position, normal, uv and a white colour.</summary>
    private sealed class Surface
    {
        public readonly List<Vector3> V = new();
        public readonly List<Vector3> N = new();
        public readonly List<Vector2> Uv = new();
        public readonly List<Color> C = new();
        public readonly List<int> I = new();

        public int Add(Vector3 v, Vector3 n, Vector2 uv, float shade = 1f)
        {
            V.Add(v); N.Add(n); Uv.Add(uv); C.Add(new Color(shade, shade, shade, 1f));
            return V.Count - 1;
        }

        public void Tri(int a, int b, int c) { I.Add(a); I.Add(b); I.Add(c); }

        public void Commit(ArrayMesh mesh, Material mat)
        {
            if (I.Count == 0) return;
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = V.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = N.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = Uv.ToArray();
            arrays[(int)Mesh.ArrayType.Color] = C.ToArray();
            arrays[(int)Mesh.ArrayType.Index] = I.ToArray();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, mat);
        }
    }

    /// <summary>Tube along a spine of (point, radius); rings are oriented by the local tangent. Vertex colour darkens toward the ground (baked AO).</summary>
    private static void Tube(Surface s, List<(Vector3 p, float r)> spine, int segs, float uvPerMetre, RandomNumberGenerator rng)
    {
        int rings = spine.Count;
        var ringStart = new int[rings];
        float arc = 0f;
        for (int i = 0; i < rings; i++)
        {
            var (p, r) = spine[i];
            var tan = (i < rings - 1 ? spine[i + 1].p - p : p - spine[i - 1].p).Normalized();
            if (i > 0) arc += (p - spine[i - 1].p).Length();
            var side = MathF.Abs(tan.Y) > 0.9f ? Vector3.Right : Vector3.Up.Cross(tan).Normalized();
            var fwd = tan.Cross(side).Normalized();
            ringStart[i] = s.V.Count;
            float shade = Mathf.Lerp(0.72f, 1f, Mathf.Clamp(p.Y / 6f, 0f, 1f));
            for (int k = 0; k <= segs; k++)
            {
                float a = k / (float)segs * MathF.Tau;
                var n = (side * MathF.Cos(a) + fwd * MathF.Sin(a)).Normalized();
                // Slight lumpiness so the trunk is not a lathe.
                float bump = 1f + 0.06f * MathF.Sin(a * 3f + i * 1.7f) + 0.04f * MathF.Sin(a * 5f + i);
                s.Add(p + n * r * bump, n, new Vector2(k / (float)segs, arc * uvPerMetre), shade);
            }
        }
        for (int i = 0; i < rings - 1; i++)
        for (int k = 0; k < segs; k++)
        {
            int a = ringStart[i] + k, b = a + 1, c = ringStart[i + 1] + k, d = c + 1;
            s.Tri(a, c, b); s.Tri(b, c, d);
        }
        // Cap the far end (branch tips) with a small fan so no hole shows under the canopy.
        var tip = spine[^1];
        int centre = s.Add(tip.p, Vector3.Up, new Vector2(0.5f, 0.5f));
        for (int k = 0; k < segs; k++) s.Tri(centre, ringStart[rings - 1] + k, ringStart[rings - 1] + k + 1);
    }

    /// <summary>A quad centred at c with normal n, rotated by yaw around n, half extents (hw, hh); double-sided via the material.</summary>
    private static void AddCard(Surface s, Vector3 c, Vector3 n, float yaw, float hw, float hh, float shade, Vector3? upVector = null)
    {
        n = n.Normalized();
        var up = upVector ?? (MathF.Abs(n.Y) > 0.9f ? Vector3.Forward : Vector3.Up);
        var right = up.Cross(n).Normalized();
        up = n.Cross(right).Normalized();
        if (yaw != 0f) { right = right.Rotated(n, yaw); up = up.Rotated(n, yaw); }
        float sh = 1f;
        int a = s.Add(c - right * hw - up * hh, n, new Vector2(0, 1), sh);
        int b = s.Add(c + right * hw - up * hh, n, new Vector2(1, 1), sh);
        int cc = s.Add(c + right * hw + up * hh, n, new Vector2(1, 0), sh);
        int d = s.Add(c - right * hw + up * hh, n, new Vector2(0, 0), sh);
        s.Tri(a, b, cc); s.Tri(a, cc, d);
    }

    /// <summary>Vertical card hanging DOWN from its top-centre at c (moss curtains): uv v=0 at the top.</summary>
    private static void AddHanging(Surface s, Vector3 c, float yaw, float w, float h)
    {
        var right = new Vector3(MathF.Cos(yaw), 0, MathF.Sin(yaw));
        var n = Vector3.Up.Cross(right).Normalized();
        float hw = w * 0.5f;
        // Darker toward the bottom: the strands hang in the tree's own shade.
        int a = s.Add(c - right * hw, n, new Vector2(0, 0), 1f);
        int b = s.Add(c + right * hw, n, new Vector2(1, 0), 1f);
        int cc = s.Add(c + right * hw - new Vector3(0, h, 0), n, new Vector2(1, 1), 0.8f);
        int d = s.Add(c - right * hw - new Vector3(0, h, 0), n, new Vector2(0, 1), 0.8f);
        s.Tri(a, b, cc); s.Tri(a, cc, d);
    }

    /// <summary>Vertical card standing UP from its bottom-centre at c (reeds): uv v=1 at the ground.</summary>
    private static void AddUpright(Surface s, Vector3 c, float yaw, float w, float h)
    {
        var right = new Vector3(MathF.Cos(yaw), 0, MathF.Sin(yaw));
        var n = Vector3.Up.Cross(right).Normalized();
        float hw = w * 0.5f;
        int a = s.Add(c - right * hw, n, new Vector2(0, 1), 0.75f);
        int b = s.Add(c + right * hw, n, new Vector2(1, 1), 0.75f);
        int cc = s.Add(c + right * hw + new Vector3(0, h, 0), n, new Vector2(1, 0), 1f);
        int d = s.Add(c - right * hw + new Vector3(0, h, 0), n, new Vector2(0, 0), 1f);
        s.Tri(a, b, cc); s.Tri(a, cc, d);
    }
}
