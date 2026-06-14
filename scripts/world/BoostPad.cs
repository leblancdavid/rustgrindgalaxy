using Godot;

public partial class BoostPad : Area2D
{
	[Export] public float BoostMultiplier = 2.0f;
	[Export] public float BoostDuration = 1.5f;
	[Export] public float BoostImpulse = 200.0f;
	[Export] public float BoostAccelMultiplier = 3.0f;
	[Export] public float PadWidth = 48.0f;
	[Export] public float PadHeight = 10.0f;

	public override void _Ready()
	{
		AddCollisionShape();
		AddVisuals();
		BodyEntered += OnBodyEntered;
	}

	private void AddCollisionShape()
	{
		var shape = new RectangleShape2D();
		shape.Size = new Vector2(PadWidth, PadHeight);
		var collisionShape = new CollisionShape2D();
		collisionShape.Shape = shape;
		AddChild(collisionShape);
	}

	private void AddVisuals()
	{
		var greenColor = new Color(0.2f, 0.7f, 0.2f);

		var hw = PadWidth / 2f;
		var hh = PadHeight / 2f;

		var glow = RectGlow.CreateGlow(PadWidth + 6f, PadHeight + 6f, ZIndex + 1);
		AddChild(glow);

		var bg = new Polygon2D();
		bg.Polygon = new Vector2[]
		{
			new Vector2(-hw, -hh),
			new Vector2(hw, -hh),
			new Vector2(hw, hh),
			new Vector2(-hw, hh),
		};
		bg.Color = greenColor;
		AddChild(bg);

		var chevronColor = Colors.White;
		AddChevron(chevronColor, new Vector2(-6f, 0f), 1.0f);
		AddChevron(chevronColor, new Vector2(8f, 0f), 1.0f);
	}

	private void AddChevron(Color color, Vector2 offset, float scale)
	{
		var chevron = new Polygon2D();
		var size = 4f * scale;
		chevron.Polygon = new Vector2[]
		{
			new Vector2(0f, -size),
			new Vector2(size, 0f),
			new Vector2(0f, size),
		};
		chevron.Color = color;
		chevron.Position = offset;
		AddChild(chevron);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not PlayerController player)
			return;

		if (!player.IsOnFloor())
			return;

		player.ApplySpeedBoost(BoostMultiplier, BoostDuration, BoostImpulse, BoostAccelMultiplier);
	}
}
