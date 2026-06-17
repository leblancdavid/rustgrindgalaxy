using Godot;

public partial class PlayerController : CharacterBody2D
{
	private void ApplyHorizontalMovement(ref Vector2 velocity, float inputDirection, float deltaSeconds, bool onFloor, float gravity)
	{
		if (_pendingBoostImpulse != Vector2.Zero)
		{
			velocity += _pendingBoostImpulse;
			_pendingBoostImpulse = Vector2.Zero;
		}

		float slopeAcceleration = 0.0f;

		if (onFloor)
		{
			var floorNormal = GetFloorNormal();
			var floorTangent = GetSlopeTangent(floorNormal);
			slopeAcceleration = floorTangent.Dot(Vector2.Down) * gravity * (SlopeGravityStrength / gravity);

			// Cancel slope gravity when inputting uphill so steep ramps are climbable
			if (Mathf.IsZeroApprox(inputDirection) || slopeAcceleration * inputDirection >= 0.0f)
			{
				velocity.X += slopeAcceleration * deltaSeconds;
			}

			// Ramp adhesion: pushes player toward floor surface to maintain contact
			// at high speeds on steep slopes. Only vertical component is used
			// to avoid altering horizontal speed (which would speed up/slow down
			// the player on slopes regardless of travel direction).
			var tangentSpeed = Mathf.Abs(velocity.Dot(floorTangent));
			var steepness = Mathf.Abs(floorTangent.Dot(Vector2.Down));
			velocity.Y += -floorNormal.Y * tangentSpeed * steepness * RampAdhesionFactor * deltaSeconds;
		}

		if (Mathf.IsZeroApprox(inputDirection))
		{
			if (onFloor)
			{
				velocity.X = Mathf.MoveToward(velocity.X, 0.0f, CoastDeceleration * deltaSeconds);
			}

			return;
		}

		var effectiveSpeed = HasSpeedBoost ? MoveSpeed * _boostMultiplier : MoveSpeed;
		var targetSpeed = inputDirection * effectiveSpeed;
		var acceleration = onFloor ? GetBoostAcceleration(GroundAcceleration) : GetBoostAcceleration(AirAcceleration);

		if (Mathf.Abs(velocity.X) > Mathf.Abs(targetSpeed) && Mathf.Sign(velocity.X) == Mathf.Sign(inputDirection))
		{
			if (onFloor && slopeAcceleration * inputDirection > 0.0f)
			{
				return;
			}

			acceleration = CoastDeceleration * 0.5f;
		}

		velocity.X = Mathf.MoveToward(velocity.X, targetSpeed, acceleration * deltaSeconds);
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
		_airRotation = _visualContainer.Rotation;
	}

	private void UpdateGroundRotationState()
	{
		var floorTangent = GetSlopeTangent(GetFloorNormal());
		_groundTilt = floorTangent.Angle();
		_airRotation = _groundTilt;
		_railRotationOffset = 0.0f;
		ClearFailedLandingState();
	}
}
