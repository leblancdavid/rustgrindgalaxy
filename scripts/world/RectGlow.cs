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
    float axialDist = min(xDist, yDist);
    float cornerR = min(min(glow_size.x, glow_size.y) * 0.5, 8.0);
    float roundPush = cornerR * (1.0 - smoothstep(0.0, cornerR, max(xDist, yDist)));
    float edgeDist = max(0.0, axialDist - roundPush);
    float alpha = smoothstep(0.0, 12.0, edgeDist);

    vec2 g = floor(UV * vec2(32.0, 32.0));
    float n = fract(sin(g.x * 12.9898 + g.y * 78.233) * 43758.5453);
    float grain = 1.0 + (n - 0.5) * 0.16;

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
