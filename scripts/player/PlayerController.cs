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
	[Export] public float RailFriction = 12.0f;
	[Export] public float RailGravityStrength = 900.0f;
	[Export] public float GrindIntentSeconds = 1.0f;
	[Export] public float TravelIntentMemorySeconds = 0.15f;
	[Export] public float RailAttachCooldownSeconds = 0.18f;
	[Export] public float MaxRailSpeed = 420.0f;
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
	private float _groundTilt;
	private float _grindIntentTimeRemaining;
	private float _railProgress;
	private float _railSpeed;
	private float _lastTravelDirection;
	private float _travelIntentTimeRemaining;
	private bool _isChargingJump;
	private Marker2D _boardContact = null!;
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
		_boardContact = GetNode<Marker2D>("BoardContact");
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

		if (_activeRail != null)
		{
			var activeRailAngle = _activeRail.Angle;
			HandleGrinding(ref velocity, inputDirection, deltaSeconds, gravity);
			Velocity = velocity;
			MoveAndSlide();
			UpdateVisualRotation(deltaSeconds, _activeRail?.Angle ?? activeRailAngle);
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

		if (TryStartBufferedGrinding(ref velocity, inputDirection, deltaSeconds, gravity))
		{
			var activeRailAngle = _activeRail?.Angle ?? GetTargetRotation();
			Velocity = velocity;
			MoveAndSlide();
			UpdateVisualRotation(deltaSeconds, activeRailAngle);
			return;
		}

		if (TryReleaseJump(ref velocity, inputDirection, wasOnFloor, false))
		{
			Velocity = velocity;
			MoveAndSlide();
			return;
		}

		ApplyHorizontalMovement(ref velocity, inputDirection, deltaSeconds, wasOnFloor, gravity);

		Velocity = velocity;
		MoveAndSlide();
		UpdateVisualRotation(deltaSeconds, GetTargetRotation());
	}

	private bool CanStartGrinding(float inputDirection, float currentVelocityX)
	{
		if (_nearbyRail == null || _grindIntentTimeRemaining <= 0.0f || _railAttachCooldownRemaining > 0.0f)
		{
			return false;
		}

		if (_isChargingJump)
		{
			return false;
		}

		var contactPoint = GetRailContactPoint();
		if (_nearbyRail.TryGetSnapProgress(contactPoint, out _) == false)
		{
			return false;
		}

		var requestedDirection = GetRequestedDirection(inputDirection, currentVelocityX);
		if (Mathf.IsZeroApprox(requestedDirection))
		{
			return false;
		}

		return true;
	}

	private void EnterRail(GrindRail rail, float travelDirection)
	{
		_activeRail = rail;
		_grindIntentTimeRemaining = 0.0f;
		_grindDirection = Mathf.Sign(travelDirection);

		if (Mathf.IsZeroApprox(_grindDirection))
		{
			_grindDirection = 1.0f;
		}

		_railArmorTimeRemaining = Mathf.Max(_railArmorTimeRemaining, _resolvedEffects.RailEntryArmorSeconds);
		_railProgress = rail.GetProgressAtPoint(GetRailContactPoint());
		var railNormal = new Vector2(-rail.Tangent.Y, rail.Tangent.X);
		_railSpeed = Mathf.Abs(Velocity.Dot(railNormal));

		if (_railSpeed < rail.GetSpeed(_resolvedEffects.RailSpeedBonus))
		{
			_railSpeed = rail.GetSpeed(_resolvedEffects.RailSpeedBonus);
		}

		_railSpeed *= _grindDirection;
		var boardOffset = _boardContact.Position.Rotated(Rotation);
		GlobalPosition = rail.GetPointAtProgress(_railProgress) - boardOffset;
		Rotation = rail.Angle;
		Velocity = rail.Tangent * _railSpeed;
	}

	private void ExitRail()
	{
		_activeRail = null;
		_railSpeed = 0.0f;
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

	private void HandleGrinding(ref Vector2 velocity, float inputDirection, float deltaSeconds, float gravity)
	{
		var rail = _activeRail!;

		if (TryReleaseJump(ref velocity, inputDirection, false, true))
		{
			return;
		}

		if (Mathf.IsZeroApprox(_grindDirection))
		{
			_grindDirection = 1.0f;
		}

		var downhillAcceleration = rail.Tangent.Dot(Vector2.Down) * gravity * (RailGravityStrength / gravity);
		_railSpeed += downhillAcceleration * deltaSeconds;
		_railSpeed = Mathf.MoveToward(_railSpeed, 0.0f, RailFriction * deltaSeconds);

		_railSpeed = Mathf.Clamp(_railSpeed, -MaxRailSpeed, MaxRailSpeed);
		_railProgress += (_railSpeed * deltaSeconds) / Mathf.Max(rail.Length, 0.001f);

		if (_railProgress <= 0.0f || _railProgress >= 1.0f)
		{
			_railProgress = Mathf.Clamp(_railProgress, 0.0f, 1.0f);
			var boardOffset = _boardContact.Position.Rotated(Rotation);
			GlobalPosition = rail.GetPointAtProgress(_railProgress) - boardOffset;
			velocity = rail.Tangent * _railSpeed;
			ExitRail();
			return;
		}

		var currentBoardOffset = _boardContact.Position.Rotated(Rotation);
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
			_groundTilt = floorTangent.Angle();
		}
		else
		{
			_groundTilt = 0.0f;
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
		if (Input.IsActionJustPressed("ui_accept") && (onFloor || onRail))
		{
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
			velocity.Y = chargedJumpVelocity - _resolvedEffects.LaunchHeightBonus;
			var launchVelocity = tangent * railSpeed;
			launchVelocity += tangent * (_grindDirection * _resolvedEffects.BurstTakeoffSpeedBonus);
			velocity.X = launchVelocity.X;
			velocity.Y += launchVelocity.Y;
			ExitRail();
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
		if (Input.IsActionJustPressed(GrindAction))
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

	private bool TryStartBufferedGrinding(ref Vector2 velocity, float inputDirection, float deltaSeconds, float gravity)
	{
		if (CanStartGrinding(inputDirection, velocity.X) == false)
		{
			return false;
		}

		EnterRail(_nearbyRail!, GetRequestedDirection(inputDirection, velocity.X));
		HandleGrinding(ref velocity, inputDirection, deltaSeconds, gravity);
		return true;
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

	private float GetTargetRotation()
	{
		if (_activeRail != null)
		{
			return _activeRail.Angle;
		}

		if (IsOnFloor())
		{
			return _groundTilt;
		}

		return 0.0f;
	}

	private void UpdateVisualRotation(float deltaSeconds, float targetRotation)
	{
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
	}
}
