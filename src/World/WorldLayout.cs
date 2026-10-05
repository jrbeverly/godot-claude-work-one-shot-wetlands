using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Authored description of the zone, loaded from <c>assets/world/layout.json</c>.
/// Coordinates are metres from the map centre: X east, Z south (Godot -Z = north), Y = height.
/// Everything in the world (terrain, roads, rivers, biomes, settlements, hero shots) is generated from this.
/// </summary>
public sealed class WorldLayout
{
    public string Name { get; set; } = "Unnamed Vale";
    public float MapSize { get; set; } = 1024f;
    public float CellSize { get; set; } = 2f;
    public SunSpec Sun { get; set; } = new();
    public Dictionary<string, string> Palette { get; set; } = new();
    public FogSpec Fog { get; set; } = new();
    public TerrainSpec Terrain { get; set; } = new();
    public LakeSpec? Lake { get; set; }
    public MarshSpec? Marsh { get; set; }
    public List<RiverSpec> Rivers { get; set; } = new();
    public List<RoadSpec> Roads { get; set; } = new();
    public List<BiomeSpec> Biomes { get; set; } = new();
    public List<SiteSpec> Sites { get; set; } = new();
    public SpawnSpec Spawn { get; set; } = new();
    public List<ShotSpec> Shots { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static WorldLayout Load(string resPath = "res://assets/world/layout.json")
    {
        // Godot's FileAccess reads res:// from the exported PCK as well as from disk; System.IO cannot.
        using var file = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read)
                         ?? throw new FileNotFoundException($"Could not open {resPath}: {Godot.FileAccess.GetOpenError()}");
        var text = file.GetAsText();
        return JsonSerializer.Deserialize<WorldLayout>(text, JsonOptions)
               ?? throw new InvalidDataException($"Could not parse {resPath}");
    }

    /// <summary>Palette colour by role, with a fallback.</summary>
    public Color Col(string key, string fallbackHex)
        => Palette.TryGetValue(key, out var hex) && !string.IsNullOrWhiteSpace(hex) ? new Color(hex) : new Color(fallbackHex);
}

public sealed class SunSpec
{
    /// <summary>0 = light travelling from the north, 90 = from the east, clockwise.</summary>
    public float AzimuthDeg { get; set; } = 220f;
    public float ElevationDeg { get; set; } = 35f;
    public string Color { get; set; } = "#FFE9C4";
    public float Energy { get; set; } = 1.2f;
    public float ShadowOpacity { get; set; } = 0.6f;
}

/// <summary>Fog plus the handful of mood knobs that belong with it (ambient strength, grading, cloud cover, stars).</summary>
public sealed class FogSpec
{
    public float StartM { get; set; } = 60f;
    public float EndM { get; set; } = 700f;
    /// <summary>Depth-fog opacity ceiling (0..1): below 1 the far rim keeps a little of its own shading.</summary>
    public float Density { get; set; } = 1.0f;
    /// <summary>Depth-fog curve; below 1 the fog arrives early and saturates late (the references' 150-300 m band).</summary>
    public float Curve { get; set; } = 1.0f;
    public bool HeightFogEnabled { get; set; }
    public float HeightFogLevel { get; set; } = 0f;
    public float HeightFogDensity { get; set; } = 0.0045f;
    public float AmbientEnergy { get; set; } = 1.2f;
    public float Saturation { get; set; } = 1.0f;
    public float Contrast { get; set; } = 0.98f;
    /// <summary>0..1 opacity of the painted cloud banks (0 = clear sky).</summary>
    public float CloudOpacity { get; set; } = 0.92f;
    /// <summary>Cloud noise threshold: higher = fewer clouds (0.56 = the original scattered banks).</summary>
    public float CloudCoverage { get; set; } = 0.56f;
    public float HorizonWarmth { get; set; } = 0.045f;
    /// <summary>Sky gradient curve; >1 keeps more of the sky in the horizon tone.</summary>
    public float SkyCurve { get; set; } = 1.0f;
    /// <summary>0..1 strength of a painted star field (night skies).</summary>
    public float Stars { get; set; } = 0f;
    /// <summary>How much the fog blushes toward the sun colour on the sun side (Environment.FogSunScatter).</summary>
    public float SunScatter { get; set; } = 0.04f;
    public float SunDiscDeg { get; set; } = 3.0f;
    public float SunDiscEnergy { get; set; } = 2.4f;
    public float SunGlowPower { get; set; } = 140f;
    public float SunGlowStrength { get; set; } = 0.6f;
    public float SunHaloPower { get; set; } = 24f;
    public float SunHaloStrength { get; set; } = 0.22f;
}

