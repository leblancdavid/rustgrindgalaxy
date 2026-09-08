using Godot;
using System.Collections.Generic;

/// <summary>
/// Seamless-grayscale texture materials for tile ground fills and grind rails.
/// Textures are multiplied by the polygon's flat color, so the existing
/// LevelColorPalette tinting (ApplyVisualPalette) stays the sole color source.
///
/// Two-layer system:
/// - FOUNDATION: Thick structural fill, world-space UV shader, seamless 64x64 tiling
/// - SURFACE:    Thin walkable layer (4-8px), follows FloorSegments, baked UVs, strip textures
/// </summary>
public static class TileTextures
{
	// Legacy single-theme enum (kept for backward compatibility)
	public enum Theme { None, Building, Terrain, Catwalk }

	// New two-layer theme system
	public enum SurfaceTheme { None, Grate, MetalPlate, Concrete, RockTop, Organic, Ice }
	public enum FoundationTheme { None, MetalPanel, RockStrata, ConcreteBlock, Dirt, Organic, Ice }

	public struct ThemePair
	{
		public SurfaceTheme Surface;
		public FoundationTheme Foundation;
		public float SurfaceThickness;  // 4-8px per theme
		public int SurfaceVariants;     // 2-4 strip variants
	}

	/// <summary>Curated theme presets - surface + foundation pairs that visually belong together.</summary>
	public static readonly Dictionary<string, ThemePair> ThemePresets = new()
	{
		["Industrial"]      = new ThemePair { Surface = SurfaceTheme.Grate,        Foundation = FoundationTheme.MetalPanel,     SurfaceThickness = 4f, SurfaceVariants = 2 },
		["Catwalk"]         = new ThemePair { Surface = SurfaceTheme.Grate,        Foundation = FoundationTheme.MetalPanel,     SurfaceThickness = 4f, SurfaceVariants = 2 },
		["HeavyIndustrial"] = new ThemePair { Surface = SurfaceTheme.MetalPlate,   Foundation = FoundationTheme.MetalPanel,     SurfaceThickness = 6f, SurfaceVariants = 3 },
		["Derelict"]        = new ThemePair { Surface = SurfaceTheme.Concrete,     Foundation = FoundationTheme.ConcreteBlock,  SurfaceThickness = 8f, SurfaceVariants = 4 },
		["Surface"]         = new ThemePair { Surface = SurfaceTheme.RockTop,      Foundation = FoundationTheme.RockStrata,     SurfaceThickness = 8f, SurfaceVariants = 3 },
		["Organic"]         = new ThemePair { Surface = SurfaceTheme.Organic,      Foundation = FoundationTheme.Dirt,           SurfaceThickness = 6f, SurfaceVariants = 4 },
		["Ice"]             = new ThemePair { Surface = SurfaceTheme.Ice,          Foundation = FoundationTheme.Ice,            SurfaceThickness = 4f, SurfaceVariants = 2 },
	};

	private const string Dir = "res://assets/tiles/";

	// Foundation shader (existing) - world-space UV, seamless tiling with block jitter
	private const string FoundationShaderCode = @"shader_type canvas_item;

uniform sampler2D tex_a : filter_nearest;
uniform sampler2D tex_b : filter_nearest;
uniform vec2 tex_px = vec2(64.0, 64.0);
uniform vec2 tex_scale = vec2(1.0, 1.0);
uniform float block_px = 96.0;
uniform float weight = 0.55;

varying vec2 world_uv;

float hash21(vec2 p) {
	p = fract(p * vec2(233.34, 851.73));
	p += dot(p, p + 23.45);
	return fract(p.x * p.y * 511.73);
}

void vertex() {
	world_uv = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy / (tex_px * tex_scale);
}

void fragment() {
	vec2 world = world_uv * tex_px;
	vec2 block = floor(world / block_px);
	float sel = hash21(block + vec2(3.7, 9.1));
	float ox = hash21(block + vec2(17.31, 5.9));
	float oy = hash21(block + vec2(47.97, 31.7));
	float flip = hash21(block + vec2(113.0, 57.0));

	vec2 uv = fract(world_uv + vec2(ox, oy));
	if (flip > 0.5) {
		uv.x = -uv.x;
	}
	vec4 ta = texture(tex_a, uv);
	vec4 tb = texture(tex_b, uv);
	vec4 t = mix(ta, tb, step(0.5, sel));
	COLOR.rgb *= mix(vec3(1.0), clamp(t.rgb * 2.0, 0.0, 1.0), weight);
}
";

