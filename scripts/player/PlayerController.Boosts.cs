using Godot;

public partial class PlayerController : CharacterBody2D
{
	private float _speedBoostTimer;
	private float _grindBoostTimer;
	private float _boostMultiplier = 2.0f;

	private float _pendingBoostImpulse;
	private float _boostAccelMultiplier = 1.0f;

	public bool HasSpeedBoost => _speedBoostTimer > 0.0f;

	public bool HasGrindBoost => _grindBoostTimer > 0.0f;

	public void ApplySpeedBoost(float multiplier, float duration, float impulse = 0f, float accelMultiplier = 1f)
	{
		_boostMultiplier = multiplier;
		_speedBoostTimer = duration;
		_pendingBoostImpulse = impulse;
		_boostAccelMultiplier = accelMultiplier;
	}

	public void ApplyGrindBoost(float multiplier, float duration, float impulse = 0f, float accelMultiplier = 1f)
	{
		_boostMultiplier = multiplier;
		_grindBoostTimer = duration;
		_boostAccelMultiplier = accelMultiplier;

		if (_activeRail != null && !Mathf.IsZeroApprox(_railSpeed))
		{
			_railSpeed += Mathf.Sign(_railSpeed) * impulse;
		}
		else if (_activeRail != null)
		{
			_railSpeed = impulse;
		}
	}

	public float GetBoostAcceleration(float baseAcceleration)
	{
		return (HasSpeedBoost || HasGrindBoost) ? baseAcceleration * _boostAccelMultiplier : baseAcceleration;
	}

	private void UpdateBoostTimers(float delta)
	{
		_speedBoostTimer = Mathf.Max(0.0f, _speedBoostTimer - delta);
		_grindBoostTimer = Mathf.Max(0.0f, _grindBoostTimer - delta);
	}
}
