using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Trees, undergrowth, grass tufts and rocks from <see cref="BiomeSpec"/>s via MultiMeshInstance3D. Runs last.
/// Classic-feel rules baked in: trees come in clumped stands with open glades between them, never a uniform
/// carpet; tufts and flowers are sparse accents; nothing grows on roads, in water, on cliffs or in settlements.
/// </summary>
public static class FoliagePlacer
{
    private const int MaxTrees = 3500;
    private const int MaxUndergrowth = 45000;
    private const float EdgeMargin = 40f;      // keep the map rim bare so the mountain silhouette stays clean
    private const float TreeAltitudeLimit = 125f;
    private const float PineAltitudeLimit = 165f;
    private const float ChunkSize = 128f;      // MultiMeshes are split spatially so the camera can cull/LOD them
    // Kinds with a visibility range are chunked finer: the range is judged on the whole MultiMesh AABB, so a big
    // chunk would fade out its near instances along with its far ones.
    private const float RangedChunkSize = 40f;

    private enum Kind { Tree, Bush, Tuft, Rock, Pebble }
    /// <summary>Boulders match the warm palette rock (#857C70) instead of the kit's cool blue-grey.</summary>
    private static readonly Color RockTint = new(1.0f, 0.95f, 0.88f);

    private readonly record struct Instance(Transform3D Xf, Color Tint);
    private readonly record struct GroupKey(string Model, Color Tint, Kind Kind, int Cx, int Cz);

    public static void Build(WorldRoot world)
    {
        var L = world.Layout;
        var f = world.Field;
        var rng = new RandomNumberGenerator { Seed = (ulong)world.Seed * 7919UL + 17UL };
        // Clump mask: ~40 m stands and glades. Clumpiness raises the threshold so stands get tighter and rarer.
        var clump = new FastNoiseLite { Seed = world.Seed + 101, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 38f, FractalOctaves = 2, FractalGain = 0.4f };
        // Separate patchiness for flowers/tufts so they gather in drifts instead of following the tree stands.
        var patch = new FastNoiseLite { Seed = world.Seed + 211, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 16f, FractalOctaves = 2, FractalGain = 0.5f };
        // Slow hue/value drift across the map so one stand reads a little warmer or darker than the next (baked-light look).
        var drift = new FastNoiseLite { Seed = world.Seed + 307, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1f / 55f };
        Color Vary(float x, float z, float amount)
        {
            float d = drift.GetNoise2D(x, z);                     // -1..1, coherent
            float v = 1f + amount * (0.7f * d + 0.3f * rng.RandfRange(-1f, 1f));
            float warm = amount * 0.6f * d;
            return new Color(v * (1f + warm), v, v * (1f - warm), 1f);
        }

        var root = new Node3D { Name = "Foliage" };
        world.AddChild(root);

        var groups = new Dictionary<GroupKey, List<Instance>>();
        var biomes = L.Biomes.OrderByDescending(b => b.Priority).ToList();

        // ---- Trees ---------------------------------------------------------------------------
        var trees = new List<(GroupKey key, Instance inst)>();
        foreach (var biome in biomes)
        {
            if (biome.Trees.Count == 0 || biome.TreeDensity <= 0f) continue;
            float threshold = ClumpThreshold(biome);
            // Base spacing 1 -> 5 m, 0.05 -> ~22 m; then tightened inside stands to give back part of what the
            // clump cut removes, so a dense wood stays dense where it exists and the glades carry the openness.
            float spacing = 5f / MathF.Sqrt(Mathf.Clamp(biome.TreeDensity, 0.04f, 1f));
            spacing *= Mathf.Lerp(1f, MathF.Sqrt(Mathf.Clamp(1f - threshold, 0.2f, 1f)), 0.6f);
            var tint = biome.LeafTint is { Length: > 0 } ? new Color(biome.LeafTint) : Colors.White;
            float totalWeight = biome.Trees.Sum(t => MathF.Max(t.Weight, 0f));
            if (totalWeight <= 0f) continue;

            ForEachGridCell(biome, spacing, rng, (x, z) =>
            {
                if (!Owns(biomes, biome, x, z)) return;
                float c = clump.GetNoise2D(x, z) * 0.5f + 0.5f;
                // Ragged stand edges: a soft probability band around the threshold instead of a hard cut.
                float keep = Mathf.SmoothStep(threshold - 0.08f, threshold + 0.1f, c);
                // In the bog, stands gather on the hummock crowns and islands; the open sheet between stays treeless.
                if (L.Marsh is { } mk)
                {
                    float dxm = x - mk.X, dzm = z - mk.Z;
                    if (dxm * dxm + dzm * dzm < mk.Radius * mk.Radius)
                        keep *= Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(mk.WaterLevel + 0.8f, mk.WaterLevel + 2.2f, f.SampleHeight(x, z)));
                }
                if (rng.Randf() > keep) return;

                var choice = PickTree(biome.Trees, totalWeight, rng.Randf() * totalWeight);
                bool pine = choice.Model.StartsWith("Pine", StringComparison.OrdinalIgnoreCase);
                // Marsh trees stand in the shallows and on the banks; everything else keeps off the water.
                bool wader = choice.Model.StartsWith("MossTree", StringComparison.OrdinalIgnoreCase) || choice.Model.StartsWith("BogOak", StringComparison.OrdinalIgnoreCase);
                float scale = choice.Scale * rng.RandfRange(0.85f, 1.2f);
                if (!TreeSpotOk(f, x, z, scale, pine ? PineAltitudeLimit : TreeAltitudeLimit, wader)) return;

                var model = ModelLibrary.Get(choice.Model);
                if (model == null) return;
                float y = f.SampleHeight(x, z) - 0.3f * scale; // sink the trunk so roots do not float on slopes
                var xf = Upright(x, y, z, rng.RandfRange(0f, Mathf.Tau), scale);
                trees.Add((Key(choice.Model, tint, Kind.Tree, x, z), new Instance(xf, Vary(x, z, 0.09f))));
            });
        }
        if (trees.Count > MaxTrees)
        {
            GD.Print($"Foliage: {trees.Count} tree candidates, thinning to {MaxTrees}");
            Shuffle(trees, rng);
            trees.RemoveRange(MaxTrees, trees.Count - MaxTrees);
        }
        foreach (var (key, inst) in trees) Add(groups, key, inst);