	// Surface shader (NEW) - simple sampled texture with pre-baked UVs, variant selection via uniform
	private const string SurfaceShaderCode = @"shader_type canvas_item;

uniform sampler2D tex_a : filter_nearest;
uniform sampler2D tex_b : filter_nearest;
uniform sampler2D tex_c : filter_nearest;
uniform sampler2D tex_d : filter_nearest;
uniform float variant_index = 0.0;

varying vec2 uv;

void vertex() {
	uv = UV;
}

void fragment() {
	vec4 tex = texture(tex_a, uv);
	if (variant_index > 0.5) tex = texture(tex_b, uv);
	if (variant_index > 1.5) tex = texture(tex_c, uv);
	if (variant_index > 2.5) tex = texture(tex_d, uv);
	
	COLOR = tex;
	// Multiply by vertex color for palette tinting
	COLOR.rgb *= COLOR.a;
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

	private struct FoundationSpec
	{
		public string TexA;
		public string TexB;
		public Vector2 TexPx;
		public Vector2 TexScale;
		public float BlockPx;
	}

	private struct SurfaceSpec
	{
		public string[] Variants;  // 2-4 strip texture filenames
		public Vector2 TexPx;      // Strip texture size (e.g., 64x8)
	}

	private static readonly Dictionary<FoundationTheme, FoundationSpec> FoundationSpecs = new()
	{
		[FoundationTheme.MetalPanel]    = new FoundationSpec { TexA = "panel_00.png", TexB = "panel_01.png", TexPx = new Vector2(64, 64), TexScale = new Vector2(1.0f, 1.0f), BlockPx = 96 },
		[FoundationTheme.RockStrata]    = new FoundationSpec { TexA = "rock_00.png", TexB = "rock_01.png", TexPx = new Vector2(64, 64), TexScale = new Vector2(1.0f, 1.0f), BlockPx = 96 },
		[FoundationTheme.ConcreteBlock] = new FoundationSpec { TexA = "concrete_00.png", TexB = "concrete_01.png", TexPx = new Vector2(64, 64), TexScale = new Vector2(1.0f, 1.0f), BlockPx = 96 },
		[FoundationTheme.Dirt]          = new FoundationSpec { TexA = "dirt_00.png", TexB = "dirt_01.png", TexPx = new Vector2(64, 64), TexScale = new Vector2(1.0f, 1.0f), BlockPx = 96 },
		[FoundationTheme.Organic]       = new FoundationSpec { TexA = "organic_00.png", TexB = "organic_01.png", TexPx = new Vector2(64, 64), TexScale = new Vector2(1.0f, 1.0f), BlockPx = 96 },
		[FoundationTheme.Ice]           = new FoundationSpec { TexA = "ice_00.png", TexB = "ice_01.png", TexPx = new Vector2(64, 64), TexScale = new Vector2(1.0f, 1.0f), BlockPx = 96 },
	};

	private static readonly Dictionary<SurfaceTheme, SurfaceSpec> SurfaceSpecs = new()
	{
		[SurfaceTheme.Grate]       = new SurfaceSpec { Variants = new[] { "grate_strip_00.png", "grate_strip_01.png" }, TexPx = new Vector2(64, 8) },
		[SurfaceTheme.MetalPlate]  = new SurfaceSpec { Variants = new[] { "plate_strip_00.png", "plate_strip_01.png", "plate_strip_02.png" }, TexPx = new Vector2(64, 8) },
		[SurfaceTheme.Concrete]    = new SurfaceSpec { Variants = new[] { "concrete_strip_00.png", "concrete_strip_01.png", "concrete_strip_02.png", "concrete_strip_03.png" }, TexPx = new Vector2(64, 8) },
		[SurfaceTheme.RockTop]     = new SurfaceSpec { Variants = new[] { "rocktop_strip_00.png", "rocktop_strip_01.png", "rocktop_strip_02.png" }, TexPx = new Vector2(64, 8) },
		[SurfaceTheme.Organic]     = new SurfaceSpec { Variants = new[] { "organic_strip_00.png", "organic_strip_01.png", "organic_strip_02.png", "organic_strip_03.png" }, TexPx = new Vector2(64, 8) },
		[SurfaceTheme.Ice]         = new SurfaceSpec { Variants = new[] { "ice_strip_00.png", "ice_strip_01.png" }, TexPx = new Vector2(64, 8) },
	};

	private static Shader _foundationShader;
	private static Shader _surfaceShader;
	private static Shader _railShader;
	private static ShaderMaterial _railMat;
	private static readonly Dictionary<FoundationTheme, ShaderMaterial> _foundationMats = new();
	private static readonly Dictionary<SurfaceTheme, ShaderMaterial> _surfaceMats = new();
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

	/// <summary>Foundation material - world-space UV shader for thick structural fill.</summary>
	public static ShaderMaterial GetFoundationMaterial(FoundationTheme theme)
	{
		if (theme == FoundationTheme.None || !FoundationSpecs.TryGetValue(theme, out var spec))
			return null;
		if (_foundationMats.TryGetValue(theme, out var cached))
			return cached;

		var texA = LoadTex(spec.TexA);
		var texB = LoadTex(spec.TexB);
		if (texA == null || texB == null)
		{
			_foundationMats[theme] = null;
			return null;
		}

		_foundationShader ??= new Shader { Code = FoundationShaderCode };
		var mat = new ShaderMaterial { Shader = _foundationShader };
		mat.SetShaderParameter("tex_a", texA);
		mat.SetShaderParameter("tex_b", texB);
		mat.SetShaderParameter("tex_px", spec.TexPx);
		mat.SetShaderParameter("tex_scale", spec.TexScale);
		mat.SetShaderParameter("block_px", spec.BlockPx);
		_foundationMats[theme] = mat;
		return mat;
	}

	/// <summary>Surface material - simple texture with pre-baked UVs, variant selection via uniform.</summary>
	public static ShaderMaterial GetSurfaceMaterial(SurfaceTheme theme)
	{
		if (theme == SurfaceTheme.None || !SurfaceSpecs.TryGetValue(theme, out var spec))
			return null;
		if (_surfaceMats.TryGetValue(theme, out var cached))
			return cached;

		var variants = spec.Variants;
		var texA = variants.Length > 0 ? LoadTex(variants[0]) : null;
		var texB = variants.Length > 1 ? LoadTex(variants[1]) : null;
		var texC = variants.Length > 2 ? LoadTex(variants[2]) : null;
		var texD = variants.Length > 3 ? LoadTex(variants[3]) : null;

		if (texA == null)
		{
			_surfaceMats[theme] = null;
			return null;
		}

		_surfaceShader ??= new Shader { Code = SurfaceShaderCode };
		var mat = new ShaderMaterial { Shader = _surfaceShader };
		mat.SetShaderParameter("tex_a", texA);
		mat.SetShaderParameter("tex_b", texB);
		mat.SetShaderParameter("tex_c", texC);
		mat.SetShaderParameter("tex_d", texD);
		// variant_index set per-polygon in LevelTile.BuildSurfaceVisuals()
		_surfaceMats[theme] = mat;
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
	/// Legacy single-theme ApplyTheme (backward compatibility).
	/// Applies foundation material to all Visual polygons.
	/// </summary>
	public static void ApplyTheme(LevelTile tile, Theme theme)
	{
		var mat = theme switch
		{
			Theme.Building => GetFoundationMaterial(FoundationTheme.MetalPanel),
			Theme.Terrain => GetFoundationMaterial(FoundationTheme.RockStrata),
			Theme.Catwalk => GetFoundationMaterial(FoundationTheme.MetalPanel),
			_ => null
		};

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

	/// <summary>
	/// New two-layer ApplyThemePair.
	/// Foundation -> Foundation*Visual polygons (thick, world-space UV)
	/// Surface -> Surface*Visual/Trim polygons (thin, baked UVs)
	/// </summary>
	public static void ApplyThemePair(LevelTile tile, ThemePair pair)
	{
		var foundationMat = GetFoundationMaterial(pair.Foundation);
		var surfaceMat = GetSurfaceMaterial(pair.Surface);

		foreach (var child in tile.GetChildren())
		{
			if (child is GrindRail rail)
			{
				rail.SetVisualTexture(RailMaterial());
				continue;
			}
			if (child is not Polygon2D poly || poly.Polygon.Length < 3)
				continue;

			var name = (string)poly.Name;

			// Foundation layer: FoundationVisual, FoundationRampVisual, FoundationLandingVisual, Foundation*Trim, Foundation*Rise
			bool isFoundation = name.StartsWith("Foundation");
			// Surface layer: SurfaceVisual, SurfaceTrim
			bool isSurface = name.StartsWith("Surface");

			if (isFoundation)
			{
				if (foundationMat == null)
					poly.Material = null;
				else
					poly.Material = foundationMat;
			}
			else if (isSurface)
			{
				if (surfaceMat == null)
				{
					poly.Material = null;
				}
				else
				{
					// Create instance material per polygon to set variant_index
					// Variant index is encoded in name: SurfaceVisual_{segmentIndex}_{variantIndex}
					var instanceMat = (ShaderMaterial)surfaceMat.Duplicate();
					int variant = 0;
					var parts = name.Split('_');
					if (parts.Length >= 3 && int.TryParse(parts[^1], out var parsedVariant))
						variant = parsedVariant;
					instanceMat.SetShaderParameter("variant_index", (float)variant);
					poly.Material = instanceMat;
				}
			}
		}
	}
}