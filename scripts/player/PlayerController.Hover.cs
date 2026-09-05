using System.Collections.Generic;
using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export] public float HoverBobAmplitude = 1.4f;
	[Export] public float HoverBobSpeed = 4.0f;
	[Export] public float BoardHoverBobAmplitude = 0.6f;
	[Export] public float BeamFlickerAmount = 0.14f;
	[Export] public float BoardOpacity = 0.6f;

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

	[Export] public bool UseIdleAnimation = true;
	[Export] public bool UseMoveAnimation = true;
	[Export] public bool UseChargeAnimation = true;
	[Export] public float IdleAnimFps = 8.0f;
	[Export] public float MoveAnimFps = 12.0f;
	[Export] public float MoveSpeedThreshold = 40.0f;
	[Export] public int MoveLoopTail = 3;
	[Export] public float ChargeAnimFps = 8.0f;
	[Export] public int ChargeLoopTail = 3;

	[Export] public bool UseBoardIdleAnimation = true;
	[Export] public float BoardIdleAnimFps = 8.0f;
	[Export] public bool UseBoardGrindAnimation = true;
	[Export] public float BoardGrindAnimFps = 12.0f;
	[Export] public bool UseBoardMoveAnimation = true;
	[Export] public float BoardMoveAnimFps = 14.0f;
	[Export] public float BoardGlowScale = 1.0f;
	[Export] public float BoardGlowStrength = 0.7f;
	[Export] public Color BoardGlowColor = Colors.White;

	[Export] public bool LogFlipSelection = false;

	private const string AnimRoot = "res://assets/characters/player/anim";
	private const string BoardAnimRoot = "res://assets/hoverboards/player/anim";
	private const string BoardGlowTexPath = "res://assets/hoverboards/player/board_glow.png";
	// board_glow.png is a 4x-resolution Gaussian of the board silhouette (192px
	// covering the same 48 world units as the board frames), so its base scale
	// relative to BoardSprite is 1/4.
	private const float BoardGlowBaseScale = 0.25f;

	private bool _animInit;
	private float _hoverTime;
	private float _ringTime;
	private float _jumpTimer;
	private float _idleTimer;
	private float _moveTimer;
	private float _chargeLoopTimer;
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
	private Texture2D[]? _idleFrames;
	private Texture2D[]? _moveFrames;
	private Texture2D[]? _chargeFrames;
	private Texture2D[]? _grindFrames;
	private Texture2D[]? _flipFrontFrames;
	private Texture2D[]? _flipBackFrames;
	private Texture2D[]? _flipFrames;
	private Texture2D[]? _boardIdleFrames;
	private Texture2D[]? _boardGrindFrames;
	private Texture2D[]? _boardMoveFrames;
	private Texture2D[]? _boardFramesApplied;
	private float _boardAnimTimer;
	private Sprite2D? _boardGlow;

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
			_idleFrames = LoadFrames(AnimRoot + "/idle", "idle_");
			_moveFrames = LoadFrames(AnimRoot + "/move", "move_");
			_chargeFrames = LoadFrames(AnimRoot + "/charge", "charge_");
			_grindFrames = LoadFrames(AnimRoot + "/grind", "grind_");
			_flipFrontFrames = LoadFrames(AnimRoot + "/frontflip", "flip_front_");
			_flipBackFrames = LoadFrames(AnimRoot + "/backflip", "flip_back_");
			_boardIdleFrames = LoadFrames(BoardAnimRoot + "/idle", "board_");
			_boardGrindFrames = LoadFrames(BoardAnimRoot + "/grind", "board_");
			_boardMoveFrames = LoadFrames(BoardAnimRoot + "/move", "board_");
			if (_boardVisual != null)
			{
				_boardVisual.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
				if (_boardGlow == null && ResourceLoader.Exists(BoardGlowTexPath))
				{
					var glowTex = GD.Load<Texture2D>(BoardGlowTexPath);
					if (glowTex != null)
					{
						var glowMat = new CanvasItemMaterial();
						glowMat.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
						_boardGlow = new Sprite2D
						{
							Name = "BoardGlow",
							Texture = glowTex,
							Scale = new Vector2(BoardGlowBaseScale, BoardGlowBaseScale),
							Material = glowMat,
						};
						_boardVisual.AddChild(_boardGlow);
					}
				}
				InitBoardFx();
			}
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

		UpdateBoardVisual(deltaSeconds, onFloor, grinding);
		UpdateBoardFx(deltaSeconds, onFloor, grinding, airborne);

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

		if (_isChargingJump && UseChargeAnimation && _chargeFrames != null && _chargeFrames.Length > 0)
		{
			var ratio = MaxJumpHoldTime <= 0.0f
				? 1.0f
				: Mathf.Clamp(_jumpChargeTime / MaxJumpHoldTime, 0.0f, 1.0f);
			if (ratio >= 1.0f)
			{
				_chargeLoopTimer += deltaSeconds;
			}
			else
			{
				_chargeLoopTimer = 0.0f;
			}

			ApplyPose(_chargeFrames[ChargeIndex(_chargeFrames.Length, ratio, _chargeLoopTimer, ChargeAnimFps, ChargeLoopTail)]);
			return;
		}

		var movingGround = Mathf.Abs(Velocity.X) > MoveSpeedThreshold;
		var groundTex = _baseTex;
		if (movingGround && UseMoveAnimation && _moveFrames != null && _moveFrames.Length > 0)
		{
			_moveTimer += deltaSeconds;
			groundTex = _moveFrames[MoveIndex(_moveFrames.Length, _moveTimer, MoveAnimFps, MoveLoopTail)];
		}
		else
		{
			_moveTimer = 0.0f;
			if (UseIdleAnimation && _idleFrames != null && _idleFrames.Length > 0)
			{
				_idleTimer += deltaSeconds;
				groundTex = _idleFrames[LoopIndex(_idleFrames.Length, _idleTimer, IdleAnimFps)];
			}
		}

		if (groundTex != null)
		{
			_visual.Texture = groundTex;
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

	private static int LoopIndex(int count, float time, float fps)
	{
		if (count <= 1)
		{
			return 0;
		}

		var cycle = 2 * (count - 1);
		var pos = ((int)(time * fps)) % cycle;
		return pos < (count - 1) ? pos : cycle - pos;
	}

	// Plays frames 0..count-1 once (the lean-in), then ping-pongs only the last `tail` frames.
	private static int MoveIndex(int count, float time, float fps, int tail)
	{
		if (count <= 1)
		{
			return 0;
		}

		var steps = (int)(time * fps);
		if (steps <= count - 1)
		{
			return steps;
		}

		var tailStart = Mathf.Clamp(count - Mathf.Max(1, tail), 0, count - 1);
		var len = count - tailStart;
		if (len <= 1)
		{
			return count - 1;
		}

		var period = 2 * (len - 1);
		var rel = steps - (count - 1);
		var pos = rel % period;
		var m = pos <= len - 1 ? (len - 1 - pos) : (period - pos);
		return tailStart + m;
	}

	// Charge: builds frames by ratio (0..count-1) while charging, then ping-pongs
	// only the last `tail` frames once the charge is full and still held.
	private static int ChargeIndex(int count, float ratio, float holdTime, float fps, int tail)
	{
		if (count <= 1)
		{
			return 0;
		}

		if (ratio < 1.0f)
		{
			return Mathf.Clamp(Mathf.RoundToInt(ratio * (count - 1)), 0, count - 1);
		}

		var tailStart = Mathf.Clamp(count - Mathf.Max(1, tail), 0, count - 1);
		var len = count - tailStart;
		if (len <= 1)
		{
			return count - 1;
		}

		var period = 2 * (len - 1);
		var pos = ((int)(holdTime * fps)) % period;
		var m = pos <= len - 1 ? (len - 1 - pos) : (period - pos);
		return tailStart + m;
	}

	private void UpdateBoardVisual(float deltaSeconds, bool onFloor, bool grinding)
	{
		if (_boardVisual == null)
		{
			return;
		}

		_boardVisual.Scale = new Vector2(_boardVisualBaseScale.X * _facing, _boardVisualBaseScale.Y);
		_boardVisual.Modulate = new Color(1.0f, 1.0f, 1.0f, BoardOpacity);

		if (_boardGlow != null)
		{
			var glowColor = new Color(BoardGlowColor.R, BoardGlowColor.G, BoardGlowColor.B, BoardGlowColor.A * BoardGlowStrength);
			_boardGlow.SelfModulate = glowColor;
			var glowScale = BoardGlowBaseScale * BoardGlowScale;
			_boardGlow.Scale = new Vector2(glowScale, glowScale);
		}

		var frames = PickBoardFrames(onFloor, grinding);
		var fps = frames == _boardGrindFrames ? BoardGrindAnimFps
			: frames == _boardMoveFrames ? BoardMoveAnimFps
			: BoardIdleAnimFps;

		if (frames == null || frames.Length == 0)
		{
			return;
		}

		if (!ReferenceEquals(frames, _boardFramesApplied))
		{
			_boardFramesApplied = frames;
			_boardAnimTimer = 0.0f;
		}

		_boardAnimTimer += deltaSeconds;
		_boardVisual.Texture = frames[LoopIndex(frames.Length, _boardAnimTimer, fps)];
	}

	// grind > ground-move > idle shimmer; falls back to idle when a set is
	// missing or disabled, so behavior is unchanged if art has not imported yet.
	private Texture2D[]? PickBoardFrames(bool onFloor, bool grinding)
	{
		if (grinding && UseBoardGrindAnimation && _boardGrindFrames is { Length: > 0 })
		{
			return _boardGrindFrames;
		}

		if (onFloor && !grinding && Mathf.Abs(Velocity.X) > MoveSpeedThreshold && UseBoardMoveAnimation && _boardMoveFrames is { Length: > 0 })
		{
			return _boardMoveFrames;
		}

		return UseBoardIdleAnimation ? _boardIdleFrames : null;
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
