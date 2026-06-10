using System.Collections.Generic;
using System.Text;
using Godot;

public partial class PlayerController : CharacterBody2D
{
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

	private void PublishTrickStart(string trickName)
	{
		LastStartedTrickName = trickName;
		TrickStartSequence++;
	}

	private string GetInstalledTrickName(ModuleType moduleType)
	{
		return Loadout?.GetModule(moduleType).DisplayName ?? moduleType.ToString();
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
}