public sealed class TerrainSpec
{
    public float BaseHeight { get; set; } = 4f;
    /// <summary>Absolute snow line in metres; null = two thirds up the rim (the old default). Set very high for snowless zones.</summary>
    public float? SnowLine { get; set; }
    /// <summary>Width of the snow/pale-wall gradient in metres; null = a tenth of the rim height.</summary>
    public float? SnowBlend { get; set; }
    public RimSpec Rim { get; set; } = new();
    public List<HillSpec> Hills { get; set; } = new();
    public List<BasinSpec> Basins { get; set; } = new();
    public List<RidgeSpec> Ridges { get; set; } = new();
    public List<FlatSpec> Flats { get; set; } = new();
}

public sealed class RimSpec
{
    public float StartRadius { get; set; } = 380f;
    public float EdgeHeight { get; set; } = 180f;
    public float Roughness { get; set; } = 0.3f;
    /// <summary>Exponent on the rim ramp: 1.25 = long easy slope, 2+ = a short apron then near-sheer walls.</summary>
    public float RampPower { get; set; } = 1.25f;
    /// <summary>Optional per-side edge heights (north = -Z, south = +Z, east = +X, west = -X); null = EdgeHeight.</summary>
    public float? EdgeHeightNorth { get; set; }
    public float? EdgeHeightSouth { get; set; }
    public float? EdgeHeightEast { get; set; }
    public float? EdgeHeightWest { get; set; }

    /// <summary>Edge height in the direction of (x, z), blended smoothly around the compass.</summary>
    public float HeightToward(float x, float z)
    {
        float n = EdgeHeightNorth ?? EdgeHeight, s = EdgeHeightSouth ?? EdgeHeight;
        float e = EdgeHeightEast ?? EdgeHeight, w = EdgeHeightWest ?? EdgeHeight;
        float len = MathF.Max(MathF.Sqrt(x * x + z * z), 1e-3f);
        float dx = x / len, dz = z / len;
        float wx = dx * dx, wz = dz * dz;                     // sums to 1
        float ew = dx >= 0 ? e : w, ns = dz >= 0 ? s : n;
        return wx * ew + wz * ns;
    }
}

public sealed class HillSpec
{
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } = 60f;
    public float Height { get; set; } = 20f;
    /// <summary>dome | plateau | cliff</summary>
    public string Shape { get; set; } = "dome";
}

public sealed class BasinSpec
{
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } = 60f;
    public float Depth { get; set; } = 6f;
}

public sealed class RidgeSpec
{
    public string Name { get; set; } = "";
    public List<float[]> Points { get; set; } = new();
    public float Width { get; set; } = 40f;
    public float Height { get; set; } = 30f;
}

public sealed class FlatSpec
{
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } = 30f;
    public float Height { get; set; } = 6f;
}

public sealed class LakeSpec
{
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float RadiusX { get; set; } = 60f;
    public float RadiusZ { get; set; } = 60f;
    public float WaterLevel { get; set; } = 2f;
    public float Depth { get; set; } = 5f;
}

/// <summary>
/// Bog water: one flat sheet at <see cref="WaterLevel"/> covering a disc of the lowland. The ground is sculpted to sit
/// just above it, with authored <see cref="Pools"/> carved below it and a field of low <see cref="Hummocks"/> rising
/// out of it, so pools, channels and islands all come from one plane (the Classic way: water is a height, not a mesh).
/// </summary>
public sealed class MarshSpec
{
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } = 300f;
    public float WaterLevel { get; set; } = 4f;
    /// <summary>Ground inside the disc is pulled toward WaterLevel + this (metres) before pools and hummocks; 0 = leave the authored terrain.</summary>
    public float FloorAbove { get; set; } = 0.9f;
    /// <summary>How strongly the floor is pulled (0..1); the pull fades out toward the disc edge.</summary>
    public float FloorStrength { get; set; } = 0.8f;
    /// <summary>Metres of broad noise added to the flattened floor so the sheet breaks into random shallow pools.</summary>
    public float FloorNoise { get; set; } = 1.4f;
    public List<PoolSpec> Pools { get; set; } = new();
    public HummockSpec Hummocks { get; set; } = new();
}

public sealed class PoolSpec
{
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float RadiusX { get; set; } = 30f;
    public float RadiusZ { get; set; } = 30f;
    /// <summary>Bed depth below the marsh water level at the centre.</summary>
    public float Depth { get; set; } = 1.5f;
}

/// <summary>Rounded earthen mounds scattered over the marsh floor (the dark peat domes of the references).</summary>
public sealed class HummockSpec
{
    public bool Enabled { get; set; } = true;
    /// <summary>Average spacing of mound centres in metres (jittered).</summary>
    public float Spacing { get; set; } = 34f;
    public float RadiusMin { get; set; } = 9f;
    public float RadiusMax { get; set; } = 22f;
    public float HeightMin { get; set; } = 1.2f;
    public float HeightMax { get; set; } = 3.2f;
    /// <summary>Fraction of grid cells that get a mound (0..1).</summary>
    public float Coverage { get; set; } = 0.55f;
}