        // ---- Undergrowth per biome --------------------------------------------------------------
        var under = new List<(GroupKey key, Instance inst)>();
        foreach (var biome in biomes)
        {
            var biomeTint = biome.LeafTint is { Length: > 0 } ? new Color(biome.LeafTint) : Colors.White;
            float threshold = 0.5f + 0.3f * Mathf.Clamp(biome.Clumpiness, 0f, 1f);
            foreach (var choice in biome.Undergrowth)
            {
                if (choice.Density <= 0f) continue;
                var tint = choice.Tint is { Length: > 0 } ? new Color(choice.Tint) : biomeTint;
                var model = ModelLibrary.Get(choice.Model);
                if (model == null) continue;
                var kind = ClassifyUndergrowth(choice.Model);
                float spacing = 1.7f / MathF.Sqrt(Mathf.Clamp(choice.Density, 0.01f, 1f)); // 0.2 -> 3.8 m, 1 -> 1.7 m
                bool shadeLover = kind == Kind.Bush && (choice.Model.StartsWith("Fern") || choice.Model.StartsWith("Mushroom"));
                bool sunLover = choice.Model.StartsWith("Flower") || choice.Model.StartsWith("Clover");
                // Reeds crowd the water's edge and stand in the shallows; away from water they thin to stragglers.
                bool reed = choice.Model.StartsWith("Reeds", StringComparison.OrdinalIgnoreCase);
                float keepBase = Mathf.Clamp(choice.Weight, 0.05f, 1f);

                ForEachGridCell(biome, spacing, rng, (x, z) =>
                {
                    if (!Owns(biomes, biome, x, z)) return;
                    float c = clump.GetNoise2D(x, z) * 0.5f + 0.5f;
                    float p = patch.GetNoise2D(x, z) * 0.5f + 0.5f;
                    // Ferns/mushrooms live in the stands, flowers in the glades; tufts drift in patches everywhere.
                    float keep = keepBase * Mathf.SmoothStep(0.3f, 0.8f, p);
                    if (shadeLover) keep *= Mathf.SmoothStep(threshold - 0.15f, threshold + 0.1f, c);
                    else if (sunLover) keep *= 1f - 0.8f * Mathf.SmoothStep(threshold - 0.05f, threshold + 0.15f, c);
                    if (reed)
                    {
                        // Full density within ~7 m of water, a quarter elsewhere; the patch noise still breaks it up.
                        keep = keepBase * (NearWater(f, x, z, 7f) ? 1f : 0.25f) * Mathf.SmoothStep(0.2f, 0.7f, p);
                        if (rng.Randf() > keep) return;
                        if (!ReedSpotOk(f, x, z)) return;
                    }
                    else
                    {
                        if (rng.Randf() > keep) return;
                        if (!UndergrowthSpotOk(f, x, z)) return;
                    }

                    float scale = BaseScale(choice.Model) * choice.Scale * rng.RandfRange(0.8f, 1.25f);
                    float y = f.SampleHeight(x, z) - 0.06f - 0.1f * MathF.Min(f.SampleSlopeDeg(x, z) / 30f, 1f);
                    var xf = Upright(x, y, z, rng.RandfRange(0f, Mathf.Tau), scale);
                    under.Add((Key(choice.Model, tint, kind, x, z), new Instance(xf, Vary(x, z, 0.12f))));
                });
            }
        }

