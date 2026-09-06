using Godot;

public partial class LaunchPad : Area2D
{
	[Export] public float LaunchVelocity = -700.0f;
	[Export] public float PadWidth = 48.0f;
	[Export] public float PadHeight = 10.0f;

	public override void _Ready()
	{
		CollisionMask = 2;
		AddCollisionShape();
		AddVisuals();
		Shadow.Attach(this).MaxAlpha = 0.3f;
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
		var orangeColor = new Color(0.9f, 0.5f, 0.1f);

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
		bg.Color = orangeColor;
		AddChild(bg);

		var chevronColor = Colors.White;
		AddUpChevron(chevronColor, new Vector2(-8f, -2f), 1.0f);
		AddUpChevron(chevronColor, new Vector2(8f, -2f), 1.0f);
	}

	private void AddUpChevron(Color color, Vector2 offset, float scale)
	{
		var chevron = new Polygon2D();
		var size = 4f * scale;
		chevron.Polygon = new Vector2[]
		{
			new Vector2(-size, 0f),
			new Vector2(0f, -size),
			new Vector2(size, 0f),
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

		var velocity = player.Velocity;
		velocity.Y = LaunchVelocity;
		player.Velocity = velocity;
	}
}
