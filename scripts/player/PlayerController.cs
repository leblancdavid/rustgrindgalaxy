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
	[Export] public float GrindEntryToleranceDegrees = 60.0f;
	[Export] public float GrindEntryBalanceCurvePower = 2.2f;
	[Export] public float GrindEntryBalanceMaxRatio = 0.85f;
	[Export] public float BalanceDriftSeverityScale = 0.8f;
	[Export] public float JunctionBalanceJerkRatio = 0.25f;
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
	[Export] public float AirTiltMaxDegrees = 14.0f;
	[Export] public float AirTiltSpeedRef = 420.0f;
	[Export] public float AirTiltResponseSpeed = 7.0f;
	[Export] public float GrindBoardTiltDegrees = 5.0f;
	[Export] public float GrindTiltResponseSpeed = 8.0f;
	[Export] public float GrindBobDegrees = 1.5f;
	[Export] public float GrindBobSpeed = 7.0f;
	[Export] public float GrindVisualMinimumStrength = 0.45f;
	[Export] public float GrindBobOffsetPixels = 0.75f;
	[Export] public float BalanceMaxOffset = 1.0f;
	[Export] public float BalanceDriftRate = 0.35f;
	[Export] public float BalanceCorrectionSpeed = 2.5f;
	[Export] public float BalanceNoiseMagnitude = 0.15f;
	[Export] public float BalanceNoiseMinFrames = 15f;
	[Export] public float BalanceNoiseMaxFrames = 60f;
	[Export] public float BalancePhysicsForce = 35.0f;
	[Export] public float BalanceVisualTiltDegrees = 10.0f;
	[Export] public float BalanceIndicatorWidth = 24.0f;
	[Export] public float BalanceIndicatorHeight = 5.0f;
	[Export] public float BalanceIndicatorY = -36.0f;
	[Export] public float GrindTimeToMaxDifficulty = 10.0f;
	[Export] public float BalanceMaxDriftRate = 3.5f;
	[Export] public float BalanceDriftWobbleRange = 0.3f;
	[Export] public float BalanceDriftWobbleInterval = 1.8f;
	[Export] public float BalanceMinDriftWobbleInterval = 0.4f;
	[Export] public float BalanceComboRecovery = 0.45f;
	[Export] public float RailTransitionSmoothDuration = 0.12f;
	[Export] public float MinGrindEntrySpeedTransfer = 0.4f;
	[Export] public float AirRotationRampUpTime = 0.5f;
	[Export] public int MaxHealth = 5;
	[Export] public float InvulnerabilityDuration = 0.75f;
	[Export] public float FloorSnapDistance = 20.0f;
	[Export] public float FloorMaxAngleDegrees = 65.0f;
	[Export] public float RampAdhesionFactor = 3.0f;

	private const string GrindAction = "grind";
	private const string TrickFlipAction = "trick_flip";
	private const string TrickGrabAction = "trick_grab";
	private const string TrickAltFlipAction = "trick_alt_flip";
	private const string TrickSlot4Action = "trick_slot_4";
	// Enter-only grab confirm. ui_accept also matches Space (the jump key), so
	// grab must not listen to it or charging a jump would queue a grab.
	private const string TrickGrabConfirmAction = "trick_grab_confirm";
	private const float FailedLandingSeparation = 2.0f;
	private const float FailedLandingFallSpeed = 90.0f;
	// Hard upright limit for air grind entry, independent of rail slope.
	private const float MaxGrindEntryUprightDegrees = 90.0f;
	// Spin speed (half-turns per second) used when a definition leaves it at 0.
	private const float TrickDefaultSpinSpeed = 4.0f;
	// Floor for |cos(theta)| while an axis flip is edge-on, so the board shows
	// a thin edge sliver instead of vanishing for a frame.
	private const float TrickEdgeMinScale = 0.06f;
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
		Slot1,
		Slot2,
		Slot3,
		Slot4,
		Grab,
	}

	// Rotations around each screen axis in 180-degree increments. The board is
	// drawn from the side, so Z turns become in-plane rotation while X/Y turns
	// are faked by squashing the sprite through the edge-on moment (negative
	// scale = the mirrored far side), see ApplyTrickVisual.
	private struct TrickDefinition
	{
		public readonly int HalfTurnsX;
		public readonly int HalfTurnsY;
		public readonly int HalfTurnsZ;
		// Half-turns (180 deg) completed per second; 0 falls back to TrickDefaultSpinSpeed.
		public readonly float SpinSpeed;

		public TrickDefinition(int halfTurnsX, int halfTurnsY, int halfTurnsZ, float spinSpeed = 0f)
		{
			HalfTurnsX = halfTurnsX;
			HalfTurnsY = halfTurnsY;
			HalfTurnsZ = halfTurnsZ;
			SpinSpeed = spinSpeed;
		}

		public readonly int MaxHalfTurns => Mathf.Max(HalfTurnsX, Mathf.Max(HalfTurnsY, HalfTurnsZ));

		public readonly float DurationSeconds
		{
			get
			{
				var speed = SpinSpeed > 0.0f ? SpinSpeed : TrickDefaultSpinSpeed;
				return Mathf.Max(1, MaxHalfTurns) / speed ;
			}
		}
	}

	private static TrickDefinition GetTrickDefinition(TrickKind trick)
	{
		return trick switch
		{
			TrickKind.Slot1 => new TrickDefinition(6, 0, 0, 4f),
			TrickKind.Slot2 => new TrickDefinition(0, 6, 0, 6f),
			TrickKind.Slot3 => new TrickDefinition(0, 0, 6, 8f),
			TrickKind.Slot4 => new TrickDefinition(0, 1, 0, 2f),
			_ => default,
		};
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
	private float _balanceDriftWobble = 1.0f;
	private float _balanceDriftWobbleTimer;
	private float _balanceNoiseTimer;
	private float _railTransitionTimer;
	private float _railTransitionVisualRotation;
	private float _grindElapsedTime;
	private float _airRotationRamp;
	private float _airRotationRampDirection;
	private Node2D _balanceIndicator = null!;
	private Polygon2D _balanceArrow = null!;
	private bool _isChargingJump;
	private Marker2D _boardContact = null!;
	private Node2D _visualContainer = null!;
	private Sprite2D _boardVisual = null!;
	private Sprite2D _visual = null!;
	private Color _baseColor;
	private Vector2 _boardVisualBasePosition;
	private Vector2 _boardVisualBaseScale;
	private Vector2 _previousBoardContactPoint;
	private TrickKind _activeTrick;
	private TrickPhase _activeTrickPhase;
	private TrickKind _queuedTrick;
	private bool _flipQueueReady = true;
	private bool _grabQueueReady = true;
	private bool _jumpGrabQueueReady = true;
	private bool _altFlipQueueReady = true;
	private bool _slot4QueueReady = true;
	private float _trickElapsed;
	private float _trickRotationOffset;
	private float _trickAngleX;
	private float _trickAngleY;
	private Vector2 _trickSquash = Vector2.One;
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
		AddToGroup("player");
		EnsureGrindInput();
		_boardContact = GetNode<Marker2D>("BoardContact");
		_visualContainer = GetNode<Node2D>("VisualContainer");
		_boardVisual = GetNode<Sprite2D>("VisualContainer/BoardSprite");
		_visual = GetNode<Sprite2D>("VisualContainer/PlayerSprite");
		_baseColor = _visual.SelfModulate;
		_boardVisualBasePosition = _boardVisual.Position;
		_boardVisualBaseScale = _boardVisual.Scale;
		_airRotation = Rotation;
		_previousBoardContactPoint = GetRailContactPoint();
		CurrentHealth = MaxHealth;
		CreateBalanceIndicator();
		FloorSnapLength = FloorSnapDistance;
		FloorMaxAngle = Mathf.DegToRad(FloorMaxAngleDegrees);
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

		if (TryStartBufferedGrinding(previousBoardContactPoint, GetRailContactPoint(), ref velocity, inputDirection, deltaSeconds, gravity, wasOnFloor))
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

		if (_activeRail == null && TryStartBufferedGrinding(previousBoardContactPoint, GetRailContactPoint(), ref velocity, inputDirection, deltaSeconds, gravity, wasOnFloor))
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

	public bool GodMode { get; set; }

	public void TakeDamage(int amount)
	{
		if (amount <= 0 || IsDead || _invulnerabilityTimeRemaining > 0.0f)
			return;

		if (GodMode)
		{
			_invulnerabilityTimeRemaining = InvulnerabilityDuration;
			return;
		}

		if (_railArmorTimeRemaining > 0.0f)
			amount = Mathf.Max(0, amount - 1);

		if (amount <= 0)
			return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		_invulnerabilityTimeRemaining = InvulnerabilityDuration;

		if (CurrentHealth <= 0)
		{
			Velocity = Vector2.Zero;
			_visual.SelfModulate = new Color(0.35f, 0.35f, 0.35f, 1.0f);
		}
	}

	private void UpdateDamageFlash()
	{
		if (_invulnerabilityTimeRemaining > 0.0f)
		{
			var flashOn = Mathf.PosMod(Time.GetTicksMsec() / 100, 2) == 0;
			_visual.SelfModulate = flashOn ? new Color(1.0f, 0.45f, 0.45f, 1.0f) : _baseColor;
			return;
		}

		_visual.SelfModulate = _baseColor;
	}
}
