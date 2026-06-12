using Godot;

public partial class PlayerController : CharacterBody2D
{
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

	private void UpdateGrindBalance(float deltaSeconds, float inputDirection)
	{
		var t = Mathf.Clamp((_grindElapsedTime * _resolvedEffects.BalanceDifficultyRate) / Mathf.Max(GrindTimeToMaxDifficulty, 0.01f), 0.0f, 1.0f);

		var currentDriftRate = Mathf.Lerp(BalanceDriftRate, BalanceMaxDriftRate, t);
		var currentInterval = Mathf.Lerp(BalanceDriftChangeInterval, BalanceMinDriftInterval, t);
		var currentTargetRange = Mathf.Lerp(1.0f, BalanceMaxDriftTargetRange, t);

		_balanceDriftTimer -= deltaSeconds;

		if (_balanceDriftTimer <= 0.0f)
		{
			_balanceDriftTarget = (float)GD.RandRange(-currentTargetRange, currentTargetRange);
			_balanceDriftTimer = currentInterval * (float)GD.RandRange(0.5f, 1.5f);
		}

		var drift = _balanceDriftTarget * currentDriftRate * deltaSeconds;
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

		_balanceJitterTimer -= deltaSeconds;
		if (_balanceJitterTimer <= 0.0f)
		{
			_balanceJitterValue = (float)GD.RandRange(-BalanceJitterAmplitude, BalanceJitterAmplitude);
			_balanceJitterTimer = BalanceJitterInterval * (float)GD.RandRange(0.5f, 1.5f);
		}

		_balanceValue += _balanceJitterValue * deltaSeconds;

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
}
