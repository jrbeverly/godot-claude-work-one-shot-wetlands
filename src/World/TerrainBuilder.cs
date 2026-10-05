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
                float ramp = MathF.Pow(t * t * (3f - 2f * t), rim.RampPower);
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
        Terrace(f, 10f, 0.25f, rimNoise);

        // 2c. Marsh floor and hummocks (before the lake/rivers so pools and channels still cut through them).
        if (L.Marsh is { } marsh)
        {
            var marshNoise = new FastNoiseLite { Seed = seed + 41, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 48f, FractalOctaves = 2, FractalGain = 0.5f };
            // Pull the ground toward a floor just above the water; broad noise lets it dip below in places.
            if (marsh.FloorStrength > 0f) ApplyRect(f, marsh.X - marsh.Radius, marsh.Z - marsh.Radius, marsh.X + marsh.Radius, marsh.Z + marsh.Radius, (x, z, k) =>
            {
                float dn = MathF.Sqrt((x - marsh.X) * (x - marsh.X) + (z - marsh.Z) * (z - marsh.Z)) / marsh.Radius;
                if (dn >= 1f) return;
                float w = marsh.FloorStrength * (1f - Smooth((dn - 0.7f) / 0.3f));
                float floor = marsh.WaterLevel + marsh.FloorAbove + marshNoise.GetNoise2D(x, z) * marsh.FloorNoise;
                float h = f.Heights[k];
                // Only ground that is already low joins the bog; hills and the rim keep their height.
                float lowness = 1f - Smooth((h - floor - 6f) / 10f);
                f.Heights[k] = Mathf.Lerp(h, floor, w * lowness);
            });
            if (marsh.Hummocks.Enabled)
            {
                var hm = marsh.Hummocks;
                var hrng = new RandomNumberGenerator { Seed = (ulong)seed * 13UL + 5UL };
                for (float gz = marsh.Z - marsh.Radius; gz <= marsh.Z + marsh.Radius; gz += hm.Spacing)
                for (float gx = marsh.X - marsh.Radius; gx <= marsh.X + marsh.Radius; gx += hm.Spacing)
                {
                    float cx = gx + hrng.RandfRange(-0.45f, 0.45f) * hm.Spacing, cz = gz + hrng.RandfRange(-0.45f, 0.45f) * hm.Spacing;
                    float r = hrng.RandfRange(hm.RadiusMin, hm.RadiusMax), hh = hrng.RandfRange(hm.HeightMin, hm.HeightMax);
                    bool keep = hrng.Randf() < hm.Coverage;
                    float dn = MathF.Sqrt((cx - marsh.X) * (cx - marsh.X) + (cz - marsh.Z) * (cz - marsh.Z)) / marsh.Radius;
                    if (!keep || dn > 0.97f) continue;
                    // Not under roads (they would leave a shelf beside the ribbon) nor on settlement flats (a ring step).
                    bool blocked = false;
                    foreach (var road in L.Roads) { var rp = Polyline.FromControlPoints(road.Points, 3f); if (rp.Closest(new Vector2(cx, cz)).dist < r + 5f) { blocked = true; break; } }
                    if (!blocked) foreach (var site in L.Sites) { float sdx = site.X - cx, sdz = site.Z - cz; if (MathF.Sqrt(sdx * sdx + sdz * sdz) < site.ClearRadius + r * 0.6f) { blocked = true; break; } }
                    if (blocked) continue;
                    // Mounds only grow out of low ground; on hills they would read as warts.
                    float ground = f.SampleHeight(cx, cz);
                    if (ground > marsh.WaterLevel + 5f) continue;
                    float squash = hrng.RandfRange(0.7f, 1.3f);     // elliptical, not all round
                    float ang = hrng.RandfRange(0f, MathF.PI);
                    ApplyRect(f, cx - r * 1.4f, cz - r * 1.4f, cx + r * 1.4f, cz + r * 1.4f, (x, z, k) =>
                    {
                        float dx = x - cx, dz = z - cz;
                        float u = (dx * MathF.Cos(ang) + dz * MathF.Sin(ang)) / (r * squash), v = (-dx * MathF.Sin(ang) + dz * MathF.Cos(ang)) / (r / squash);
                        float d = MathF.Sqrt(u * u + v * v);
                        if (d >= 1f) return;
                        // Flat-topped dome with a small step at the rim: once the sheet laps it, it reads as a cut peat bank.
                        f.Heights[k] += hh * (0.12f + 0.88f * Smooth(Mathf.Clamp((1f - d) * 1.5f, 0f, 1f)));
                    });
                }
            }
            foreach (var pool in marsh.Pools)
            {
                float rmax = MathF.Max(pool.RadiusX, pool.RadiusZ) * 1.3f;
                ApplyRect(f, pool.X - rmax, pool.Z - rmax, pool.X + rmax, pool.Z + rmax, (x, z, k) =>
                {
                    float dx = (x - pool.X) / pool.RadiusX, dz = (z - pool.Z) / pool.RadiusZ;
                    float dn = MathF.Sqrt(dx * dx + dz * dz);
                    if (dn > 1.25f) return;
                    float bed = marsh.WaterLevel - pool.Depth * Cosine(MathF.Min(dn, 1f));
                    float h = f.Heights[k];
                    // Inside: never above the bed. Rim: ease the bank down toward it over the outer 25 %.
                    if (dn < 1f) f.Heights[k] = MathF.Min(h, bed);
                    else f.Heights[k] = Mathf.Lerp(MathF.Min(h, marsh.WaterLevel + 0.6f), h, Smooth((dn - 1f) / 0.25f));
                });
            }
        }

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

        // 5b. Flats may raise islets/promontories above the lake: recompute lake depth from final heights.
        if (L.Lake is { } lk)
        {
            float rmax = MathF.Max(lk.RadiusX, lk.RadiusZ) * 1.1f;
            ApplyRect(f, lk.X - rmax, lk.Z - rmax, lk.X + rmax, lk.Z + rmax, (x, z, k) =>
            {
                float dx = (x - lk.X) / lk.RadiusX, dz = (z - lk.Z) / lk.RadiusZ;
                if (dx * dx + dz * dz < 1.02f * 1.02f) f.WaterDepth[k] = MathF.Max(0f, lk.WaterLevel - f.Heights[k]);
            });
        }

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
            // A causeway, never a ford: inside the marsh the road rides at least 0.6 m above the sheet.
            if (L.Marsh is { } mr)
                for (int s = 0; s < n; s++)
                {
                    var p = pl.PointAtArc(s * 2f);
                    if ((p.X - mr.X) * (p.X - mr.X) + (p.Y - mr.Z) * (p.Y - mr.Z) < mr.Radius * mr.Radius) prof[s] = MathF.Max(prof[s], mr.WaterLevel + 0.6f);
                }
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
                float edge = road.Kind == "path" ? 0.45f : 0.6f;
                float jitter = 0.18f * splatNoise.GetNoise2D(f.WorldX(k % size) * 2f, f.WorldZ(k / size) * 2f);
                float paint = 1f - Smooth(Mathf.Clamp((dn - edge + jitter) / 0.6f, 0f, 1f));
                if (road.Kind == "path") paint *= 0.8f;
                f.SplatRoad[k] = MathF.Max(f.SplatRoad[k], paint);
            });
        }

        // 6b. Marsh water depth from the final heights: everything inside the disc below the sheet is under water,
        // and nothing a road or flat lifted above it counts as wet any more.
        if (L.Marsh is { } ms)
            ApplyRadial(f, ms.X, ms.Z, ms.Radius, (dn, h) => h, (x, z, k) =>
            {
                float depth = ms.WaterLevel - f.Heights[k];
                if (depth > 0f) f.WaterDepth[k] = MathF.Max(f.WaterDepth[k], depth);
                else if (f.WaterDepth[k] > 0f && f.WaterDepth[k] < 1.5f) f.WaterDepth[k] = 0f;
            });

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

            float rock = Smooth(Mathf.Clamp((slopeDeg - 36f + sn * 5f) / 12f, 0f, 1f));
            // High fells are bare rock whatever their slope (the snow line sits above them on all but the north rim).
            rock = MathF.Max(rock, Smooth((h - 90f + sn * 10f) / 45f));
            foreach (var b in rockBiomes) rock = MathF.Max(rock, BiomeWeight(b, x, z, sn));
            float forest = 0f;
            foreach (var b in forestBiomes) forest = MathF.Max(forest, BiomeWeight(b, x, z, sn));
            float road = f.SplatRoad[k] * (1f - 0.22f * forest);   // the road darkens into the wood
            // Lake/river beds read as muddy forest floor + rock.
            float wet = Mathf.Clamp(f.WaterDepth[k] / 1.5f, 0f, 1f);
            forest = MathF.Max(forest, wet * 0.7f);

            float meadow = 0f;
            foreach (var b in meadowBiomes) meadow = MathF.Max(meadow, BiomeWeight(b, x, z, sn));

            // Marsh ground: peat (the forest-floor layer) covers the low bog, a sedge band (meadow layer) rings the
            // water, and plain grass survives only on the crowns of hummocks and the higher ground around the bog.
            if (L.Marsh is { } mh)
            {
                float dn = MathF.Sqrt((x - mh.X) * (x - mh.X) + (z - mh.Z) * (z - mh.Z)) / mh.Radius;
                if (dn < 1f)
                {
                    float inMarsh = 1f - Smooth((dn - 0.78f) / 0.22f);
                    float above = h - mh.WaterLevel;
                    float peat = inMarsh * (1f - Smooth((above - 1.9f + sn * 0.7f) / 2.6f));
                    float sedge = inMarsh * (1f - Smooth((above - 0.25f + sn * 0.35f) / 1.0f)) * Smooth((above + 0.4f) / 0.6f);
                    meadow = MathF.Max(meadow, sedge);
                    forest = MathF.Max(forest, peat * (1f - sedge * 0.75f));
                }
            }
            meadow *= (1f - forest) * (1f - rock);

            float grass = Mathf.Clamp(1f - rock - forest * 0.9f - meadow, 0f, 1f);
            f.SplatRock[k] = rock * (1f - road * 0.7f);
            f.SplatForest[k] = forest * (1f - road) * (1f - rock);
            f.SplatRoad[k] = road * (1f - rock * 0.6f);
            f.SplatMeadow[k] = meadow * (1f - road);
            f.SplatGrass[k] = grass * (1f - road);

            // Tint: warm/cool hue drift, sunlit south-west facing slopes a touch lighter, hollows cooler.
            float tn = tintNoise.GetNoise2D(x, z);
            float facing = Mathf.Clamp(nrm.X * -0.35f + nrm.Z * 0.6f, -1f, 1f); // faces towards +Z (south) and -X (west)
            float v = 1f + 0.10f * tn + 0.05f * facing;
            float warm = 0.5f + 0.5f * tn;
            // Warm pole = reddish peat, cool pole = olive: the baked patchiness Classic ground has instead of lighting.
            var tint = new Color(v * (1f + 0.06f * (warm - 0.5f)), v * (1f + 0.02f * (warm - 0.5f)), v * (1f - 0.09f * (warm - 0.5f)), 1f);
            // Banks darken toward the water (damp peat lip).
            if (L.Marsh is { } mt && f.WaterDepth[k] <= 0f)
            {
                float above = h - mt.WaterLevel;
                float damp = 1f - Smooth((above - 0.2f) / 0.9f);
                tint *= 1f - 0.12f * Mathf.Clamp(damp, 0f, 1f);
                tint.A = 1f;
            }
            // Meadow has its own straw tile; only a whisper of warmth in the tint so the blend edge stays soft.
            if (meadow > 0f) tint = tint.Lerp(new Color(tint.R * 1.03f, tint.G * 1.0f, tint.B * 0.95f, 1f), meadow);
            f.Tint[k] = tint;
        }
        f.NormaliseSplat();
        return res;
    }

    // ---- helpers -------------------------------------------------------------------------------

    /// <summary>Blend steep slopes toward soft height steps so cliffs and mountains read as banded strata.</summary>
    private static void Terrace(HeightField f, float step, float strength, FastNoiseLite phase)
    {
        var src = (float[])f.Heights.Clone();
        for (int j = 1; j < f.Size - 1; j++)
        for (int i = 1; i < f.Size - 1; i++)
        {
            int k = f.Index(i, j);
            // Tilt the strata a little so bands are not dead level contour lines.
            float shift = phase.GetNoise2D(f.WorldX(i) * 0.6f, f.WorldZ(j) * 0.6f) * step * 0.7f;
            float hl = src[f.Index(i - 1, j)], hr = src[f.Index(i + 1, j)];
            float hd = src[f.Index(i, j - 1)], hu = src[f.Index(i, j + 1)];
            float gx = (hr - hl) / (2f * f.CellSize), gz = (hu - hd) / (2f * f.CellSize);
            float slopeDeg = Mathf.RadToDeg(MathF.Atan(MathF.Sqrt(gx * gx + gz * gz)));
            float w = Smooth((slopeDeg - 30f) / 22f) * strength;
            if (w <= 0f) continue;
            float h = src[k] + shift;
            float q = MathF.Floor(h / step) * step;
            float fr = (h - q) / step;
            float terr = q + step * Smooth((fr - 0.2f) / 0.6f) - shift;
            f.Heights[k] = Mathf.Lerp(src[k], terr, w);
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

    private static void ApplyRadial(HeightField f, float cx, float cz, float radius, Func<float, float, float> fn, Action<float, float, int>? extra = null)
    {
        ApplyRect(f, cx - radius, cz - radius, cx + radius, cz + radius, (x, z, k) =>
        {
            float dn = MathF.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz)) / radius;
            if (dn >= 1f) return;
            f.Heights[k] = fn(dn, f.Heights[k]);
            extra?.Invoke(x, z, k);
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
