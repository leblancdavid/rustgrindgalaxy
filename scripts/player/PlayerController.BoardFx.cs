using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export] public bool UseDustFx = true;
	[Export] public bool UseSparkFx = true;
	[Export] public bool UseWispFx = true;

	[Export] public float DustFxFps = 10.0f;
	[Export] public float SparkFxFps = 16.0f;
	[Export] public float WispFxFps = 12.0f;

	[Export] public float DustFxAlpha = 0.65f;
	[Export] public float SparkFxAlpha = 1.0f;
	[Export] public float WispFxAlpha = 0.18f;

	// Sparks: each burst erupts at a fixed spot/angle on the rail contact point
	// and its scale grows (SparkGrowFrom -> random burst max) as it plays; bursts
	// are separated by random invisible gaps. Two emitters (main + smaller) keep
	// the pattern ununiform.
	// Anchor point of the spark flash core, in spark-texture px relative to the
	// sprite center (64px canvas: top-right quadrant core ~ (+14,-15)). Rotation
	// and scale pivot around this point so the flash stays welded to the rail.
	// Tune in the inspector while grinding if the anchor drifts.
	[Export] public Vector2 SparkFlashPivot = new Vector2(14.0f, -15.0f);
	// Spray fan centered on straight-back (art rests ~45deg below that axis),
	// spread = -15deg under the board to +15deg above it, always pointing behind.
	[Export] public float SparkRotateMaxDegrees = 15.0f;
	[Export] public float SparkRotateBackDegrees = 45.0f;
	[Export] public float SparkScaleMin = 0.69f;
	[Export] public float SparkScaleMax = 1.33f;
	[Export] public float SparkScaleCap = 1.56f;
	[Export] public float SparkBurstScaleJitter = 0.25f;
	[Export] public float SparkGrowFrom = 0.35f;
	[Export] public float SparkSmallScaleBias = 0.55f;
	[Export] public float SparkSmallJitterBias = 1.0f;
	// How far behind the main contact (board-local px) the smaller secondary
	// sparks scatter. 0 = both emitters fixed on the contact point; the small
	// one differs only in size/angle/timing.
	[Export] public float SparkSecondaryTrailPixels = 0.0f;
	[Export] public float SparkPauseChance = 0.35f;
	[Export] public float SparkPauseSecondsMin = 0.05f;
	[Export] public float SparkPauseSecondsMax = 0.2f;
	[Export] public int SparkBurstFramesMin = 2;
	[Export] public int SparkBurstFramesMax = 6;
	// Per-burst random position offset around the contact anchor. 0 = both
	// emitters fire exactly from the contact point (angle/size/timing vary).
	[Export] public float SparkBurstJitterPixels = 0.0f;
	// Pushes the spark anchor along the board's local down axis (rail normal);
	// the physics contact rides the rail's center line, so lift it onto the
	// drawn rail top.
	[Export] public float SparkContactLiftPixels = 4.0f;

	[Export] public Color DustFxColor = new Color(0.78f, 0.76f, 0.72f);
	[Export] public Color SparkFxColor = new Color(0.55f, 0.9f, 1.0f);
	[Export] public Color WispFxColor = new Color(0.9f, 0.97f, 1.0f);

	// Offsets are in pixels: dust in board-local space (pre 0.75 board scale),
	// wind in VisualContainer-local space; +x is the facing direction, +y is
	// down. Wind is a child of VisualContainer (drawn behind PlayerSprite) so it
	// inherits the body's air spin but not the board's tilt/bob/trick rotation;
	// it mirrors itself for facing/falling. Sparks are children of BoardSprite
	// so they inherit flip/tilt/bob/spin; dust lives on its own world-space node
	// and only borrows the board's transform to compute its spawn anchor. Sparks
	// ignore offsets: their anchor is the board's contact point (center for
	// now) + SparkContactLiftPixels.
	// Dust X is a raw trailing offset; dust Y is an adjust ON TOP of the computed
	// anchor that welds the puff's alpha-bottom to the board's alpha-bottom.
	[Export] public Vector2 DustFxOffset = new Vector2(-30.0f, 0.0f);
	// Wind X trails behind the player (mirrored by facing); Y sits behind the torso.
	[Export] public Vector2 WispFxOffset = new Vector2(-16.0f, -16.0f);

	// Dust: puffs erupt from the board tail at random intervals (mean interval
	// shrinks with speed), freeze their world anchor so they get LEFT BEHIND as
	// the player drives off, then expand, drift slightly back + lift, and fade
	// over a short one-shot lifetime. Base size follows speed; jitter varies
	// each puff's size and animation rate.
	[Export] public int DustFxPoolSize = 8;
	[Export] public float DustFxIntervalFast = 0.09f;
	[Export] public float DustFxIntervalSlow = 0.28f;
	[Export] public float DustFxLifetime = 0.5f;
	// Per-puff random size variation (fraction of base, +/-).
	[Export] public float DustFxScaleJitter = 0.35f;
	// Scale multiplier the puff expands to by the end of its life.
	[Export] public float DustFxGrowTo = 1.4f;
	// Drift velocity (world px/s): backward against facing + a little upward lift.
	[Export] public float DustFxDriftBack = 26.0f;
	[Export] public float DustFxDriftLift = 10.0f;
	// Per-puff animation-speed variation (fraction, +/-).
	[Export] public float DustFpsJitter = 0.2f;
	[Export] public float DustFxScaleMin = 0.5f;
	[Export] public float DustFxScaleMax = 0.85f;
	[Export] public float DustFxFadeFloor = 0.1f;
	// Pulls the puff color toward the level's ground palette at spawn so dust
	// matches the terrain (0 = plain DustFxColor, 1 = pure palette slot).
	// Light slot by default: dust must read against the dark ground body.
	[Export] public float DustFxTintStrength = 0.5f;
	[Export] public PaletteSlot DustFxTintSlot = PaletteSlot.SecondaryLight;
	// Scale bias for the one-off puff kicked at the takeoff spot when jumping
	// off the ground (not rails).
	[Export] public float DustFxJumpScaleBias = 1.5f;

	[Export] public float FxFadeSpeed = 8.0f;
	// Total air speed (px/s, horizontal+vertical) at which wind reaches full
	// target opacity; below it the alpha scales linearly with a small floor so
	// the effect fades near the jump apex instead of popping off.
	[Export] public float WispAirFullSpeed = 420.0f;
	[Export] public float WispMinStrength = 0.1f;
	// Pulls the wind color toward the level palette (0 = plain WispFxColor,
	// 1 = pure palette slot). Primary light by default: wind should carry the
	// level's key glow tint.
	[Export] public float WispFxTintStrength = 0.7f;
	[Export] public PaletteSlot WispFxTintSlot = PaletteSlot.PrimaryLight;
	[Export] public float DustMinStrength = 0.45f;

	private const string BoardFxRoot = "res://assets/hoverboards/player/fx";
	// If the level moves the player instantly (respawn/teleport) the frozen world
	// anchors would fling puffs across the map, so drop them past this step.
	private const float DustTeleportCutoffPx = 220.0f;

	private Sprite2D? _wispsFx;
	private Texture2D[]? _dustFrames;
	private Texture2D[]? _sparkFrames;
	private Texture2D[]? _wispFrames;
	private float _wispTimer;
	private float _sparkAlpha;
	private float _wispAlpha;
	private float _dustBottomFromCenter;
	private float _boardBottomFromCenter;
	private DustPuffFx[] _dustPuffs = null!;
	private Node2D _dustFxRoot = null!;
	private float _dustEmitTimer;
	private bool _wasDustOn;
	private Vector2 _dustPrevGlobalPos;
	private LevelColorPalette _levelPalette;
	private bool _hasLevelPalette;
	private SparkBurstFx? _sparksMain;
	private SparkBurstFx? _sparksSmall;

	// The active level's ground palette; dust puffs tint toward it at spawn so
	// they match the terrain. Levels that build via TileLevelGenerator.Initialize
	// get this wired automatically.
	public void SetLevelPalette(LevelColorPalette palette)
	{
		_levelPalette = palette;
		_hasLevelPalette = true;
	}

	private Color DustPuffColor()
	{
		if (!_hasLevelPalette)
		{
			return DustFxColor;
		}

		return DustFxColor.Lerp(_levelPalette.Resolve(DustFxTintSlot), Mathf.Clamp(DustFxTintStrength, 0.0f, 1.0f));
	}

	private Color WindTintedColor()
	{
		if (!_hasLevelPalette)
		{
			return WispFxColor;
		}

		return WispFxColor.Lerp(_levelPalette.Resolve(WispFxTintSlot), Mathf.Clamp(WispFxTintStrength, 0.0f, 1.0f));
	}

	private void InitBoardFx()
	{
		_dustFrames = LoadFrames(BoardFxRoot + "/dust", "boardfx_");
		_sparkFrames = LoadFrames(BoardFxRoot + "/sparks", "boardfx_");
		_wispFrames = LoadFrames(BoardFxRoot + "/wisps", "boardfx_");

		_dustBottomFromCenter = ContentBottomFromCenter(_dustFrames?[0]);
		_boardBottomFromCenter = ContentBottomFromCenter(_boardIdleFrames?[0] ?? _boardVisual?.Texture);

		// Dust lives OUTSIDE the board/container chain (which rotates, flips,
		// bobs and squashes): a plain node on the unscaled player root, first
		// sibling so puffs draw behind the player sprite but over the level.
		// Puffs then sit in world space with zero compensation math.
		_dustFxRoot = new Node2D { Name = "DustPuffs" };
		AddChild(_dustFxRoot);
		MoveChild(_dustFxRoot, 0);

		_dustPuffs = new DustPuffFx[DustFxPoolSize];
		for (var i = 0; i < DustFxPoolSize; i++)
		{
			_dustPuffs[i] = new DustPuffFx(this, CreateFxChild(_dustFxRoot, $"BoardDust{i:00}", behindParent: false));
		}
		_dustPrevGlobalPos = GlobalPosition;

		// Wind rides the body, not the board: a child of VisualContainer (moved
		// to the front so it draws behind PlayerSprite) so it inherits the air
		// spin — and therefore the flip direction — but not the board's
		// tilt/bob/trick swirl. Facing and fall mirroring are applied in ApplyWind.
		_wispsFx = CreateFxChild(_visualContainer, "WindWisps", behindParent: false);
		_visualContainer.MoveChild(_wispsFx, 0);
		_sparksMain = new SparkBurstFx(this, CreateFxChild(_boardVisual, "BoardSparks", behindParent: true), _sparkFrames, 1.0f, 1.0f, 0.0f);
		_sparksSmall = new SparkBurstFx(this, CreateFxChild(_boardVisual, "BoardSmallSparks", behindParent: true), _sparkFrames, SparkSmallScaleBias, SparkSmallJitterBias, 1.0f);
	}

	private Sprite2D CreateFxChild(Node parent, string name, bool behindParent)
	{
		var node = new Sprite2D
		{
			Name = name,
			ShowBehindParent = behindParent,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
			Visible = false,
		};
		parent.AddChild(node);
		return node;
	}

	private void UpdateBoardFx(float deltaSeconds, bool onFloor, bool grinding, bool airborne)
	{
		if (_wispsFx == null)
		{
			return;
		}

		var speed = Mathf.Abs(Velocity.X);
		var airSpeed = Velocity.Length();
		var moveRatio = Mathf.Clamp(speed / Mathf.Max(MoveSpeed, 1.0f), 0.0f, 1.0f);
		var railRatio = Mathf.Clamp(Mathf.Abs(_railSpeed) / Mathf.Max(MaxRailSpeed, 1.0f), 0.0f, 1.0f);

		if ((GlobalPosition - _dustPrevGlobalPos).LengthSquared() > DustTeleportCutoffPx * DustTeleportCutoffPx)
		{
			ClearDustPuffs();
		}
		_dustPrevGlobalPos = GlobalPosition;

		var dustOn = UseDustFx && !IsDead && onFloor && !grinding && speed > MoveSpeedThreshold;
		var sparkTarget = UseSparkFx && !IsDead && grinding ? SparkFxAlpha : 0.0f;
		var wispTarget = UseWispFx && !IsDead && airborne
			? WispFxAlpha * Mathf.Clamp(airSpeed / Mathf.Max(WispAirFullSpeed, 1.0f), WispMinStrength, 1.0f)
			: 0.0f;

		_sparkAlpha = Mathf.MoveToward(_sparkAlpha, sparkTarget, deltaSeconds * FxFadeSpeed);
		_wispAlpha = Mathf.MoveToward(_wispAlpha, wispTarget, deltaSeconds * FxFadeSpeed);

		if (dustOn)
		{
			// First puff lands the instant movement starts; after that, random
			// intervals whose mean shrinks with speed (each roll +/-50%).
			if (!_wasDustOn)
			{
				_dustEmitTimer = 0.0f;
			}
			_dustEmitTimer -= deltaSeconds;
			if (_dustEmitTimer <= 0.0f)
			{
				EmitDustPuff(DustFxAlpha * Mathf.Lerp(DustMinStrength, 1.0f, moveRatio), moveRatio);
				var interval = Mathf.Lerp(DustFxIntervalSlow, DustFxIntervalFast, moveRatio);
				_dustEmitTimer = interval * (0.5f + GD.Randf());
			}
		}
		_wasDustOn = dustOn;

		for (var i = 0; i < _dustPuffs.Length; i++)
		{
			_dustPuffs[i].Update(deltaSeconds);
		}

		ApplyWind(deltaSeconds);

		// Fixed contact anchor for now: the board center (board-local origin,
		// +y down onto the rail). Each grind trick will eventually provide its
		// own contact point through this parameter.
		var contactLocal = new Vector2(0.0f, SparkContactLiftPixels);

		_sparksMain?.Update(deltaSeconds, contactLocal, _sparkAlpha, railRatio);
		_sparksSmall?.Update(deltaSeconds, contactLocal, _sparkAlpha, railRatio);
	}

	// Wind art authored flowing down-left. The streaks trail on the side the
	// player is leaving (offset X mirrored by facing) and mirror with the facing
	// so they always sweep AWAY from the body; Y flips while falling so the
	// streaks sweep up-left. Rotation comes free from the VisualContainer spin;
	// the node's own rotation stays zero so the flip/trick code never fights it.
	private void ApplyWind(float deltaSeconds)
	{
		if (_wispsFx == null)
		{
			return;
		}

		var frames = _wispFrames;
		if (_wispAlpha <= 0.001f || frames == null || frames.Length == 0)
		{
			_wispsFx.Visible = false;
			return;
		}

		_wispsFx.Visible = true;
		_wispTimer += deltaSeconds;
		_wispsFx.Texture = frames[((int)(_wispTimer * WispFxFps)) % frames.Length];
		_wispsFx.Position = new Vector2(_facing * WispFxOffset.X, WispFxOffset.Y);
		_wispsFx.Scale = new Vector2(_facing, Velocity.Y > 0.0f ? -1.0f : 1.0f);
		var windColor = WindTintedColor();
		_wispsFx.SelfModulate = new Color(windColor.R, windColor.G, windColor.B, _wispAlpha);
	}

	// One bigger puff at the takeoff spot when a ground jump launches.
	public void EmitJumpPuff()
	{
		if (UseDustFx && !IsDead)
		{
			EmitDustPuff(DustFxAlpha, 1.0f, DustFxJumpScaleBias);
		}
	}

	private void EmitDustPuff(float strength, float moveRatio, float scaleBias = 1.0f)
	{
		if (_dustFrames == null || _dustFrames.Length == 0)
		{
			return;
		}

		for (var i = 0; i < _dustPuffs.Length; i++)
		{
			if (_dustPuffs[i].Spawn(strength, moveRatio, scaleBias))
			{
				return;
			}
		}
	}

	private void ClearDustPuffs()
	{
		for (var i = 0; i < _dustPuffs.Length; i++)
		{
			_dustPuffs[i].Deactivate();
		}
	}

	// Distance from the sprite's center to the bottom of its visible (non-transparent)
	// content, in texture pixels. Scanned once at init so FX anchoring follows the
	// art instead of hardcoded numbers.
	private static float ContentBottomFromCenter(Texture2D? tex)
	{
		if (tex == null)
		{
			return 0.0f;
		}

		var img = tex.GetImage();
		for (var y = img.GetHeight() - 1; y >= 0; y--)
		{
			for (var x = 0; x < img.GetWidth(); x++)
			{
				if (img.GetPixel(x, y).A > 0.04f)
				{
					return y - (img.GetHeight() - 1) * 0.5f;
				}
			}
		}

		return 0.0f;
	}

	// One spark emitter: eruption at a fixed jittered contact point and angle,
	// scale growing from SparkGrowFrom toward a rolled per-burst max over the
	// burst's frame run; random gaps between bursts.
	private sealed class SparkBurstFx
	{
		private readonly PlayerController _p;
		private readonly Sprite2D _node;
		private readonly Texture2D[]? _frames;
		private readonly float _scaleBias;
		private readonly float _jitterBias;
		private readonly float _trailBias;

		private float _frameAccum;
		private float _gapTimer;
		private float _rotation;
		private float _maxScale = 1.0f;
		private Vector2 _burstJitter;
		private int _burstPos;
		private int _framesLeft;
		private int _framesTotal = 1;

		public SparkBurstFx(PlayerController owner, Sprite2D node, Texture2D[]? frames, float scaleBias, float jitterBias, float trailBias)
		{
			_p = owner;
			_node = node;
			_frames = frames;
			_scaleBias = scaleBias;
			_jitterBias = jitterBias;
			_trailBias = trailBias;
		}

		public void Update(float deltaSeconds, Vector2 contactLocal, float alpha, float railRatio)
		{
			if (_frames == null || _frames.Length == 0 || alpha <= 0.001f)
			{
				_node.Visible = false;
				return;
			}

			if (_gapTimer > 0.0f)
			{
				_gapTimer -= deltaSeconds;
				_node.Visible = false;
				if (_gapTimer > 0.0f)
				{
					return;
				}
			}

			if (_framesLeft <= 0)
			{
				Roll(railRatio);
			}

			_frameAccum += deltaSeconds * _p.SparkFxFps;
			while (_frameAccum >= 1.0f && _framesLeft > 0)
			{
				_frameAccum -= 1.0f;
				_burstPos++;
				_framesLeft--;
				if (_framesLeft <= 0)
				{
					if (GD.Randf() < _p.SparkPauseChance)
					{
						_gapTimer = _p.SparkPauseSecondsMin + GD.Randf() * Mathf.Max(0.0f, _p.SparkPauseSecondsMax - _p.SparkPauseSecondsMin);
						_node.Visible = false;
						return;
					}
					Roll(railRatio);
				}
			}

			var grow = _framesTotal > 1
				? Mathf.Clamp((_burstPos + _frameAccum) / (_framesTotal - 1), 0.0f, 1.0f)
				: 1.0f;
			var eased = 1.0f - (1.0f - grow) * (1.0f - grow);
			var scale = Mathf.Min(_maxScale * Mathf.Lerp(_p.SparkGrowFrom, 1.0f, eased), _p.SparkScaleCap);

			_node.Visible = true;
			_node.Texture = _frames[Mathf.Min(_burstPos, _frames.Length - 1)];
			// Fixed spot/angle for the whole burst; pivot keeps the flare core
			// on the contact point while the sprite grows.
			_node.Position = contactLocal + _burstJitter - _p.SparkFlashPivot.Rotated(_rotation) * scale;
			_node.Rotation = _rotation;
			_node.Scale = Vector2.One * scale;
			_node.SelfModulate = new Color(_p.SparkFxColor.R, _p.SparkFxColor.G, _p.SparkFxColor.B, alpha / Mathf.Max(_p.BoardOpacity, 0.05f));
		}

		private void Roll(float railRatio)
		{
			_burstPos = 0;
			_frameAccum = 0.0f;
			_framesTotal = (int)GD.RandRange(_p.SparkBurstFramesMin, _p.SparkBurstFramesMax);
			_framesLeft = _framesTotal;
			_rotation = Mathf.DegToRad(_p.SparkRotateBackDegrees + (float)GD.RandRange(-(int)_p.SparkRotateMaxDegrees, (int)_p.SparkRotateMaxDegrees));
			var ang = GD.Randf() * Mathf.Tau;
			_burstJitter = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.5f) * (GD.Randf() * _p.SparkBurstJitterPixels * _jitterBias);
			// Scatter secondary bursts behind the contact, along the rail.
			_burstJitter.X -= _p.SparkSecondaryTrailPixels * _trailBias;
			var baseScale = Mathf.Lerp(_p.SparkScaleMin, _p.SparkScaleMax, railRatio) * _scaleBias;
			_maxScale = baseScale * (1.0f + (GD.Randf() * 2.0f - 1.0f) * Mathf.Max(0.0f, _p.SparkBurstScaleJitter));
		}
	}

	// One dust puff. Lives on the separate DustPuffs node in world space, so the
	// board's tilt/flip/bob/spin cannot touch it: it captures a frozen world
	// anchor of the board bottom at spawn and plays the frame strip once while
	// growing, drifting and fading in place.
	private sealed class DustPuffFx
	{
		private readonly PlayerController _p;
		private readonly Sprite2D _node;
		private bool _active;
		private float _age;
		private float _baseWorldScale;
		private float _pxScale;
		private float _fps;
		private float _strength;
		private Color _color;
		private Vector2 _worldAnchor;
		private Vector2 _driftVel;

		public DustPuffFx(PlayerController owner, Sprite2D node)
		{
			_p = owner;
			_node = node;
		}

		public bool Spawn(float strength, float moveRatio, float scaleBias = 1.0f)
		{
			if (_active || _p._dustFrames == null || _p._dustFrames.Length == 0)
			{
				return false;
			}

			// The dust art was authored against the board art, so bake the
			// board's current world scale in at spawn; afterwards the puff is
			// fully independent of the board transform.
			_pxScale = _p._boardVisual.GlobalScale.X;
			_baseWorldScale = Mathf.Lerp(_p.DustFxScaleMin, _p.DustFxScaleMax, moveRatio)
				* (1.0f + (GD.Randf() * 2.0f - 1.0f) * Mathf.Max(0.0f, _p.DustFxScaleJitter))
				* scaleBias * _pxScale;

			_active = true;
			_age = 0.0f;
			_strength = strength;
			_color = _p.DustPuffColor();
			_fps = _p.DustFxFps * (1.0f + (GD.Randf() * 2.0f - 1.0f) * Mathf.Max(0.0f, _p.DustFpsJitter));
			// Frozen ground point: the board's alpha-bottom (plus the Y adjust) at
			// the trailing DustFxOffset.X; the puff's bottom is kept welded to it.
			_worldAnchor = _p._boardVisual.ToGlobal(new Vector2(_p.DustFxOffset.X, _p._boardBottomFromCenter + _p.DustFxOffset.Y));
			var driftBias = 0.7f + GD.Randf() * 0.6f;
			_driftVel = new Vector2(-_p._facing * _p.DustFxDriftBack, -_p.DustFxDriftLift) * driftBias;
			_node.Visible = true;
			return true;
		}

		public void Update(float deltaSeconds)
		{
			if (!_active)
			{
				return;
			}

			var frames = _p._dustFrames;
			if (frames == null || frames.Length == 0)
			{
				Deactivate();
				return;
			}

			_age += deltaSeconds;
			var k = _age / Mathf.Max(_p.DustFxLifetime, 0.05f);
			if (k >= 1.0f)
			{
				Deactivate();
				return;
			}

			var scale = _baseWorldScale * Mathf.Lerp(1.0f, _p.DustFxGrowTo, k);
			_node.Texture = frames[Mathf.Min((int)(_age * _fps), frames.Length - 1)];
			// All-world units: lift the sprite so its content bottom stays on the
			// frozen anchor while it grows.
			_node.GlobalPosition = _worldAnchor + _driftVel * _age
				- new Vector2(0.0f, _p._dustBottomFromCenter * scale - _p.DustFxOffset.Y * _pxScale);
			_node.Rotation = 0.0f;
			_node.Scale = Vector2.One * scale;
			_node.SelfModulate = new Color(_color.R, _color.G, _color.B,
				_strength * Mathf.Lerp(1.0f, _p.DustFxFadeFloor, k));
		}

		public void Deactivate()
		{
			_active = false;
			_node.Visible = false;
		}
	}
}
