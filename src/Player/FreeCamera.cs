using Godot;

namespace GodotWarcraft.Player;

/// <summary>
/// Fly camera for exploring and framing screenshots. Toggled with F from the wanderer; it starts where the
/// orbit camera was so the switch is seamless. WASD moves along the view direction, Q/E down/up, the wheel
/// scales the speed, Shift is 4x, Escape releases the mouse and a left-click captures it again.
/// </summary>
public partial class FreeCamera : Camera3D
{
    [Export] public float Speed { get; set; } = 12f;
    [Export] public float Sensitivity { get; set; } = 0.0035f;

    public bool Active { get; private set; }

    private OrbitCamera _orbit = null!;
    private float _yaw, _pitch;

    public void Setup(OrbitCamera orbit) => _orbit = orbit;

    public void Activate()
    {
        Active = true;
        GlobalTransform = _orbit.Camera.GlobalTransform;
        var e = GlobalTransform.Basis.GetEuler();
        _yaw = e.Y; _pitch = e.X;
        _orbit.SetEnabled(false);
        MakeCurrent();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GD.Print($"FreeCamera: on at {GlobalPosition} (Escape releases the mouse, F returns to the wanderer)");
    }

    public void Deactivate()
    {
        Active = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _orbit.SetEnabled(true);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Active) return;
        switch (@event)
        {
            case InputEventMouseMotion mm when Input.MouseMode == Input.MouseModeEnum.Captured:
                _yaw -= mm.Relative.X * Sensitivity;
                _pitch = Mathf.Clamp(_pitch - mm.Relative.Y * Sensitivity, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
                Rotation = new Vector3(_pitch, _yaw, 0f);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
                Speed = Mathf.Min(Speed * 1.25f, 400f);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
                Speed = Mathf.Max(Speed / 1.25f, 0.5f);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }:
                Input.MouseMode = Input.MouseModeEnum.Captured;
                break;
            case InputEvent ev when ev.IsActionPressed(Wanderer.ActRelease):
                Input.MouseMode = Input.MouseModeEnum.Visible;
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (!Active) return;
        var input = Input.GetVector(Wanderer.ActLeft, Wanderer.ActRight, Wanderer.ActForward, Wanderer.ActBack);
        float lift = Input.GetAxis(Wanderer.ActDown, Wanderer.ActUp);
        var b = GlobalTransform.Basis;
        var dir = b.X * input.X + b.Z * input.Y + Vector3.Up * lift;
        if (dir == Vector3.Zero) return;
        float speed = Speed * (Input.IsActionPressed(Wanderer.ActRun) ? 4f : 1f);
        GlobalPosition += dir.Normalized() * speed * (float)delta;
    }
}
