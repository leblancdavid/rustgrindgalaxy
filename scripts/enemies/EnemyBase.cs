using Godot;
using System;
using System.Collections.Generic;

public enum EnemyState
{
    Idle,
    Patrol,
    Alert,
    Chase,
    Attack,
    Stagger,
    Dead,
}

public abstract partial class EnemyBase : CharacterBody2D
{
    private const float ScreenReference = 640f;

    [Export] public int MaxHealth = 2;
    [Export] public int ContactDamage = 1;
    [Export] public float DetectionRange = 1.5f;
    [Export] public float KnockbackResistance = 0.0f;
    [Export] public float GravityScale = 1.0f;

    [Export] public float SeparationRadius = 22.0f;
    [Export] public float SeparationStrength = 80.0f;
    [Export] public float StuckToleranceSeconds = 0.35f;
    [Export] public float StepUpImpulse = -260.0f;
    [Export] public float StepUpProbeDistance = 14.0f;
    [Export] public float StuckActionCooldown = 0.5f;
    [Export] public float StuckRetryDelay = 0.25f;

    protected float AggroDistance => ScreenReference * DetectionRange;

    public int CurrentHealth { get; private set; }
    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;

    protected PlayerController? Player { get; private set; }
    protected Node2D? VisualContainer { get; private set; }
    protected Node2D? FacingNode { get; private set; }
    protected float HurtFlashTimer;
    protected bool IsHurtFlashing => HurtFlashTimer > 0.0f;
    private float _collisionHalfWidth;

    private Area2D? _hurtArea;
    private float _stateTimer;
    private bool _pendingDie;
    protected Vector2 _desiredHorizontalVelocity;
    private ProgressWatchdog _progress;
    private float _lastPositionX;
    private float _stuckCooldownRemaining;
    private List<Node2D>? _peerCache;
    protected float _hoverPhase;
    protected float _hoverAmplitudeScale = 1.0f;
    protected float _hoverYOffset;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        Player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        AddToGroup("mobs");
        VisualContainer = GetNodeOrNull<Node2D>("VisualContainer");
        FacingNode = VisualContainer?.GetNodeOrNull<Node2D>("Sprite");
        _hurtArea = GetNodeOrNull<Area2D>("HurtArea");
        FloorSnapLength = 20.0f;
        FloorMaxAngle = Mathf.DegToRad(65f);
        _collisionHalfWidth = GetCollisionHalfWidth();
        if (_hurtArea != null)
        {
            _hurtArea.BodyEntered += OnHurtAreaBodyEntered;
        }
        _progress = new ProgressWatchdog(StuckToleranceSeconds, 0.3f);
        _lastPositionX = GlobalPosition.X;
        DetectionRange = Mathf.Max(DetectionRange, 0.5f);
        SetupState(EnemyState.Patrol);
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        HurtFlashTimer = Mathf.Max(0.0f, HurtFlashTimer - dt);

        if (CurrentState == EnemyState.Dead)
            return;

