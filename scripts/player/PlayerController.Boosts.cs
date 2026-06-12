using Godot;

public partial class PlayerController : CharacterBody2D
{
	private float _speedBoostTimer;
	private float _grindBoostTimer;
	private float _boostMultiplier = 2.0f;

	public bool HasSpeedBoost => _speedBoostTimer > 0.0f;

	public bool HasGrindBoost => _grindBoostTimer > 0.0f;

	public void ApplySpeedBoost(float multiplier, float duration)
	{
		_boostMultiplier = multiplier;
		_speedBoostTimer = duration;
	}

	public void ApplyGrindBoost(float multiplier, float duration)
	{
		_boostMultiplier = multiplier;
		_grindBoostTimer = duration;

		if (_activeRail != null && !Mathf.IsZeroApprox(_railSpeed))
		{
			_railSpeed *= 1.5f;
		}
	}

	private void UpdateBoostTimers(float delta)
	{
		_speedBoostTimer = Mathf.Max(0.0f, _speedBoostTimer - delta);
		_grindBoostTimer = Mathf.Max(0.0f, _grindBoostTimer - delta);
	}
}
