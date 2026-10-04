using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Turns the authored <see cref="WorldLayout"/> into a <see cref="HeightField"/>:
/// broad rolling base, a mountain rim that hides the map edge, authored hills/ridges/basins,
/// a lake, rivers in shallow trenches, roads flattened into the ground and painted as lighter ribbons,
/// levelled settlement flats, slope-driven rock, biome ground layers and a baked-light hue tint.
/// Chunky and smooth on purpose: no micro-detail noise.
/// </summary>
public static class TerrainBuilder
{
    public sealed class Result
    {
        public HeightField Field = null!;
        public List<Polyline> Roads = new();
        public List<Polyline> Rivers = new();
    }

    public static Result Build(WorldLayout L, int seed = 1337)
    {
        int size = (int)MathF.Round(L.MapSize / L.CellSize) + 1;
        var f = new HeightField(size, L.CellSize);
        var res = new Result { Field = f };
        float half = f.HalfExtent;

        // Nothing finer than ~60 m in the base: Classic ground is chunky and smooth, detail comes from texture.
        var broad = new FastNoiseLite { Seed = seed, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 150f, FractalOctaves = 2, FractalLacunarity = 2f, FractalGain = 0.45f };
        var medium = new FastNoiseLite { Seed = seed + 7, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 62f, FractalOctaves = 1, FractalGain = 0.5f };
        var rimNoise = new FastNoiseLite { Seed = seed + 11, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 90f, FractalOctaves = 3, FractalGain = 0.5f };
        var tintNoise = new FastNoiseLite { Seed = seed + 23, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 70f, FractalOctaves = 2, FractalGain = 0.5f };
        var splatNoise = new FastNoiseLite { Seed = seed + 31, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 14f, FractalOctaves = 2, FractalGain = 0.5f };

        // 1. Base: gentle rolling meadow + mountain rim.
        var rim = L.Terrain.Rim;
        for (int j = 0; j < size; j++)
        for (int i = 0; i < size; i++)
        {
            float x = f.WorldX(i), z = f.WorldZ(j);
            float h = L.Terrain.BaseHeight + broad.GetNoise2D(x, z) * 4.5f + medium.GetNoise2D(x, z) * 0.8f;

            // Rim: squircle distance so corners are not oddly high. Broad massive forms (very low frequency
            // ridged noise) rather than spiky peaks: Classic mountains are a few big readable shapes.
            float dEuclid = MathF.Sqrt(x * x + z * z);
            float dBox = MathF.Max(MathF.Abs(x), MathF.Abs(z));
            float d = Mathf.Lerp(dEuclid, dBox, 0.55f);
            float t = Mathf.Clamp((d - rim.StartRadius) / MathF.Max(half * 1.05f - rim.StartRadius, 1f), 0f, 1f);
            if (t > 0f)
            {
                float m1 = rimNoise.GetNoise2D(x * 0.33f, z * 0.33f);          // ~270 m features
                float ridged = MathF.Pow(1f - MathF.Abs(m1), 1.6f);             // soft crests and saddles
                float mass = (1f - rim.Roughness * 0.9f) + rim.Roughness * 0.9f * ridged;
                float ramp = MathF.Pow(t * t * (3f - 2f * t), 1.25f);
                float edgeH = rim.HeightToward(x, z);
                float shoulder = 0.12f * edgeH * Smooth(t * 2.2f) * (0.5f + 0.5f * medium.GetNoise2D(x * 0.5f, z * 0.5f));
                h += edgeH * ramp * mass + shoulder;
            }
            f.Heights[f.Index(i, j)] = h;
        }

        // 2. Authored hills, ridges and basins.
        foreach (var hill in L.Terrain.Hills) ApplyRadial(f, hill.X, hill.Z, hill.Radius, (dn, h) => h + hill.Height * HillProfile(dn, hill.Shape));
        foreach (var basin in L.Terrain.Basins) ApplyRadial(f, basin.X, basin.Z, basin.Radius, (dn, h) => h - basin.Depth * Cosine(dn));
        foreach (var ridge in L.Terrain.Ridges)
        {
            var pl = Polyline.FromControlPoints(ridge.Points, 4f);
            ApplyAlong(f, pl, ridge.Width * 1.6f, (dist, arc, h, x, z) =>
            {
                float dn = dist / ridge.Width;
                float along = 0.75f + 0.25f * rimNoise.GetNoise2D(x * 2f, z * 2f);
                return h + ridge.Height * Cosine(MathF.Min(dn / 1.6f, 1f)) * along;
            });
        }

        // 2b. Banded rock: steep ground steps softly into terraces (reads as painted strata, not noise).
        Terrace(f, 7f, 0.55f);

        // 3. Lake: bowl with the shoreline exactly at water level.
        if (L.Lake is { } lake)
        {
            float rmax = MathF.Max(lake.RadiusX, lake.RadiusZ) * 1.35f;
            ApplyRect(f, lake.X - rmax, lake.Z - rmax, lake.X + rmax, lake.Z + rmax, (x, z, k) =>
            {
                float dx = (x - lake.X) / lake.RadiusX, dz = (z - lake.Z) / lake.RadiusZ;
                float dn = MathF.Sqrt(dx * dx + dz * dz);
                if (dn > 1.3f) return;
                float bed = lake.WaterLevel - lake.Depth * (1f - MathF.Min(dn, 1f) * MathF.Min(dn, 1f));
                float shoreTarget = lake.WaterLevel + 0.6f * Mathf.Clamp((dn - 1f) / 0.3f, 0f, 1f);
                float target = dn < 1f ? bed : shoreTarget;
                float w = dn < 1f ? 1f : 1f - Smooth((dn - 1f) / 0.3f);
                float h = f.Heights[k];
                f.Heights[k] = Mathf.Lerp(h, dn < 1f ? MathF.Min(h, target) : MathF.Max(MathF.Min(h, target + 1.5f), target), w);
                if (dn < 1.02f) f.WaterDepth[k] = MathF.Max(f.WaterDepth[k], lake.WaterLevel - f.Heights[k]);
            });
        }

        // 4. Rivers: shallow trench whose bank tops follow the authored water level.
        foreach (var river in L.Rivers)
        {
            var pl = Polyline.FromControlPoints(river.Points, 3f);
            res.Rivers.Add(pl);
            float w = river.Width * 0.5f;
            ApplyAlong(f, pl, w * 4f, (dist, arc, h, x, z) =>
            {
                float t = pl.Length > 0 ? arc / pl.Length : 0f;
                float level = Mathf.Lerp(river.WaterLevelStart, river.WaterLevelEnd, t);
                float dn = dist / w;                        // 0 at centre, 1 at water edge
                float bank = level + 0.7f + 0.9f * Smooth(Mathf.Clamp((dn - 1f) / 1.2f, 0f, 1f));
                float bed = level - river.Depth * (1f - MathF.Min(dn, 1f) * MathF.Min(dn, 1f));
                float target = dn < 1f ? bed : bank;
                float blend = 1f - Smooth(Mathf.Clamp((dn - 2.2f) / 1.8f, 0f, 1f));
                return Mathf.Lerp(h, target, blend);
            });
            // Water depth pass (after heights are final for this river).
            ApplyAlong(f, pl, w * 1.15f, (dist, arc, h, x, z) => h, (dist, arc, k) =>
            {
                float t = pl.Length > 0 ? arc / pl.Length : 0f;
                float level = Mathf.Lerp(river.WaterLevelStart, river.WaterLevelEnd, t);
                f.WaterDepth[k] = MathF.Max(f.WaterDepth[k], level - f.Heights[k]);
            });
        }

        // 5. Flats for settlements.
        foreach (var flat in L.Terrain.Flats)
            ApplyRadial(f, flat.X, flat.Z, flat.Radius * 1.5f, (dn, h) => Mathf.Lerp(flat.Height, h, Smooth(Mathf.Clamp((dn * 1.5f - 0.8f) / 0.7f, 0f, 1f))));

        // 6. Roads: smooth the centreline height, then flatten the ground across the ribbon and paint it.
        foreach (var road in L.Roads)
        {
            var pl = Polyline.FromControlPoints(road.Points, 3f);
            res.Roads.Add(pl);
            float w = road.Width * 0.5f;
            // Centreline profile sampled every 2 m and smoothed over ~40 m so the road never jolts.
            int n = Math.Max(2, (int)(pl.Length / 2f) + 1);
            var prof = new float[n];
            for (int s = 0; s < n; s++) { var p = pl.PointAtArc(s * 2f); prof[s] = f.SampleHeight(p.X, p.Y); }
            prof = Smooth1D(prof, 10);
            ApplyAlong(f, pl, w * 3.2f, (dist, arc, h, x, z) =>
            {
                float target = prof[Math.Clamp((int)MathF.Round(arc / 2f), 0, n - 1)];
                float dn = dist / w;
                float blend = 1f - Smooth(Mathf.Clamp((dn - 1.0f) / 2.2f, 0f, 1f));
                return Mathf.Lerp(h, target, blend);
            }, (dist, arc, k) =>
            {
                float dn = dist / w;
                f.RoadDistance[k] = MathF.Min(f.RoadDistance[k], dist);
                float edge = road.Kind == "path" ? 0.55f : 0.75f;
                float jitter = 0.18f * splatNoise.GetNoise2D(f.WorldX(k % size) * 2f, f.WorldZ(k / size) * 2f);
                float paint = 1f - Smooth(Mathf.Clamp((dn - edge + jitter) / 0.6f, 0f, 1f));
                if (road.Kind == "path") paint *= 0.8f;
                f.SplatRoad[k] = MathF.Max(f.SplatRoad[k], paint);
            });
        }

        // 7. Ground layers and tint.
        var forestBiomes = L.Biomes.FindAll(b => b.GroundTexture == "forestFloor");
        var rockBiomes = L.Biomes.FindAll(b => b.GroundTexture == "rock");
        var meadowBiomes = L.Biomes.FindAll(b => b.GroundTexture == "meadow");
        for (int j = 0; j < size; j++)
        for (int i = 0; i < size; i++)
        {
            int k = f.Index(i, j);
            float x = f.WorldX(i), z = f.WorldZ(j), h = f.Heights[k];
            var nrm = f.NormalAt(i, j);
            float slopeDeg = Mathf.RadToDeg(MathF.Acos(Mathf.Clamp(nrm.Y, -1f, 1f)));
            float sn = splatNoise.GetNoise2D(x, z);

            float rock = Smooth(Mathf.Clamp((slopeDeg - 30f + sn * 6f) / 14f, 0f, 1f));
            foreach (var b in rockBiomes) rock = MathF.Max(rock, BiomeWeight(b, x, z, sn));
            float forest = 0f;
            foreach (var b in forestBiomes) forest = MathF.Max(forest, BiomeWeight(b, x, z, sn));
            float road = f.SplatRoad[k];
            // Lake/river beds read as muddy forest floor + rock.
            float wet = Mathf.Clamp(f.WaterDepth[k] / 1.5f, 0f, 1f);
            forest = MathF.Max(forest, wet * 0.7f);

            float grass = Mathf.Clamp(1f - rock - forest * 0.9f, 0f, 1f);
            f.SplatRock[k] = rock * (1f - road * 0.7f);
            f.SplatForest[k] = forest * (1f - road) * (1f - rock);
            f.SplatRoad[k] = road * (1f - rock * 0.6f);
            f.SplatGrass[k] = grass * (1f - road);

            // Tint: warm/cool hue drift, sunlit south-west facing slopes a touch lighter, hollows cooler.
            float tn = tintNoise.GetNoise2D(x, z);
            float facing = Mathf.Clamp(nrm.X * -0.35f + nrm.Z * 0.6f, -1f, 1f); // faces towards +Z (south) and -X (west)
            float v = 1f + 0.07f * tn + 0.05f * facing;
            float warm = 0.5f + 0.5f * tn;
            var tint = new Color(v * (1f + 0.04f * (warm - 0.5f)), v * (1f + 0.03f * (warm - 0.5f)), v * (1f - 0.06f * (warm - 0.5f)), 1f);
            foreach (var b in meadowBiomes)
            {
                float mw = BiomeWeight(b, x, z, sn);
                if (mw > 0f) tint = tint.Lerp(new Color(tint.R * 1.08f, tint.G * 1.02f, tint.B * 0.82f, 1f), mw * 0.8f);
            }
            f.Tint[k] = tint;
        }
        f.NormaliseSplat();
        return res;
    }

