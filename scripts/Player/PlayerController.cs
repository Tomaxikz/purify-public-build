using Godot;

public partial class PlayerController : CharacterBody3D
{
    [ExportGroup("Movement")]
    [Export] public float WalkSpeed { get; set; } = 4.5f;
    [Export] public float SprintSpeed { get; set; } = 7.25f;
    [Export] public float Acceleration { get; set; } = 18f;
    [Export] public float AirAcceleration { get; set; } = 6f;
    [Export] public float JumpVelocity { get; set; } = 5.2f;
    [Export] public float Gravity { get; set; } = 16f;

    [ExportGroup("Look")]
    [Export] public NodePath CameraPivotPath { get; set; } = new NodePath("CameraPivot");
    [Export(PropertyHint.Range, "0.0005,0.02,0.0005")] public float MouseSensitivity { get; set; } = 0.003f;
    [Export(PropertyHint.Range, "45,89,1")] public float MaxLookAngleDegrees { get; set; } = 84f;
    [Export] public bool CaptureMouseOnReady { get; set; } = true;
    [Export] public bool MovementEnabled { get; set; } = true;
    [Export] public bool LookEnabled { get; set; } = true;

    [ExportGroup("Input Actions")]
    [Export] public string MoveForwardAction { get; set; } = "move_forward";
    [Export] public string MoveBackwardAction { get; set; } = "move_backward";
    [Export] public string MoveLeftAction { get; set; } = "move_left";
    [Export] public string MoveRightAction { get; set; } = "move_right";
    [Export] public string JumpAction { get; set; } = "jump";
    [Export] public string SprintAction { get; set; } = "sprint";

    private Node3D? _cameraPivot;
    private float _pitchRadians;

    public override void _Ready()
    {
        _cameraPivot = GetNodeOrNull<Node3D>(CameraPivotPath);

        if (_cameraPivot != null)
        {
            _pitchRadians = _cameraPivot.Rotation.X;
        }

        if (CaptureMouseOnReady)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (LookEnabled && @event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);

            if (_cameraPivot != null)
            {
                float maxLookRadians = Mathf.DegToRad(MaxLookAngleDegrees);
                _pitchRadians = Mathf.Clamp(_pitchRadians - mouseMotion.Relative.Y * MouseSensitivity, -maxLookRadians, maxLookRadians);
                _cameraPivot.Rotation = new Vector3(_pitchRadians, 0f, 0f);
            }
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!LookEnabled && !MovementEnabled)
        {
            return;
        }

        if (@event is not InputEventMouseButton mouseButton
            || !mouseButton.Pressed
            || Input.MouseMode == Input.MouseModeEnum.Captured
            || IsPointerOverUi())
        {
            return;
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        float frameDelta = (float)delta;
        if (!MovementEnabled)
        {
            Velocity = Vector3.Zero;
            MoveAndSlide();
            return;
        }

        Vector2 moveInput = GetMoveInput();

        Vector3 direction = Vector3.Zero;
        if (moveInput.LengthSquared() > 0f)
        {
            moveInput = moveInput.Normalized();
            direction = (GlobalTransform.Basis.X * moveInput.X + GlobalTransform.Basis.Z * moveInput.Y).Normalized();
        }

        bool sprinting = IsPressed(SprintAction, Key.Shift);
        float targetSpeed = sprinting ? SprintSpeed : WalkSpeed;
        float moveAcceleration = IsOnFloor() ? Acceleration : AirAcceleration;

        Vector3 velocity = Velocity;
        Vector3 targetVelocity = direction * targetSpeed;
        velocity.X = Mathf.MoveToward(velocity.X, targetVelocity.X, moveAcceleration * frameDelta);
        velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocity.Z, moveAcceleration * frameDelta);

        if (IsOnFloor())
        {
            if (IsJustPressed(JumpAction, Key.Space))
            {
                velocity.Y = JumpVelocity;
            }
            else if (velocity.Y < 0f)
            {
                velocity.Y = -0.1f;
            }
        }
        else
        {
            velocity.Y -= Gravity * frameDelta;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private Vector2 GetMoveInput()
    {
        float x = GetStrength(MoveRightAction, Key.D) - GetStrength(MoveLeftAction, Key.A);
        float y = GetStrength(MoveBackwardAction, Key.S) - GetStrength(MoveForwardAction, Key.W);
        return new Vector2(x, y);
    }

    private static float GetStrength(string actionName, Key fallbackKey)
    {
        return InputMap.HasAction(actionName)
            ? Input.GetActionStrength(actionName)
            : (Input.IsKeyPressed(fallbackKey) ? 1f : 0f);
    }

    private static bool IsPressed(string actionName, Key fallbackKey)
    {
        return InputMap.HasAction(actionName)
            ? Input.IsActionPressed(actionName)
            : Input.IsKeyPressed(fallbackKey);
    }

    private static bool IsJustPressed(string actionName, Key fallbackKey)
    {
        return InputMap.HasAction(actionName)
            ? Input.IsActionJustPressed(actionName)
            : Input.IsKeyPressed(fallbackKey);
    }

    public void SetControlEnabled(bool enabled)
    {
        MovementEnabled = enabled;
        LookEnabled = enabled;
    }

    private static bool IsPointerOverUi()
    {
        Viewport? viewport = Engine.GetMainLoop() is SceneTree tree ? tree.Root.GetViewport() : null;
        return viewport?.GuiGetHoveredControl() != null;
    }
}
