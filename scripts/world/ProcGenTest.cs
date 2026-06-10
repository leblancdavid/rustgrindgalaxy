using Godot;

public partial class ProcGenTest : Node2D
{
    [Export] public Vector2 SpawnPosition = new(200.0f, 96.0f);
    [Export] public float FallRespawnY = 420.0f;

    private PlayerController _player = null!;
    private Camera2D _camera = null!;
    private Hud _hud = null!;
    private TileLevelGenerator _tileGenerator = null!;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("Player");
        _camera = _player.GetNode<Camera2D>("Camera2D");
        _hud = GetNode<Hud>("Hud");
        _tileGenerator = GetNode<TileLevelGenerator>("TileGenerator");

        RemoveCameraBounds();

        var generator = new ModuleGenerator();
        _player.SetLoadout(generator.GenerateDebugLoadout(ModuleRarity.Rare));
        _player.GlobalPosition = SpawnPosition;

        _tileGenerator.Initialize(_player, 42);
        _tileGenerator.BuildInitial();
        _tileGenerator.UpdateStreaming();
    }

    public override void _Process(double delta)
    {
        _hud.UpdatePlayerState(_player);
        _tileGenerator.UpdateStreaming();

        if (_player.GlobalPosition.Y > FallRespawnY)
        {
            RespawnPlayer();
        }
    }

    private void RemoveCameraBounds()
    {
        _camera.LimitLeft = -10000;
        _camera.LimitTop = -10000;
        _camera.LimitRight = 10000;
        _camera.LimitBottom = 10000;
    }

    private void RespawnPlayer()
    {
        _player.ResetTransientState();
        _player.GlobalPosition = SpawnPosition;
        _player.Velocity = Vector2.Zero;
    }
}
