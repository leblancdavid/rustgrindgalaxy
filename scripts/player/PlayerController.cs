using System.Collections.Generic;
using System.Text;
using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export] public float MoveSpeed = 225.0f;
	[Export] public float GroundAcceleration = 720.0f;
	[Export] public float CoastDeceleration = 80.0f;
	[Export] public float AirAcceleration = 320.0f;
	[Export] public float MinimumJumpVelocity = -100.0f;
	[Export] public float JumpVelocity = -500.0f;
	[Export] public float MaxJumpHoldTime = 0.5f;
	[Export] public float GravityScale = 1.0f;
	[Export] public float SlopeGravityStrength = 900.0f;
	[Export] public float RotationLerpSpeed = 20.0f;
	[Export] public float RotationSpeedDegrees = 480.0f;
	[Export] public float LandingToleranceDegrees = 20.0f;
	[Export] public float RailFriction = 12.0f;
	[Export] public float RailGravityStrength = 900.0f;
	[Export] public float MinimumRailEntrySpeed = 20.0f;
	[Export] public float GrindIntentSeconds = 1.0f;
	[Export] public float TravelIntentMemorySeconds = 0.15f;
	[Export] public float RailAttachCooldownSeconds = 0.18f;
	[Export] public float MaxRailSpeed = 420.0f;
	[Export] public float JumpChargeBoardTiltDegrees = 7.0f;
	[Export] public float OllieTakeoffTiltDegrees = 12.0f;
	[Export] public float OllieTiltRecoverSpeed = 10.0f;
	[Export] public float GrindBoardTiltDegrees = 5.0f;
	[Export] public float GrindTiltResponseSpeed = 8.0f;
	[Export] public float GrindBobDegrees = 1.5f;
	[Export] public float GrindBobSpeed = 7.0f;
	[Export] public float GrindVisualMinimumStrength = 0.45f;
	[Export] public float GrindBobOffsetPixels = 0.75f;
	[Export] public float BalanceMaxOffset = 1.0f;
	[Export] public float BalanceDriftRate = 0.8f;
	[Export] public float BalanceDriftChangeInterval = 1.8f;
	[Export] public float BalanceCorrectionSpeed = 2.5f;
	[Export] public float BalanceRecoverySpeed = 1.0f;
	[Export] public float BalancePhysicsForce = 35.0f;
	[Export] public float BalanceVisualTiltDegrees = 10.0f;
	[Export] public float BalanceIndicatorWidth = 24.0f;
	[Export] public float BalanceIndicatorHeight = 5.0f;
	[Export] public float BalanceIndicatorY = -36.0f;
	[Export] public float AirRotationRampUpTime = 0.5f;
	[Export] public int MaxHealth = 5;
	[Export] public float InvulnerabilityDuration = 0.75f;

	private const string GrindAction = "grind";
	private const string TrickFlipAction = "trick_flip";
	private const string TrickGrabAction = "trick_grab";
	private const string TrickAltFlipAction = "trick_alt_flip";
	private const float FailedLandingSeparation = 2.0f;
	private const float FailedLandingFallSpeed = 90.0f;
	private const float FlipDurationSeconds = 0.45f;
	private const float AltFlipDurationSeconds = 0.30f;
	private const float GrabSetupDurationSeconds = 0.10f;
	private const float GrabReleaseDurationSeconds = 0.12f;
	private const float FailedLandingVisualRecoverSpeed = 7.5f;
	private const float FailedLandingBodyTiltDegrees = 60.0f;
	private const float FailedLandingBoardTiltDegrees = 22.0f;
	private const float LandedComboDisplaySeconds = 2.0f;
	private static readonly float GrabHoldAngleRadians = Mathf.DegToRad(15.0f);

	private enum TrickKind
	{
		None,
		Flip,
		Grab,
		AltFlip,
	}

	private enum TrickPhase
	{
		None,
		Startup,
		Active,
		Recovery,
	}

	private ResolvedModuleEffects _resolvedEffects = new();
	private GrindRail? _nearbyRail;
	private GrindRail? _activeRail;
	private float _grindDirection;
	private float _railArmorTimeRemaining;
	private float _invulnerabilityTimeRemaining;
	private float _railAttachCooldownRemaining;
	private float _jumpChargeTime;
	private float _groundTilt;
	private float _airRotation;
	private float _railRotationOffset;
	private float _grindIntentTimeRemaining;
	private float _railProgress;
	private float _railSpeed;
	private float _lastTravelDirection;
	private float _travelIntentTimeRemaining;
	private float _boardAnimationTilt;
	private float _ollieTakeoffTilt;
	private float _grindBobTime;
	private float _balanceValue;
	private float _balanceDriftTarget;
	private float _balanceDriftTimer;
	private float _airRotationRamp;
	private float _airRotationRampDirection;
	private Node2D _balanceIndicator = null!;
	private Polygon2D _balanceArrow = null!;
	private bool _isChargingJump;
	private Marker2D _boardContact = null!;
	private Polygon2D _boardVisual = null!;
	private Polygon2D _visual = null!;
	private Color _baseColor;
	private Vector2 _boardVisualBasePosition;
	private Vector2 _previousBoardContactPoint;
	private TrickKind _activeTrick;
	private TrickPhase _activeTrickPhase;
	private TrickKind _queuedTrick;
	private bool _flipQueueReady = true;
	private bool _grabQueueReady = true;
	private bool _jumpGrabQueueReady = true;
	private bool _altFlipQueueReady = true;
	private float _trickElapsed;
	private float _trickRotationOffset;
	private float _trickRecoveryStartRotation;
	private readonly List<string> _comboTrickSequence = new();
	private float _failedLandingVisualBlend;
	private float _failedLandingDirection;
	private bool _isFailedLandingFalling;

	public PlayerLoadout? Loadout { get; private set; }

	public bool IsNearRail => _nearbyRail != null;

	public bool IsGrinding => _activeRail != null;

	public bool IsDead => CurrentHealth <= 0;

	public ResolvedModuleEffects ResolvedEffects => _resolvedEffects;

	public float RailArmorTimeRemaining => _railArmorTimeRemaining;

	public float InvulnerabilityTimeRemaining => _invulnerabilityTimeRemaining;

	public int CurrentHealth { get; private set; }

	public uint TrickStartSequence { get; private set; }

	public string LastStartedTrickName { get; private set; } = string.Empty;

	public IReadOnlyList<string> CurrentComboTrickSequence => _comboTrickSequence;

	public string CurrentComboSummary { get; private set; } = string.Empty;

	public string LastLandedComboSummary { get; private set; } = string.Empty;

	public float LandedComboDisplayTimeRemaining { get; private set; }

	public override void _Ready()
	{
		EnsureGrindInput();
		_boardContact = GetNode<Marker2D>("BoardContact");
		_boardVisual = GetNode<Polygon2D>("BoardVisual");
		_visual = GetNode<Polygon2D>("Visual");
		_baseColor = _visual.Color;
		_boardVisualBasePosition = _boardVisual.Position;
		_airRotation = Rotation;
		_previousBoardContactPoint = GetRailContactPoint();
		CurrentHealth = MaxHealth;
		CreateBalanceIndicator();
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
	}

	public override void _PhysicsProcess(double delta)
	{
		var deltaSeconds = (float)delta;
		LandedComboDisplayTimeRemaining = Mathf.Max(0.0f, LandedComboDisplayTimeRemaining - deltaSeconds);

		if (IsDead)
		{
			CancelActiveTrick();
			ClearQueuedTrick();
			CancelJumpCharge();
			ResetComboAndFallState();
			Velocity = Vector2.Zero;
			UpdateBoardAnimationTilt(deltaSeconds);
			UpdateFailedLandingVisual(deltaSeconds);
			return;
		}

		var velocity = Velocity;
		var inputDirection = Input.GetAxis("ui_left", "ui_right");
		var wasOnFloor = IsOnFloor();
		var previousBoardContactPoint = GetRailContactPoint();
		var gravityMultiplier = _resolvedEffects.HangTimeGravityMultiplier;
		var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity") * GravityScale * gravityMultiplier;
		_railArmorTimeRemaining = Mathf.Max(0.0f, _railArmorTimeRemaining - deltaSeconds);
		_invulnerabilityTimeRemaining = Mathf.Max(0.0f, _invulnerabilityTimeRemaining - deltaSeconds);
		_railAttachCooldownRemaining = Mathf.Max(0.0f, _railAttachCooldownRemaining - deltaSeconds);
		_grindIntentTimeRemaining = Mathf.Max(0.0f, _grindIntentTimeRemaining - deltaSeconds);
		_travelIntentTimeRemaining = Mathf.Max(0.0f, _travelIntentTimeRemaining - deltaSeconds);
		UpdateBoostTimers(deltaSeconds);

		UpdateDamageFlash();
		UpdateGrindIntent();
		UpdateTravelIntent(inputDirection, velocity.X);
		UpdateJumpCharge(deltaSeconds, wasOnFloor, _activeRail != null);
		UpdateRotationInput(inputDirection, deltaSeconds, wasOnFloor);
		UpdateTrickState(deltaSeconds, wasOnFloor);

		if (_activeRail != null)
		{
			HandleGrinding(ref velocity, inputDirection, deltaSeconds, gravity);
			Velocity = velocity;
			MoveAndSlide();
			_previousBoardContactPoint = GetRailContactPoint();
			UpdateVisualRotation(deltaSeconds, GetTargetRotation());
			UpdateBoardAnimationTilt(deltaSeconds);
			UpdateFailedLandingVisual(deltaSeconds);
			return;
		}

		if (!wasOnFloor)
		{
			velocity.Y += gravity * deltaSeconds;

			if (_isChargingJump)
			{
				CancelJumpCharge();
			}
		}

		if (TryStartBufferedGrinding(previousBoardContactPoint, GetRailContactPoint(), ref velocity, inputDirection, deltaSeconds, gravity))
		{
			Velocity = velocity;
			_previousBoardContactPoint = GetRailContactPoint();
			MoveAndSlide();
			_previousBoardContactPoint = GetRailContactPoint();
			UpdateVisualRotation(deltaSeconds, GetTargetRotation());
			UpdateBoardAnimationTilt(deltaSeconds);
			UpdateFailedLandingVisual(deltaSeconds);
			return;
		}

		if (TryReleaseJump(ref velocity, inputDirection, wasOnFloor, false))
		{
			Velocity = velocity;
			MoveAndSlide();
			_previousBoardContactPoint = GetRailContactPoint();
			UpdateBoardAnimationTilt(deltaSeconds);
			UpdateFailedLandingVisual(deltaSeconds);
			return;
		}

		ApplyHorizontalMovement(ref velocity, inputDirection, deltaSeconds, wasOnFloor, gravity);

		Velocity = velocity;
		MoveAndSlide();

		velocity = Velocity;

		if (RejectInvalidLanding(wasOnFloor, ref velocity))
		{
			Velocity = velocity;
		}
		else if (IsOnFloor())
		{
			UpdateGroundRotationState();
		}

		if (_activeRail == null && wasOnFloor == false && TryStartBufferedGrinding(previousBoardContactPoint, GetRailContactPoint(), ref velocity, inputDirection, deltaSeconds, gravity))
		{
			Velocity = velocity;
			_previousBoardContactPoint = GetRailContactPoint();
			UpdateVisualRotation(deltaSeconds, _activeRail?.Angle ?? GetTargetRotation());
			UpdateBoardAnimationTilt(deltaSeconds);
			UpdateFailedLandingVisual(deltaSeconds);
			return;
		}

		_previousBoardContactPoint = GetRailContactPoint();
		UpdateVisualRotation(deltaSeconds, GetTargetRotation());
		UpdateBoardAnimationTilt(deltaSeconds);
		UpdateFailedLandingVisual(deltaSeconds);
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
}
