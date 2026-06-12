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
    private ColorRect _spaceRect = null!;
    private ColorRect _upperBand = null!;
    private ColorRect _glowStripe = null!;
    private Sprite2D _fogMist = null!;

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
        var rng = new RandomNumberGenerator { Seed = (ulong)seed };

        var primary = (MineralType)(rng.Randi() % 6);
        var secondary = (MineralType)(rng.Randi() % 6);
        while (secondary == primary) secondary = (MineralType)(rng.Randi() % 6);
        var palette = LevelColorPalette.FromMinerals(primary, secondary);
        GD.Print($"Palette: {primary}/{secondary}");
        var darkenBg = rng.Randf() < 0.5f;
        var bgDim = darkenBg ? 0.65f : 1.0f;
        var fgPalette = darkenBg ? palette : palette.WithBrightness(0.85f);

        ApplyPalette(palette, bgDim);

        _tileGenerator.Initialize(_player, null!, seed, fgPalette);
        _tileGenerator.BuildInitial();
        _tileGenerator.UpdateStreaming();
    }

    private void ApplyPalette(LevelColorPalette palette, float bgDim = 1.0f)
    {
        _spaceRect = GetNode<ColorRect>("ParallaxBackground/DeepSpace/SpaceRect");
        _upperBand = GetNode<ColorRect>("ParallaxBackground/FarLayer/UpperBand");
        _glowStripe = GetNode<ColorRect>("ParallaxBackground/FarLayer/GlowStripe");

        _spaceRect.Color = new Color(palette.PrimaryDark.R * bgDim, palette.PrimaryDark.G * bgDim, palette.PrimaryDark.B * bgDim, 1f);
        _upperBand.Color = new Color(palette.PrimaryMedium.R * bgDim, palette.PrimaryMedium.G * bgDim, palette.PrimaryMedium.B * bgDim, 1f);
        _glowStripe.Color = new Color(palette.SecondaryLight.R * bgDim, palette.SecondaryLight.G * bgDim, palette.SecondaryLight.B * bgDim, 0.2f);

        var farLayer = GetNode("ParallaxBackground/FarLayer");
        foreach (var child in farLayer.GetChildren())
        {
            if (child is Polygon2D poly && child.Name.ToString().StartsWith("Silhouette"))
                poly.Color = new Color(palette.PrimaryDark.R * bgDim, palette.PrimaryDark.G * bgDim, palette.PrimaryDark.B * bgDim, 0.55f);
        }

        var midLayer = GetNode("ParallaxBackground/MidLayer");
        foreach (var child in midLayer.GetChildren())
        {
            if (child is Polygon2D poly)
            {
                var name = child.Name.ToString();
                if (name.StartsWith("MidPanel"))
                    poly.Color = new Color(palette.PrimaryMedium.R * bgDim, palette.PrimaryMedium.G * bgDim, palette.PrimaryMedium.B * bgDim, 0.38f);
                else if (name.StartsWith("Support"))
                    poly.Color = new Color(palette.PrimaryLight.R * bgDim, palette.PrimaryLight.G * bgDim, palette.PrimaryLight.B * bgDim, 0.3f);
            }
        }

        _fogMist = new Sprite2D();
        _fogMist.Centered = false;
        _fogMist.Position = new Vector2(0, 144);
        AddChild(_fogMist);
        MoveChild(_fogMist, 1);

        var fogVis = new Color(palette.SecondaryMedium.R * bgDim, palette.SecondaryMedium.G * bgDim, palette.SecondaryMedium.B * bgDim, 0.55f);
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0, 0, 0, 0));
        gradient.SetColor(1, fogVis);
        gradient.AddPoint(0.3f, new Color(fogVis.R, fogVis.G, fogVis.B, 0.15f));
        var tex = new GradientTexture2D();
        tex.Gradient = gradient;
        tex.Fill = GradientTexture2D.FillEnum.Linear;
        tex.FillFrom = new Vector2(0, 0);
        tex.FillTo = new Vector2(0, 1);
        tex.Width = 640;
        tex.Height = 216;
        _fogMist.Texture = tex;
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
                return $"Tile [{i}/{_tileGenerator.GeneratedTileCount}]: {typeName}";
            }
        }
        return "Tile: —";
    }
}
