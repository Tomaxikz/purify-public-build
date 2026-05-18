using Godot;

public partial class EntityChaseController : CharacterBody3D
{
    public enum EntityState
    {
        Idle,
        Wander,
        Chase
    }

    [Export] public EntityState StartState { get; set; } = EntityState.Wander;
    [Export] public NodePath TargetPath { get; set; } = new NodePath("../Player");
    [Export] public NodePath NavigationAgentPath { get; set; } = new NodePath("NavigationAgent3D");
    [Export] public NodePath VisualPath { get; set; } = new NodePath("Visual");
    [Export] public NodePath DeathScreenPath { get; set; } = new NodePath("../../PostProcessLayer/DeathScreen");
    [Export] public bool UseNavigationAgent { get; set; } = false;

    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "0,12,0.05")] public float ChaseSpeed { get; set; } = 3.4f;
    [Export(PropertyHint.Range, "0,6,0.05")] public float WanderSpeed { get; set; } = 1.1f;
    [Export(PropertyHint.Range, "0,40,0.5")] public float Acceleration { get; set; } = 14f;
    [Export(PropertyHint.Range, "0,40,0.5")] public float Gravity { get; set; } = 16f;
    [Export(PropertyHint.Range, "0.1,6,0.05")] public float StopDistance { get; set; } = 1.15f;
    [Export(PropertyHint.Range, "0.1,6,0.05")] public float KillDistance { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0.5,10,0.1")] public float TurnResponse { get; set; } = 4.5f;

    [ExportGroup("Senses")]
    [Export(PropertyHint.Range, "0.5,80,0.5")] public float DetectionRange { get; set; } = 17f;
    [Export(PropertyHint.Range, "0.5,100,0.5")] public float LoseInterestRange { get; set; } = 27f;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float PathRefreshSeconds { get; set; } = 0.18f;
    [Export] public bool RequireLineOfSight { get; set; } = false;

    [ExportGroup("Wander")]
    [Export(PropertyHint.Range, "0,30,0.25")] public float WanderRadius { get; set; } = 8f;
    [Export(PropertyHint.Range, "0.2,8,0.1")] public float WanderRetargetSeconds { get; set; } = 2.6f;
    [Export] public bool FaceTargetWhenIdle { get; set; } = true;

    private readonly RandomNumberGenerator _random = new();
    private Node3D? _target;
    private NavigationAgent3D? _navigationAgent;
    private Node3D? _visual;
    private DeathScreenController? _deathScreen;
    private EntityState _state;
    private Vector3 _spawnPosition;
    private Vector3 _wanderTarget;
    private double _nextPathRefreshTime;
    private double _nextWanderRetargetTime;
    private double _elapsedSeconds;

    public EntityState State => _state;
    public bool IsChasing => _state == EntityState.Chase;

    public override void _Ready()
    {
        _random.Randomize();
        _state = StartState;
        _spawnPosition = GlobalPosition;
        _wanderTarget = _spawnPosition;
        ResolveNodes();

        if (_navigationAgent != null)
        {
            _navigationAgent.PathDesiredDistance = 0.5f;
            _navigationAgent.TargetDesiredDistance = StopDistance;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float frameDelta = (float)delta;
        _elapsedSeconds += delta;
        ResolveNodes();
        UpdateState();

        Vector3 desiredVelocity = GetDesiredVelocity();
        Vector3 velocity = Velocity;
        velocity.X = Mathf.MoveToward(velocity.X, desiredVelocity.X, Acceleration * frameDelta);
        velocity.Z = Mathf.MoveToward(velocity.Z, desiredVelocity.Z, Acceleration * frameDelta);

        if (IsOnFloor())
        {
            velocity.Y = Mathf.Min(velocity.Y, -0.1f);
        }
        else
        {
            velocity.Y -= Gravity * frameDelta;
        }

        Velocity = velocity;
        MoveAndSlide();
        UpdateFacing(frameDelta);
    }

    public void ForceChase()
    {
        _state = EntityState.Chase;
        RefreshPathToTarget(true);
    }

    public void SetState(EntityState state)
    {
        _state = state;
    }

    private void ResolveNodes()
    {
        if (_target == null || !GodotObject.IsInstanceValid(_target))
        {
            _target = GetNodeOrNull<Node3D>(TargetPath);
        }

        if (_navigationAgent == null || !GodotObject.IsInstanceValid(_navigationAgent))
        {
            _navigationAgent = GetNodeOrNull<NavigationAgent3D>(NavigationAgentPath);
        }

        if (_visual == null || !GodotObject.IsInstanceValid(_visual))
        {
            _visual = GetNodeOrNull<Node3D>(VisualPath);
        }

        if (_deathScreen == null || !GodotObject.IsInstanceValid(_deathScreen))
        {
            _deathScreen = GetNodeOrNull<DeathScreenController>(DeathScreenPath);
        }
    }

    private void UpdateState()
    {
        if (_target == null)
        {
            _state = StartState == EntityState.Chase ? EntityState.Wander : StartState;
            return;
        }

        float distanceToTarget = FlatDistance(GlobalPosition, _target.GlobalPosition);

        if (_state == EntityState.Chase)
        {
            if (distanceToTarget <= KillDistance)
            {
                _deathScreen?.TriggerDeath();
                _state = EntityState.Idle;
                Velocity = Vector3.Zero;
                return;
            }

            if (distanceToTarget > LoseInterestRange)
            {
                _state = EntityState.Wander;
                PickWanderTarget();
            }

            return;
        }

        if (distanceToTarget <= DetectionRange && CanSeeTarget())
        {
            _state = EntityState.Chase;
            RefreshPathToTarget(true);
        }
    }

    private Vector3 GetDesiredVelocity()
    {
        return _state switch
        {
            EntityState.Chase => GetChaseVelocity(),
            EntityState.Wander => GetWanderVelocity(),
            _ => Vector3.Zero
        };
    }

    private Vector3 GetChaseVelocity()
    {
        if (_target == null)
        {
            return Vector3.Zero;
        }

        float distanceToTarget = FlatDistance(GlobalPosition, _target.GlobalPosition);
        if (distanceToTarget <= StopDistance)
        {
            return Vector3.Zero;
        }

        Vector3 nextPosition = _target.GlobalPosition;

        if (UseNavigationAgent)
        {
            RefreshPathToTarget(false);
            nextPosition = GetNextPathPosition(_target.GlobalPosition);
        }

        Vector3 direction = GetFlatDirection(nextPosition);

        if (direction.LengthSquared() < 0.01f)
        {
            direction = GetFlatDirection(_target.GlobalPosition);
        }

        return direction * ChaseSpeed;
    }

    private Vector3 GetWanderVelocity()
    {
        if (_elapsedSeconds >= _nextWanderRetargetTime || FlatDistance(GlobalPosition, _wanderTarget) <= 0.75f)
        {
            PickWanderTarget();
        }

        Vector3 direction = GetFlatDirection(_wanderTarget);
        return direction * WanderSpeed;
    }

    private void RefreshPathToTarget(bool force)
    {
        if (_target == null || _navigationAgent == null)
        {
            return;
        }

        if (!force && _elapsedSeconds < _nextPathRefreshTime)
        {
            return;
        }

        _nextPathRefreshTime = _elapsedSeconds + Mathf.Max(PathRefreshSeconds, 0.05f);
        _navigationAgent.TargetPosition = _target.GlobalPosition;
    }

    private Vector3 GetNextPathPosition(Vector3 fallbackPosition)
    {
        if (_navigationAgent == null || _navigationAgent.IsNavigationFinished())
        {
            return fallbackPosition;
        }

        Vector3 nextPosition = _navigationAgent.GetNextPathPosition();
        return FlatDistance(GlobalPosition, nextPosition) > 0.1f ? nextPosition : fallbackPosition;
    }

    private void PickWanderTarget()
    {
        _nextWanderRetargetTime = _elapsedSeconds + WanderRetargetSeconds;

        if (WanderRadius <= 0.05f)
        {
            _wanderTarget = _spawnPosition;
            return;
        }

        float angle = _random.RandfRange(0f, Mathf.Tau);
        float radius = _random.RandfRange(WanderRadius * 0.25f, WanderRadius);
        _wanderTarget = _spawnPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }

    private void UpdateFacing(float frameDelta)
    {
        Vector3 faceDirection = new Vector3(Velocity.X, 0f, Velocity.Z);

        if (faceDirection.LengthSquared() < 0.02f && FaceTargetWhenIdle && _target != null)
        {
            faceDirection = _target.GlobalPosition - GlobalPosition;
            faceDirection.Y = 0f;
        }

        if (faceDirection.LengthSquared() < 0.02f)
        {
            return;
        }

        float targetYaw = Mathf.Atan2(faceDirection.X, faceDirection.Z);
        Rotation = new Vector3(Rotation.X, Mathf.LerpAngle(Rotation.Y, targetYaw, TurnResponse * frameDelta), Rotation.Z);

        if (_visual != null && _target != null)
        {
            Vector3 lookAt = _target.GlobalPosition;
            lookAt.Y = _visual.GlobalPosition.Y;
            _visual.LookAt(lookAt, Vector3.Up);
            _visual.RotateY(Mathf.Pi);
        }
    }

    private bool CanSeeTarget()
    {
        if (!RequireLineOfSight || _target == null)
        {
            return true;
        }

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
            GlobalPosition + Vector3.Up * 0.8f,
            _target.GlobalPosition + Vector3.Up * 0.8f);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count == 0 || (hit.TryGetValue("collider", out Variant collider) && collider.Obj == _target);
    }

    private Vector3 GetFlatDirection(Vector3 destination)
    {
        Vector3 direction = destination - GlobalPosition;
        direction.Y = 0f;
        return direction.LengthSquared() > 0.001f ? direction.Normalized() : Vector3.Zero;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.Y = 0f;
        b.Y = 0f;
        return a.DistanceTo(b);
    }
}
