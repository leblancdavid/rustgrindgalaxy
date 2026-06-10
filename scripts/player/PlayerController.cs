using System.Collections.Generic;
using System.Text;
using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export] public float MoveSpeed = 150.0f;
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
	// Rotation now uses left/right arrows directly via inputDirection
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

	private void CreateBalanceIndicator()
	{
		_balanceIndicator = new Node2D();
		_balanceIndicator.Name = "BalanceIndicator";
		_balanceIndicator.Position = new Vector2(0.0f, BalanceIndicatorY);
		_balanceIndicator.Visible = false;
		AddChild(_balanceIndicator);

		var bar = new Polygon2D();
		bar.Name = "Bar";
		var halfW = BalanceIndicatorWidth * 0.5f;
		var halfH = BalanceIndicatorHeight * 0.5f;
		bar.Polygon = new Vector2[]
		{
			new Vector2(-halfW, -halfH),
			new Vector2(halfW, -halfH),
			new Vector2(halfW, halfH),
			new Vector2(-halfW, halfH),
		};
		bar.Color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
		bar.ZIndex = 2;
		_balanceIndicator.AddChild(bar);

		var centerMark = new Polygon2D();
		centerMark.Name = "CenterMark";
		centerMark.Polygon = new Vector2[]
		{
			new Vector2(-0.5f, -halfH),
			new Vector2(0.5f, -halfH),
			new Vector2(0.5f, halfH),
			new Vector2(-0.5f, halfH),
		};
		centerMark.Color = new Color(0.6f, 0.6f, 0.6f, 0.9f);
		centerMark.ZIndex = 3;
		_balanceIndicator.AddChild(centerMark);

		_balanceArrow = new Polygon2D();
		_balanceArrow.Name = "Arrow";
		_balanceArrow.Polygon = new Vector2[]
		{
			new Vector2(0.0f, halfH + 2.0f),
			new Vector2(-2.5f, -halfH),
			new Vector2(2.5f, -halfH),
		};
		_balanceArrow.Color = new Color(0.96f, 0.81f, 0.30f, 1.0f);
		_balanceArrow.ZIndex = 3;
		_balanceIndicator.AddChild(_balanceArrow);
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

	private bool CanStartGrinding()
	{
		var grindHeld = Input.IsActionPressed(GrindAction);
		var grindBuffered = _grindIntentTimeRemaining > 0.0f;

		if ((!grindHeld && !grindBuffered) || _railAttachCooldownRemaining > 0.0f)
		{
			return false;
		}

		if (HasActiveTrick())
		{
			return false;
		}

		if (_isChargingJump)
		{
			return false;
		}

		return true;
	}

	private void EnterRail(GrindRail rail, float travelDirection, float railProgress)
	{
		_activeRail = rail;
		ClearQueuedTrick();
		RegisterCompletedTrickName(GetInstalledTrickName(ModuleType.Grind));
		_grindIntentTimeRemaining = 0.0f;
		_balanceValue = 0.0f;
		_balanceDriftTarget = (float)GD.RandRange(-0.6, 0.6);
		_balanceDriftTimer = BalanceDriftChangeInterval * (float)GD.RandRange(0.5f, 1.5f);
		if (_balanceIndicator != null)
		{
			_balanceIndicator.Visible = true;
			_balanceArrow.Position = new Vector2(0.0f, 0.0f);
		}
		_grindDirection = Mathf.Sign(travelDirection);

		if (Mathf.IsZeroApprox(_grindDirection))
		{
			_grindDirection = 1.0f;
		}

		_railArmorTimeRemaining = Mathf.Max(_railArmorTimeRemaining, _resolvedEffects.RailEntryArmorSeconds);
		_railProgress = Mathf.Clamp(railProgress, 0.0f, 1.0f);
		_railRotationOffset = GetAngleDifference(rail.Angle, GetBoardAngle());
		var tangentSpeed = Velocity.Dot(rail.Tangent);

		if (!Mathf.IsZeroApprox(tangentSpeed))
		{
			_grindDirection = Mathf.Sign(tangentSpeed);
		}

		_railSpeed = Mathf.Abs(tangentSpeed);

		if (_railSpeed < MinimumRailEntrySpeed)
		{
			_railSpeed = MinimumRailEntrySpeed;
		}

		_railSpeed *= _grindDirection;
		var boardRotation = GetRailBoardAngle(rail);
		var boardOffset = _boardContact.Position.Rotated(boardRotation);
		GlobalPosition = rail.GetPointAtProgress(_railProgress) - boardOffset;
		Rotation = boardRotation;
		Velocity = rail.Tangent * _railSpeed;
	}

	private void ExitRail()
	{
		_activeRail = null;
		_railSpeed = 0.0f;
		_airRotation = GetBoardAngle();
		_railAttachCooldownRemaining = RailAttachCooldownSeconds;
		_balanceValue = 0.0f;
		_balanceDriftTarget = 0.0f;
		_balanceDriftTimer = 0.0f;
		if (_balanceIndicator != null)
		{
			_balanceIndicator.Visible = false;
		}
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

	private void HandleGrinding(ref Vector2 velocity, float inputDirection, float deltaSeconds, float gravity)
	{
		var rail = _activeRail!;

		if (TryReleaseJump(ref velocity, inputDirection, false, true))
		{
			return;
		}

		UpdateGrindBalance(deltaSeconds, inputDirection);

		if (_balanceIndicator.Visible && (Mathf.Abs(_balanceValue) >= BalanceMaxOffset - 0.001f))
		{
			FailGrindBalance(ref velocity, rail);
			return;
		}

		if (Mathf.IsZeroApprox(_grindDirection))
		{
			_grindDirection = 1.0f;
		}

		if (Mathf.Abs(_railRotationOffset) > GetLandingToleranceRadians())
		{
			ExitRail();
			velocity = rail.Tangent * _railSpeed;
			return;
		}

		var downhillAcceleration = rail.Tangent.Dot(Vector2.Down) * gravity * (RailGravityStrength / gravity);
		_railSpeed += downhillAcceleration * deltaSeconds;
		_railSpeed = Mathf.MoveToward(_railSpeed, 0.0f, RailFriction * deltaSeconds);
		_railSpeed += _balanceValue * BalancePhysicsForce * deltaSeconds;
		_railSpeed = Mathf.Clamp(_railSpeed, -MaxRailSpeed, MaxRailSpeed);
		_railProgress += (_railSpeed * deltaSeconds) / Mathf.Max(rail.Length, 0.001f);

		if (_railProgress <= 0.0f || _railProgress >= 1.0f)
		{
			_railProgress = Mathf.Clamp(_railProgress, 0.0f, 1.0f);
			var boardRotation = GetRailBoardAngle(rail);
			var boardOffset = _boardContact.Position.Rotated(boardRotation);
			GlobalPosition = rail.GetPointAtProgress(_railProgress) - boardOffset;
			velocity = rail.Tangent * _railSpeed;
			ExitRail();
			return;
		}

		var currentBoardRotation = GetRailBoardAngle(rail);
		var currentBoardOffset = _boardContact.Position.Rotated(currentBoardRotation);
		GlobalPosition = rail.GetPointAtProgress(_railProgress) - currentBoardOffset;
		velocity = rail.Tangent * _railSpeed;
	}

	private void ApplyHorizontalMovement(ref Vector2 velocity, float inputDirection, float deltaSeconds, bool onFloor, float gravity)
	{
		if (onFloor)
		{
			var floorNormal = GetFloorNormal();
			var floorTangent = GetSlopeTangent(floorNormal);
			var slopeAcceleration = floorTangent.Dot(Vector2.Down) * gravity * (SlopeGravityStrength / gravity);
			velocity.X += slopeAcceleration * deltaSeconds;
		}

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
		if (HasActiveTrick())
		{
			CancelJumpCharge();
			return;
		}

		if (Input.IsActionJustPressed("ui_accept") && (onFloor || onRail))
		{
			ClearQueuedTrick();
			_grindIntentTimeRemaining = 0.0f;
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
			var rail = _activeRail;
			var railSpeed = _railSpeed;
			var tangent = rail?.Tangent ?? new Vector2(_grindDirection, 0.0f);
			RegisterCompletedTrickName(GetInstalledTrickName(ModuleType.Ollie));
			velocity.Y = chargedJumpVelocity - _resolvedEffects.LaunchHeightBonus;
			var launchVelocity = tangent * railSpeed;
			launchVelocity += tangent * (_grindDirection * _resolvedEffects.BurstTakeoffSpeedBonus);
			velocity.X = launchVelocity.X;
			velocity.Y += launchVelocity.Y;
			StartOllieTakeoffTilt(Mathf.Sign(launchVelocity.X));
			ExitRail();
			return true;
		}

		if (onFloor == false)
		{
			return false;
		}

		RegisterCompletedTrickName(GetInstalledTrickName(ModuleType.Ollie));
		velocity.Y = chargedJumpVelocity - _resolvedEffects.LaunchHeightBonus;
		velocity.X = ApplyTakeoffBonus(velocity.X, inputDirection);
		StartOllieTakeoffTilt(GetRequestedDirection(inputDirection, velocity.X));
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

		if (_travelIntentTimeRemaining > 0.0f && !Mathf.IsZeroApprox(_lastTravelDirection))
		{
			return _lastTravelDirection;
		}

		return 0.0f;
	}

	private void CancelJumpCharge()
	{
		_isChargingJump = false;
		_jumpChargeTime = 0.0f;
	}

	private void UpdateGrindIntent()
	{
		if (Input.IsActionJustReleased(GrindAction))
		{
			_grindIntentTimeRemaining = GrindIntentSeconds;
		}
	}

	private void UpdateTravelIntent(float inputDirection, float currentVelocityX)
	{
		if (!Mathf.IsZeroApprox(inputDirection))
		{
			_lastTravelDirection = Mathf.Sign(inputDirection);
			_travelIntentTimeRemaining = TravelIntentMemorySeconds;
			return;
		}

		if (Mathf.Abs(currentVelocityX) >= 20.0f)
		{
			_lastTravelDirection = Mathf.Sign(currentVelocityX);
			_travelIntentTimeRemaining = TravelIntentMemorySeconds;
		}
	}

	private bool TryStartBufferedGrinding(Vector2 fromBoardContactPoint, Vector2 toBoardContactPoint, ref Vector2 velocity, float inputDirection, float deltaSeconds, float gravity)
	{
		if (CanStartGrinding() == false)
		{
			return false;
		}

		if (TryFindGrindRail(fromBoardContactPoint, toBoardContactPoint, out var rail, out var railProgress) == false)
		{
			return false;
		}

		if (IsWithinLandingTolerance(rail!.Angle) == false)
		{
			return false;
		}

		EnterRail(rail!, ResolveGrindDirection(rail!, inputDirection, velocity), railProgress);
		HandleGrinding(ref velocity, inputDirection, deltaSeconds, gravity);
		return true;
	}

	private bool TryFindGrindRail(Vector2 fromBoardContactPoint, Vector2 toBoardContactPoint, out GrindRail? rail, out float railProgress)
	{
		rail = null;
		railProgress = 0.0f;

		if (_nearbyRail != null && _nearbyRail.TryGetSweepSnap(fromBoardContactPoint, toBoardContactPoint, out railProgress))
		{
			rail = _nearbyRail;
			return true;
		}

		foreach (var node in GetTree().GetNodesInGroup(GrindRail.RailGroupName))
		{
			if (node is not GrindRail candidate || candidate == _nearbyRail)
			{
				continue;
			}

			if (candidate.TryGetSweepSnap(fromBoardContactPoint, toBoardContactPoint, out railProgress))
			{
				rail = candidate;
				return true;
			}
		}

		return false;
	}

	private float ResolveGrindDirection(GrindRail rail, float inputDirection, Vector2 currentVelocity)
	{
		var requestedDirection = GetRequestedDirection(inputDirection, currentVelocity.X);

		if (!Mathf.IsZeroApprox(requestedDirection))
		{
			return requestedDirection;
		}

		var tangentVelocity = currentVelocity.Dot(rail.Tangent);
		if (!Mathf.IsZeroApprox(tangentVelocity))
		{
			return Mathf.Sign(tangentVelocity);
		}

		var downhillDirection = rail.GetDownhillSign();
		if (!Mathf.IsZeroApprox(downhillDirection))
		{
			return downhillDirection;
		}

		return 1.0f;
	}

	private Vector2 GetRailContactPoint()
	{
		return _boardContact.GlobalPosition;
	}

	private Vector2 GetSlopeTangent(Vector2 floorNormal)
	{
		var tangent = new Vector2(floorNormal.Y, -floorNormal.X).Normalized();
		return tangent.X < 0.0f ? -tangent : tangent;
	}

	private void UpdateRotationInput(float rotationInput, float deltaSeconds, bool wasOnFloor)
	{
		var rotationStep = Mathf.DegToRad(RotationSpeedDegrees) * rotationInput * deltaSeconds;

		if (_activeRail != null)
		{
			return;
		}

		if (wasOnFloor)
		{
			_railRotationOffset = 0.0f;
			return;
		}

		float currentDirection = rotationInput > 0.0f ? 1.0f : (rotationInput < 0.0f ? -1.0f : 0.0f);
		if (currentDirection != 0.0f && currentDirection == _airRotationRampDirection)
		{
			_airRotationRamp = Mathf.Min(_airRotationRamp + deltaSeconds / AirRotationRampUpTime, 1.0f);
		}
		else
		{
			_airRotationRamp = 0.0f;
		}
		_airRotationRampDirection = currentDirection;

		_airRotation = NormalizeAngle(_airRotation + rotationStep * _airRotationRamp);
	}

	private bool RejectInvalidLanding(bool wasOnFloor, ref Vector2 velocity)
	{
		if (wasOnFloor || IsOnFloor() == false)
		{
			return false;
		}

		if (HasActiveTrick())
		{
			CancelActiveTrick();
			FailCurrentCombo();
			ApplyFailedLanding(ref velocity, GetBoardAngleDifferenceForSurface(GetFloorNormal()));
			return true;
		}

		var floorAngle = GetSlopeTangent(GetFloorNormal()).Angle();

		if (IsWithinLandingTolerance(floorAngle))
		{
			FinalizeSuccessfulLandingCombo();
			ClearFailedLandingState();
			return false;
		}

		FailCurrentCombo();
		ApplyFailedLanding(ref velocity, GetAngleDifference(floorAngle, GetBoardAngle()));
		return true;
	}

	private void UpdateTrickState(float deltaSeconds, bool wasOnFloor)
	{
		UpdateTrickQueueRearmer();
		CaptureQueuedTrickInput(wasOnFloor);

		if (HasActiveTrick())
		{
			UpdateActiveTrick(deltaSeconds);

			if (HasActiveTrick() == false)
			{
				TryStartQueuedTrick(wasOnFloor);
			}

			ApplyTrickVisual();
			return;
		}

		if (wasOnFloor || _activeRail != null || _isChargingJump)
		{
			if (_isChargingJump == false)
			{
				ClearQueuedTrick();
			}

			_trickRotationOffset = 0.0f;
			ApplyTrickVisual();
			return;
		}

		if (TryStartQueuedTrick(wasOnFloor))
		{
			ApplyTrickVisual();
			return;
		}

		if (Input.IsActionJustPressed(TrickFlipAction))
		{
			StartFlipTrick(TrickKind.Flip);
		}
		else if (Input.IsActionJustPressed(TrickGrabAction) || Input.IsActionJustPressed("ui_accept"))
		{
			StartGrabTrick();
		}
		else if (Input.IsActionJustPressed(TrickAltFlipAction))
		{
			StartFlipTrick(TrickKind.AltFlip);
		}

		ApplyTrickVisual();
	}

	private void UpdateActiveTrick(float deltaSeconds)
	{
		_trickElapsed += deltaSeconds;

		switch (_activeTrick)
		{
			case TrickKind.Flip:
			case TrickKind.AltFlip:
			{
				var duration = _activeTrick == TrickKind.Flip ? FlipDurationSeconds : AltFlipDurationSeconds;
				var progress = Mathf.Clamp(_trickElapsed / duration, 0.0f, 1.0f);
				_trickRotationOffset = progress * Mathf.Tau;

				if (progress >= 1.0f)
				{
					CompleteActiveTrick();
				}

				break;
			}

			case TrickKind.Grab:
			{
				switch (_activeTrickPhase)
				{
					case TrickPhase.Startup:
					{
						var progress = Mathf.Clamp(_trickElapsed / GrabSetupDurationSeconds, 0.0f, 1.0f);
						_trickRotationOffset = Mathf.LerpAngle(0.0f, GrabHoldAngleRadians, progress);

					if (progress >= 1.0f)
					{
						_activeTrickPhase = IsGrabInputHeld() ? TrickPhase.Active : TrickPhase.Recovery;
						_trickRecoveryStartRotation = _trickRotationOffset;
						_trickElapsed = 0.0f;
					}

						break;
					}

				case TrickPhase.Active:
					_trickRotationOffset = GrabHoldAngleRadians;

					if (IsGrabInputHeld() == false)
					{
						_activeTrickPhase = TrickPhase.Recovery;
						_trickRecoveryStartRotation = _trickRotationOffset;
							_trickElapsed = 0.0f;
						}

						break;

					case TrickPhase.Recovery:
					{
						var progress = Mathf.Clamp(_trickElapsed / GrabReleaseDurationSeconds, 0.0f, 1.0f);
						_trickRotationOffset = Mathf.LerpAngle(_trickRecoveryStartRotation, 0.0f, progress);

						if (progress >= 1.0f)
						{
							CompleteActiveTrick();
						}

						break;
					}
				}

				break;
			}
		}
	}

	private void StartFlipTrick(TrickKind trick)
	{
		ClearQueuedTrick();
		ConsumeStartedTrickInput(trick);
		_activeTrick = trick;
		_activeTrickPhase = TrickPhase.Active;
		_trickElapsed = 0.0f;
		_trickRotationOffset = 0.0f;
		_trickRecoveryStartRotation = 0.0f;
		PublishTrickStart(GetInstalledTrickName(ModuleType.Flip));
	}

	private void StartGrabTrick()
	{
		ClearQueuedTrick();
		ConsumeStartedTrickInput(TrickKind.Grab);
		_activeTrick = TrickKind.Grab;
		_activeTrickPhase = TrickPhase.Startup;
		_trickElapsed = 0.0f;
		_trickRotationOffset = 0.0f;
		_trickRecoveryStartRotation = 0.0f;
		PublishTrickStart(GetInstalledTrickName(ModuleType.Grab));
	}

	private bool HasActiveTrick()
	{
		return _activeTrick != TrickKind.None;
	}

	private void CompleteActiveTrick()
	{
		RegisterCompletedTrick(_activeTrick);
		_activeTrick = TrickKind.None;
		_activeTrickPhase = TrickPhase.None;
		_trickElapsed = 0.0f;
		_trickRotationOffset = 0.0f;
		_trickRecoveryStartRotation = 0.0f;
		ApplyTrickVisual();
	}

	private void CancelActiveTrick()
	{
		_activeTrick = TrickKind.None;
		_activeTrickPhase = TrickPhase.None;
		_trickElapsed = 0.0f;
		_trickRotationOffset = 0.0f;
		_trickRecoveryStartRotation = 0.0f;
		ApplyTrickVisual();
	}

	private void CaptureQueuedTrickInput(bool wasOnFloor)
	{
		if (_isChargingJump)
		{
			if (TryQueueHeldTrickInput(TrickFlipAction, ref _flipQueueReady, TrickKind.Flip))
			{
				return;
			}

			if (TryQueueHeldTrickInput(TrickGrabAction, ref _grabQueueReady, TrickKind.Grab))
			{
				return;
			}

			if (TryQueueHeldTrickInput(TrickAltFlipAction, ref _altFlipQueueReady, TrickKind.AltFlip))
			{
				return;
			}

			return;
		}

		if (wasOnFloor || _activeRail != null || HasActiveTrick() == false)
		{
			return;
		}

		if (TryQueuePressedTrickInput(TrickFlipAction, ref _flipQueueReady, TrickKind.Flip))
		{
			return;
		}

		if (TryQueuePressedTrickInput(TrickAltFlipAction, ref _altFlipQueueReady, TrickKind.AltFlip))
		{
			return;
		}

		if (TryQueuePressedTrickInput(TrickGrabAction, ref _grabQueueReady, TrickKind.Grab) || TryQueuePressedTrickInput("ui_accept", ref _jumpGrabQueueReady, TrickKind.Grab))
		{
			return;
		}

		if (TryQueueHeldTrickInput(TrickFlipAction, ref _flipQueueReady, TrickKind.Flip))
		{
			return;
		}

		if (TryQueueHeldTrickInput(TrickAltFlipAction, ref _altFlipQueueReady, TrickKind.AltFlip))
		{
			return;
		}
	}

	private bool TryQueueHeldTrickInput(string actionName, ref bool queueReady, TrickKind trick)
	{
		if (queueReady == false)
		{
			return false;
		}

		if (Input.IsActionJustPressed(actionName) || Input.IsActionPressed(actionName))
		{
			_queuedTrick = trick;
			queueReady = false;
			return true;
		}

		return false;
	}

	private bool TryQueuePressedTrickInput(string actionName, ref bool queueReady, TrickKind trick)
	{
		if (queueReady == false || Input.IsActionJustPressed(actionName) == false)
		{
			return false;
		}

		_queuedTrick = trick;
		queueReady = false;
		return true;
	}

	private bool TryStartQueuedTrick(bool wasOnFloor)
	{
		if (_queuedTrick == TrickKind.None || wasOnFloor || _activeRail != null || _isChargingJump)
		{
			return false;
		}

		switch (_queuedTrick)
		{
			case TrickKind.Flip:
				StartFlipTrick(TrickKind.Flip);
				return true;

			case TrickKind.Grab:
				StartGrabTrick();
				return true;

			case TrickKind.AltFlip:
				StartFlipTrick(TrickKind.AltFlip);
				return true;
		}

		return false;
	}

	private bool IsGrabInputHeld()
	{
		return Input.IsActionPressed(TrickGrabAction) || Input.IsActionPressed("ui_accept");
	}

	private void ConsumeStartedTrickInput(TrickKind trick)
	{
		switch (trick)
		{
			case TrickKind.Flip:
				if (Input.IsActionPressed(TrickFlipAction))
				{
					_flipQueueReady = false;
				}

				break;

			case TrickKind.Grab:
				if (Input.IsActionPressed(TrickGrabAction))
				{
					_grabQueueReady = false;
				}

				if (Input.IsActionPressed("ui_accept"))
				{
					_jumpGrabQueueReady = false;
				}

				break;

			case TrickKind.AltFlip:
				if (Input.IsActionPressed(TrickAltFlipAction))
				{
					_altFlipQueueReady = false;
				}

				break;
		}
	}

	private void UpdateTrickQueueRearmer()
	{
		if (Input.IsActionPressed(TrickFlipAction) == false)
		{
			_flipQueueReady = true;
		}

		if (Input.IsActionPressed(TrickGrabAction) == false)
		{
			_grabQueueReady = true;
		}

		if (Input.IsActionPressed("ui_accept") == false)
		{
			_jumpGrabQueueReady = true;
		}

		if (Input.IsActionPressed(TrickAltFlipAction) == false)
		{
			_altFlipQueueReady = true;
		}
	}

	private void ClearQueuedTrick()
	{
		_queuedTrick = TrickKind.None;
	}

	private void ApplyTrickVisual()
	{
		var boardFallRotation = Mathf.DegToRad(FailedLandingBoardTiltDegrees) * _failedLandingDirection * _failedLandingVisualBlend;
		_boardVisual.Rotation = _trickRotationOffset + boardFallRotation + _boardAnimationTilt;
	}

	private void UpdateBoardAnimationTilt(float deltaSeconds)
	{
		_boardVisual.Position = _boardVisualBasePosition;
		_ollieTakeoffTilt = Mathf.MoveToward(
			_ollieTakeoffTilt,
			0.0f,
			Mathf.DegToRad(OllieTakeoffTiltDegrees) * OllieTiltRecoverSpeed * deltaSeconds);

		var targetTilt = _ollieTakeoffTilt;
		var responseSpeed = OllieTiltRecoverSpeed;

		if (_activeRail != null)
		{
			var speedRatio = Mathf.Clamp(Mathf.Abs(_railSpeed) / Mathf.Max(MaxRailSpeed, 1.0f), 0.0f, 1.0f);
			var visualStrength = Mathf.Lerp(GrindVisualMinimumStrength, 1.0f, speedRatio);
			float grindDirection = Mathf.Sign(_grindDirection);

			if (Mathf.IsZeroApprox(grindDirection))
			{
				grindDirection = GetVisualTravelDirection();
			}

			_grindBobTime += deltaSeconds * GrindBobSpeed;
			var grindBobWave = Mathf.Sin(_grindBobTime);
			var grindLean = Mathf.DegToRad(GrindBoardTiltDegrees) * grindDirection * visualStrength;
			var grindBob = Mathf.DegToRad(GrindBobDegrees) * visualStrength * grindBobWave;
			targetTilt = grindLean + grindBob;
			_boardVisual.Position = _boardVisualBasePosition + new Vector2(0.0f, grindBobWave * GrindBobOffsetPixels * visualStrength);
			responseSpeed = GrindTiltResponseSpeed;
		}
		else
		{
			_grindBobTime = 0.0f;

			if (_isChargingJump)
			{
				var chargeRatio = MaxJumpHoldTime <= 0.0f
					? 1.0f
					: Mathf.Clamp(_jumpChargeTime / MaxJumpHoldTime, 0.0f, 1.0f);
				targetTilt += -GetVisualTravelDirection() * Mathf.DegToRad(JumpChargeBoardTiltDegrees) * chargeRatio;
			}
		}

		_boardAnimationTilt = Mathf.LerpAngle(
			_boardAnimationTilt,
			targetTilt,
			Mathf.Clamp(responseSpeed * deltaSeconds, 0.0f, 1.0f));
	}

	private void StartOllieTakeoffTilt(float direction)
	{
		if (Mathf.IsZeroApprox(direction))
		{
			direction = GetVisualTravelDirection();
		}

		_boardAnimationTilt = -direction * Mathf.DegToRad(OllieTakeoffTiltDegrees);
		_ollieTakeoffTilt = _boardAnimationTilt;
	}

	private void UpdateGrindBalance(float deltaSeconds, float inputDirection)
	{
		_balanceDriftTimer -= deltaSeconds;

		if (_balanceDriftTimer <= 0.0f)
		{
			_balanceDriftTarget = (float)GD.RandRange(-1.0, 1.0);
			_balanceDriftTimer = BalanceDriftChangeInterval * (float)GD.RandRange(0.5f, 1.5f);
		}

		var drift = _balanceDriftTarget * BalanceDriftRate * deltaSeconds;
		var correction = inputDirection * BalanceCorrectionSpeed * deltaSeconds;

		_balanceValue += drift + correction;

		if (Mathf.IsZeroApprox(drift) && Mathf.IsZeroApprox(correction))
		{
			var recovery = -Mathf.Sign(_balanceValue) * BalanceRecoverySpeed * deltaSeconds;
			if (Mathf.Abs(recovery) >= Mathf.Abs(_balanceValue))
			{
				_balanceValue = 0.0f;
			}
			else
			{
				_balanceValue += recovery;
			}
		}

		_balanceValue = Mathf.Clamp(_balanceValue, -BalanceMaxOffset, BalanceMaxOffset);

		var halfW = BalanceIndicatorWidth * 0.5f;
		var arrowX = (_balanceValue / Mathf.Max(BalanceMaxOffset, 0.01f)) * halfW;
		_balanceArrow.Position = new Vector2(arrowX, 0.0f);

		var severity = Mathf.Abs(_balanceValue) / Mathf.Max(BalanceMaxOffset, 0.01f);
		_balanceArrow.Color = new Color(
			Mathf.Lerp(0.96f, 1.0f, severity),
			Mathf.Lerp(0.81f, 0.3f, severity),
			Mathf.Lerp(0.30f, 0.1f, severity),
			1.0f);
	}

	private void FailGrindBalance(ref Vector2 velocity, GrindRail rail)
	{
		float failureDirection = Mathf.Sign(_balanceValue);
		if (Mathf.IsZeroApprox(failureDirection))
		{
			failureDirection = 1.0f;
		}

		FailCurrentCombo();
		_isFailedLandingFalling = true;
		_failedLandingDirection = failureDirection;

		var railTangent = rail.Tangent;
		velocity = railTangent * _railSpeed;
		velocity += railTangent * (_balanceValue * BalancePhysicsForce);
		velocity += Vector2.Down * FailedLandingFallSpeed;
		_airRotation = Rotation;

		ExitRail();
	}

	private void PublishTrickStart(string trickName)
	{
		LastStartedTrickName = trickName;
		TrickStartSequence++;
	}

	private string GetInstalledTrickName(ModuleType moduleType)
	{
		return Loadout?.GetModule(moduleType).DisplayName ?? moduleType.ToString();
	}

	private void ApplyFailedLanding(ref Vector2 velocity, float landingAngleDifference)
	{
		var floorNormal = GetFloorNormal();
		var floorTangent = GetSlopeTangent(floorNormal);
		var tangentialSpeed = Velocity.Dot(floorTangent);
		var fallSpeed = Mathf.Max(Velocity.Dot(-floorNormal), FailedLandingFallSpeed);
		var failureDirection = Mathf.Sign(landingAngleDifference);

		if (Mathf.IsZeroApprox(failureDirection))
		{
			failureDirection = Mathf.Sign(Velocity.X);
		}

		if (Mathf.IsZeroApprox(failureDirection))
		{
			failureDirection = 1;
		}

		_isFailedLandingFalling = true;
		_failedLandingDirection = failureDirection;
		velocity = (floorTangent * tangentialSpeed) + (-floorNormal * fallSpeed);
		GlobalPosition += floorNormal * FailedLandingSeparation;
		_airRotation = Rotation;
	}

	private void UpdateGroundRotationState()
	{
		var floorTangent = GetSlopeTangent(GetFloorNormal());
		_groundTilt = floorTangent.Angle();
		_airRotation = _groundTilt;
		_railRotationOffset = 0.0f;
		ClearFailedLandingState();
	}

	private void UpdateFailedLandingVisual(float deltaSeconds)
	{
		var targetBlend = _isFailedLandingFalling ? 1.0f : 0.0f;
		_failedLandingVisualBlend = Mathf.MoveToward(_failedLandingVisualBlend, targetBlend, FailedLandingVisualRecoverSpeed * deltaSeconds);

		if (_isFailedLandingFalling == false && _failedLandingVisualBlend <= 0.0f)
		{
			_failedLandingDirection = 0.0f;
		}

		var balanceTilt = _activeRail != null
			? Mathf.DegToRad(BalanceVisualTiltDegrees) * (_balanceValue / Mathf.Max(BalanceMaxOffset, 0.01f))
			: 0.0f;
		_visual.Rotation = Mathf.DegToRad(FailedLandingBodyTiltDegrees) * _failedLandingDirection * _failedLandingVisualBlend + balanceTilt;
		ApplyTrickVisual();
	}

	private void RegisterCompletedTrick(TrickKind trick)
	{
		var trickName = GetCompletedTrickName(trick);
		RegisterCompletedTrickName(trickName);
	}

	private void RegisterCompletedTrickName(string trickName)
	{

		if (string.IsNullOrWhiteSpace(trickName))
		{
			return;
		}

		_comboTrickSequence.Add(trickName);
		CurrentComboSummary = BuildComboSummary(_comboTrickSequence);
	}

	private void FinalizeSuccessfulLandingCombo()
	{
		if (_comboTrickSequence.Count == 0)
		{
			return;
		}

		LastLandedComboSummary = CurrentComboSummary;
		LandedComboDisplayTimeRemaining = LandedComboDisplaySeconds;
		ClearCurrentCombo();
	}

	private void FailCurrentCombo()
	{
		ClearCurrentCombo();
		LastLandedComboSummary = string.Empty;
		LandedComboDisplayTimeRemaining = 0.0f;
	}

	private void ClearCurrentCombo()
	{
		_comboTrickSequence.Clear();
		CurrentComboSummary = string.Empty;
	}

	private void ClearFailedLandingState()
	{
		_isFailedLandingFalling = false;
	}

	private void ResetComboAndFallState()
	{
		ClearCurrentCombo();
		LastLandedComboSummary = string.Empty;
		LandedComboDisplayTimeRemaining = 0.0f;
		_isFailedLandingFalling = false;
		_failedLandingVisualBlend = 0.0f;
		_failedLandingDirection = 0.0f;
		_boardAnimationTilt = 0.0f;
		_ollieTakeoffTilt = 0.0f;
		_grindBobTime = 0.0f;
		_balanceValue = 0.0f;
		_balanceDriftTarget = 0.0f;
		_balanceDriftTimer = 0.0f;
		_airRotationRamp = 0.0f;
		_airRotationRampDirection = 0.0f;
		_boardVisual.Position = _boardVisualBasePosition;
		_visual.Rotation = 0.0f;
		if (_balanceIndicator != null)
		{
			_balanceIndicator.Visible = false;
		}
		ApplyTrickVisual();
	}

	public void ResetTransientState()
	{
		CancelActiveTrick();
		ClearQueuedTrick();
		CancelJumpCharge();
		ResetComboAndFallState();
	}

	private string GetCompletedTrickName(TrickKind trick)
	{
		return trick switch
		{
			TrickKind.Flip => GetInstalledTrickName(ModuleType.Flip),
			TrickKind.AltFlip => GetInstalledTrickName(ModuleType.Flip),
			TrickKind.Grab => GetInstalledTrickName(ModuleType.Grab),
			_ => string.Empty,
		};
	}

	private static string BuildComboSummary(IReadOnlyList<string> trickSequence)
	{
		if (trickSequence.Count == 0)
		{
			return string.Empty;
		}

		var orderedNames = new List<string>();
		var counts = new Dictionary<string, int>();
		foreach (var trickName in trickSequence)
		{
			if (counts.TryGetValue(trickName, out var count))
			{
				counts[trickName] = count + 1;
				continue;
			}

			counts[trickName] = 1;
			orderedNames.Add(trickName);
		}

		var summary = new StringBuilder();
		for (var i = 0; i < orderedNames.Count; i++)
		{
			var trickName = orderedNames[i];
			if (i > 0)
			{
				summary.Append(", ");
			}

			summary.Append(trickName);
			var count = counts[trickName];
			if (count > 1)
			{
				summary.Append(" x");
				summary.Append(count);
			}
		}

		return summary.ToString();
	}

	private float GetBoardAngleDifferenceForSurface(Vector2 floorNormal)
	{
		return GetAngleDifference(GetSlopeTangent(floorNormal).Angle(), GetBoardAngle());
	}

	private bool IsWithinLandingTolerance(float surfaceAngle)
	{
		return Mathf.Abs(GetAngleDifference(surfaceAngle, GetBoardAngle())) <= GetLandingToleranceRadians();
	}

	private float GetLandingToleranceRadians()
	{
		return Mathf.DegToRad(Mathf.Max(0.0f, LandingToleranceDegrees));
	}

	private static float GetAngleDifference(float targetAngle, float currentAngle)
	{
		return NormalizeAngle(currentAngle - targetAngle);
	}

	private static float NormalizeAngle(float angle)
	{
		return Mathf.PosMod(angle + Mathf.Pi, Mathf.Tau) - Mathf.Pi;
	}

	private float GetVisualTravelDirection()
	{
		if (_activeRail != null && !Mathf.IsZeroApprox(_grindDirection))
		{
			return Mathf.Sign(_grindDirection);
		}

		if (Mathf.Abs(Velocity.X) >= 5.0f)
		{
			return Mathf.Sign(Velocity.X);
		}

		if (_travelIntentTimeRemaining > 0.0f && !Mathf.IsZeroApprox(_lastTravelDirection))
		{
			return _lastTravelDirection;
		}

		return 1.0f;
	}

	private float GetBoardAngle()
	{
		if (_activeRail != null)
		{
			return GetRailBoardAngle(_activeRail);
		}

		if (IsOnFloor())
		{
			return _groundTilt;
		}

		return _airRotation;
	}

	private float GetRailBoardAngle(GrindRail rail)
	{
		return NormalizeAngle(rail.Angle + _railRotationOffset);
	}

	private float GetTargetRotation()
	{
		return GetBoardAngle();
	}

	private void UpdateVisualRotation(float deltaSeconds, float targetRotation)
	{
		if (_activeRail != null || IsOnFloor() == false)
		{
			Rotation = targetRotation;
			return;
		}

		Rotation = Mathf.LerpAngle(Rotation, targetRotation, Mathf.Clamp(RotationLerpSpeed * deltaSeconds, 0.0f, 1.0f));
	}

	private static void EnsureGrindInput()
	{
		if (InputMap.HasAction(GrindAction) == false)
		{
			InputMap.AddAction(GrindAction);
		}

		InputMap.ActionEraseEvents(GrindAction);
		InputMap.ActionAddEvent(GrindAction, new InputEventKey
		{
			Keycode = Key.Shift,
		});
		InputMap.ActionAddEvent(GrindAction, new InputEventKey
		{
			PhysicalKeycode = Key.Shift,
		});

		EnsureActionKeyBinding(TrickFlipAction, Key.Key1);
		EnsureActionKeyBinding(TrickGrabAction, Key.Key2);
		EnsureActionKeyBinding(TrickAltFlipAction, Key.Key3);
	}

	private static void EnsureActionKeyBinding(string actionName, Key key)
	{
		if (InputMap.HasAction(actionName) == false)
		{
			InputMap.AddAction(actionName);
		}

		InputMap.ActionEraseEvents(actionName);
		InputMap.ActionAddEvent(actionName, new InputEventKey
		{
			Keycode = key,
		});
		InputMap.ActionAddEvent(actionName, new InputEventKey
		{
			PhysicalKeycode = key,
		});
	}
}