        // ---- Rocks on rocky ground, pebbles along road verges -------------------------------------
        int rocks = 0, pebbles = 0;
        {
            string[] rockModels = { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3" };
            float half = f.HalfExtent - EdgeMargin;
            for (float z = -half; z < half; z += 14f)
            for (float x = -half; x < half; x += 14f)
            {
                float px = x + rng.RandfRange(-6f, 6f), pz = z + rng.RandfRange(-6f, 6f);
                if (f.SampleRock(px, pz) < 0.45f || rng.Randf() > 0.45f) continue;
                if (f.SampleWaterDepth(px, pz) > 0f || f.SampleRoadDistance(px, pz) < 4f || f.SampleSiteClear(px, pz) > 0.25f) continue;
                // Boulders belong on the lower slopes; on the high walls they would float as dark blobs in the fog.
                if (f.SampleSlopeDeg(px, pz) > 42f || f.SampleHeight(px, pz) > 60f) continue;
                string m = rockModels[rng.RandiRange(0, rockModels.Length - 1)];
                if (ModelLibrary.Get(m) == null) continue;
                // Mostly modest boulders with the odd big one, so slopes read as clusters rather than gravel.
                float scale = rng.Randf() < 0.1f ? rng.RandfRange(2.6f, 3.5f) : rng.RandfRange(0.8f, 2.2f);
                // Boulders lean into the slope a little; sunk so they look bedded, not dropped.
                var basis = Basis.Identity.Rotated(Vector3.Up, rng.RandfRange(0f, Mathf.Tau));
                var n = f.SampleNormal(px, pz);
                basis = TiltTowards(basis, n, 0.5f).Scaled(Vector3.One * scale);
                var xf = new Transform3D(basis, new Vector3(px, f.SampleHeight(px, pz) - 0.35f * scale, pz));
                Add(groups, Key(m, RockTint, Kind.Rock, px, pz), new Instance(xf, Vary(px, pz, 0.08f) * RockTint));
                rocks++;
            }

            string[] pebbleModels = { "Pebble_Round_1", "Pebble_Round_3", "Pebble_Square_1", "Pebble_Square_3" };
            foreach (var road in world.Terrain.Roads)
            {
                for (float arc = 0f; arc < road.Length; arc += 2.5f)
                {
                    if (rng.Randf() > 0.2f) continue;
                    var p = road.PointAtArc(arc);
                    var t = road.TangentAtArc(arc);
                    var side = new Vector2(-t.Y, t.X).Normalized() * (rng.Randf() < 0.5f ? -1f : 1f);
                    float off = rng.RandfRange(2f, 4f);
                    float px = p.X + side.X * off, pz = p.Y + side.Y * off;
                    float rd = f.SampleRoadDistance(px, pz);
                    if (rd < 2f || rd > 4f || !f.InBounds(px, pz)) continue;
                    if (f.SampleWaterDepth(px, pz) > 0f || f.SampleSiteClear(px, pz) > 0.6f) continue;
                    string m = pebbleModels[rng.RandiRange(0, pebbleModels.Length - 1)];
                    if (ModelLibrary.Get(m) == null) continue;
                    float scale = rng.RandfRange(0.9f, 1.6f);  // kit pebbles are 0.4 m: fist- to head-sized verge stones, half sunk
                    var xf = Upright(px, f.SampleHeight(px, pz) - 0.08f * scale, pz, rng.RandfRange(0f, Mathf.Tau), scale);
                    Add(groups, Key(m, Colors.White, Kind.Pebble, px, pz), new Instance(xf, Colors.White));
                    pebbles++;
                }
            }
        }

        if (under.Count > MaxUndergrowth)
        {
            GD.Print($"Foliage: {under.Count} undergrowth candidates, thinning to {MaxUndergrowth}");
            Shuffle(under, rng);
            under.RemoveRange(MaxUndergrowth, under.Count - MaxUndergrowth);
        }
        foreach (var (key, inst) in under) Add(groups, key, inst);

        // ---- Emit MultiMeshes ------------------------------------------------------------------------
        int nodes = 0;
        foreach (var (key, list) in groups)
        {
            var model = ModelLibrary.Get(key.Model)!;
            var (sway, swayH) = key.Kind switch
            {
                Kind.Tree => (0.11f, MathF.Max(model.Bounds.End.Y * 0.8f, 1f)),
                Kind.Bush => (0.08f, MathF.Max(model.Bounds.End.Y, 0.5f)),
                Kind.Tuft => (0.16f, MathF.Max(model.Bounds.End.Y, 0.3f)),
                _ => (0f, 1f),
            };
            var mesh = ModelLibrary.GetStyledMesh(model, key.Tint, sway, swayH);
            // UseColors: the per-instance tint reaches the shaders as COLOR (foliage.gdshader multiplies it in,
            // StandardMaterial3D does so via vertex-colour-as-albedo, which the importer enables for these kits).
            var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = list.Count };
            var partXf = model.Parts[0].Local;
            for (int i = 0; i < list.Count; i++)
            {
                mm.SetInstanceTransform(i, list[i].Xf * partXf);
                mm.SetInstanceColor(i, list[i].Tint);
            }

            var mmi = new MultiMeshInstance3D
            {
                Name = $"{key.Kind}_{key.Model}_{key.Cx}_{key.Cz}",
                Multimesh = mm,
                CastShadow = key.Kind is Kind.Tree or Kind.Rock
                    ? GeometryInstance3D.ShadowCastingSetting.On
                    : GeometryInstance3D.ShadowCastingSetting.Off,
            };
            // Reeds are the bog's main ground cover (a few quads each), so they stay visible much further than tufts.
            float rangeEnd = key.Model.StartsWith("Reeds", StringComparison.OrdinalIgnoreCase) ? 170f
                : key.Kind switch { Kind.Tuft => 90f, Kind.Pebble => 70f, Kind.Bush => 220f, _ => 0f };
            if (rangeEnd > 0f)
            {
                mmi.VisibilityRangeEnd = rangeEnd;
                mmi.VisibilityRangeEndMargin = rangeEnd * 0.2f;
                // Dithered self-fade only works on the foliage ShaderMaterial here; on the kit's StandardMaterial3D
                // (pebbles, mushrooms) it hid the instances outright, so those get a plain hysteresis cut instead.
                bool shaderMat = mesh.SurfaceGetMaterial(0) is ShaderMaterial;
                mmi.VisibilityRangeFadeMode = shaderMat
                    ? GeometryInstance3D.VisibilityRangeFadeModeEnum.Self
                    : GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled;
            }
            root.AddChild(mmi);
            nodes++;
        }

