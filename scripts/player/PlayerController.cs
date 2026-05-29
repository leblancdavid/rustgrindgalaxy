using Godot;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float MoveSpeed = 120.0f;
    [Export] public float JumpVelocity = -260.0f;
    [Export] public float GravityScale = 1.0f;
    [Export] public float RailSnapDistance = 12.0f;
    [Export] public int MaxHealth = 5;
    [Export] public float InvulnerabilityDuration = 0.75f;

    private ResolvedModuleEffects _resolvedEffects = new();
    private GrindRail? _nearbyRail;
    private GrindRail? _activeRail;
    private float _grindDirection;
    private float _railArmorTimeRemaining;
    private float _invulnerabilityTimeRemaining;
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
            Velocity = Vector2.Zero;
            return;
        }

        var velocity = Velocity;
        var inputDirection = Input.GetAxis("ui_left", "ui_right");
        _railArmorTimeRemaining = Mathf.Max(0.0f, _railArmorTimeRemaining - (float)delta);
        _invulnerabilityTimeRemaining = Mathf.Max(0.0f, _invulnerabilityTimeRemaining - (float)delta);

        if (_invulnerabilityTimeRemaining > 0.0f)
        {
            var flashOn = Mathf.PosMod(Time.GetTicksMsec() / 100, 2) == 0;
            _visual.Color = flashOn ? new Color(1.0f, 0.45f, 0.45f, 1.0f) : _baseColor;
        }
        else
        {
            _visual.Color = _baseColor;
        }

        if (_activeRail != null)
        {
            HandleGrinding(ref velocity, inputDirection);
            Velocity = velocity;
            MoveAndSlide();
            return;
        }

        var gravityMultiplier = _resolvedEffects.HangTimeGravityMultiplier;
        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale * gravityMultiplier;

        if (!IsOnFloor())
        {
            velocity.Y += gravity * (float)delta;

            if (CanStartGrinding(inputDirection))
            {
                EnterRail(_nearbyRail!, inputDirection);
                HandleGrinding(ref velocity, inputDirection);
                Velocity = velocity;
                MoveAndSlide();
                return;
            }
        }

        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
        {
            velocity.Y = JumpVelocity - _resolvedEffects.LaunchHeightBonus;
            velocity.X = inputDirection * MoveSpeed;

            if (!Mathf.IsZeroApprox(inputDirection))
            {
                velocity.X += Mathf.Sign(inputDirection) * _resolvedEffects.BurstTakeoffSpeedBonus;
            }
        }
        else
        {
            velocity.X = inputDirection * MoveSpeed;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private bool CanStartGrinding(float inputDirection)
    {
        if (_nearbyRail == null || Mathf.IsZeroApprox(inputDirection))
        {
            return false;
        }

        if (_nearbyRail.CanSnap(this) == false)
        {
            return false;
        }

        return Mathf.Abs(GlobalPosition.Y - _nearbyRail.RailY) <= RailSnapDistance;
    }

    private void EnterRail(GrindRail rail, float inputDirection)
    {
        _activeRail = rail;
        _grindDirection = Mathf.Sign(inputDirection);
        _railArmorTimeRemaining = Mathf.Max(_railArmorTimeRemaining, _resolvedEffects.RailEntryArmorSeconds);
        GlobalPosition = new Vector2(GlobalPosition.X, rail.RailY);
    }

    private void ExitRail()
    {
        _activeRail = null;
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

        if (Input.IsActionJustPressed("ui_accept"))
        {
            ExitRail();
            velocity.Y = JumpVelocity - _resolvedEffects.LaunchHeightBonus;
            velocity.X = _grindDirection * (MoveSpeed + _resolvedEffects.BurstTakeoffSpeedBonus);
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
}
