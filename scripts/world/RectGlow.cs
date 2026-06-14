using Godot;

public static class RectGlow
{
	private static Shader _shader;
	private static Texture2D _whiteTexture;

	private static Shader GetOrCreateShader()
	{
		if (_shader != null)
			return _shader;

		_shader = new Shader();
		_shader.Code = @"
shader_type canvas_item;
uniform vec2 glow_size;

void fragment() {
    float xDist = min(UV.x, 1.0 - UV.x) * glow_size.x;
    float yDist = min(UV.y, 1.0 - UV.y) * glow_size.y;
    float edgeDist = min(xDist, yDist);
    float t = edgeDist / 3.0;
    float cornerFade = smoothstep(0.0, 3.0, max(xDist, yDist));
    float peakAlpha = 0.35;
    float alpha = peakAlpha * clamp(t, 0.0, 1.0) * (1.0 - step(1.0, t)) * cornerFade;

    vec2 g = floor(UV * vec2(32.0, 32.0));
    float n = fract(sin(g.x * 12.9898 + g.y * 78.233) * 43758.5453);
    float grain = 1.0 + (n - 0.5) * 0.12;

    COLOR = vec4(1.0, 1.0, 1.0, alpha * grain);
}";
		return _shader;
	}

	private static Texture2D GetWhiteTexture()
	{
		if (_whiteTexture != null)
			return _whiteTexture;

		var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		image.SetPixel(0, 0, Colors.White);
		_whiteTexture = ImageTexture.CreateFromImage(image);
		return _whiteTexture;
	}

	public static Polygon2D CreateGlow(float width, float height, int zIndex)
	{
		var hw = width / 2f;
		var hh = height / 2f;

		var glow = new Polygon2D();
		glow.Polygon = new Vector2[]
		{
			new Vector2(-hw, -hh),
			new Vector2(hw, -hh),
			new Vector2(hw, hh),
			new Vector2(-hw, hh),
		};
		glow.UV = new Vector2[]
		{
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(1, 1),
			new Vector2(0, 1),
		};
		glow.Color = Colors.White;
		glow.Texture = GetWhiteTexture();
		glow.ZIndex = zIndex;

		var material = new ShaderMaterial();
		material.Shader = GetOrCreateShader();
		material.SetShaderParameter("glow_size", new Vector2(width, height));
		glow.Material = material;

		return glow;
	}
}