    // ---- helpers -------------------------------------------------------------------------------

    /// <summary>Blend steep slopes toward soft height steps so cliffs and mountains read as banded strata.</summary>
    private static void Terrace(HeightField f, float step, float strength)
    {
        var src = (float[])f.Heights.Clone();
        for (int j = 1; j < f.Size - 1; j++)
        for (int i = 1; i < f.Size - 1; i++)
        {
            int k = f.Index(i, j);
            float hl = src[f.Index(i - 1, j)], hr = src[f.Index(i + 1, j)];
            float hd = src[f.Index(i, j - 1)], hu = src[f.Index(i, j + 1)];
            float gx = (hr - hl) / (2f * f.CellSize), gz = (hu - hd) / (2f * f.CellSize);
            float slopeDeg = Mathf.RadToDeg(MathF.Atan(MathF.Sqrt(gx * gx + gz * gz)));
            float w = Smooth((slopeDeg - 26f) / 22f) * strength;
            if (w <= 0f) continue;
            float h = src[k];
            float q = MathF.Floor(h / step) * step;
            float fr = (h - q) / step;
            float terr = q + step * Smooth((fr - 0.25f) / 0.5f);
            f.Heights[k] = Mathf.Lerp(h, terr, w);
        }
    }

    private static float Smooth(float t) { t = Mathf.Clamp(t, 0f, 1f); return t * t * (3f - 2f * t); }
    private static float Cosine(float dn) => dn >= 1f ? 0f : 0.5f + 0.5f * MathF.Cos(MathF.PI * dn);

