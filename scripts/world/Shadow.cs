using Godot;
using System.Collections.Generic;

public partial class Shadow : Node2D
{
	[Export] public float BaseWidth = 18.0f;
	[Export] public float BaseHeight = 5.0f;
	[Export] public float MinScale = 0.4f;
	[Export] public float MaxAlpha = 0.5f;
	[Export] public float MinAlpha = 0.08f;
	[Export] public float MaxDistance = 128.0f;
	[Export] public float GroundOffset = 6.0f;
	[Export] public float RotationLerpSpeed = 20.0f;
	[Export] public float DistanceSmoothing = 0.0f;
	[Export] public uint CollisionMask = 1;
	[Export] public float SizeMultiplier = 1.1f;

	// Empty = auto-detect the first real Sprite2D under the parent.
	[Export] public NodePath TargetSprite = new();
	// Silhouette shadow width/height as a fraction of the tracked visual.
	[Export] public float WidthOverVisual = 1.0f;
	// Vertical compression faking the ground plane seen from the side.
	[Export] public float ShadowSquash = 0.5f;
	[Export] public float ShadowSkewStrength = 1.0f;
	// NaN = follow WorldSun (random per level); set for editor preview.
	[Export] public float SunAngleDegrees = float.NaN;
	// Silhouettes are anchored at their feet row, so they need less offset
	// than the centered ellipse.
	[Export] public float SilhouetteGroundOffset = 1.5f;

	private const int TextureSize = 64;

	private static readonly string[] ExcludedNameParts =
	{
		"Glow", "Ripple", "Beam", "Fx", "Spark", "Wisp", "Dust", "Flash", "Trail", "Board",
	};

	private Sprite2D _sprite = null!;
	private Texture2D _ellipse = null!;
	private Node2D? _owner2D;
	private Sprite2D? _target;
	private Texture2D? _lastSource;
	private bool _hasSilhouette;
	private bool _targetLost;
	private float _currentRotation;
	private float _smoothedDistance;

	public override void _Ready()
	{
		_owner2D = GetParentOrNull<Node2D>();
		_ellipse = CreateSoftEllipseTexture(TextureSize);
		_sprite = new Sprite2D
		{
			Texture = _ellipse,
			Centered = false,
			Offset = new Vector2(-TextureSize * 0.5f, -TextureSize * 0.5f),
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
		};
		AddChild(_sprite);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_owner2D == null || !IsInstanceValid(_owner2D))
		{
			_sprite.Visible = false;
			return;
		}

		UpdateTargetTexture();
		if (_targetLost)
		{
			_sprite.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}

