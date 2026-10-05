using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Lake and river surfaces. The lake is a subdivided ellipse at <see cref="LakeSpec.WaterLevel"/>, each river a
/// flat ribbon along its <see cref="Polyline"/> whose height slides from WaterLevelStart to WaterLevelEnd.
/// Both are a touch wider than the carved trench/bowl so their edges are buried in the bank and the
/// depth-based shoreline in <c>shaders/water.gdshader</c> does the visible edge.
/// </summary>
public static class WaterBuilder
{
    private const float LakeOversize = 1.05f;
    private const float RiverOversize = 1.15f;
    /// <summary>Rivers sit a hair below their authored level so a river meeting the lake never z-fights with it.</summary>
    private const float RiverSink = 0.03f;

    public static void Build(WorldRoot world)
    {
        var L = world.Layout;
        var shader = GD.Load<Shader>("res://shaders/water.gdshader");
        var root = new Node3D { Name = "Water" };
        world.AddChild(root);

        if (L.Marsh is { } marsh)
        {
            root.AddChild(new MeshInstance3D
            {
                Name = $"Marsh_{Safe(marsh.Name)}",
                Mesh = BuildMarshMesh(marsh, world.Field),
                MaterialOverride = MakeMaterial(shader, L, flowPerSecond: 0f, marsh: true),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                GIMode = GeometryInstance3D.GIModeEnum.Disabled,
            });
        }

        // A lake at the marsh's own level is already covered by the sheet; a separate mesh would only z-fight.
        if (L.Lake is { } lake && !(L.Marsh is { } m2 && MathF.Abs(m2.WaterLevel - lake.WaterLevel) < 0.05f))
        {
            var mesh = BuildLakeMesh(lake);
            root.AddChild(new MeshInstance3D
            {
                Name = $"Lake_{Safe(lake.Name)}",
                Mesh = mesh,
                MaterialOverride = MakeMaterial(shader, L, flowPerSecond: 0f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                GIMode = GeometryInstance3D.GIModeEnum.Disabled,
            });
        }

        for (int r = 0; r < L.Rivers.Count && r < world.Terrain.Rivers.Count; r++)
        {
            var spec = L.Rivers[r];
            var pl = world.Terrain.Rivers[r];
            var mesh = BuildRiverMesh(spec, pl, L.Lake);
            if (mesh == null) continue;
            root.AddChild(new MeshInstance3D
            {
                Name = $"River_{Safe(spec.Name)}",
                Mesh = mesh,
                // ~0.6 m/s downstream drift reads as a lazy brook; UV.x is in metres.
                MaterialOverride = MakeMaterial(shader, L, flowPerSecond: 0.6f, marsh: L.Marsh != null),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                GIMode = GeometryInstance3D.GIModeEnum.Disabled,
            });
        }
        GD.Print($"Water: marsh={(L.Marsh != null ? 1 : 0)} lake={(L.Lake != null ? 1 : 0)} rivers={world.Terrain.Rivers.Count}");
    }

    private static ShaderMaterial MakeMaterial(Shader shader, WorldLayout L, float flowPerSecond, bool marsh = false)
    {
        var water = L.Col("water", "#4C7A8C");
        var deep = L.Col("waterDeep", water.Darkened(0.4f).ToHtml());
        var horizon = L.Col("skyHorizon", "#C9D7E2");
        var top = L.Col("skyTop", "#6F95C2");
        var mat = new ShaderMaterial { Shader = shader };
        mat.SetShaderParameter("color_shallow", water);
        mat.SetShaderParameter("color_deep", deep);
        // Shoreline: a lighter version of the water colour (not the grey horizon), so shallows stay coloured.
        // Shoreline a touch lighter than the water, never the sky: bog water stays dark under the pink haze.
        mat.SetShaderParameter("color_shore", L.Col("waterShore", water.Lightened(0.18f).ToHtml()));
        // The "sky" the water leans toward is the palette's waterSky if given, else a greyed horizon.
        mat.SetShaderParameter("sky_color", L.Col("waterSky", horizon.Lerp(new Color("#8A8E90"), 0.55f).ToHtml()));
        mat.SetShaderParameter("flow", new Vector2(flowPerSecond, 0f));
        if (marsh)
        {
            // Bog water: shallow everywhere, so it goes opaque-murky sooner and the pale painted glints carry the read.
            // Murky and near-opaque: the bed never shows, the pale glints and a broad sheen carry the read.
            mat.SetShaderParameter("depth_fade", 1.2f);
            mat.SetShaderParameter("shore_band", 1.4f);
            mat.SetShaderParameter("alpha_shallow", 0.35f);
            mat.SetShaderParameter("alpha_base", 0.93f);
            mat.SetShaderParameter("streak_strength", 0.2f);
            mat.SetShaderParameter("ripple_tint", 0.08f);
            mat.SetShaderParameter("fresnel_strength", 0.06f);
            mat.SetShaderParameter("roughness", 0.7f);
            mat.SetShaderParameter("specular", 0.04f);
            mat.SetShaderParameter("wobble_scale", 0.4f);
            mat.SetShaderParameter("wobble_strength", 0.05f);
            mat.SetShaderParameter("glint_strength", 0.9f);
            mat.SetShaderParameter("glint_color", L.Col("waterGlint", "#B9BCA6"));
        }
        return mat;
    }

    /// <summary>
    /// One flat sheet over the marsh disc as a grid of quads (cell ~12 m), clipped to the disc, skipping cells whose
    /// four corners are all comfortably above the water so the terrain does not have to hide it. Vertex density keeps
    /// the fog/lighting gradients smooth across the long views the bog is made for.
    /// </summary>
    private static ArrayMesh BuildMarshMesh(MarshSpec marsh, HeightField field)
    {
        const float cell = 12f;
        int n = (int)MathF.Ceiling(marsh.Radius * 2f / cell) + 1;
        float x0 = marsh.X - marsh.Radius, z0 = marsh.Z - marsh.Radius;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var verts = new List<Vector3>();
        var index = new Dictionary<(int, int), int>();
        int Vert(int i, int j)
        {
            if (index.TryGetValue((i, j), out var k)) return k;
            var v = new Vector3(x0 + i * cell, marsh.WaterLevel, z0 + j * cell);
            AddVertex(st, verts, v);
            index[(i, j)] = verts.Count - 1;
            return verts.Count - 1;
        }
        for (int j = 0; j < n - 1; j++)
        for (int i = 0; i < n - 1; i++)
        {
            float cx = x0 + (i + 0.5f) * cell, cz = z0 + (j + 0.5f) * cell;
            if ((cx - marsh.X) * (cx - marsh.X) + (cz - marsh.Z) * (cz - marsh.Z) > marsh.Radius * marsh.Radius) continue;
            bool dry = true;
            for (int dj = 0; dj <= 1 && dry; dj++)
            for (int di = 0; di <= 1 && dry; di++)
                if (field.SampleHeight(x0 + (i + di) * cell, z0 + (j + dj) * cell) < marsh.WaterLevel + 1.5f) dry = false;
            // Also test the cell centre: a pool smaller than a cell could sit between the corners.
            if (dry && field.SampleHeight(cx, cz) >= marsh.WaterLevel + 1.5f) continue;
            int a = Vert(i, j), b = Vert(i + 1, j), c = Vert(i, j + 1), d = Vert(i + 1, j + 1);
            AddTriUp(st, verts, a, b, c);
            AddTriUp(st, verts, b, d, c);
        }
        return st.Commit();
    }

    /// <summary>Concentric rings of quads so the big surface still has vertices for fog/lighting gradients.</summary>
    private static ArrayMesh BuildLakeMesh(LakeSpec lake)
    {
        const int rings = 10, segs = 56;
        float rx = lake.RadiusX * LakeOversize, rz = lake.RadiusZ * LakeOversize;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var verts = new List<Vector3>();
        // Vertex 0 is the centre; ring k (1..rings) has `segs` vertices.
        AddVertex(st, verts, new Vector3(lake.X, lake.WaterLevel, lake.Z));
        for (int k = 1; k <= rings; k++)
        {
            float t = k / (float)rings;
            // Denser rings toward the shore, where the depth gradient actually changes.
            float f = MathF.Pow(t, 0.7f);
            for (int s = 0; s < segs; s++)
            {
                float a = s / (float)segs * MathF.Tau;
                AddVertex(st, verts, new Vector3(lake.X + MathF.Cos(a) * rx * f, lake.WaterLevel, lake.Z + MathF.Sin(a) * rz * f));
            }
        }
        int Idx(int ring, int seg) => ring == 0 ? 0 : 1 + (ring - 1) * segs + ((seg % segs + segs) % segs);
        for (int s = 0; s < segs; s++) AddTriUp(st, verts, 0, Idx(1, s), Idx(1, s + 1));
        for (int k = 1; k < rings; k++)
        for (int s = 0; s < segs; s++)
        {
            int a = Idx(k, s), b = Idx(k, s + 1), c = Idx(k + 1, s), d = Idx(k + 1, s + 1);
            AddTriUp(st, verts, a, b, c);
            AddTriUp(st, verts, b, d, c);
        }
        return st.Commit();
    }

    /// <summary>
    /// Ribbon of quads along the river. UV.x = metres along the arc (flow direction), UV.y = 0..1 across.
    /// The ribbon stops once its centreline is inside the lake so the two surfaces do not overlap.
    /// </summary>
    private static ArrayMesh? BuildRiverMesh(RiverSpec spec, Polyline pl, LakeSpec? lake)
    {
        if (pl.Points.Count < 2 || pl.Length < 1f) return null;
        const float step = 3f;
        float halfW = spec.Width * RiverOversize * 0.5f;
        var left = new List<Vector3>();
        var right = new List<Vector3>();
        var arcs = new List<float>();
        int n = Math.Max(2, (int)MathF.Ceiling(pl.Length / step) + 1);
        bool wasOutside = false;
        for (int i = 0; i < n; i++)
        {
            float arc = MathF.Min(i * step, pl.Length);
            var p = pl.PointAtArc(arc);
            var tan = pl.TangentAtArc(arc);
            var side = new Vector2(-tan.Y, tan.X);
            float y = Mathf.Lerp(spec.WaterLevelStart, spec.WaterLevelEnd, arc / pl.Length) - RiverSink;
            bool inLake = lake != null && InsideEllipse(p, lake, 0.97f);
            if (!inLake) wasOutside = true;
            left.Add(new Vector3(p.X - side.X * halfW, y, p.Y - side.Y * halfW));
            right.Add(new Vector3(p.X + side.X * halfW, y, p.Y + side.Y * halfW));
            arcs.Add(arc);
            // One section inside the lake, then stop; a river that STARTS in the lake (an outlet) keeps going.
            if (inLake && wasOutside && arcs.Count > 1) break;
        }
        if (arcs.Count < 2) return null;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var verts = new List<Vector3>();
        for (int i = 0; i < arcs.Count; i++)
        {
            st.SetUV(new Vector2(arcs[i], 0f)); st.SetNormal(Vector3.Up); st.AddVertex(left[i]); verts.Add(left[i]);
            st.SetUV(new Vector2(arcs[i], 1f)); st.SetNormal(Vector3.Up); st.AddVertex(right[i]); verts.Add(right[i]);
        }
        for (int i = 0; i < arcs.Count - 1; i++)
        {
            int l0 = i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
            AddTriUp(st, verts, l0, r0, l1);
            AddTriUp(st, verts, r0, r1, l1);
        }
        return st.Commit();
    }

    private static bool InsideEllipse(Vector2 p, LakeSpec lake, float scale)
    {
        float dx = (p.X - lake.X) / (lake.RadiusX * scale), dz = (p.Y - lake.Z) / (lake.RadiusZ * scale);
        return dx * dx + dz * dz < 1f;
    }

    // Lake UVs are world metres so the wobble scale matches the river's metre-based UVs.
    private static void AddVertex(SurfaceTool st, List<Vector3> verts, Vector3 v)
    {
        st.SetUV(new Vector2(v.X, v.Z));
        st.SetNormal(Vector3.Up);
        st.AddVertex(v);
        verts.Add(v);
    }

    /// <summary>
    /// Adds a triangle wound so it is visible from above. Godot front faces are clockwise when seen from the front,
    /// so a triangle whose right-hand normal (b-a)x(c-a) points up must be emitted as (a, c, b).
    /// </summary>
    private static void AddTriUp(SurfaceTool st, List<Vector3> verts, int a, int b, int c)
    {
        var nrm = (verts[b] - verts[a]).Cross(verts[c] - verts[a]);
        if (nrm.Y >= 0f) { st.AddIndex(a); st.AddIndex(c); st.AddIndex(b); }
        else { st.AddIndex(a); st.AddIndex(b); st.AddIndex(c); }
    }

    private static string Safe(string s)
    {
        var chars = new List<char>(s.Length);
        foreach (var c in s) chars.Add(char.IsLetterOrDigit(c) ? c : '_');
        return chars.Count == 0 ? "Unnamed" : new string(chars.ToArray());
    }
}
