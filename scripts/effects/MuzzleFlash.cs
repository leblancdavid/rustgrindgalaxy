using Godot;

public partial class MuzzleFlash : Node2D
{
    private float _lifetime = 0.08f;
    private float _timer;
    private Polygon2D _flash;

    public override void _Ready()
    {
        _timer = _lifetime;
        _flash = new Polygon2D();
        var pts = new Vector2[12];
        for (int i = 0; i < 12; i++)
        {
            float a = Mathf.Pi * 2 * i / 12;
            pts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4;
        }
        _flash.Polygon = pts;
        _flash.Color = new Color(1, 1, 0.8f, 1);
        AddChild(_flash);
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer <= 0 || _flash == null)
        {
            QueueFree();
            return;
        }

        var t = 1.0f - (_timer / _lifetime);
        var s = 1.0f + t * 3.0f;
        _flash.Scale = new Vector2(s, s);
        _flash.Color = new Color(1, 1, 0.8f, 1.0f - t);
    }
}
