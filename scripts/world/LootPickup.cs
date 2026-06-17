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
	private Polygon2D _visual;
	private Polygon2D _glow;
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
		var color = GetMineralColor(Mineral);
		const float radius = 6f;

		_glow = new Polygon2D();
		_glow.Polygon = BuildHexagon(radius + 4f);
		_glow.Color = new Color(color.R, color.G, color.B, 0.25f);
		_glow.ZIndex = ZIndex - 1;
		AddChild(_glow);

		_visual = new Polygon2D();
		_visual.Polygon = BuildHexagon(radius);
		_visual.Color = color;
		_visual.ZIndex = ZIndex;
		AddChild(_visual);
	}

	private void BuildScrapVisual()
	{
		_glow = new Polygon2D();
		_glow.Polygon = new Vector2[]
		{
			new Vector2(-9f, -7f),
			new Vector2(9f, -7f),
			new Vector2(9f, 7f),
			new Vector2(-9f, 7f),
		};
		_glow.Color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
		_glow.ZIndex = ZIndex - 1;
		AddChild(_glow);

		_visual = new Polygon2D();
		_visual.Polygon = new Vector2[]
		{
			new Vector2(-7f, -5f),
			new Vector2(7f, -5f),
			new Vector2(7f, 5f),
			new Vector2(-7f, 5f),
		};
		_visual.Color = new Color(0.55f, 0.52f, 0.48f);
		_visual.ZIndex = ZIndex;
		AddChild(_visual);
	}

	private static Vector2[] BuildHexagon(float radius)
	{
		var points = new Vector2[6];
		for (int i = 0; i < 6; i++)
		{
			var angle = i * Mathf.Pi / 3f - Mathf.Pi / 2f;
			points[i] = new Vector2(
				Mathf.Cos(angle) * radius,
				Mathf.Sin(angle) * radius
			);
		}
		return points;
	}

	private static Color GetMineralColor(MineralType mineral)
	{
		return mineral switch
		{
			MineralType.Cinder => new Color(0.9098f, 0.3804f, 0.2627f),
			MineralType.Verdant => new Color(0.3725f, 0.7569f, 0.4039f),
			MineralType.Azure => new Color(0.3569f, 0.6745f, 0.9451f),
			MineralType.Solar => new Color(0.9686f, 0.8078f, 0.2706f),
			MineralType.Lumen => new Color(0.9255f, 0.9412f, 0.9804f),
			MineralType.Umbra => new Color(0.3216f, 0.2745f, 0.4078f),
			_ => Colors.White,
		};
	}
}
