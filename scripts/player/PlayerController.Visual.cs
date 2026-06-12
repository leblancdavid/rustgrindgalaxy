using Godot;

public partial class PlayerController : CharacterBody2D
{
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
			var tiltDirection = Mathf.Sign(Velocity.X);

			if (Mathf.IsZeroApprox(tiltDirection))
			{
				tiltDirection = Mathf.Sign(_grindDirection);
			}

			_grindBobTime += deltaSeconds * GrindBobSpeed;
			var grindBobWave = Mathf.Sin(_grindBobTime);
			var grindLean = Mathf.DegToRad(GrindBoardTiltDegrees) * tiltDirection * visualStrength;
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
		if (_activeRail != null)
		{
			_visualContainer.Rotation = 0.0f;
			return;
		}

		if (IsOnFloor() == false)
		{
			_visualContainer.Rotation = targetRotation;
			return;
		}

		_visualContainer.Rotation = Mathf.LerpAngle(_visualContainer.Rotation, targetRotation, Mathf.Clamp(RotationLerpSpeed * deltaSeconds, 0.0f, 1.0f));
	}
}