		var spaceState = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(
			_owner2D.GlobalPosition,
			_owner2D.GlobalPosition + new Vector2(0.0f, MaxDistance + 8.0f),
			CollisionMask);
		if (_owner2D is PhysicsBody2D body)
			query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };
		var result = spaceState.IntersectRay(query);

		if (result.Count == 0 || !result.ContainsKey("position"))
		{
			_sprite.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}

		var hitPos = (Vector2)result["position"];
		var distance = hitPos.Y - _owner2D.GlobalPosition.Y;

		if (DistanceSmoothing > 0.0f)
		{
			var smoothAlpha = 1.0f - Mathf.Exp(-DistanceSmoothing * (float)delta);
			_smoothedDistance = Mathf.Lerp(_smoothedDistance, distance, smoothAlpha);
		}
		else
		{
			_smoothedDistance = distance;
		}

		if (distance < -1.0f || distance > MaxDistance)
		{
			_sprite.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}

		_sprite.Visible = true;
		var groundOffset = _hasSilhouette ? SilhouetteGroundOffset : GroundOffset;
		GlobalPosition = new Vector2(_owner2D.GlobalPosition.X, hitPos.Y + groundOffset);

		var t = Mathf.Clamp(_smoothedDistance / Mathf.Max(MaxDistance, 0.001f), 0.0f, 1.0f);
		var scale = Mathf.Lerp(1.0f, MinScale, t);

		if (_hasSilhouette && _target != null)
		{
			var sx = Mathf.Abs(_target.Scale.X) * WidthOverVisual * scale;
			var sy = Mathf.Abs(_target.Scale.Y) * ShadowSquash * scale;
			var k = SunShear() * sx;
			// x-axis scaled, y-axis scaled + sheared: feet row (local y = 0)
			// stays welded while the far end of the shadow slides away from the sun.
			_sprite.Transform = new Transform2D(
				new Vector2(sx, 0.0f),
				new Vector2(k, sy),
				Vector2.Zero);
		}
		else
		{
			_sprite.Scale = new Vector2(
				BaseWidth * SizeMultiplier * scale / TextureSize,
				BaseHeight * SizeMultiplier * scale / TextureSize);
		}

		_sprite.Modulate = new Color(0.0f, 0.0f, 0.0f, Mathf.Lerp(MaxAlpha, MinAlpha, t));

		LerpRotationTowards(ComputeSlopeRotation(), delta);
	}

	private void UpdateTargetTexture()
	{
		if (_target != null && !IsInstanceValid(_target))
			_target = null;
		_target ??= ResolveTarget();

		if (_target == null)
		{
			// Dynamic scenes create their sprite after _Ready; retry next frame.
			return;
		}

		if (!_target.IsVisibleInTree() || _target.Texture == null)
		{
			// Tracked visual gone (e.g. crate shattered, body despawned).
			_targetLost = _lastSource != null;
			return;
		}
		_targetLost = false;

		var src = _target.Texture;
		if (src == _lastSource)
			return;

		var silhouette = ShadowSilhouette.Get(src);
		if (silhouette != null)
		{
			_sprite.Texture = silhouette;
			_sprite.Offset = new Vector2(-silhouette.GetWidth() * 0.5f, 0.0f);
			_hasSilhouette = true;
		}
		else
		{
			_sprite.Texture = _ellipse;
			_sprite.Offset = new Vector2(-TextureSize * 0.5f, -TextureSize * 0.5f);
			_hasSilhouette = false;
		}
		_lastSource = src;
	}

	private Sprite2D? ResolveTarget()
	{
		// Unassigned NodePath exports marshal as null, not empty.
		if (TargetSprite != null && !TargetSprite.IsEmpty)
			return GetNodeOrNull<Sprite2D>(TargetSprite);

		var root = GetParent();
		if (root == null)
			return null;

		var queue = new Queue<Node>();
		queue.Enqueue(root);
		while (queue.Count > 0)
		{
			foreach (var child in queue.Peek().GetChildren())
			{
				if (child is Sprite2D spr && spr != _sprite && !IsFxName(spr) && spr.Texture != null)
					return spr;
				queue.Enqueue(child);
			}
			queue.Dequeue();
		}
		return null;
	}

	private static bool IsFxName(Sprite2D spr)
	{
		var name = spr.Name.ToString();
		foreach (var part in ExcludedNameParts)
		{
			if (name.Contains(part, System.StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}

	private float SunShear()
	{
		if (float.IsNaN(SunAngleDegrees))
			return WorldSun.Shear * ShadowSkewStrength;
		return WorldSun.ShearForAngle(SunAngleDegrees) * ShadowSkewStrength;
	}

	private void LerpRotationTowards(float target, double delta)
	{
		var lerpFactor = Mathf.Clamp(RotationLerpSpeed * (float)delta, 0.0f, 1.0f);
		_currentRotation = Mathf.LerpAngle(_currentRotation, target, lerpFactor);
		Rotation = _currentRotation;
	}

	private float ComputeSlopeRotation()
	{
		if (_owner2D is not CharacterBody2D body || !body.IsOnFloor())
			return 0.0f;

		var floorNormal = body.GetFloorNormal();
		if (floorNormal == Vector2.Zero)
			return 0.0f;

		var tangent = new Vector2(floorNormal.Y, -floorNormal.X);
		if (tangent.X < 0.0f) tangent = -tangent;
		if (tangent.LengthSquared() > 0.0f) tangent = tangent.Normalized();
		return tangent.Angle();
	}

	private static ImageTexture CreateSoftEllipseTexture(int size)
	{
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var half = size * 0.5f;
		for (var x = 0; x < size; x++)
		{
			for (var y = 0; y < size; y++)
			{
				var dx = (x - half) / half;
				var dy = (y - half) / half;
				var dist = Mathf.Sqrt(dx * dx + dy * dy);
				var alpha = Mathf.Clamp(Mathf.Pow(1.0f - dist, 0.5f), 0.0f, 1.0f);
				img.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
			}
		}
		return ImageTexture.CreateFromImage(img);
	}
}
