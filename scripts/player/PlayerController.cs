using Godot;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float MoveSpeed = 150.0f;
    [Export] public float GroundAcceleration = 720.0f;
    [Export] public float CoastDeceleration = 80.0f;
    [Export] public float AirAcceleration = 320.0f;
    [Export] public float MinimumJumpVelocity = -250.0f;
    [Export] public float JumpVelocity = -500.0f;
    [Export] public float MaxJumpHoldTime = 0.28f;
    [Export] public float GravityScale = 1.0f;
    [Export] public float RailSnapDistance = 12.0f;
    [Export] public float RailAttachCooldownSeconds = 0.18f;
    [Export] public int MaxHealth = 5;
    [Export] public float InvulnerabilityDuration = 0.75f;

    private const string GrindAction = "grind";

    private ResolvedModuleEffects _resolvedEffects = new();
    private GrindRail? _nearbyRail;
    private GrindRail? _activeRail;
    private float _grindDirection;
    private float _railArmorTimeRemaining;
    private float _invulnerabilityTimeRemaining;
    private float _railAttachCooldownRemaining;
    private float _jumpChargeTime;
    private bool _isChargingJump;
    private Polygon2D _visual = null!;
    private Color _baseColor;

    public PlayerLoadout? Loadout { get; private set; }

    public bool IsNearRail => _nearbyRail != null;

    public bool IsGrinding => _activeRail != null;

    public bool IsDead => CurrentHealth <= 0;

    public ResolvedModuleEffects ResolvedEffects => _resolvedEffects;

    public float RailArmorTimeRemaining => _railArmorTimeRemaining;

    public float InvulnerabilityTimeRemaining => _invulnerabilityTimeRemaining;

    public int CurrentHealth { get; private set; }

    public override void _Ready()
    {
        EnsureGrindInput();
        _visual = GetNode<Polygon2D>("Visual");
        _baseColor = _visual.Color;
        CurrentHealth = MaxHealth;
    }

    public void SetLoadout(PlayerLoadout loadout)
    {
        Loadout = loadout;
        _resolvedEffects = ModuleEffectResolver.Resolve(loadout);
    }

    public void SetNearbyRail(GrindRail rail)
    {
        _nearbyRail = rail;
    }

    public void ClearNearbyRail(GrindRail rail)
    {
        if (_nearbyRail == rail)
        {
            _nearbyRail = null;
        }

        if (_activeRail == rail)
        {
            ExitRail();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead)
        {
            CancelJumpCharge();
            Velocity = Vector2.Zero;
            return;
        }

        var deltaSeconds = (float)delta;
        var velocity = Velocity;
        var inputDirection = Input.GetAxis("ui_left", "ui_right");
        var wasOnFloor = IsOnFloor();
        _railArmorTimeRemaining = Mathf.Max(0.0f, _railArmorTimeRemaining - deltaSeconds);
        _invulnerabilityTimeRemaining = Mathf.Max(0.0f, _invulnerabilityTimeRemaining - deltaSeconds);
        _railAttachCooldownRemaining = Mathf.Max(0.0f, _railAttachCooldownRemaining - deltaSeconds);

        UpdateDamageFlash();
        UpdateJumpCharge(deltaSeconds, wasOnFloor, _activeRail != null);

        if (_activeRail != null)
        {
            HandleGrinding(ref velocity, inputDirection);
            Velocity = velocity;
            MoveAndSlide();
            return;
        }

        var gravityMultiplier = _resolvedEffects.HangTimeGravityMultiplier;
        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale * gravityMultiplier;

        if (!wasOnFloor)
        {
            velocity.Y += gravity * deltaSeconds;

            if (_isChargingJump)
            {
                CancelJumpCharge();
            }

            if (CanStartGrinding(inputDirection, velocity.X))
            {
                EnterRail(_nearbyRail!, GetRequestedDirection(inputDirection, velocity.X));
                HandleGrinding(ref velocity, inputDirection);
                Velocity = velocity;
                MoveAndSlide();
                return;
            }
        }

        if (TryReleaseJump(ref velocity, inputDirection, wasOnFloor, false))
        {
            Velocity = velocity;
            MoveAndSlide();
            return;
        }

        ApplyHorizontalMovement(ref velocity, inputDirection, deltaSeconds, wasOnFloor);

        Velocity = velocity;
        MoveAndSlide();
    }

    private bool CanStartGrinding(float inputDirection, float currentVelocityX)
    {
        if (_nearbyRail == null || Input.IsActionPressed(GrindAction) == false || _railAttachCooldownRemaining > 0.0f)
        {
            return false;
        }

        if (_nearbyRail.CanSnap(this) == false)
        {
            return false;
        }

        var requestedDirection = GetRequestedDirection(inputDirection, currentVelocityX);
        if (Mathf.IsZeroApprox(requestedDirection))
        {
            return false;
        }

        return Mathf.Abs(GlobalPosition.Y - _nearbyRail.RailY) <= RailSnapDistance;
    }

    private void EnterRail(GrindRail rail, float travelDirection)
    {
        _activeRail = rail;
        _grindDirection = Mathf.Sign(travelDirection);

        if (Mathf.IsZeroApprox(_grindDirection))
        {
            _grindDirection = 1.0f;
        }

        _railArmorTimeRemaining = Mathf.Max(_railArmorTimeRemaining, _resolvedEffects.RailEntryArmorSeconds);
        GlobalPosition = new Vector2(GlobalPosition.X, rail.RailY);
        Velocity = new Vector2(_grindDirection * rail.GetSpeed(_resolvedEffects.RailSpeedBonus), 0.0f);
    }

    private void ExitRail()
    {
        _activeRail = null;
        _railAttachCooldownRemaining = RailAttachCooldownSeconds;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead || _invulnerabilityTimeRemaining > 0.0f)
        {
            return;
        }

        if (_railArmorTimeRemaining > 0.0f)
        {
            amount = Mathf.Max(0, amount - 1);
        }

        if (amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        _invulnerabilityTimeRemaining = InvulnerabilityDuration;

        if (CurrentHealth <= 0)
        {
            Velocity = Vector2.Zero;
            _visual.Color = new Color(0.35f, 0.35f, 0.35f, 1.0f);
        }
    }

    private void HandleGrinding(ref Vector2 velocity, float inputDirection)
    {
        var rail = _activeRail!;

        if (TryReleaseJump(ref velocity, inputDirection, false, true))
        {
            return;
        }

        if (!Mathf.IsZeroApprox(inputDirection))
        {
            _grindDirection = Mathf.Sign(inputDirection);
        }

        if (Mathf.IsZeroApprox(_grindDirection))
        {
            _grindDirection = 1.0f;
        }

        velocity.Y = 0.0f;
        velocity.X = _grindDirection * rail.GetSpeed(_resolvedEffects.RailSpeedBonus);
        GlobalPosition = new Vector2(Mathf.Clamp(GlobalPosition.X, rail.LeftX, rail.RightX), rail.RailY);

        var nextX = GlobalPosition.X + (velocity.X * (float)GetPhysicsProcessDeltaTime());
        if (nextX <= rail.LeftX || nextX >= rail.RightX)
        {
            ExitRail();
        }
    }

    private void ApplyHorizontalMovement(ref Vector2 velocity, float inputDirection, float deltaSeconds, bool onFloor)
    {
        if (Mathf.IsZeroApprox(inputDirection))
        {
            if (onFloor)
            {
                velocity.X = Mathf.MoveToward(velocity.X, 0.0f, CoastDeceleration * deltaSeconds);
            }

            return;
        }

        var targetSpeed = inputDirection * MoveSpeed;
        var acceleration = onFloor ? GroundAcceleration : AirAcceleration;
        velocity.X = Mathf.MoveToward(velocity.X, targetSpeed, acceleration * deltaSeconds);
    }

    private void UpdateDamageFlash()
    {
        if (_invulnerabilityTimeRemaining > 0.0f)
        {
            var flashOn = Mathf.PosMod(Time.GetTicksMsec() / 100, 2) == 0;
            _visual.Color = flashOn ? new Color(1.0f, 0.45f, 0.45f, 1.0f) : _baseColor;
            return;
        }

        _visual.Color = _baseColor;
    }

    private void UpdateJumpCharge(float deltaSeconds, bool onFloor, bool onRail)
    {
        if (Input.IsActionJustPressed("ui_accept") && (onFloor || onRail))
        {
            _isChargingJump = true;
            _jumpChargeTime = 0.0f;
        }

        if (_isChargingJump == false)
        {
            return;
        }

        if (onFloor == false && onRail == false)
        {
            CancelJumpCharge();
            return;
        }

        _jumpChargeTime = Mathf.Min(_jumpChargeTime + deltaSeconds, MaxJumpHoldTime);
    }

    private bool TryReleaseJump(ref Vector2 velocity, float inputDirection, bool onFloor, bool onRail)
    {
        if (_isChargingJump == false || Input.IsActionJustReleased("ui_accept") == false)
        {
            return false;
        }

        var chargedJumpVelocity = GetChargedJumpVelocity();
        CancelJumpCharge();

        if (onRail)
        {
            ExitRail();
            velocity.Y = chargedJumpVelocity - _resolvedEffects.LaunchHeightBonus;
            velocity.X = _grindDirection * (MoveSpeed + _resolvedEffects.BurstTakeoffSpeedBonus);
            return true;
        }

        if (onFloor == false)
        {
            return false;
        }

        velocity.Y = chargedJumpVelocity - _resolvedEffects.LaunchHeightBonus;
        velocity.X = ApplyTakeoffBonus(velocity.X, inputDirection);
        return true;
    }

    private float GetChargedJumpVelocity()
    {
        if (MaxJumpHoldTime <= 0.0f)
        {
            return JumpVelocity;
        }

        var ratio = Mathf.Clamp(_jumpChargeTime / MaxJumpHoldTime, 0.0f, 1.0f);
        return Mathf.Lerp(MinimumJumpVelocity, JumpVelocity, ratio);
    }

    private float ApplyTakeoffBonus(float currentVelocityX, float inputDirection)
    {
        var direction = GetRequestedDirection(inputDirection, currentVelocityX);
        if (Mathf.IsZeroApprox(direction))
        {
            return currentVelocityX;
        }

        return currentVelocityX + (direction * _resolvedEffects.BurstTakeoffSpeedBonus);
    }

    private float GetRequestedDirection(float inputDirection, float currentVelocityX)
    {
        if (!Mathf.IsZeroApprox(inputDirection))
        {
            return Mathf.Sign(inputDirection);
        }

        if (!Mathf.IsZeroApprox(currentVelocityX))
        {
            return Mathf.Sign(currentVelocityX);
        }

        return 0.0f;
    }

    private void CancelJumpCharge()
    {
        _isChargingJump = false;
        _jumpChargeTime = 0.0f;
    }

    private static void EnsureGrindInput()
    {
        if (InputMap.HasAction(GrindAction))
        {
            return;
        }

        InputMap.AddAction(GrindAction);
        InputMap.ActionAddEvent(GrindAction, new InputEventKey
        {
            PhysicalKeycode = Key.Shift,
        });
    }
}
