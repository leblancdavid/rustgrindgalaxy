using Godot;

public partial class HitSpark : Node2D
{
    private float _lifetime = 0.12f;
    private float _timer;
    private Polygon2D _spark;

    public override void _Ready()
    {
        _timer = _lifetime;
        _spark = new Polygon2D();
        var pts = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            float a = Mathf.Pi * 2 * i / 8;
            float r = i % 2 == 0 ? 6.0f : 2.5f;
            pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        _spark.Polygon = pts;
        _spark.Color = new Color(1, 1, 1, 1);
        AddChild(_spark);
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer <= 0 || _spark == null)
        {
            QueueFree();
            return;
        }

        var t = 1.0f - (_timer / _lifetime);
        var alpha = 1.0f - t;
        _spark.Color = new Color(1, 1, 1, alpha);
        _spark.Scale = new Vector2(1 + t, 1 + t);
    }
}