    private static float HillProfile(float dn, string shape) => shape switch
    {
        "plateau" => Smooth(Mathf.Clamp((1f - dn) * 2.2f, 0f, 1f)),
        "cliff" => dn < 0.62f ? 1f : (dn < 0.82f ? 1f - Smooth((dn - 0.62f) / 0.2f) * 0.85f : 0.15f * Cosine((dn - 0.82f) / 0.18f)),
        _ => Cosine(dn),
    };

    private static float BiomeWeight(BiomeSpec b, float x, float z, float noise)
    {
        float dx = x - b.X, dz = z - b.Z;
        float dn = MathF.Sqrt(dx * dx + dz * dz) / MathF.Max(b.Radius, 1f);
        return 1f - Smooth(Mathf.Clamp((dn - 0.72f + noise * 0.12f) / 0.3f, 0f, 1f));
    }

    private static void ApplyRadial(HeightField f, float cx, float cz, float radius, Func<float, float, float> fn)
    {
        ApplyRect(f, cx - radius, cz - radius, cx + radius, cz + radius, (x, z, k) =>
        {
            float dn = MathF.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz)) / radius;
            if (dn < 1f) f.Heights[k] = fn(dn, f.Heights[k]);
        });
    }

    private static void ApplyRect(HeightField f, float x0, float z0, float x1, float z1, Action<float, float, int> fn)
    {
        var (gi0, gj0) = f.ToGrid(x0, z0);
        var (gi1, gj1) = f.ToGrid(x1, z1);
        int i0 = Math.Clamp((int)MathF.Floor(gi0), 0, f.Size - 1), i1 = Math.Clamp((int)MathF.Ceiling(gi1), 0, f.Size - 1);
        int j0 = Math.Clamp((int)MathF.Floor(gj0), 0, f.Size - 1), j1 = Math.Clamp((int)MathF.Ceiling(gj1), 0, f.Size - 1);
        for (int j = j0; j <= j1; j++)
        for (int i = i0; i <= i1; i++)
            fn(f.WorldX(i), f.WorldZ(j), f.Index(i, j));
    }

    /// <summary>Visit vertices within <paramref name="influence"/> of the polyline; heightFn returns the new height.</summary>
    private static void ApplyAlong(HeightField f, Polyline pl, float influence,
        Func<float, float, float, float, float, float> heightFn, Action<float, float, int>? extra = null)
    {
        if (pl.Points.Count < 2) return;
        ApplyRect(f, pl.Min.X - influence, pl.Min.Y - influence, pl.Max.X + influence, pl.Max.Y + influence, (x, z, k) =>
        {
            var (dist, arc) = pl.Closest(new Vector2(x, z));
            if (dist > influence) return;
            f.Heights[k] = heightFn(dist, arc, f.Heights[k], x, z);
            extra?.Invoke(dist, arc, k);
        });
    }

    private static float[] Smooth1D(float[] a, int radius)
    {
        var o = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            float s = 0f; int c = 0;
            for (int d = -radius; d <= radius; d++)
            {
                int q = Math.Clamp(i + d, 0, a.Length - 1);
                s += a[q]; c++;
            }
            o[i] = s / c;
        }
        return o;
    }
}
