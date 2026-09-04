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
	[Export] public float SparkFxAlpha = 0.95f;
	[Export] public float WispFxAlpha = 0.4f;

	[Export] public Color DustFxColor = new Color(0.78f, 0.76f, 0.72f);
	[Export] public Color SparkFxColor = new Color(0.55f, 0.9f, 1.0f);
	[Export] public Color WispFxColor = new Color(0.9f, 0.97f, 1.0f);

	// Offsets are in board-local pixels (pre 0.75 board scale); +x is the facing
	// direction, +y is down. FX nodes are children of BoardSprite so they inherit
	// flip/tilt/bob/spin.
	[Export] public Vector2 DustFxOffset = new Vector2(-30.0f, 8.0f);
	[Export] public Vector2 SparkFxOffset = new Vector2(-12.0f, 26.0f);
	[Export] public Vector2 WispFxOffset = new Vector2(-10.0f, -2.0f);

	[Export] public float FxFadeSpeed = 8.0f;
	[Export] public float WispFullSpeedRatio = 0.55f;
	[Export] public float SparkMinStrength = 0.4f;
	[Export] public float DustMinStrength = 0.45f;

	private const string BoardFxRoot = "res://assets/hoverboards/player/fx";

	private Sprite2D? _dustFx;
	private Sprite2D? _sparksFx;
	private Sprite2D? _wispsFx;
	private Texture2D[]? _dustFrames;
	private Texture2D[]? _sparkFrames;
	private Texture2D[]? _wispFrames;
	private float _dustTimer;
	private float _sparkTimer;
	private float _wispTimer;
	private float _dustAlpha;
	private float _sparkAlpha;
	private float _wispAlpha;

	private void InitBoardFx()
	{
		_dustFrames = LoadFrames(BoardFxRoot + "/dust", "boardfx_");
		_sparkFrames = LoadFrames(BoardFxRoot + "/sparks", "boardfx_");
		_wispFrames = LoadFrames(BoardFxRoot + "/wisps", "boardfx_");

		_dustFx = CreateFxChild("BoardDust", behindParent: true);
		_wispsFx = CreateFxChild("BoardWisps", behindParent: true);
		_sparksFx = CreateFxChild("BoardSparks", behindParent: false);
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
		var sparkTarget = UseSparkFx && !IsDead && grinding
			? SparkFxAlpha * Mathf.Lerp(SparkMinStrength, 1.0f, railRatio)
			: 0.0f;
		var wispTarget = UseWispFx && !IsDead && airborne
			? WispFxAlpha * Mathf.Clamp(speed / Mathf.Max(MoveSpeed * WispFullSpeedRatio, 1.0f), 0.25f, 1.0f)
			: 0.0f;

		_dustAlpha = Mathf.MoveToward(_dustAlpha, dustTarget, deltaSeconds * FxFadeSpeed);
		_sparkAlpha = Mathf.MoveToward(_sparkAlpha, sparkTarget, deltaSeconds * FxFadeSpeed);
		_wispAlpha = Mathf.MoveToward(_wispAlpha, wispTarget, deltaSeconds * FxFadeSpeed);

		ApplyFx(_dustFx, _dustFrames, ref _dustTimer, _dustAlpha, DustFxColor, DustFxFps, DustFxOffset, deltaSeconds);
		ApplyFx(_sparksFx, _sparkFrames, ref _sparkTimer, _sparkAlpha, SparkFxColor, SparkFxFps, SparkFxOffset, deltaSeconds);
		ApplyFx(_wispsFx, _wispFrames, ref _wispTimer, _wispAlpha, WispFxColor, WispFxFps, WispFxOffset, deltaSeconds);
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
		// Children inherit BoardSprite.Modulate (BoardOpacity), so divide it back out.
		node.SelfModulate = new Color(color.R, color.G, color.B, alpha / Mathf.Max(BoardOpacity, 0.05f));
	}
}
