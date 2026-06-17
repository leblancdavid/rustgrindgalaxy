using Godot;

public partial class PlayerController : CharacterBody2D
{
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
		_railRotationOffset = 0.0f;
		_grindIntentTimeRemaining = 0.0f;
		_balanceValue = 0.0f;
		_balanceDriftWobble = 1.0f;
		_balanceDriftWobbleTimer = 0.1f;
		_balanceNoiseTimer = 0.0f;
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
		_visualContainer.Rotation = 0.0f;
		Velocity = rail.Tangent * _railSpeed;
	}

	private void ExitRail()
	{
		_airRotation = GetBoardAngle();
		_activeRail = null;
		_railSpeed = 0.0f;
		_railAttachCooldownRemaining = RailAttachCooldownSeconds;
		_railRotationOffset = 0.0f;
		_grindElapsedTime = 0.0f;
		_balanceValue = 0.0f;
		_balanceDriftWobble = 1.0f;
		_balanceDriftWobbleTimer = 0.0f;
		if (_balanceIndicator != null)
		{
			_balanceIndicator.Visible = false;
		}
		Rotation = 0.0f;
	}

	private void HandleGrinding(ref Vector2 velocity, float inputDirection, float deltaSeconds, float gravity)
	{
		var maxTransitions = 16;

		for (var transitionCount = 0; transitionCount < maxTransitions; transitionCount++)
		{
			var rail = _activeRail!;

			if (TryReleaseJump(ref velocity, inputDirection, false, true))
			{
				return;
			}

			_grindElapsedTime += deltaSeconds;
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
			var friction = HasGrindBoost ? RailFriction / _boostAccelMultiplier : RailFriction;
			_railSpeed = Mathf.MoveToward(_railSpeed, 0.0f, friction * deltaSeconds);
			_railSpeed += _balanceValue * BalancePhysicsForce * deltaSeconds;
			var maxRailSpeed = HasGrindBoost ? MaxRailSpeed * _boostMultiplier : MaxRailSpeed;
			_railSpeed = Mathf.Clamp(_railSpeed, -maxRailSpeed, maxRailSpeed);
			_railProgress += (_railSpeed * deltaSeconds) / Mathf.Max(rail.Length, 0.001f);

			if (_railProgress <= 0.0f || _railProgress >= 1.0f)
			{
				_railProgress = Mathf.Clamp(_railProgress, 0.0f, 1.0f);

				float transitionDir = Mathf.Sign(_railSpeed);
				if (Mathf.IsZeroApprox(transitionDir))
					transitionDir = _grindDirection;
				if (TryFindConnectingRail(rail, transitionDir, out var nextRail, out var nextProgress))
				{
					_grindElapsedTime *= BalanceComboRecovery;
					EnterRail(nextRail, transitionDir, nextProgress);
					continue;
				}

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
			break;
		}
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

	private bool TryFindConnectingRail(GrindRail current, float direction, out GrindRail? nextRail, out float nextProgress)
	{
		nextRail = null;
		nextProgress = 0.0f;

		var searchPoint = direction > 0 ? current.EndPoint : current.StartPoint;

		var linked = direction >= 0 ? current.NextRail : current.PrevRail;
		if (linked != null)
		{
			var angleDiff = Mathf.Abs(current.Tangent.AngleTo(linked.Tangent));
			if (angleDiff <= Mathf.DegToRad(90.0f))
			{
				nextProgress = direction >= 0 ? 0.0f : 1.0f;
				nextRail = linked;
				return true;
			}
		}

		foreach (var node in GetTree().GetNodesInGroup(GrindRail.RailGroupName))
		{
			if (node is not GrindRail candidate || candidate == current)
				continue;

			if (candidate.TryGetSnapProgress(searchPoint, out nextProgress))
			{
				var angleDiff = Mathf.Abs(current.Tangent.AngleTo(candidate.Tangent));
				if (angleDiff > Mathf.DegToRad(90.0f))
					continue;

				nextRail = candidate;
				return true;
			}
		}

		return false;
	}

	private Vector2 GetRailContactPoint()
	{
		return _boardContact.GlobalPosition;
	}
}
