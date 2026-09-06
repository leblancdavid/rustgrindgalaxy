using Godot;

public enum LootPickupType
{
	Mineral,
	Scrap,
}

public partial class LootPickup : Area2D
{
	private enum State { Launch, Magnetize }

	private State _state = State.Launch;
	private PlayerController _player;
	private Vector2 _velocity;
	private Sprite2D _visual;
	private float _timer;
	private bool _collected;

    private const float FallGravity = 600f;
    private const float LaunchDuration = 0.4f;
    private const float MaxLifetime = 5f;

	public LootPickupType PickupType { get; set; }
	public MineralType Mineral { get; set; } = MineralType.Cinder;
	public int MineralAmount { get; set; } = 1;
	public int ScrapAmount { get; set; } = 1;

	public static void Spawn(Node parent, Vector2 position, PlayerController player,
		LootPickupType type, MineralType mineral, int amount)
	{
		var pickup = new LootPickup();
		pickup._player = player;
		pickup.PickupType = type;
		pickup.Mineral = mineral;
		pickup.MineralAmount = amount;
		pickup.ScrapAmount = amount;

		parent.AddChild(pickup);
		pickup.GlobalPosition = position;
	}

	public override void _Ready()
	{
		CollisionMask = 2;

		var rng = new RandomNumberGenerator();
		rng.Randomize();

		_velocity = new Vector2(
			0f,
			rng.RandfRange(-300f, -200f)
		);

		if (PickupType == LootPickupType.Mineral)
			BuildMineralVisual();
		else
			BuildScrapVisual();

		var collision = new CollisionShape2D();
		collision.Shape = new CircleShape2D { Radius = 8f };
		AddChild(collision);

		Monitoring = false;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		var dt = (float)delta;
		_timer += dt;

		if (_timer >= MaxLifetime)
		{
			QueueFree();
			return;
		}

		switch (_state)
		{
			case State.Launch:
				_velocity.Y += FallGravity * dt;
				if (_timer >= LaunchDuration)
				{
					_state = State.Magnetize;
					Monitoring = true;
				}
				break;

            case State.Magnetize:
                if (_player != null && !_player.IsDead)
                {
                    var dir = (_player.GlobalPosition - GlobalPosition).Normalized();
                    var playerSpeed = _player.Velocity.Length();
                    _velocity = dir * Mathf.Max(playerSpeed * 2f, 200f);
                }
                else
                {
                    _velocity.Y += FallGravity * dt;
                }
                break;
		}

		Position += _velocity * dt;
		_visual.Rotation += _velocity.Length() * 0.002f;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected)
			return;
		if (body is not PlayerController player)
			return;

		_collected = true;

		var world = player.GetParentOrNull<World>();
		if (PickupType == LootPickupType.Mineral)
			world?.CollectMineral(Mineral, MineralAmount);
		else
			world?.CollectScrap(ScrapAmount);

		QueueFree();
	}

	private void BuildMineralVisual()
	{
		var sprite = LootVisuals.PickMineral();
		_visual = new Sprite2D();
		_visual.Texture = sprite.Art;
		_visual.Scale = Vector2.One * LootVisuals.PickupVisualScale;
		_visual.Modulate = LevelColorPalette.GetMineralLight(Mineral);
		_visual.ZIndex = ZIndex;
		LootVisuals.AttachGlow(_visual, sprite);
		AddChild(_visual);
		Shadow.Attach(this).MaxAlpha = 0.3f;
	}

	private void BuildScrapVisual()
	{
		var sprite = LootVisuals.PickScrap();
		_visual = new Sprite2D();
		_visual.Texture = sprite.Art;
		_visual.Scale = Vector2.One * LootVisuals.PickupVisualScale;
		_visual.ZIndex = ZIndex;
		LootVisuals.AttachGlow(_visual, sprite);
		AddChild(_visual);
		Shadow.Attach(this).MaxAlpha = 0.3f;
	}
}