        int tufts = under.Count(u => u.key.Kind == Kind.Tuft), bushes = under.Count - tufts;
        GD.Print($"Foliage: {trees.Count} trees, {under.Count} undergrowth ({bushes} bushes/ferns/mushrooms, {tufts} tufts/flowers), {rocks} rocks, {pebbles} pebbles in {nodes} MultiMeshes");
    }

    // ---- placement rules ---------------------------------------------------------------------------

    private static bool TreeSpotOk(HeightField f, float x, float z, float scale, float altitudeLimit, bool wader = false)
    {
        float margin = f.HalfExtent - EdgeMargin;
        if (MathF.Abs(x) > margin || MathF.Abs(z) > margin) return false;
        if (f.SampleRoadDistance(x, z) < 4.5f + scale) return false;
        if (f.SampleSlopeDeg(x, z) > 36f) return false;
        if (f.SampleSiteClear(x, z) > 0.25f) return false;
        if (f.SampleHeight(x, z) > altitudeLimit) return false;
        // Waders may stand in up to half a metre of water (roots in the bog, as in the references).
        if (wader) return f.SampleWaterDepth(x, z) < 0.5f;
        // Banks: no trunks in the water nor overhanging it from within 3 m.
        if (f.SampleWaterDepth(x, z) > 0f) return false;
        for (int k = 0; k < 4; k++)
        {
            float a = k * Mathf.Pi * 0.5f;
            if (f.SampleWaterDepth(x + 3f * MathF.Cos(a), z + 3f * MathF.Sin(a)) > 0f) return false;
        }
        return true;
    }

    private static bool UndergrowthSpotOk(HeightField f, float x, float z)
    {
        float margin = f.HalfExtent - EdgeMargin;
        if (MathF.Abs(x) > margin || MathF.Abs(z) > margin) return false;
        if (f.SampleRoadDistance(x, z) < 2.8f) return false;
        if (f.SampleWaterDepth(x, z) > 0f || f.SampleWaterDepth(x + 1f, z) > 0f || f.SampleWaterDepth(x, z + 1f) > 0f) return false;
        if (f.SampleSlopeDeg(x, z) > 40f) return false;
        if (f.SampleSiteClear(x, z) > 0.5f) return false;
        if (f.SampleHeight(x, z) > TreeAltitudeLimit) return false;
        return true;
    }

    /// <summary>Reeds: like other undergrowth but allowed to stand in up to 0.6 m of water.</summary>
    private static bool ReedSpotOk(HeightField f, float x, float z)
    {
        float margin = f.HalfExtent - EdgeMargin;
        if (MathF.Abs(x) > margin || MathF.Abs(z) > margin) return false;
        if (f.SampleRoadDistance(x, z) < 2.4f) return false;
        if (f.SampleWaterDepth(x, z) > 0.6f) return false;
        if (f.SampleSlopeDeg(x, z) > 40f) return false;
        if (f.SampleSiteClear(x, z) > 0.5f) return false;
        if (f.SampleHeight(x, z) > TreeAltitudeLimit) return false;
        return true;
    }

    /// <summary>Any water within <paramref name="r"/> metres (eight compass samples plus the centre).</summary>
    private static bool NearWater(HeightField f, float x, float z, float r)
    {
        if (f.SampleWaterDepth(x, z) > 0f) return true;
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.Pi * 0.25f;
            if (f.SampleWaterDepth(x + r * MathF.Cos(a), z + r * MathF.Sin(a)) > 0f) return true;
        }
        return false;
    }

    /// <summary>True when <paramref name="biome"/> is the highest-priority biome covering the point.</summary>
    private static bool Owns(List<BiomeSpec> byPriority, BiomeSpec biome, float x, float z)
    {
        foreach (var b in byPriority)
        {
            if (b == biome) return true;
            if (b.Priority <= biome.Priority) return true; // same priority: order in the list decides; first wins
            float dx = x - b.X, dz = z - b.Z;
            if (dx * dx + dz * dz < b.Radius * b.Radius) return false;
        }
        return true;
    }

    /// <summary>Jittered grid over the biome disc, with a soft radial falloff so biome edges thin out instead of ending in a circle.</summary>
    private static void ForEachGridCell(BiomeSpec b, float spacing, RandomNumberGenerator rng, Action<float, float> fn)
    {
        float r = b.Radius;
        for (float z = b.Z - r; z <= b.Z + r; z += spacing)
        for (float x = b.X - r; x <= b.X + r; x += spacing)
        {
            float px = x + rng.RandfRange(-0.45f, 0.45f) * spacing;
            float pz = z + rng.RandfRange(-0.45f, 0.45f) * spacing;
            float dn = MathF.Sqrt((px - b.X) * (px - b.X) + (pz - b.Z) * (pz - b.Z)) / MathF.Max(r, 1f);
            if (dn >= 1f) continue;
            float edge = 1f - Mathf.SmoothStep(0.7f, 1f, dn);
            if (rng.Randf() > edge) continue;
            fn(px, pz);
        }
    }

    /// <summary>Clump-noise cut (0..1): 0.25 at clumpiness 0 still leaves a few gaps; 0.8 at 1 gives tight, rare stands.</summary>
    private static float ClumpThreshold(BiomeSpec b) => 0.25f + 0.55f * Mathf.Clamp(b.Clumpiness, 0f, 1f);

    private static TreeChoice PickTree(List<TreeChoice> choices, float total, float roll)
    {
        foreach (var c in choices)
        {
            roll -= MathF.Max(c.Weight, 0f);
            if (roll <= 0f) return c;
        }
        return choices[^1];
    }

    private static Kind ClassifyUndergrowth(string model)
        => model.StartsWith("Bush") || model.StartsWith("Fern") || model.StartsWith("Mushroom") || model.StartsWith("Rock")
            ? Kind.Bush
            : Kind.Tuft;

    /// <summary>Kit meshes are not all to the same scale: the fern is 9 m wide and the flower groups 2 m tall.</summary>
    private static float BaseScale(string model)
    {
        if (model.StartsWith("Fern")) return 0.3f;
        if (model.StartsWith("Flower")) return 0.45f;
        if (model.StartsWith("Clover")) return 0.7f;
        if (model.StartsWith("Grass_Common")) return 0.5f;
        if (model.StartsWith("Grass_Wispy")) return 0.65f;
        if (model.StartsWith("Mushroom")) return 1.1f;
        if (model.StartsWith("Bush")) return 1.15f;
        return 1f;
    }

    // ---- helpers ------------------------------------------------------------------------------------

    private static Transform3D Upright(float x, float y, float z, float yaw, float scale)
        => new(Basis.Identity.Rotated(Vector3.Up, yaw).Scaled(Vector3.One * scale), new Vector3(x, y, z));

    private static Basis TiltTowards(Basis b, Vector3 normal, float amount)
    {
        var axis = Vector3.Up.Cross(normal);
        if (axis.LengthSquared() < 1e-6f) return b;
        float angle = Vector3.Up.AngleTo(normal) * amount;
        return Basis.Identity.Rotated(axis.Normalized(), angle) * b;
    }

    private static GroupKey Key(string model, Color tint, Kind kind, float x, float z)
    {
        float cs = kind is Kind.Tree or Kind.Rock ? ChunkSize : RangedChunkSize;
        return new(model, tint, kind, (int)MathF.Floor(x / cs), (int)MathF.Floor(z / cs));
    }

    private static void Add(Dictionary<GroupKey, List<Instance>> groups, GroupKey key, Instance inst)
    {
        if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<Instance>();
        list.Add(inst);
    }

    private static void Shuffle<T>(List<T> list, RandomNumberGenerator rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
