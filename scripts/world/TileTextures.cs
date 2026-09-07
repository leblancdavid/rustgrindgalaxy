using Godot;
using System.Collections.Generic;

/// <summary>
/// Seamless-grayscale texture materials for tile ground fills and grind rails.
/// Textures are multiplied by the polygon's flat color, so the existing
/// LevelColorPalette tinting (ApplyVisualPalette) stays the sole color source.
///
/// The ground shader derives its sampling position in the vertex stage from
/// MODEL_MATRIX * VERTEX (the polygon's world position) / tex_px, so the
/// pattern stays continuous across tile seams (1280 is a multiple of every
/// tile size) with no per-polygon UV baking — ground tiles never move after
/// placement (same assumption as GroundClip). The shader blends
/// tex_a/tex_b and jitters/flips them by blocky world-position hash so the
/// tiling never visibly repeats. The rail shader keeps the per-instance
/// baked UVs (offset/flip vertical mapping) that GrindRail supplies.
/// </summary>
public static class TileTextures
{
	public enum Theme { None, Building, Terrain, Catwalk }

	private const string Dir = "res://assets/tiles/";

	private const string GroundShaderCode = @"shader_type canvas_item;

uniform sampler2D tex_a : filter_nearest;
uniform sampler2D tex_b : filter_nearest;
uniform vec2 tex_px = vec2(64.0, 64.0);
uniform float block_px = 96.0;
uniform float weight = 0.55;

varying vec2 world_uv;

float hash21(vec2 p) {
	p = fract(p * vec2(233.34, 851.73));
	p += dot(p, p + 23.45);
	return fract(p.x * p.y * 511.73);
}

void vertex() {
	world_uv = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy / tex_px;
}

void fragment() {
	vec2 world = world_uv * tex_px;
	vec2 block = floor(world / block_px);
	float sel = hash21(block + vec2(3.7, 9.1));
	float ox = hash21(block + vec2(17.31, 5.9));
	float oy = hash21(block + vec2(47.97, 31.7));
	float flip = hash21(block + vec2(113.0, 57.0));

	vec2 uv = world_uv + vec2(ox, oy);
	if (flip > 0.5) {
		uv.x = -uv.x;
	}
	vec4 t = sel < 0.5 ? texture(tex_a, uv) : texture(tex_b, uv);
	COLOR.rgb *= mix(vec3(1.0), clamp(t.rgb * 2.0, 0.0, 1.0), weight);
}
";

	private const string RailShaderCode = @"shader_type canvas_item;

uniform sampler2D tex_a : filter_nearest;

varying vec2 rail_uv;

void vertex() {
	rail_uv = UV;
}

void fragment() {
	COLOR *= texture(tex_a, rail_uv);
}
";

	private struct ThemeSpec
	{
		public string TexA;
		public string TexB;
		public Vector2 TexPx;
		public float BlockPx;
	}

	private static readonly Dictionary<Theme, ThemeSpec> Specs = new()
	{
		[Theme.Building] = new ThemeSpec { TexA = "panel_00.png", TexB = "panel_01.png", TexPx = new Vector2(64, 64), BlockPx = 96 },
		[Theme.Terrain] = new ThemeSpec { TexA = "rock_00.png", TexB = "rock_01.png", TexPx = new Vector2(64, 64), BlockPx = 96 },
		[Theme.Catwalk] = new ThemeSpec { TexA = "grate_00.png", TexB = "grate_01.png", TexPx = new Vector2(32, 16), BlockPx = 80 },
	};

	private static Shader _groundShader;
	private static Shader _railShader;
	private static ShaderMaterial _railMat;
	private static readonly Dictionary<Theme, ShaderMaterial> _groundMats = new();
	private static readonly Dictionary<string, Texture2D> _texCache = new();

	private static Texture2D LoadTex(string file)
	{
		if (_texCache.TryGetValue(file, out var cached))
			return cached;
		var path = Dir + file;
		var tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		_texCache[file] = tex;
		return tex;
	}

	/// <summary>Shared ground material for a theme, or null when unavailable (flat fill fallback).</summary>
	public static ShaderMaterial GroundMaterial(Theme theme)
	{
		if (theme == Theme.None || !Specs.TryGetValue(theme, out var spec))
			return null;
		if (_groundMats.TryGetValue(theme, out var cached))
			return cached;

		var texA = LoadTex(spec.TexA);
		var texB = LoadTex(spec.TexB);
		if (texA == null || texB == null)
		{
			_groundMats[theme] = null;
			return null;
		}

		_groundShader ??= new Shader { Code = GroundShaderCode };
		var mat = new ShaderMaterial { Shader = _groundShader };
		mat.SetShaderParameter("tex_a", texA);
		mat.SetShaderParameter("tex_b", texB);
		mat.SetShaderParameter("tex_px", spec.TexPx);
		mat.SetShaderParameter("block_px", spec.BlockPx);
		_groundMats[theme] = mat;
		return mat;
	}

	public static ShaderMaterial RailMaterial()
	{
		if (_railMat != null)
			return _railMat;
		var tex = LoadTex("rail_00.png");
		if (tex == null)
			return null;
		_railShader ??= new Shader { Code = RailShaderCode };
		_railMat = new ShaderMaterial { Shader = _railShader };
		_railMat.SetShaderParameter("tex_a", tex);
		return _railMat;
	}

	/// <summary>
	/// Applies the ground-fill material to every ground visual polygon of the
	/// tile (same name filter as ApplyVisualPalette's fill branch) and to all
	/// of its rails. Theme None restores the flat fill. The shader computes
	/// world UVs itself, so no per-polygon data is baked.
	/// </summary>
	public static void ApplyTheme(LevelTile tile, Theme theme)
	{
		var mat = GroundMaterial(theme);

		foreach (var child in tile.GetChildren())
		{
			if (child is GrindRail rail)
			{
				rail.SetVisualTexture(theme == Theme.None ? null : RailMaterial());
				continue;
			}
			if (child is not Polygon2D poly || poly.Polygon.Length < 3)
				continue;

			var name = (string)poly.Name;
			var isFill = name.Contains("Visual") && !name.Contains("Rise") && !name.Contains("Edge") && !name.Contains("Trim");
			if (!isFill)
				continue;

			if (mat == null)
			{
				poly.Material = null;
				continue;
			}

			poly.Material = mat;
		}
	}
}
