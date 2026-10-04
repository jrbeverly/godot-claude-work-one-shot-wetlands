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
        var abs = ProjectSettings.GlobalizePath(resPath);
        var text = File.ReadAllText(abs);
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
}

public sealed class FogSpec
{
    public float StartM { get; set; } = 60f;
    public float EndM { get; set; } = 700f;
    public float Density { get; set; } = 0.0025f;
    public bool HeightFogEnabled { get; set; }
    public float HeightFogLevel { get; set; } = 0f;
}

public sealed class TerrainSpec
{
    public float BaseHeight { get; set; } = 4f;
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
    public string? LeafTint { get; set; }
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