        UpdateHurtFlash(dt);
        UpdateState(dt);
    }

    public virtual void TakeDamage(int amount, Node2D? damageSource = null)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HurtFlashTimer = 0.15f;

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (damageSource != null)
        {
            ApplyKnockback(damageSource.GlobalPosition);
        }

        SetState(EnemyState.Stagger);
    }

    protected virtual void Die()
    {
        if (_pendingDie)
            return;
        _pendingDie = true;
        SetState(EnemyState.Dead);

        var explosion = GD.Load<PackedScene>("res://scenes/effects/ExplosionEffect.tscn");
        if (explosion != null)
        {
            var instance = explosion.Instantiate<Node2D>();
            GetParent().AddChild(instance);
            instance.GlobalPosition = GlobalPosition;
        }

        QueueFree();
    }

    protected virtual void ApplyKnockback(Vector2 fromPosition)
    {
        var dir = GlobalPosition.DirectionTo(fromPosition) * -1;
        Velocity = dir * (80.0f * (1.0f - KnockbackResistance));
    }

    protected virtual void SetState(EnemyState newState)
    {
        if (CurrentState == newState)
            return;
        ExitState(CurrentState);
        CurrentState = newState;
        SetupState(newState);
    }

    protected virtual void SetupState(EnemyState state)
    {
        _stateTimer = 0.0f;
        switch (state)
        {
            case EnemyState.Dead:
                Velocity = Vector2.Zero;
                break;
            case EnemyState.Stagger:
                _stateTimer = 0.4f;
                break;
        }
    }

    protected virtual void ExitState(EnemyState state) { }

    private void UpdateGroundRotation(float delta)
    {
        if (VisualContainer == null) return;

        var lerpFactor = Mathf.Clamp(20f * delta, 0f, 1f);

        if (IsOnFloor())
        {
            var floorNormal = GetFloorNormal();
            var tangent = new Vector2(floorNormal.Y, -floorNormal.X).Normalized();
            if (tangent.X < 0f) tangent = -tangent;
            var targetAngle = tangent.Angle();

            // Match mid prop formula: baseSink + slope * halfWidth
            var slope = Mathf.Abs(floorNormal.X) / Mathf.Max(Mathf.Abs(floorNormal.Y), 0.01f);
            var targetOffset = 5f + slope * _collisionHalfWidth;
            VisualContainer.Position = new Vector2(0f, Mathf.Lerp(VisualContainer.Position.Y, targetOffset, lerpFactor));
            VisualContainer.Rotation = Mathf.LerpAngle(VisualContainer.Rotation, targetAngle, lerpFactor);
        }
        else
        {
            VisualContainer.Position = new Vector2(0f, Mathf.Lerp(VisualContainer.Position.Y, 0f, lerpFactor));
            VisualContainer.Rotation = Mathf.LerpAngle(VisualContainer.Rotation, 0f, lerpFactor);
        }
    }

    private float GetCollisionHalfWidth()
    {
        var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collisionShape?.Shape is RectangleShape2D rect)
            return rect.Size.X * 0.5f;
        return 0f;
    }

    protected void ApplyRampAdhesion(ref Vector2 velocity, float delta)
    {
        if (!IsOnFloor()) return;
        var floorNormal = GetFloorNormal();
        var floorTangent = new Vector2(floorNormal.Y, -floorNormal.X).Normalized();
        if (floorTangent.X < 0f) floorTangent = -floorTangent;
        var tangentSpeed = Mathf.Abs(velocity.Dot(floorTangent));
        var steepness = Mathf.Abs(floorTangent.Dot(Vector2.Down));
        velocity.Y += -floorNormal.Y * tangentSpeed * steepness * 3.0f * delta;
    }

    protected virtual void UpdateState(float delta)
    {
        _stateTimer += delta;
        UpdateGroundRotation(delta);

        _desiredHorizontalVelocity = Vector2.Zero;

        switch (CurrentState)
        {
            case EnemyState.Stagger:
                UpdateStaggerState(delta);
                break;
            case EnemyState.Patrol:
                UpdatePatrolState(delta);
                break;
            case EnemyState.Alert:
                UpdateAlertState(delta);
                break;
            case EnemyState.Chase:
                UpdateChaseState(delta);
                break;
            case EnemyState.Attack:
                UpdateAttackState(delta);
                break;
        }

        CheckTransitions();
        UpdateSteering(delta);
    }

    protected virtual void UpdateSteering(float delta)
    {
        if (CurrentState == EnemyState.Stagger
            || CurrentState == EnemyState.Dead
            || CurrentState == EnemyState.Attack)
        {
            _lastPositionX = GlobalPosition.X;
            return;
        }

        if (!IsOnFloor() && !IsHoverMover())
        {
            _progress.Reset();
            _lastPositionX = GlobalPosition.X;
            return;
        }

        var actualDeltaX = GlobalPosition.X - _lastPositionX;
        _lastPositionX = GlobalPosition.X;
        var actualSpeed = actualDeltaX / Mathf.Max(delta, 0.0001f);
        _progress.Sample(_desiredHorizontalVelocity.X, actualSpeed, delta);

        var push = ComputeSeparationPush();
        if (push != Vector2.Zero)
        {
            GlobalPosition += push * delta;
        }

        _stuckCooldownRemaining = Mathf.Max(0f, _stuckCooldownRemaining - delta);
        if (_progress.State == WatchdogState.Stuck && _stuckCooldownRemaining <= 0f)
        {
            if (OnStuckAction())
            {
                _stuckCooldownRemaining = StuckActionCooldown;
                MoveAndSlide();
            }
            else
            {
                _stuckCooldownRemaining = StuckRetryDelay;
            }
        }
    }

    protected virtual bool IsHoverMover() => false;

    protected void InitializeHoverVariance(Godot.RandomNumberGenerator rng, float offsetRange)
    {
        _hoverPhase = rng.Randf() * Mathf.Tau;
        _hoverAmplitudeScale = rng.RandfRange(0.6f, 1.4f);
        _hoverYOffset = rng.RandfRange(-offsetRange, offsetRange);
    }

    protected virtual Vector2 ComputeSeparationPush()
    {
        return EnemySteering.ComputeSeparationPush(
            GlobalPosition,
            GetPeerMobs(),
            GetSeparationRadius(),
            GetSeparationStrength());
    }

    protected virtual float GetSeparationRadius() => SeparationRadius;
    protected virtual float GetSeparationStrength() => SeparationStrength;

    protected List<Node2D> GetPeerMobs()
    {
        if (_peerCache == null)
            _peerCache = new List<Node2D>(8);
        _peerCache.Clear();
        var tree = GetTree();
        if (tree == null)
            return _peerCache;
        foreach (var node in tree.GetNodesInGroup("mobs"))
        {
            if (node is Node2D mob && mob != this && !mob.IsQueuedForDeletion())
                _peerCache.Add(mob);
        }
        return _peerCache;
    }

    protected virtual bool OnStuckAction()
    {
        if (!IsOnFloor())
            return false;
        if (Mathf.Abs(_desiredHorizontalVelocity.X) < 0.5f)
            return false;
        return TryStepUp(Mathf.Sign(_desiredHorizontalVelocity.X));
    }

    protected bool TryStepUp(int direction)
    {
        if (direction == 0)
            return false;
        var forward = new Vector2(direction, 0);
        if (!EnemySteering.CanStepUp(this, forward, StepUpProbeDistance))
            return false;
        var v = Velocity;
        v.Y = StepUpImpulse;
        v.X = direction * Mathf.Max(40f, Mathf.Abs(v.X));
        Velocity = v;
        return true;
    }

    protected virtual void UpdateStaggerState(float delta)
    {
        MoveAndSlide();
        if (_stateTimer >= 0.4f)
        {
            SetState(EnemyState.Patrol);
        }
    }

    protected virtual void UpdatePatrolState(float delta) { }
    protected virtual void UpdateAlertState(float delta) { }
    protected virtual void UpdateChaseState(float delta) { }
    protected virtual void UpdateAttackState(float delta) { }

    protected virtual void CheckTransitions()
    {
        if (Player == null || Player.IsDead)
            return;

        var distance = GlobalPosition.DistanceTo(Player.GlobalPosition);

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                if (DetectionRange > 0 && distance <= AggroDistance)
                    SetState(EnemyState.Alert);
                break;
            case EnemyState.Alert:
                if (distance > AggroDistance * 1.5f)
                    SetState(EnemyState.Patrol);
                else if (distance <= AggroDistance * 0.7f)
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (distance > AggroDistance * 1.5f)
                    SetState(EnemyState.Patrol);
                break;
        }
    }

    protected void FaceDirection(float directionX)
    {
        if (FacingNode == null) return;
        var scale = FacingNode.Scale;
        scale.X = directionX >= 0 ? Mathf.Abs(scale.X) : -Mathf.Abs(scale.X);
        FacingNode.Scale = scale;
    }

    protected void FacePlayer()
    {
        if (Player == null) return;
        FaceDirection(Player.GlobalPosition.X - GlobalPosition.X);
    }

    protected void ClampAboveFloor(float minDistance, float maxRaycastDistance = 500f)
    {
        if (minDistance <= 0f) return;

        var spaceState = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + new Vector2(0, maxRaycastDistance), 1);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0 && result.ContainsKey("position"))
        {
            var floorY = ((Vector2)result["position"]).Y;
            var minY = floorY - minDistance;
            if (GlobalPosition.Y > minY)
            {
                var pos = GlobalPosition;
                pos.Y = minY;
                GlobalPosition = pos;
            }
        }
    }

    private void UpdateHurtFlash(float delta)
    {
        if (_hurtArea == null) return;
        var children = _hurtArea.GetChildren();
        foreach (var child in children)
        {
            if (child is Polygon2D poly)
            {
                poly.Color = IsHurtFlashing
                    ? new Color(1.0f, 0.3f, 0.3f, 1.0f)
                    : poly.Color = Colors.White;
            }
        }
    }

    private void OnHurtAreaBodyEntered(Node2D body)
    {
        if (ContactDamage <= 0 || CurrentHealth <= 0)
            return;
        if (body is PlayerController player)
        {
            player.TakeDamage(ContactDamage);
        }
    }
}
