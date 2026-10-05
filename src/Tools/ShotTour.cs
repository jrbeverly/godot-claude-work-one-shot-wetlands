using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using GodotWarcraft.World;

namespace GodotWarcraft.Tools;

/// <summary>
/// Headless-friendly screenshot tour. Run the project with user args after "--":
///   --shots            capture every shot listed in the world layout
///   --shots=a,b        capture only the named shots
///   --shot-dir=PATH    output folder (default: screenshots/ in the project)
///   --settle=N         frames to wait per shot before capture (default 8)
///   --nofog / --noshadow / --debug-draw=unshaded|wireframe|overdraw   rendering diagnostics
/// Each shot is written as PNG, then the game quits. Without --shots this node does nothing.
/// </summary>
public partial class ShotTour : Node
{
    [Export] public NodePath? WorldPath { get; set; }

    private List<ShotSpec> _shots = new();
    private string _dir = "";
    private int _settle = 8;
    private Camera3D? _cam;
    private WorldRoot? _world;

    public static bool Requested()
        => OS.GetCmdlineUserArgs().Any(a => a == "--shots" || a.StartsWith("--shots="));

    public override void _Ready()
    {
        if (!Requested()) { SetProcess(false); return; }

        var args = OS.GetCmdlineUserArgs();
        string? only = null;
        _dir = ProjectSettings.GlobalizePath("res://screenshots");
        foreach (var a in args)
        {
            if (a.StartsWith("--shots=")) only = a["--shots=".Length..];
            else if (a.StartsWith("--shot-dir=")) _dir = a["--shot-dir=".Length..];
            else if (a.StartsWith("--settle=")) _settle = int.Parse(a["--settle=".Length..]);
        }
        Directory.CreateDirectory(_dir);

        _world = GetNodeOrNull<WorldRoot>(WorldPath ?? "../World");
        var all = _world?.Layout.Shots ?? new List<ShotSpec>();
        if (all.Count == 0)
            all.Add(new ShotSpec { Name = "default", From = new[] { 0f, 12f, 30f }, LookAt = new[] { 0f, 0f, 0f } });
        _shots = only == null ? all : all.Where(s => only.Split(',').Contains(s.Name)).ToList();
        if (_shots.Count == 0)
        {
            GD.PrintErr($"ShotTour: no shots matched '{only}'. Available: {string.Join(", ", all.Select(s => s.Name))}");
            GetTree().Quit(2);
            return;
        }

        _cam = new Camera3D { Name = "ShotCamera", Far = 4000f };
        AddChild(_cam);
        _cam.MakeCurrent();
        CallDeferred(nameof(RunTour));
    }

    private async void RunTour()
    {
        var rs = RenderingServer.Singleton;
        ApplyDiagnostics();
        // Let the world finish building/importing before the first shot.
        for (int i = 0; i < 4; i++) await ToSignal(rs, RenderingServer.SignalName.FramePostDraw);

        foreach (var shot in _shots)
        {
            var from = new Vector3(shot.From[0], shot.From[1], shot.From[2]);
            var at = new Vector3(shot.LookAt[0], shot.LookAt[1], shot.LookAt[2]);
            if (_world != null && shot.FromAboveGround is { } fa) from.Y = _world.GroundHeight(from.X, from.Z) + fa;
            if (_world != null && shot.LookAtAboveGround is { } la) at.Y = _world.GroundHeight(at.X, at.Z) + la;
            _cam!.Fov = shot.FovDeg;
            _cam.GlobalPosition = from;
            if ((at - from).LengthSquared() > 1e-4f) _cam.LookAt(at, Vector3.Up);
            GD.Print($"ShotTour: '{shot.Name}' from {from} ground {( _world?.GroundHeight(from.X, from.Z) ?? float.NaN):F1} -> {at}");
            for (int i = 0; i < _settle; i++) await ToSignal(rs, RenderingServer.SignalName.FramePostDraw);

            var img = GetViewport().GetTexture().GetImage();
            var path = Path.Combine(_dir, SafeName(shot.Name) + ".png");
            var err = img.SavePng(path);
            GD.Print(err == Error.Ok ? $"ShotTour: wrote {path}" : $"ShotTour: FAILED {path} ({err})");
        }
        GetTree().Quit();
    }

    private void ApplyDiagnostics()
    {
        var args = OS.GetCmdlineUserArgs();
        var root = GetTree().CurrentScene;
        if (args.Contains("--nofog"))
            foreach (var we in root.FindChildren("*", "WorldEnvironment", true, false))
                if (we is WorldEnvironment w && w.Environment != null) { w.Environment.FogEnabled = false; GD.Print("ShotTour: fog disabled"); }
        if (args.Contains("--noshadow"))
            foreach (var l in root.FindChildren("*", "DirectionalLight3D", true, false))
                if (l is DirectionalLight3D d) { d.ShadowEnabled = false; GD.Print("ShotTour: shadows disabled"); }
        foreach (var a in args)
            if (a.StartsWith("--debug-draw="))
            {
                GetViewport().DebugDraw = a["--debug-draw=".Length..] switch
                {
                    "unshaded" => Viewport.DebugDrawEnum.Unshaded,
                    "wireframe" => Viewport.DebugDrawEnum.Wireframe,
                    "overdraw" => Viewport.DebugDrawEnum.Overdraw,
                    "lighting" => Viewport.DebugDrawEnum.Lighting,
                    _ => Viewport.DebugDrawEnum.Disabled,
                };
                GD.Print($"ShotTour: debug draw {GetViewport().DebugDraw}");
            }
    }

    private static string SafeName(string s)
        => string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
}
