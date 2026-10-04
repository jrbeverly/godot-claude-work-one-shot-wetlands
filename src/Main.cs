using Godot;

namespace GodotWarcraft;

/// <summary>Bootstrap entry point: proves the C# assembly loads and the scene runs.</summary>
public partial class Main : Node3D
{
    public override void _Ready()
    {
        GD.Print($"GodotWarcraft: Main ready (Godot {Engine.GetVersionInfo()["string"]})");
    }
}
