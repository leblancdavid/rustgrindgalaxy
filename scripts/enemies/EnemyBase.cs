using Godot;
using System;

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
    [Export] public int MaxHealth = 2;
    [Export] public int ContactDamage = 1;
    [Export] public float DetectionRange = 0.0f;
    [Export] public float KnockbackResistance = 0.0f;
    [Export] public float GravityScale = 1.0f;
    [Export] public float GroundSink = 0f;

    public int CurrentHealth { get; private set; }
    public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;

    protected PlayerController? Player { get; private set; }
    protected Node2D? VisualContainer { get; private set; }
    protected float HurtFlashTimer;
    protected bool IsHurtFlashing => HurtFlashTimer > 0.0f;

    private Area2D? _hurtArea;
    private float _stateTimer;
    private bool _pendingDie;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        Player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        VisualContainer = GetNodeOrNull<Node2D>("VisualContainer");
        _hurtArea = GetNodeOrNull<Area2D>("HurtArea");
        if (_hurtArea != null)
        {
            _hurtArea.BodyEntered += OnHurtAreaBodyEntered;
        }
        SetupState(EnemyState.Patrol);

        ApplyGroundSink();
    }

    private void ApplyGroundSink()
    {
        if (GroundSink == 0f) return;
        if (VisualContainer != null)
        {
            VisualContainer.Position += new Vector2(0, GroundSink);
            return;
        }
        foreach (var child in GetChildren())
        {
            if (child is Polygon2D poly)
            {
                poly.Position += new Vector2(0, GroundSink);
            }
        }
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

        if (IsOnFloor())
        {
            var floorNormal = GetFloorNormal();
            var tangent = new Vector2(floorNormal.Y, -floorNormal.X).Normalized();
            if (tangent.X < 0f) tangent = -tangent;
            var targetAngle = tangent.Angle();
            VisualContainer.Rotation = Mathf.LerpAngle(VisualContainer.Rotation, targetAngle, Mathf.Clamp(10f * delta, 0f, 1f));
        }
        else
        {
            VisualContainer.Rotation = Mathf.LerpAngle(VisualContainer.Rotation, 0f, Mathf.Clamp(10f * delta, 0f, 1f));
        }
    }

    protected virtual void UpdateState(float delta)
    {
        _stateTimer += delta;
        UpdateGroundRotation(delta);

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
                if (DetectionRange > 0 && distance <= DetectionRange)
                    SetState(EnemyState.Alert);
                break;
            case EnemyState.Alert:
                if (distance > DetectionRange * 1.5f)
                    SetState(EnemyState.Patrol);
                else if (distance <= DetectionRange * 0.7f)
                    SetState(EnemyState.Chase);
                break;
            case EnemyState.Chase:
                if (distance > DetectionRange * 1.5f)
                    SetState(EnemyState.Patrol);
                break;
        }
    }

    protected void FaceDirection(float directionX)
    {
        var scale = Scale;
        scale.X = directionX >= 0 ? Mathf.Abs(scale.X) : -Mathf.Abs(scale.X);
        Scale = scale;
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
