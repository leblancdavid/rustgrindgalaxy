using Godot;

public partial class PlayerController : CharacterBody2D
{
	private Vector2 GetSlopeTangent(Vector2 floorNormal)
	{
		var tangent = new Vector2(floorNormal.Y, -floorNormal.X).Normalized();
		return tangent.X < 0.0f ? -tangent : tangent;
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

		if (InputMap.HasAction(TrickGrabConfirmAction) == false)
		{
			InputMap.AddAction(TrickGrabConfirmAction);
		}

		InputMap.ActionEraseEvents(TrickGrabConfirmAction);
		InputMap.ActionAddEvent(TrickGrabConfirmAction, new InputEventKey { Keycode = Key.Enter });
		InputMap.ActionAddEvent(TrickGrabConfirmAction, new InputEventKey { PhysicalKeycode = Key.Enter });
		InputMap.ActionAddEvent(TrickGrabConfirmAction, new InputEventKey { Keycode = Key.KpEnter });
		InputMap.ActionAddEvent(TrickGrabConfirmAction, new InputEventKey { PhysicalKeycode = Key.KpEnter });
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
