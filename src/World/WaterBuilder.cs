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

        if (L.Lake is { } lake)
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
                MaterialOverride = MakeMaterial(shader, L, flowPerSecond: 0.6f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                GIMode = GeometryInstance3D.GIModeEnum.Disabled,
            });
        }
        GD.Print($"Water: lake={(L.Lake != null ? 1 : 0)} rivers={world.Terrain.Rivers.Count}");
    }

    private static ShaderMaterial MakeMaterial(Shader shader, WorldLayout L, float flowPerSecond)
    {
        var water = L.Col("water", "#4C7A8C");
        var deep = L.Col("waterDeep", water.Darkened(0.4f).ToHtml());
        var horizon = L.Col("skyHorizon", "#C9D7E2");
        var top = L.Col("skyTop", "#6F95C2");
        var mat = new ShaderMaterial { Shader = shader };
        mat.SetShaderParameter("color_shallow", water);
        mat.SetShaderParameter("color_deep", deep);
        // Shoreline: the water colour pushed toward the pale horizon, so it reads as light shallows, not foam.
        mat.SetShaderParameter("color_shore", water.Lerp(horizon, 0.55f));
        mat.SetShaderParameter("sky_color", horizon.Lerp(top, 0.35f));
        mat.SetShaderParameter("flow", new Vector2(flowPerSecond, 0f));
        return mat;
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
        for (int i = 0; i < n; i++)
        {
            float arc = MathF.Min(i * step, pl.Length);
            var p = pl.PointAtArc(arc);
            var tan = pl.TangentAtArc(arc);
            var side = new Vector2(-tan.Y, tan.X);
            float y = Mathf.Lerp(spec.WaterLevelStart, spec.WaterLevelEnd, arc / pl.Length) - RiverSink;
            bool inLake = lake != null && InsideEllipse(p, lake, 0.97f);
            left.Add(new Vector3(p.X - side.X * halfW, y, p.Y - side.Y * halfW));
            right.Add(new Vector3(p.X + side.X * halfW, y, p.Y + side.Y * halfW));
            arcs.Add(arc);
            if (inLake && arcs.Count > 1) break;   // one section inside the lake, then stop
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
