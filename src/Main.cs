using Godot;
using GodotWarcraft.World;

namespace GodotWarcraft;

/// <summary>Scene entry point: sets up the sky, sun and fog around the generated world.</summary>
public partial class Main : Node3D
{
    public override void _Ready()
    {
        GD.Print($"GodotWarcraft: Main ready (Godot {Engine.GetVersionInfo()["string"]})");
        var world = GetNode<WorldRoot>("World");
        Atmosphere.Apply(this, world.Layout);
    }
}
