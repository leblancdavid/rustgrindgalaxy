using Godot;

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

	private const int TextureSize = 64;

	private Sprite2D _sprite = null!;
	private CharacterBody2D? _body;
	private float _currentRotation;
	private float _smoothedDistance;

	public override void _Ready()
	{
		_body = GetParentOrNull<CharacterBody2D>();
		_sprite = new Sprite2D
		{
			Texture = CreateSoftEllipseTexture(TextureSize),
			Centered = true,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
		};
		AddChild(_sprite);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_body == null || !IsInstanceValid(_body))
		{
			_sprite.Visible = false;
			return;
		}

		var spaceState = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(
			_body.GlobalPosition,
			_body.GlobalPosition + new Vector2(0.0f, MaxDistance + 8.0f),
			CollisionMask);
		query.Exclude = new Godot.Collections.Array<Rid> { _body.GetRid() };
		var result = spaceState.IntersectRay(query);

		if (result.Count == 0 || !result.ContainsKey("position"))
		{
			_sprite.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}

		var hitPos = (Vector2)result["position"];
		var distance = hitPos.Y - _body.GlobalPosition.Y;

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
		GlobalPosition = new Vector2(_body.GlobalPosition.X, hitPos.Y + GroundOffset);

		var t = Mathf.Clamp(_smoothedDistance / Mathf.Max(MaxDistance, 0.001f), 0.0f, 1.0f);
		var scale = Mathf.Lerp(1.0f, MinScale, t);
		_sprite.Scale = new Vector2(
			BaseWidth * SizeMultiplier * scale / TextureSize,
			BaseHeight * SizeMultiplier * scale / TextureSize
		);

		_sprite.Modulate = new Color(0.0f, 0.0f, 0.0f, Mathf.Lerp(MaxAlpha, MinAlpha, t));

		LerpRotationTowards(ComputeSlopeRotation(), delta);
	}

	private void LerpRotationTowards(float target, double delta)
	{
		var lerpFactor = Mathf.Clamp(RotationLerpSpeed * (float)delta, 0.0f, 1.0f);
		_currentRotation = Mathf.LerpAngle(_currentRotation, target, lerpFactor);
		Rotation = _currentRotation;
	}

	private float ComputeSlopeRotation()
	{
		if (_body == null || !_body.IsOnFloor())
			return 0.0f;

		var floorNormal = _body.GetFloorNormal();
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
