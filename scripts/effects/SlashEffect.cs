using Godot;

public partial class SlashEffect : Node2D
{
    private float _lifetime = 0.2f;
    private float _timer;
    private Polygon2D _arc;
    private float _startAngle = -0.5f;

    public void SetFacingRight(bool facingRight)
    {
        _startAngle = facingRight ? -0.5f : Mathf.Pi - 0.5f;
    }

    public override void _Ready()
    {
        _timer = _lifetime;
        _arc = new Polygon2D();
        _arc.Color = new Color(1, 0.85f, 0.4f, 1);
        AddChild(_arc);
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer <= 0 || _arc == null)
        {
            QueueFree();
            return;
        }

        var progress = 1.0f - (_timer / _lifetime);
        var arcAngle = Mathf.Lerp(0.3f, Mathf.Pi * 0.7f, progress);
        var radius = Mathf.Lerp(14.0f, 26.0f, progress);
        var alpha = Mathf.Lerp(1.0f, 0.0f, progress * progress);

        int resolution = 8;
        var points = new Vector2[resolution + 2];
        points[0] = Vector2.Zero;
        for (int i = 0; i <= resolution; i++)
        {
            float a = _startAngle + (arcAngle * i / resolution);
            points[i + 1] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }

        _arc.Polygon = points;
        _arc.Color = new Color(1, 0.85f, 0.4f, alpha);
    }
}
