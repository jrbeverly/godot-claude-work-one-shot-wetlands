using Godot;

namespace GodotWarcraft.Player;

/// <summary>
/// WoW-style orbit camera rig. This node is the pivot (1.5 m above the character's feet); a SpringArm3D child
/// holds the Camera3D and pulls it in when terrain or buildings get in the way.
/// Yaw/pitch are the pivot's rotation, so the camera forward is simply the pivot's -Z.
///   right-drag  orbit and steer (the wanderer reads <see cref="RightDragging"/> to turn its body)
///   left-drag   orbit only
///   wheel       zoom 2-14 m
/// </summary>
public partial class OrbitCamera : Node3D
{
    [Export] public float MinDistance { get; set; } = 2f;
    [Export] public float MaxDistance { get; set; } = 14f;
    [Export] public float ZoomStep { get; set; } = 1f;
    /// <summary>Radians per pixel of mouse travel.</summary>
    [Export] public float Sensitivity { get; set; } = 0.0045f;
    [Export] public float MinPitchDeg { get; set; } = -80f;
    [Export] public float MaxPitchDeg { get; set; } = 25f;

    public bool RightDragging { get; private set; }
    public bool LeftDragging { get; private set; }

    /// <summary>Rotation around +Y in radians; forward = (-sin yaw, 0, -cos yaw).</summary>
    public float Yaw { get; set; }
    /// <summary>Negative = camera above looking down.</summary>
    public float Pitch { get; set; } = Mathf.DegToRad(-14f);

    public Vector3 FlatForward => new(-Mathf.Sin(Yaw), 0f, -Mathf.Cos(Yaw));

    private SpringArm3D _arm = null!;
    private Camera3D _cam = null!;
    private Vector2 _dragStart;
    private bool _enabled = true;

    public Camera3D Camera => _cam;

    public override void _Ready()
    {
        _arm = GetNode<SpringArm3D>("SpringArm3D");
        _cam = _arm.GetNode<Camera3D>("Camera3D");
        _arm.SpringLength = Mathf.Clamp(_arm.SpringLength, MinDistance, MaxDistance);
        ApplyRotation();
    }

    /// <summary>Called by the wanderer once both exist: the arm must ignore the character's own capsule.</summary>
    public void Setup(CharacterBody3D owner)
    {
        _arm.AddExcludedObject(owner.GetRid());
        _cam.MakeCurrent();
    }

    /// <summary>Switching to/from the free camera: stop reacting to the mouse while it is away.</summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled) EndDrag();
        else _cam.MakeCurrent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_enabled) return;
        switch (@event)
        {
            case InputEventMouseButton mb when mb.ButtonIndex is MouseButton.Left or MouseButton.Right:
                if (mb.Pressed) BeginDrag(mb.ButtonIndex == MouseButton.Right, mb.Position);
                else if ((mb.ButtonIndex == MouseButton.Right) == RightDragging) EndDrag();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
                _arm.SpringLength = Mathf.Clamp(_arm.SpringLength - ZoomStep, MinDistance, MaxDistance);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
                _arm.SpringLength = Mathf.Clamp(_arm.SpringLength + ZoomStep, MinDistance, MaxDistance);
                break;
            case InputEventMouseMotion mm when RightDragging || LeftDragging:
                Yaw -= mm.Relative.X * Sensitivity;
                Pitch = Mathf.Clamp(Pitch - mm.Relative.Y * Sensitivity, Mathf.DegToRad(MinPitchDeg), Mathf.DegToRad(MaxPitchDeg));
                break;
        }
    }

    public override void _Process(double delta) => ApplyRotation();

    private void ApplyRotation()
    {
        Yaw = Mathf.Wrap(Yaw, -Mathf.Pi, Mathf.Pi);
        Rotation = new Vector3(Pitch, Yaw, 0f);
    }

    private void BeginDrag(bool right, Vector2 at)
    {
        if (RightDragging || LeftDragging) return;
        RightDragging = right;
        LeftDragging = !right;
        _dragStart = at;
        // Hide and capture the pointer while dragging (relative motion keeps flowing), like WoW's camera drag.
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void EndDrag()
    {
        if (!(RightDragging || LeftDragging)) return;
        RightDragging = LeftDragging = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Input.WarpMouse(_dragStart);
    }
}
