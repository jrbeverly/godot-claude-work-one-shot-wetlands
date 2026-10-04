using System;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Square heightfield centred on the origin plus the per-vertex data that everything else samples:
/// ground texture weights (splat), road and water distance, and a hue tint.
/// Vertex (i, j) sits at world (x = -HalfExtent + i*CellSize, z = -HalfExtent + j*CellSize).
/// </summary>
public sealed class HeightField
{
    public int Size { get; }
    public float CellSize { get; }
    public float HalfExtent => (Size - 1) * CellSize * 0.5f;

    public float[] Heights { get; }

    /// <summary>Ground layer weights per vertex, each 0..1: R grass, G forest floor, B dirt road, A rock. Snow is altitude-driven in the shader.</summary>
    public float[] SplatGrass { get; }
    public float[] SplatForest { get; }
    public float[] SplatRoad { get; }
    public float[] SplatRock { get; }
    /// <summary>Hue/value tint multiplied into the ground albedo (baked-light look). Default white.</summary>
    public Color[] Tint { get; }

    /// <summary>Horizontal distance (m) to the nearest road centreline, large when far.</summary>
    public float[] RoadDistance { get; }
    /// <summary>Water surface height above terrain at this vertex (0 = dry). Positive inside lakes/rivers.</summary>
    public float[] WaterDepth { get; }
    /// <summary>Set for vertices that settlement sites have claimed (no trees).</summary>
    public float[] SiteClear { get; }

    public HeightField(int size, float cellSize)
    {
        Size = size;
        CellSize = cellSize;
        int n = size * size;
        Heights = new float[n];
        SplatGrass = new float[n];
        SplatForest = new float[n];
        SplatRoad = new float[n];
        SplatRock = new float[n];
        Tint = new Color[n];
        RoadDistance = new float[n];
        WaterDepth = new float[n];
        SiteClear = new float[n];
        Array.Fill(SplatGrass, 1f);
        Array.Fill(Tint, Colors.White);
        Array.Fill(RoadDistance, 1e6f);
    }

    public int Index(int i, int j) => j * Size + i;

    public float WorldX(int i) => -HalfExtent + i * CellSize;
    public float WorldZ(int j) => -HalfExtent + j * CellSize;

    /// <summary>Continuous grid coordinate of a world position.</summary>
    public (float gi, float gj) ToGrid(float x, float z)
        => ((x + HalfExtent) / CellSize, (z + HalfExtent) / CellSize);

    public float SampleHeight(float x, float z) => Bilinear(Heights, x, z);
    public float SampleRoadDistance(float x, float z) => Bilinear(RoadDistance, x, z);
    public float SampleWaterDepth(float x, float z) => Bilinear(WaterDepth, x, z);
    public float SampleSiteClear(float x, float z) => Bilinear(SiteClear, x, z);
    public float SampleRock(float x, float z) => Bilinear(SplatRock, x, z);
    public float SampleForest(float x, float z) => Bilinear(SplatForest, x, z);

    public float Bilinear(float[] data, float x, float z)
    {
        var (gi, gj) = ToGrid(x, z);
        gi = Mathf.Clamp(gi, 0, Size - 1.0001f);
        gj = Mathf.Clamp(gj, 0, Size - 1.0001f);
        int i0 = (int)gi, j0 = (int)gj;
        int i1 = Math.Min(i0 + 1, Size - 1), j1 = Math.Min(j0 + 1, Size - 1);
        float fx = gi - i0, fz = gj - j0;
        float a = data[Index(i0, j0)], b = data[Index(i1, j0)];
        float c = data[Index(i0, j1)], d = data[Index(i1, j1)];
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
    }

    /// <summary>Surface normal from central differences (world space, Y up).</summary>
    public Vector3 SampleNormal(float x, float z)
    {
        float e = CellSize;
        float hl = SampleHeight(x - e, z), hr = SampleHeight(x + e, z);
        float hd = SampleHeight(x, z - e), hu = SampleHeight(x, z + e);
        return new Vector3(hl - hr, 2f * e, hd - hu).Normalized();
    }

    /// <summary>Slope in degrees at a world position.</summary>
    public float SampleSlopeDeg(float x, float z)
    {
        var n = SampleNormal(x, z);
        return Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(n.Y, -1f, 1f)));
    }

    public Vector3 NormalAt(int i, int j)
    {
        int il = Math.Max(i - 1, 0), ir = Math.Min(i + 1, Size - 1);
        int jd = Math.Max(j - 1, 0), ju = Math.Min(j + 1, Size - 1);
        float hl = Heights[Index(il, j)], hr = Heights[Index(ir, j)];
        float hd = Heights[Index(i, jd)], hu = Heights[Index(i, ju)];
        float dx = (ir - il) * CellSize, dz = (ju - jd) * CellSize;
        return new Vector3((hl - hr) / dx * 2f, 2f, (hd - hu) / dz * 2f).Normalized();
    }

    public bool InBounds(float x, float z)
        => x > -HalfExtent && x < HalfExtent && z > -HalfExtent && z < HalfExtent;

    /// <summary>Normalise the four splat weights at every vertex so they sum to 1.</summary>
    public void NormaliseSplat()
    {
        for (int k = 0; k < Heights.Length; k++)
        {
            float s = SplatGrass[k] + SplatForest[k] + SplatRoad[k] + SplatRock[k];
            if (s <= 1e-5f) { SplatGrass[k] = 1f; continue; }
            SplatGrass[k] /= s; SplatForest[k] /= s; SplatRoad[k] /= s; SplatRock[k] /= s;
        }
    }
}
