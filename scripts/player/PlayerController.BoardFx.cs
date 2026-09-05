using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export] public bool UseDustFx = true;
	[Export] public bool UseSparkFx = true;
	[Export] public bool UseWispFx = true;

	[Export] public float DustFxFps = 10.0f;
	[Export] public float SparkFxFps = 16.0f;
	[Export] public float WispFxFps = 12.0f;

	[Export] public float DustFxAlpha = 0.5f;
	[Export] public float SparkFxAlpha = 1.0f;
	[Export] public float WispFxAlpha = 0.4f;

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

	// Offsets are in board-local pixels (pre 0.75 board scale); +x is the facing
	// direction, +y is down. FX nodes are children of BoardSprite so they inherit
	// flip/tilt/bob/spin. Sparks ignore offsets: their anchor is the board's
	// contact point (center for now) + SparkContactLiftPixels.
	[Export] public Vector2 DustFxOffset = new Vector2(-30.0f, 8.0f);
	[Export] public Vector2 WispFxOffset = new Vector2(-10.0f, -2.0f);

	[Export] public float FxFadeSpeed = 8.0f;
	[Export] public float WispFullSpeedRatio = 0.55f;
	[Export] public float DustMinStrength = 0.45f;

	private const string BoardFxRoot = "res://assets/hoverboards/player/fx";

	private Sprite2D? _dustFx;
	private Sprite2D? _wispsFx;
	private Texture2D[]? _dustFrames;
	private Texture2D[]? _sparkFrames;
	private Texture2D[]? _wispFrames;
	private float _dustTimer;
	private float _wispTimer;
	private float _dustAlpha;
	private float _sparkAlpha;
	private float _wispAlpha;
	private SparkBurstFx? _sparksMain;
	private SparkBurstFx? _sparksSmall;

	private void InitBoardFx()
	{
		_dustFrames = LoadFrames(BoardFxRoot + "/dust", "boardfx_");
		_sparkFrames = LoadFrames(BoardFxRoot + "/sparks", "boardfx_");
		_wispFrames = LoadFrames(BoardFxRoot + "/wisps", "boardfx_");

		_dustFx = CreateFxChild("BoardDust", behindParent: true);
		_wispsFx = CreateFxChild("BoardWisps", behindParent: true);
		_sparksMain = new SparkBurstFx(this, CreateFxChild("BoardSparks", behindParent: true), _sparkFrames, 1.0f, 1.0f, 0.0f);
		_sparksSmall = new SparkBurstFx(this, CreateFxChild("BoardSmallSparks", behindParent: true), _sparkFrames, SparkSmallScaleBias, SparkSmallJitterBias, 1.0f);
	}

	private Sprite2D CreateFxChild(string name, bool behindParent)
	{
		var node = new Sprite2D
		{
			Name = name,
			ShowBehindParent = behindParent,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
			Visible = false,
		};
		_boardVisual.AddChild(node);
		return node;
	}

	private void UpdateBoardFx(float deltaSeconds, bool onFloor, bool grinding, bool airborne)
	{
		if (_dustFx == null)
		{
			return;
		}

		var speed = Mathf.Abs(Velocity.X);
		var moveRatio = Mathf.Clamp(speed / Mathf.Max(MoveSpeed, 1.0f), 0.0f, 1.0f);
		var railRatio = Mathf.Clamp(Mathf.Abs(_railSpeed) / Mathf.Max(MaxRailSpeed, 1.0f), 0.0f, 1.0f);

		var dustTarget = UseDustFx && !IsDead && onFloor && !grinding && speed > MoveSpeedThreshold
			? DustFxAlpha * Mathf.Lerp(DustMinStrength, 1.0f, moveRatio)
			: 0.0f;
		var sparkTarget = UseSparkFx && !IsDead && grinding ? SparkFxAlpha : 0.0f;
		var wispTarget = UseWispFx && !IsDead && airborne
			? WispFxAlpha * Mathf.Clamp(speed / Mathf.Max(MoveSpeed * WispFullSpeedRatio, 1.0f), 0.25f, 1.0f)
			: 0.0f;

		_dustAlpha = Mathf.MoveToward(_dustAlpha, dustTarget, deltaSeconds * FxFadeSpeed);
		_sparkAlpha = Mathf.MoveToward(_sparkAlpha, sparkTarget, deltaSeconds * FxFadeSpeed);
		_wispAlpha = Mathf.MoveToward(_wispAlpha, wispTarget, deltaSeconds * FxFadeSpeed);

		ApplyFx(_dustFx, _dustFrames, ref _dustTimer, _dustAlpha, DustFxColor, DustFxFps, DustFxOffset, deltaSeconds);
		ApplyFx(_wispsFx, _wispFrames, ref _wispTimer, _wispAlpha, WispFxColor, WispFxFps, WispFxOffset, deltaSeconds);

		// Fixed contact anchor for now: the board center (board-local origin,
		// +y down onto the rail). Each grind trick will eventually provide its
		// own contact point through this parameter.
		var contactLocal = new Vector2(0.0f, SparkContactLiftPixels);

		_sparksMain?.Update(deltaSeconds, contactLocal, _sparkAlpha, railRatio);
		_sparksSmall?.Update(deltaSeconds, contactLocal, _sparkAlpha, railRatio);
	}

	private void ApplyFx(Sprite2D node, Texture2D[]? frames, ref float timer, float alpha, Color color, float fps, Vector2 offset, float deltaSeconds)
	{
		if (alpha <= 0.001f || frames == null || frames.Length == 0)
		{
			node.Visible = false;
			return;
		}

		node.Visible = true;
		timer += deltaSeconds;
		node.Texture = frames[((int)(timer * fps)) % frames.Length];
		node.Position = offset;
		node.SelfModulate = new Color(color.R, color.G, color.B, alpha / Mathf.Max(BoardOpacity, 0.05f));
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
}
