using Godot;

public static class MistFog
{
    public static Sprite2D CreateMist(LevelColorPalette palette, float bgDim, float height, float width = 3000)
    {
        var mistColor = new Color(1f, 1f, 1f, 0.55f);

        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0, 0, 0, 0));
        gradient.SetColor(1, mistColor);
        gradient.AddPoint(0.3f, new Color(mistColor.R, mistColor.G, mistColor.B, 0.15f));
        gradient.SetOffset(1, 0.5f);

        var tex = new GradientTexture2D();
        tex.Gradient = gradient;
        tex.Fill = GradientTexture2D.FillEnum.Linear;
        tex.FillFrom = new Vector2(0, 0);
        tex.FillTo = new Vector2(0, 1);
        tex.Width = (int)width;
        tex.Height = (int)height;

        var sprite = new Sprite2D();
        sprite.Texture = tex;
        sprite.Centered = false;

        var shader = new Shader();
        shader.Code = @"
shader_type canvas_item;

void fragment() {
    vec2 uv = UV;
    uv.y += sin(uv.x * 6.0 + TIME * 0.6) * 0.04;
    uv.y += sin(uv.x * 2.4 + TIME * 0.36 + 1.5) * 0.02;
    uv.x += TIME * 0.015;
    COLOR = texture(TEXTURE, uv);
}
";
        sprite.Material = new ShaderMaterial();
        ((ShaderMaterial)sprite.Material).Shader = shader;

        return sprite;
    }
}
