using Godot;

public partial class MovementTest : Node2D
{
    [Export] public Vector2 SpawnPosition = new(72.0f, 240.0f);
    [Export] public float FallRespawnY = 420.0f;
    [Export] public Rect2I CameraBounds = new(0, 0, 1280, 360);

    private PlayerController _player = null!;
    private Camera2D _camera = null!;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("Player");
        _camera = _player.GetNode<Camera2D>("Camera2D");

        _player.GlobalPosition = SpawnPosition;
        ApplyCameraBounds();
    }

    public override void _Process(double delta)
    {
        if (_player.GlobalPosition.Y > FallRespawnY)
        {
            RespawnPlayer();
        }
    }

    private void ApplyCameraBounds()
    {
        _camera.LimitLeft = CameraBounds.Position.X;
        _camera.LimitTop = CameraBounds.Position.Y;
        _camera.LimitRight = CameraBounds.End.X;
        _camera.LimitBottom = CameraBounds.End.Y;
    }

    private void RespawnPlayer()
    {
        _player.GlobalPosition = SpawnPosition;
        _player.Velocity = Vector2.Zero;
    }
}