public sealed class RiverSpec
{
    public string Name { get; set; } = "";
    public List<float[]> Points { get; set; } = new();
    public float Width { get; set; } = 6f;
    public float Depth { get; set; } = 2f;
    public float WaterLevelStart { get; set; } = 10f;
    public float WaterLevelEnd { get; set; } = 2f;
}

public sealed class RoadSpec
{
    public string Name { get; set; } = "";
    public List<float[]> Points { get; set; } = new();
    public float Width { get; set; } = 5f;
    /// <summary>main | path</summary>
    public string Kind { get; set; } = "main";
}

public sealed class BiomeSpec
{
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Radius { get; set; } = 100f;
    public int Priority { get; set; }
    public float TreeDensity { get; set; } = 0.3f;
    public float Clumpiness { get; set; } = 0.6f;
    public List<TreeChoice> Trees { get; set; } = new();
    public List<UndergrowthChoice> Undergrowth { get; set; } = new();
    /// <summary>grass | meadow | forestFloor | rock | snow</summary>
    public string GroundTexture { get; set; } = "grass";
    public string? LeafTint { get; set; }
}

public sealed class TreeChoice
{
    public string Model { get; set; } = "CommonTree_1";
    public float Weight { get; set; } = 1f;
    public float Scale { get; set; } = 1f;
}

public sealed class UndergrowthChoice
{
    public string Model { get; set; } = "Grass_Common_Tall";
    public float Weight { get; set; } = 1f;
    public float Density { get; set; } = 0.2f;
    /// <summary>Per-choice tint; null = the biome's leafTint. On flower sheets a non-white tint repaints the petals.</summary>
    public string? Tint { get; set; }
    public float Scale { get; set; } = 1f;
}

public sealed class SiteSpec
{
    public string Name { get; set; } = "";
    /// <summary>village | farm | ruin | landmark | camp | dock | grove</summary>
    public string Type { get; set; } = "landmark";
    public float X { get; set; }
    public float Z { get; set; }
    public float RotationDeg { get; set; }
    public float ClearRadius { get; set; } = 20f;
    public List<PieceSpec> Pieces { get; set; } = new();
}

/// <summary>One placed thing inside a site. Local x/z are offsets from the site centre before the site rotation.</summary>
public sealed class PieceSpec
{
    /// <summary>cottage | landmark | fence | prop | ruinWall | standingStone | tree | field</summary>
    public string Kind { get; set; } = "prop";
    public string? Model { get; set; }
    public float X { get; set; }
    public float Z { get; set; }
    public float RotationDeg { get; set; }
    public float Scale { get; set; } = 1f;
    public List<float[]>? Points { get; set; }
    public int[]? Footprint { get; set; }
    /// <summary>plaster | timber | stone</summary>
    public string? Style { get; set; }
    public bool Chimney { get; set; } = true;
    public int Storeys { get; set; } = 1;
    public float LengthM { get; set; } = 6f;
    public float HeightM { get; set; } = 3f;
    public float WidthM { get; set; } = 20f;
    public float DepthM { get; set; } = 20f;
    /// <summary>wheat | pumpkin | fallow</summary>
    public string? Crop { get; set; }
    /// <summary>standingStone only: lying on its side.</summary>
    public bool Fallen { get; set; }
    public string? LeafTint { get; set; }
    /// <summary>ruinWall: a standing wall (no gaps, no stumps, no rubble), stacked to HeightM.</summary>
    public bool Intact { get; set; }
    /// <summary>windowRow: number of lit windows; also the brazier count for a row of lights.</summary>
    public int Count { get; set; } = 4;
    public float SpacingM { get; set; } = 2.2f;
    /// <summary>landmark: albedo multiply (e.g. weathered grey for a keep).</summary>
    public string? Tint { get; set; }
}

public sealed class SpawnSpec
{
    public float X { get; set; }
    public float Z { get; set; }
    public float YawDeg { get; set; }
}

public sealed class ShotSpec
{
    public string Name { get; set; } = "shot";
    /// <summary>Camera position [x, y, z]. When <see cref="FromAboveGround"/> is set, y is ignored and replaced by ground height + that offset.</summary>
    public float[] From { get; set; } = { 0, 10, 20 };
    public float[] LookAt { get; set; } = { 0, 0, 0 };
    public float FovDeg { get; set; } = 60f;
    /// <summary>Eye height above the terrain at From.xz (e.g. 1.7 for a walking shot, 40 for a high vista). Null = use From.y as absolute.</summary>
    public float? FromAboveGround { get; set; }
    /// <summary>Height above the terrain at LookAt.xz. Null = use LookAt.y as absolute.</summary>
    public float? LookAtAboveGround { get; set; }
}
