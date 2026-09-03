using System.Collections.Generic;
using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export] public float HoverBobAmplitude = 1.4f;
	[Export] public float HoverBobSpeed = 4.0f;
	[Export] public float BeamFlickerAmount = 0.14f;

	[Export] public float AirStretchAmount = 0.12f;
	[Export] public float LandSquashAmount = 0.24f;
	[Export] public float SquashLerpSpeed = 18.0f;
	[Export] public float StretchSpeedRef = 500.0f;

	[Export] public float RipplePeriod = 0.9f;
	[Export] public float RippleStartScale = 0.45f;
	[Export] public float RippleEndScale = 1.25f;
	[Export] public float RippleMaxAlpha = 0.85f;
	[Export] public float RippleFlatten = 0.5f;
	[Export] public Color RippleColor = new Color(0.45f, 0.9f, 1.0f, 1.0f);

	[Export] public bool UseJumpAnimation = true;
	[Export] public bool UseGrindAnimation = true;
	[Export] public bool UseFlipAnimation = true;
	[Export] public float JumpAnimFps = 12.0f;

	[Export] public float FlipRotateRampThreshold = 0.5f;
	[Export] public float FlipScrubSpeed = 4.0f;
	[Export] public float FlipRewindSpeed = 5.0f;

	[Export] public float GrindScrubSpeed = 5.0f;
	[Export] public float GrindRewindSpeed = 6.0f;

	[Export] public bool LogFlipSelection = false;

	private const string AnimRoot = "res://assets/characters/player/anim";

	private bool _animInit;
	private float _hoverTime;
	private float _ringTime;
	private float _jumpTimer;
	private float _flipProgress;
	private float _grindProgress;
	private int _facing = 1;
	private bool _wasOnFloor = true;
	private float _peakFallVy;
	private float _landSquash;
	private Vector2 _scaleCurrent = Vector2.One;
	private Vector2 _visualBasePosition;
	private Sprite2D? _beamRippleA;
	private Sprite2D? _beamRippleB;
	private Texture2D? _baseTex;
	private Texture2D[]? _jumpFrames;
	private Texture2D[]? _grindFrames;
	private Texture2D[]? _flipFrontFrames;
	private Texture2D[]? _flipBackFrames;
	private Texture2D[]? _flipFrames;

	public override void _Process(double delta)
	{
		UpdateHoverVisual((float)delta);
	}

	private void UpdateHoverVisual(float deltaSeconds)
	{
		if (_visual == null)
		{
			return;
		}

		if (!_animInit)
		{
			_visualBasePosition = _visual.Position;
			_beamRippleA = GetNodeOrNull<Sprite2D>("VisualContainer/BeamRippleA");
			_beamRippleB = GetNodeOrNull<Sprite2D>("VisualContainer/BeamRippleB");
			_baseTex = _visual.Texture;
			_jumpFrames = LoadFrames(AnimRoot + "/jump", "jump_");
			_grindFrames = LoadFrames(AnimRoot + "/grind", "grind_");
			_flipFrontFrames = LoadFrames(AnimRoot + "/frontflip", "flip_front_");
			_flipBackFrames = LoadFrames(AnimRoot + "/backflip", "flip_back_");
			_animInit = true;
		}

		var onFloor = IsOnFloor();
		var grinding = _activeRail != null;
		var airborne = !onFloor && !grinding;

		if (!onFloor && _wasOnFloor)
		{
			_peakFallVy = 0.0f;
			_jumpTimer = 0.0f;
		}

		if (!onFloor)
		{
			_peakFallVy = Mathf.Max(_peakFallVy, Mathf.Max(0.0f, Velocity.Y));
		}
		else if (!_wasOnFloor)
		{
			_landSquash = Mathf.Clamp(_peakFallVy / Mathf.Max(StretchSpeedRef, 1.0f), 0.25f, 1.0f);
		}

		_wasOnFloor = onFloor;

		_hoverTime += deltaSeconds;
		_facing = GetVisualTravelDirection() >= 0.0f ? 1 : -1;

		UpdateFlipProgress(deltaSeconds, airborne);
		UpdateGrindProgress(deltaSeconds, grinding);

		if (IsDead)
		{
			ResetPose();
			SetRipplesActive(false);
			return;
		}

		if (grinding && UseGrindAnimation && _grindFrames != null && _grindFrames.Length > 0)
		{
			ApplyPose(_grindFrames[PoseIndex(_grindFrames, _grindProgress)]);
			return;
		}

		if (_flipProgress > 0.0f && UseFlipAnimation && _flipFrames != null && _flipFrames.Length > 0)
		{
			ApplyPose(_flipFrames[PoseIndex(_flipFrames, _flipProgress)]);
			return;
		}

		if (_grindProgress > 0.0f && UseGrindAnimation && _grindFrames != null && _grindFrames.Length > 0)
		{
			ApplyPose(_grindFrames[PoseIndex(_grindFrames, _grindProgress)]);
			return;
		}

		if (airborne && UseJumpAnimation && _jumpFrames != null && _jumpFrames.Length > 0)
		{
			_jumpTimer += deltaSeconds;
			var idx = ((int)(_jumpTimer * JumpAnimFps)) % _jumpFrames.Length;
			ApplyPose(_jumpFrames[idx]);
			return;
		}

		if (_visual.Texture != _baseTex && _baseTex != null)
		{
			_visual.Texture = _baseTex;
		}

		_landSquash = Mathf.MoveToward(_landSquash, 0.0f, deltaSeconds * 4.0f);

		var stretch = airborne ? Mathf.Clamp(Mathf.Abs(Velocity.Y) / Mathf.Max(StretchSpeedRef, 1.0f), 0.0f, 1.0f) * AirStretchAmount : 0.0f;
		var targetScaleY = 1.0f + stretch - _landSquash * LandSquashAmount;
		var targetScaleX = 1.0f - stretch * 0.5f + _landSquash * LandSquashAmount * 0.7f;

		var t = Mathf.Clamp(SquashLerpSpeed * deltaSeconds, 0.0f, 1.0f);
		_scaleCurrent = _scaleCurrent.Lerp(new Vector2(targetScaleX, targetScaleY), t);
		_visual.Scale = new Vector2(_facing * _scaleCurrent.X, _scaleCurrent.Y);

		var bob = Mathf.Sin(_hoverTime * HoverBobSpeed) * HoverBobAmplitude;
		_visual.Position = _visualBasePosition + new Vector2(0.0f, bob);

		var flicker = 1.0f - BeamFlickerAmount
			* (0.5f + 0.5f * Mathf.Sin(_hoverTime * 27.0f) * Mathf.Sin(_hoverTime * 11.0f + 1.3f));
		_visual.Modulate = new Color(flicker, flicker, flicker, 1.0f);

		UpdateRipples(deltaSeconds);
	}

	private void UpdateFlipProgress(float deltaSeconds, bool airborne)
	{
		var active = airborne ? GetRotationFlipFrames() : null;

		if (active != null)
		{
			if (LogFlipSelection && _flipProgress <= 0.0f)
			{
				GD.Print($"[flip] start rotDir={(int)_airRotationRampDirection} travel={GetVisualTravelDirection():0} ramp={_airRotationRamp:0.00} -> {(active == _flipFrontFrames ? "FRONT" : "BACK")}");
			}

			_flipFrames = active;
			_flipProgress = Mathf.Min(1.0f, _flipProgress + deltaSeconds * FlipScrubSpeed);
			return;
		}

		if (_flipProgress > 0.0f)
		{
			_flipProgress = Mathf.Max(0.0f, _flipProgress - deltaSeconds * FlipRewindSpeed);
			if (_flipProgress <= 0.0f)
			{
				_flipFrames = null;
			}
		}
	}

	private void UpdateGrindProgress(float deltaSeconds, bool grinding)
	{
		if (grinding && UseGrindAnimation && _grindFrames != null && _grindFrames.Length > 0)
		{
			_grindProgress = Mathf.Min(1.0f, _grindProgress + deltaSeconds * GrindScrubSpeed);
			return;
		}

		_grindProgress = Mathf.Max(0.0f, _grindProgress - deltaSeconds * GrindRewindSpeed);
	}

	// Chosen by how the body is spinning in air vs. travel direction:
	// rotating the same way you're traveling -> front flip, opposite -> back flip.
	private Texture2D[]? GetRotationFlipFrames()
	{
		if (_airRotationRamp < FlipRotateRampThreshold)
		{
			return null;
		}

		var rotDir = (int)_airRotationRampDirection;
		if (rotDir == 0)
		{
			return null;
		}

		var travelDir = GetVisualTravelDirection();
		var frontFlip = rotDir * travelDir > 0.0f;
		return frontFlip ? _flipFrontFrames : _flipBackFrames;
	}

	private static int PoseIndex(Texture2D[] frames, float progress)
	{
		var idx = Mathf.RoundToInt(Mathf.Clamp(progress, 0.0f, 1.0f) * (frames.Length - 1));
		return Mathf.Clamp(idx, 0, frames.Length - 1);
	}

	private void ApplyPose(Texture2D tex)
	{
		_visual.Texture = tex;
		_visual.Scale = new Vector2(_facing, 1.0f);
		_visual.Position = _visualBasePosition;
		_visual.Modulate = Colors.White;
		SetRipplesActive(false);
	}

	private void ResetPose()
	{
		if (_baseTex != null)
		{
			_visual.Texture = _baseTex;
		}

		_visual.Scale = Vector2.One * _facing;
		_visual.Position = _visualBasePosition;
		_visual.Modulate = Colors.White;
	}

	private static Texture2D[]? LoadFrames(string folder, string prefix)
	{
		var list = new List<Texture2D>();
		for (var i = 0; i < 48; i++)
		{
			var path = $"{folder}/{prefix}{i:D2}.png";
			if (!ResourceLoader.Exists(path))
			{
				break;
			}

			var tex = GD.Load<Texture2D>(path);
			if (tex == null)
			{
				break;
			}

			list.Add(tex);
		}

		return list.Count > 0 ? list.ToArray() : null;
	}

	private void UpdateRipples(float deltaSeconds)
	{
		if (_beamRippleA == null && _beamRippleB == null)
		{
			return;
		}

		var speedFactor = 1.0f + Mathf.Clamp(Mathf.Abs(Velocity.X) / Mathf.Max(MoveSpeed, 1.0f), 0.0f, 1.5f);
		_ringTime += deltaSeconds * speedFactor;
		var period = Mathf.Max(RipplePeriod, 0.05f);

		SetRipple(_beamRippleA, _ringTime / period);
		SetRipple(_beamRippleB, (_ringTime / period) + 0.5f);
	}

	private void SetRipple(Sprite2D? ring, float phaseRaw)
	{
		if (ring == null)
		{
			return;
		}

		var p = phaseRaw - Mathf.Floor(phaseRaw);
		var scale = Mathf.Lerp(RippleStartScale, RippleEndScale, p);
		var alpha = (1.0f - p) * RippleMaxAlpha;
		ring.Scale = new Vector2(scale, scale * RippleFlatten);
		ring.Modulate = new Color(RippleColor.R, RippleColor.G, RippleColor.B, alpha);
	}

	private void SetRipplesActive(bool active)
	{
		if (_beamRippleA != null)
		{
			_beamRippleA.Visible = active;
		}

		if (_beamRippleB != null)
		{
			_beamRippleB.Visible = active;
		}
	}
}
