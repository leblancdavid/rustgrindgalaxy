using Godot;

public partial class Shadow : Node2D
{
	[Export] public float BaseWidth = 18.0f;
	[Export] public float BaseHeight = 5.0f;
	[Export] public float MinScale = 0.4f;
	[Export] public float MaxAlpha = 0.35f;
	[Export] public float MinAlpha = 0.08f;
	[Export] public float MaxDistance = 128.0f;
	[Export] public float GroundOffset = 6.0f;
	[Export] public float RotationLerpSpeed = 20.0f;
	[Export] public float DistanceSmoothing = 0.0f;
	[Export] public uint CollisionMask = 1;
	[Export] public int EllipseSegments = 16;

	private Polygon2D _poly = null!;
	private CharacterBody2D? _body;
	private float _currentRotation;
	private float _smoothedDistance;

	public override void _Ready()
	{
		_body = GetParentOrNull<CharacterBody2D>();
		_poly = new Polygon2D
		{
			Color = new Color(0.0f, 0.0f, 0.0f, MaxAlpha),
			Polygon = BuildEllipse(BaseWidth, BaseHeight, EllipseSegments),
		};
		AddChild(_poly);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_body == null || !IsInstanceValid(_body))
		{
			_poly.Visible = false;
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
			_poly.Visible = false;
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
			_poly.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}

		_poly.Visible = true;
		GlobalPosition = new Vector2(_body.GlobalPosition.X, hitPos.Y + GroundOffset);

		var t = Mathf.Clamp(_smoothedDistance / Mathf.Max(MaxDistance, 0.001f), 0.0f, 1.0f);
		var scale = Mathf.Lerp(1.0f, MinScale, t);
		_poly.Scale = new Vector2(scale, scale);

		var alpha = Mathf.Lerp(MaxAlpha, MinAlpha, t);
		var c = _poly.Color;
		c.A = alpha;
		_poly.Color = c;

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

	private static Vector2[] BuildEllipse(float width, float height, int segments)
	{
		var points = new Vector2[segments];
		for (var i = 0; i < segments; i++)
		{
			var angle = (float)i / segments * Mathf.Tau;
			points[i] = new Vector2(Mathf.Cos(angle) * width * 0.5f, Mathf.Sin(angle) * height * 0.5f);
		}
		return points;
	}
}
