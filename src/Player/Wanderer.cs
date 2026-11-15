using System;
using Godot;
using GodotWarcraft.World;

namespace GodotWarcraft.Player;

/// <summary>
/// Third-person wanderer: a 1.8 m human-scale figure used to walk the zone and to give scale.
///
/// Controls (WoW-style):
///   W / S          walk forward / back along the camera's (flattened) forward
///   A / D          strafe left / right
///   Q / E          turn the character (and camera) left / right
///   Shift          run (7.5 m/s instead of 4.5 m/s)
///   Right-drag     orbit the camera AND turn the character to face it (steering)
///   Left-drag      orbit the camera only; the body keeps its facing until you move
///   Mouse wheel    zoom the camera 2-14 m (default 5 m)
///   F              toggle the free fly camera (WASD + mouse look, Q/E down/up, wheel = speed, Shift = fast)
///   Escape         release the mouse (free camera); left-click recaptures it
///
/// Input actions are registered at runtime (see <see cref="EnsureInputActions"/>) so project.godot stays untouched.
/// The capsule stands on the terrain's HeightMapShape3D collider ("TerrainBody"); if it ever ends up under the
/// ground it is lifted back to <see cref="WorldRoot.GroundHeight"/> + 1.
/// </summary>
public partial class Wanderer : CharacterBody3D
{
    public const string ActForward = "wanderer_forward";
    public const string ActBack = "wanderer_back";
    public const string ActLeft = "wanderer_left";
    public const string ActRight = "wanderer_right";
    public const string ActTurnLeft = "wanderer_turn_left";
    public const string ActTurnRight = "wanderer_turn_right";
    public const string ActRun = "wanderer_run";
    public const string ActDown = "wanderer_down";
    public const string ActUp = "wanderer_up";
    public const string ActFreeCam = "wanderer_free_cam";
    public const string ActRelease = "wanderer_release";

    [Export] public float WalkSpeed { get; set; } = 4.5f;
    [Export] public float RunSpeed { get; set; } = 7.5f;
    /// <summary>Keyboard turn rate, degrees per second (Q/E).</summary>
    [Export] public float TurnDegPerSec { get; set; } = 150f;

    private OrbitCamera _orbit = null!;
    private FreeCamera _free = null!;
    private Node3D _body = null!;
    private WorldRoot? _world;
    private float _gravity = 9.8f;
    private float _bodyYaw;

    public override void _Ready()
    {
        EnsureInputActions();
        _orbit = GetNode<OrbitCamera>("CameraPivot");
        _free = GetNode<FreeCamera>("FreeCamera");
        _body = GetNode<Node3D>("Body");
        _world = GetTree().CurrentScene?.GetNodeOrNull<WorldRoot>("World");
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity", 9.8f).AsSingle();
        _bodyYaw = _orbit.Yaw;
        _body.Rotation = new Vector3(0, _bodyYaw, 0);
        _orbit.Setup(this);
        _free.Setup(_orbit);
    }

    /// <summary>Face a compass bearing: 0 = north (-Z), 90 = east (+X), clockwise seen from above.</summary>
    public void SetHeadingDeg(float bearingDeg)
    {
        // Godot's forward is -Z; rotating -Z around +Y by -bearing yields (sin b, 0, -cos b) = the compass direction.
        float yaw = Mathf.DegToRad(-bearingDeg);
        _orbit.Yaw = yaw;
        _bodyYaw = yaw;
        _body.Rotation = new Vector3(0, yaw, 0);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(ActFreeCam))
        {
            if (_free.Active) _free.Deactivate(); else _free.Activate();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        var vel = Velocity;
        if (!IsOnFloor()) vel.Y -= _gravity * dt;

        Vector3 wish = Vector3.Zero;
        if (!_free.Active)
        {
            // Q/E turn the camera heading; the body follows because forward is the camera forward.
            float turn = Input.GetAxis(ActTurnRight, ActTurnLeft);
            if (turn != 0f)
            {
                _orbit.Yaw += Mathf.DegToRad(TurnDegPerSec) * turn * dt;
                _bodyYaw = _orbit.Yaw;
            }
            if (_orbit.RightDragging) _bodyYaw = _orbit.Yaw;

            var input = Input.GetVector(ActLeft, ActRight, ActForward, ActBack);
            if (input != Vector2.Zero)
            {
                var fwd = _orbit.FlatForward;
                var right = new Vector3(-fwd.Z, 0, fwd.X);
                wish = (fwd * -input.Y + right * input.X).Normalized();
                // The visible body faces where it walks; forward = (-sin yaw, 0, -cos yaw).
                _bodyYaw = Mathf.Atan2(-wish.X, -wish.Z);
            }
        }

        float speed = Input.IsActionPressed(ActRun) ? RunSpeed : WalkSpeed;
        var target = wish * speed;
        // Snappy, WoW-like acceleration; a little smoothing keeps the capsule from popping on direction changes.
        float accel = wish == Vector3.Zero ? 40f : 25f;
        vel.X = Mathf.MoveToward(vel.X, target.X, accel * dt);
        vel.Z = Mathf.MoveToward(vel.Z, target.Z, accel * dt);
        Velocity = vel;
        MoveAndSlide();

        float k = 1f - MathF.Exp(-14f * dt);
        _body.Rotation = new Vector3(0, Mathf.LerpAngle(_body.Rotation.Y, _bodyYaw, k), 0);

        // Safety net: a spawn below the collider, or a fall through a seam, puts us back on the ground.
        if (_world != null)
        {
            var p = GlobalPosition;
            float ground = _world.GroundHeight(p.X, p.Z);
            if (p.Y < ground - 0.5f)
            {
                GlobalPosition = new Vector3(p.X, ground + 1f, p.Z);
                Velocity = Vector3.Zero;
            }
        }
    }

    /// <summary>Adds the wanderer's input actions to the InputMap (idempotent) so project.godot stays untouched.</summary>
    public static void EnsureInputActions()
    {
        Key(ActForward, Godot.Key.W);
        Key(ActBack, Godot.Key.S);
        Key(ActLeft, Godot.Key.A);
        Key(ActRight, Godot.Key.D);
        Key(ActTurnLeft, Godot.Key.Q);
        Key(ActTurnRight, Godot.Key.E);
        Key(ActDown, Godot.Key.Q);
        Key(ActUp, Godot.Key.E);
        Key(ActRun, Godot.Key.Shift);
        Key(ActFreeCam, Godot.Key.F);
        Key(ActRelease, Godot.Key.Escape);

        static void Key(string action, Key key)
        {
            if (InputMap.HasAction(action)) return;
            InputMap.AddAction(action);
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
    }
}
