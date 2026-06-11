using Godot;

public partial class ProcGenTest : Node2D
{
    [Export] public Vector2 SpawnPosition = new(200.0f, 96.0f);
    [Export] public float FallRespawnY = 420.0f;

    private Vector2 _respawnPosition;
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
        _camera.Position = new Vector2(0, -30);

        var generator = new ModuleGenerator();
        _player.SetLoadout(generator.GenerateDebugLoadout(ModuleRarity.Rare));
        _player.GlobalPosition = SpawnPosition;

        _respawnPosition = SpawnPosition;

        var seed = (long)(GD.Randi() | ((ulong)GD.Randi() << 32));
        _tileGenerator.Initialize(_player, null!, seed);
        _tileGenerator.BuildInitial();
        _tileGenerator.UpdateStreaming();
    }

    public override void _Process(double delta)
    {
        _hud.UpdatePlayerState(_player);
        _hud.UpdateTileName(GetTileLabelText());
        _tileGenerator.UpdateStreaming();

        var surfaceY = GetSurfaceYAtX(_player.GlobalPosition.X);
        var threshold = surfaceY < float.MaxValue ? surfaceY + 500f : FallRespawnY;
        if (_player.GlobalPosition.Y > threshold)
        {
            RespawnPlayer();
        }
    }

    private void RemoveCameraBounds()
    {
        _camera.LimitLeft = -10000;
        _camera.LimitTop = -10000;
        _camera.LimitRight = 25000;
        _camera.LimitBottom = 10000;
    }

    public void SetRespawnPoint(Vector2 position)
    {
        _respawnPosition = position;
    }

    private void RespawnPlayer()
    {
        _player.ResetTransientState();
        _player.Velocity = Vector2.Zero;
        _player.GlobalPosition = _respawnPosition;
    }

    private Vector2 FindSafeSpawn()
    {
        var spaceState = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(
            new Vector2(SpawnPosition.X, -500),
            new Vector2(SpawnPosition.X, FallRespawnY + 500),
            1);
        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            var hitPos = (Vector2)result["position"];
            return new Vector2(SpawnPosition.X, hitPos.Y - 30);
        }

        foreach (var tile in _tileGenerator.ActiveTiles)
        {
            if (SpawnPosition.X >= tile.GetTileLeftX() && SpawnPosition.X < tile.GetTileRightX())
            {
                var surfaceY = tile.Position.Y + tile.LeftGroundY;
                return new Vector2(SpawnPosition.X, surfaceY - 30);
            }
        }

        if (_tileGenerator.ActiveTiles.Count > 0)
        {
            var tile = _tileGenerator.ActiveTiles[0];
            var surfaceY = tile.Position.Y + tile.LeftGroundY;
            return new Vector2(tile.GetTileLeftX() + 80, surfaceY - 30);
        }

        return SpawnPosition;
    }

    private float GetSurfaceYAtX(float worldX)
    {
        foreach (var tile in _tileGenerator.ActiveTiles)
        {
            if (worldX >= tile.GetTileLeftX() && worldX < tile.GetTileRightX())
            {
                var t = (worldX - tile.GetTileLeftX()) / tile.TileWidth;
                var leftSurface = tile.Position.Y + tile.LeftGroundY;
                var rightSurface = tile.Position.Y + tile.RightGroundY;
                return Mathf.Lerp(leftSurface, rightSurface, t);
            }
        }
        return float.MaxValue;
    }

    private string GetTileLabelText()
    {
        var px = _player.GlobalPosition.X;
        for (var i = 0; i < _tileGenerator.ActiveTiles.Count; i++)
        {
            var tile = _tileGenerator.ActiveTiles[i];
            if (px >= tile.GetTileLeftX() && px < tile.GetTileRightX())
            {
                var fileName = tile.SceneFilePath.GetFile().GetBaseName();
                var typeName = fileName.EndsWith("Tile") ? fileName[..^4] : fileName;
                if (tile.Scale.X < 0)
                    typeName += " (mirrored)";
                return $"Tile [{i}/{_tileGenerator.GeneratedTileCount}]: {typeName}";
            }
        }
        return "Tile: —";
    }
}
