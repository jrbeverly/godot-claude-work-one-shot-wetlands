using System;
using System.Diagnostics;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Root of the generated zone. Loads <c>assets/world/layout.json</c>, builds the terrain and
/// (as systems come online) water, foliage, settlements and the atmosphere. Other nodes read
/// <see cref="Layout"/> and <see cref="Field"/> from here.
/// </summary>
public partial class WorldRoot : Node3D
{
    [Export] public string LayoutPath { get; set; } = "res://assets/world/layout.json";
    [Export] public int Seed { get; set; } = 1337;

    public WorldLayout Layout { get; private set; } = new();
    public HeightField Field { get; private set; } = null!;
    public TerrainBuilder.Result Terrain { get; private set; } = null!;

    public override void _Ready()
    {
        var sw = Stopwatch.StartNew();
        // Allow `godot --path . -- --layout=res://assets/world/some_other_layout.json` for subsystem test scenes.
        string? overlay = null;
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            if (a.StartsWith("--layout=")) LayoutPath = a["--layout=".Length..];
            // A mood overlay (e.g. --palette=res://assets/world/palette_night.json) merges its palette/sun/fog over the layout.
            if (a.StartsWith("--palette=")) overlay = a["--palette=".Length..];
        }
        Layout = WorldLayout.Load(LayoutPath);
        if (overlay != null) { Layout.ApplyOverlay(overlay); GD.Print($"World: palette overlay {overlay}"); }
        Terrain = TerrainBuilder.Build(Layout, Seed);
        Field = Terrain.Field;
        GD.Print($"World '{Layout.Name}': heightfield {Field.Size}x{Field.Size} built in {sw.ElapsedMilliseconds} ms");

        var mat = GD.Load<ShaderMaterial>("res://assets/materials/terrain.tres");
        // Snow starts about two thirds of the way up the rim; the palette may steer layer tints (default white).
        // Snow only on the tallest (north) rim: the other fells read as rock, as the bible wants.
        mat.SetShaderParameter("snow_line", Layout.Terrain.SnowLine ?? Layout.Terrain.BaseHeight + Layout.Terrain.Rim.EdgeHeight * 0.92f);
        mat.SetShaderParameter("snow_blend", Layout.Terrain.SnowBlend ?? MathF.Max(12f, Layout.Terrain.Rim.EdgeHeight * 0.10f));
        foreach (var (key, uniform) in new[] { ("tintGrass", "tint_grass"), ("tintForest", "tint_forest"), ("tintRoad", "tint_road"), ("tintRock", "tint_rock"), ("tintSnow", "tint_snow"), ("tintMeadow", "tint_meadow") })
            if (Layout.Palette.TryGetValue(key, out var hex) && !string.IsNullOrWhiteSpace(hex)) mat.SetShaderParameter(uniform, new Color(hex));
        AddChild(TerrainMesh.Build(Field, mat));
        GD.Print($"World: terrain meshes in {sw.ElapsedMilliseconds} ms");

        WaterBuilder.Build(this);
        SiteBuilder.Build(this);
        FoliagePlacer.Build(this);
        GD.Print($"World: complete in {sw.ElapsedMilliseconds} ms");
    }

    /// <summary>Ground height at a world xz position.</summary>
    public float GroundHeight(float x, float z) => Field.SampleHeight(x, z);
}
