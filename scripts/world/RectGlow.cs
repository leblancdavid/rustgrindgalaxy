using Godot;

public struct GlowParams
{
	public Color Color;
	public float BorderThickness;
	public float CornerRadius;
	public float PeakAlpha;

	public static GlowParams Default => new()
	{
		Color = Colors.White,
		BorderThickness = 3f,
		CornerRadius = 3f,
		PeakAlpha = 0.35f,
	};
}

public static class RectGlow
{
	private static Shader _rectShader;
	private static Shader _circleShader;
	private static Shader _alphaShader;
	private static Texture2D _whiteTexture;

	private static Shader GetOrCreateRectShader()
	{
		if (_rectShader != null)
			return _rectShader;

		_rectShader = new Shader();
		_rectShader.Code = @"
shader_type canvas_item;

uniform vec2 glow_size;
uniform float border_thickness = 3.0;
uniform float corner_radius = 3.0;
uniform float peak_alpha = 0.35;
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    float xDist = min(UV.x, 1.0 - UV.x) * glow_size.x;
    float yDist = min(UV.y, 1.0 - UV.y) * glow_size.y;
    float edgeDist = min(xDist, yDist);
    float t = edgeDist / border_thickness;
    float cornerFade = smoothstep(0.0, corner_radius, max(xDist, yDist));
    float alpha = peak_alpha * clamp(t, 0.0, 1.0) * (1.0 - step(1.0, t)) * cornerFade;

    vec2 g = floor(UV * vec2(32.0, 32.0));
    float n = fract(sin(g.x * 12.9898 + g.y * 78.233) * 43758.5453);
    float grain = 1.0 + (n - 0.5) * 0.12;

    COLOR = vec4(glow_color.rgb, alpha * grain * glow_color.a);
}";
		return _rectShader;
	}

	private static Shader GetOrCreateCircleShader()
	{
		if (_circleShader != null)
			return _circleShader;

		_circleShader = new Shader();
		_circleShader.Code = @"
shader_type canvas_item;

uniform vec2 glow_size;
uniform float radius;
uniform float border_thickness = 3.0;
uniform float peak_alpha = 0.35;
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    vec2 center = glow_size / 2.0;
    vec2 pos = UV * glow_size;
    float dist = length(pos - center);
    float R = radius + border_thickness;
    float edgeDist = R - dist;
    float t = edgeDist / border_thickness;
    float alpha = peak_alpha * clamp(t, 0.0, 1.0) * (1.0 - step(1.0, t));

    vec2 g = floor(UV * vec2(32.0, 32.0));
    float n = fract(sin(g.x * 12.9898 + g.y * 78.233) * 43758.5453);
    float grain = 1.0 + (n - 0.5) * 0.12;

    COLOR = vec4(glow_color.rgb, alpha * grain * glow_color.a);
}";
		return _circleShader;
	}

	private static Shader GetOrCreateAlphaShader()
	{
		if (_alphaShader != null)
			return _alphaShader;

		_alphaShader = new Shader();
		_alphaShader.Code = @"
shader_type canvas_item;

uniform vec2 glow_size;
uniform sampler2D object_texture : filter_nearest, repeat_disable;
uniform vec2 object_size;
uniform float border_thickness = 3.0;
uniform float corner_radius = 3.0;
uniform float peak_alpha = 0.35;
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    vec2 pad = vec2(border_thickness);
    vec2 objUV = (UV * glow_size - pad) / object_size;

    float selfAlpha = texture(object_texture, objUV).a;
    float insideMask = 1.0 - step(0.5, selfAlpha);

    float minDist = border_thickness + 1.0;
    vec2 texelSize = 1.0 / object_size;
    int sampleRadius = int(border_thickness) + 2;

    for (int x = -sampleRadius; x <= sampleRadius; x++) {
        for (int y = -sampleRadius; y <= sampleRadius; y++) {
            vec2 sampleUV = objUV + vec2(float(x), float(y)) * texelSize;
            if (sampleUV.x >= 0.0 && sampleUV.x <= 1.0 && sampleUV.y >= 0.0 && sampleUV.y <= 1.0) {
                float a = texture(object_texture, sampleUV).a;
                if (a > 0.5) {
                    float dist = length(vec2(float(x), float(y)));
                    minDist = min(minDist, dist);
                }
            }
        }
    }

    float t = minDist / border_thickness;
    float alpha = peak_alpha * clamp(1.0 - t, 0.0, 1.0) * step(0.0, t) * (1.0 - step(1.0, t));
    alpha *= insideMask;

    float xDist = min(UV.x, 1.0 - UV.x) * glow_size.x;
    float yDist = min(UV.y, 1.0 - UV.y) * glow_size.y;
    float cornerFade = smoothstep(0.0, corner_radius, max(xDist, yDist));
    alpha *= cornerFade;

    vec2 g = floor(UV * vec2(32.0, 32.0));
    float n = fract(sin(g.x * 12.9898 + g.y * 78.233) * 43758.5453);
    float grain = 1.0 + (n - 0.5) * 0.12;

    COLOR = vec4(glow_color.rgb, alpha * grain * glow_color.a);
}";
		return _alphaShader;
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

	private static void ApplyParams(ShaderMaterial material, GlowParams p)
	{
		material.SetShaderParameter("border_thickness", p.BorderThickness);
		material.SetShaderParameter("corner_radius", p.CornerRadius);
		material.SetShaderParameter("peak_alpha", p.PeakAlpha);
		material.SetShaderParameter("glow_color", p.Color);
	}

	public static Polygon2D CreateGlow(float width, float height, int zIndex)
	{
		return CreateGlow(width, height, zIndex, null);
	}

	public static Polygon2D CreateGlow(float width, float height, int zIndex, GlowParams? paramOverride)
	{
		var p = paramOverride ?? GlowParams.Default;
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
		material.Shader = GetOrCreateRectShader();
		material.SetShaderParameter("glow_size", new Vector2(width, height));
		ApplyParams(material, p);
		glow.Material = material;

		return glow;
	}

	public static Polygon2D CreateCircleGlow(float radius, int zIndex, GlowParams? paramOverride = null)
	{
		var p = paramOverride ?? GlowParams.Default;
		float R = radius + p.BorderThickness;

		int segments = 32;
		var polygon = new Vector2[segments];
		var uv = new Vector2[segments];

		for (int i = 0; i < segments; i++)
		{
			float angle = (float)i / segments * Mathf.Tau;
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			polygon[i] = dir * R;
			uv[i] = dir * 0.5f + new Vector2(0.5f, 0.5f);
		}

		var glow = new Polygon2D();
		glow.Polygon = polygon;
		glow.UV = uv;
		glow.Color = Colors.White;
		glow.Texture = GetWhiteTexture();
		glow.ZIndex = zIndex;

		var material = new ShaderMaterial();
		material.Shader = GetOrCreateCircleShader();
		material.SetShaderParameter("glow_size", new Vector2(R * 2, R * 2));
		material.SetShaderParameter("radius", radius);
		ApplyParams(material, p);
		glow.Material = material;

		return glow;
	}

	public static Polygon2D CreateAlphaGlow(Texture2D objectTexture, float padding, int zIndex, GlowParams? paramOverride = null)
	{
		var p = paramOverride ?? GlowParams.Default;
		float w = objectTexture.GetWidth() + padding * 2;
		float h = objectTexture.GetHeight() + padding * 2;
		var hw = w / 2f;
		var hh = h / 2f;

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
		material.Shader = GetOrCreateAlphaShader();
		material.SetShaderParameter("glow_size", new Vector2(w, h));
		material.SetShaderParameter("object_texture", objectTexture);
		material.SetShaderParameter("object_size", new Vector2(objectTexture.GetWidth(), objectTexture.GetHeight()));
		material.SetShaderParameter("border_thickness", p.BorderThickness);
		material.SetShaderParameter("corner_radius", p.CornerRadius);
		material.SetShaderParameter("peak_alpha", p.PeakAlpha);
		material.SetShaderParameter("glow_color", p.Color);
		glow.Material = material;

		return glow;
	}
}
