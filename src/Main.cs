using System.IO;
using System.Linq;
using Godot;
using GodotWarcraft.Player;
using GodotWarcraft.Tools;
using GodotWarcraft.World;

namespace GodotWarcraft;

/// <summary>
/// Scene entry point: sets up the sky, sun and fog around the generated world and drops the wanderer at the
/// layout's spawn point. During a screenshot tour (<c>--shots</c>) the ShotTour camera stays current and no
/// wanderer is spawned. <c>--walk-test</c> is a headless check: it lets the orbit camera run for ~60 frames,
/// saves <c>screenshots/wanderer.png</c> (plus walking and three-quarter frames) and quits.
/// </summary>
public partial class Main : Node3D
{
    private const string WalkTestArg = "--walk-test";

    public override void _Ready()
    {
        GD.Print($"GodotWarcraft: Main ready (Godot {Engine.GetVersionInfo()["string"]})");
        var world = GetNode<WorldRoot>("World");
        Atmosphere.Apply(this, world.Layout);

        if (ShotTour.Requested()) return;
        SpawnWanderer(world);
        if (OS.GetCmdlineUserArgs().Contains(WalkTestArg)) CallDeferred(nameof(RunWalkTest));
    }

    private void SpawnWanderer(WorldRoot world)
    {
        var spawn = world.Layout.Spawn;
        var wanderer = GD.Load<PackedScene>("res://scenes/Wanderer.tscn").Instantiate<Wanderer>();
        // Start a little above the terrain and let gravity + floor snapping settle the capsule on the collider.
        wanderer.Position = new Vector3(spawn.X, world.GroundHeight(spawn.X, spawn.Z) + 0.5f, spawn.Z);
        AddChild(wanderer);
        wanderer.SetHeadingDeg(spawn.YawDeg);
        GD.Print($"Wanderer: spawned at {wanderer.Position} facing {spawn.YawDeg} deg");
    }

    private async void RunWalkTest()
    {
        var rs = RenderingServer.Singleton;
        var dir = ProjectSettings.GlobalizePath("res://screenshots");
        Directory.CreateDirectory(dir);
        var wanderer = GetNode<Wanderer>("Wanderer");
        var world = GetNode<WorldRoot>("World");

        for (int i = 0; i < 60; i++) await ToSignal(rs, RenderingServer.SignalName.FramePostDraw);
        Report("standing");
        Save("wanderer.png");

        // Walk forward for a moment so the body turning / floor following can be checked too.
        Input.ActionPress(Wanderer.ActForward);
        for (int i = 0; i < 40; i++) await ToSignal(rs, RenderingServer.SignalName.FramePostDraw);
        Input.ActionRelease(Wanderer.ActForward);
        Report("after walking");
        Save("wanderer_walk.png");

        // Orbit (left-drag equivalent) to a three-quarter front view: the body must not turn with the camera.
        wanderer.GetNode<OrbitCamera>("CameraPivot").Yaw += Mathf.DegToRad(135f);
        for (int i = 0; i < 12; i++) await ToSignal(rs, RenderingServer.SignalName.FramePostDraw);
        Save("wanderer_side.png");
        GetTree().Quit();

        void Report(string when)
        {
            var p = wanderer.GlobalPosition;
            GD.Print($"WalkTest: {when} at {p}, ground {world.GroundHeight(p.X, p.Z):F2}, onFloor {wanderer.IsOnFloor()}");
        }

        void Save(string name)
        {
            var path = Path.Combine(dir, name);
            GetViewport().GetTexture().GetImage().SavePng(path);
            GD.Print($"WalkTest: saved {path}");
        }
    }
}
