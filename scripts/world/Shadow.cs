using Godot;
using System.Collections.Generic;

public partial class Shadow : Node2D
{
	[Export] public float BaseWidth = 18.0f;
	[Export] public float BaseHeight = 5.0f;
	[Export] public float MinScale = 0.4f;
	[Export] public float MaxAlpha = 0.5f;
	[Export] public float MinAlpha = 0.08f;
	[Export] public float MaxDistance = 128.0f;
	[Export] public float GroundOffset = 6.0f;
	[Export] public float RotationLerpSpeed = 20.0f;
	[Export] public float DistanceSmoothing = 0.0f;
	[Export] public uint CollisionMask = 1;
	[Export] public float SizeMultiplier = 1.1f;

	// Empty = auto-detect the first real Sprite2D/Polygon2D visual under the parent.
	[Export] public NodePath TargetSprite = new();
	// Silhouette shadow width/height as a fraction of the tracked visual.
	[Export] public float WidthOverVisual = 1.0f;
	// Vertical compression faking the ground plane seen from the side.
	[Export] public float ShadowSquash = 0.5f;
	[Export] public float ShadowSkewStrength = 1.0f;
	// NaN = follow WorldSun (random per level); set for editor preview.
	[Export] public float SunAngleDegrees = float.NaN;
	// Silhouettes are anchored at their feet row; FootLocalY records the
	// object's bottom pixel row, so 0 makes the shadow flush with the art.
	// Positive gaps it, negative overlaps into the object.
	[Export] public float SilhouetteGroundOffset = 0.0f;
	// Pads/beacons/props are placed centered on or sunk into the floor, so a
	// ray from their origin starts inside the ground and misses. Retry once
	// from this height above the origin, accepting only near-level surfaces;
	// the visible result is then clipped to opaque ground art by GroundClip.
	[Export] public float EmbeddedRetryLift = 32.0f;
	[Export] public float EmbeddedMaxSunk = 24.0f;

	private const int TextureSize = 64;
	private const int MaxClipPolys = 16;
	private const int MaxClipEdges = 256;

	private const string ClipShaderCode = @"shader_type canvas_item;

uniform vec2 edge_a[256];
uniform vec2 edge_b[256];
uniform int poly_end[16];
uniform int poly_count = 0;
uniform vec2 u_origin = vec2(0.0, 0.0);
uniform vec2 u_col0 = vec2(1.0, 0.0);
uniform vec2 u_col1 = vec2(0.0, 1.0);

varying vec2 world_xy;

void vertex() {
	world_xy = u_origin + VERTEX.x * u_col0 + VERTEX.y * u_col1;
}

void fragment() {
	vec4 col = COLOR * texture(TEXTURE, UV);
	if (col.a > 0.0 && poly_count > 0) {
		vec2 p = world_xy;
		bool covered = false;
		int start = 0;
		for (int pi = 0; pi < 16; pi++) {
			if (pi >= poly_count) {
				break;
			}
			bool in_poly = false;
			int end = poly_end[pi];
			for (int i = start; i < 256; i++) {
				if (i >= end) {
					break;
				}
				vec2 va = edge_a[i];
				vec2 vb = edge_b[i];
				if ((va.y > p.y) != (vb.y > p.y)) {
					float t = (p.y - va.y) / (vb.y - va.y);
					if (p.x < va.x + t * (vb.x - va.x)) {
						in_poly = !in_poly;
					}
				}
			}
			if (in_poly) {
				covered = true;
				break;
			}
			start = end;
		}
		if (!covered) {
			col.a = 0.0;
		}
	}
	COLOR = col;
}
";
	private static Shader? _clipShader;

	private static readonly string[] ExcludedNameParts =
	{
		"Glow", "Ripple", "Beam", "Fx", "Spark", "Wisp", "Dust", "Flash", "Trail", "Board", "Fuse",
	};

	private Sprite2D _sprite = null!;
	private Texture2D _ellipse = null!;
	private ShaderMaterial? _clipMat;
	private Node2D? _owner2D;
	private Sprite2D? _target;
	private Polygon2D? _polyTarget;
	private Texture2D? _lastSource;
	private float _footLocalY;
	private bool _hasFoot;
	private bool _hasSilhouette;
	private bool _targetLost;
	private float _currentRotation;
	private float _smoothedDistance;

	// Ground clip state.
	private readonly List<GroundClip.Entry> _clipScratch = new();
	private readonly Vector2[] _clipEdgesA = new Vector2[MaxClipEdges];
	private readonly Vector2[] _clipEdgesB = new Vector2[MaxClipEdges];
	private readonly int[] _clipEnds = new int[MaxClipPolys];
	private readonly int[] _clipLastIds = new int[MaxClipPolys];
	private int _clipLastCount = -1;
	public int ClipPolyCount { get; private set; }

	public override void _Ready()
	{
		_owner2D = GetParentOrNull<Node2D>();
		_ellipse = CreateSoftEllipseTexture(TextureSize);
		_sprite = new Sprite2D
		{
			Texture = _ellipse,
			Centered = false,
			Offset = new Vector2(-TextureSize * 0.5f, -TextureSize * 0.5f),
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
		};
		_clipShader ??= new Shader { Code = ClipShaderCode };
		_clipMat = new ShaderMaterial { Shader = _clipShader };
		_sprite.Material = _clipMat;
		AddChild(_sprite);
	}

	// Code-spawned entities call this after their visuals exist; index 0 keeps
	// the shadow behind sibling art regardless of attach order.
	public static Shadow Attach(Node2D parent)
	{
		var shadow = new Shadow { Name = "Shadow" };
		parent.AddChild(shadow);
		parent.MoveChild(shadow, 0);
		return shadow;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_owner2D == null || !IsInstanceValid(_owner2D))
		{
			_sprite.Visible = false;
			return;
		}

		UpdateTargetTexture();
		if (_targetLost)
		{
			_sprite.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}

		var spaceState = GetWorld2D().DirectSpaceState;
		var ownerPos = _owner2D.GlobalPosition;
		var grounded = TryRayHit(spaceState, ownerPos, ownerPos, MaxDistance + 8.0f, out var hitPos);
		var distance = hitPos.Y - ownerPos.Y;
		var valid = grounded && distance >= -1.0f && distance <= MaxDistance;

		if (!valid)
		{
			// Origin on or inside the ground (pads/beacons/props sit flush or
			// sunk): retry from above, accepting only near-level surfaces.
			// The direct ray may also have "hit" the body's bottom face far
			// below, which the sunk check rejects.
			var lifted = ownerPos - new Vector2(0.0f, EmbeddedRetryLift);
			if (TryRayHit(spaceState, ownerPos, lifted, MaxDistance + 8.0f + EmbeddedRetryLift, out var liftHit)
				&& liftHit.Y >= ownerPos.Y - EmbeddedMaxSunk
				&& liftHit.Y <= ownerPos.Y + MaxDistance)
			{
				hitPos = liftHit;
				distance = Mathf.Max(0.0f, hitPos.Y - ownerPos.Y);
				valid = true;
			}
			if (!valid)
			{
				_sprite.Visible = false;
				LerpRotationTowards(0.0f, delta);
				return;
			}
		}

		if (DistanceSmoothing > 0.0f)
		{
			var smoothAlpha = 1.0f - Mathf.Exp(-DistanceSmoothing * (float)delta);
			_smoothedDistance = Mathf.Lerp(_smoothedDistance, distance, smoothAlpha);
		}
		else
		{
			_smoothedDistance = distance;
		}

		var t = Mathf.Clamp(_smoothedDistance / Mathf.Max(MaxDistance, 0.001f), 0.0f, 1.0f);
		var scale = Mathf.Lerp(1.0f, MinScale, t);

		var silhouette = _hasSilhouette && (_target != null || _polyTarget != null);
		float sx;
		float sy;
		if (silhouette)
		{
			// Polygon bakes already include the polygon's own scale.
			var visualScale = _target != null ? _target.Scale : Vector2.One;
			sx = Mathf.Abs(visualScale.X) * WidthOverVisual * scale;
			sy = Mathf.Abs(visualScale.Y) * ShadowSquash * scale;
		}
		else
		{
			sx = BaseWidth * SizeMultiplier * scale / TextureSize;
			sy = BaseHeight * SizeMultiplier * scale / TextureSize;
		}

		if (distance > MaxDistance)
		{
			_sprite.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}

		_sprite.Visible = true;
		var groundOffset = _hasSilhouette ? SilhouetteGroundOffset : GroundOffset;
		// Props and crates are drawn sunk into the floor for perspective; their
		// shadow must start at the object's actual bottom pixel, not at the
		// ground line hidden behind them. Grounded objects with feet at/above
		// the surface are unaffected (max keeps the shadow on the ground).
		var startY = hitPos.Y;
		if (silhouette)
		{
			var footY = ComputeFootWorldY();
			if (footY > startY)
				startY = footY;
		}
		// Occlusion: when foreground art covers the caster's contact point,
		// the caster itself is drawn behind that art; the shadow must not
		// leak out from under it.
		if (ForegroundClip.Covers(new Vector2(ownerPos.X, startY), _owner2D))
		{
			_sprite.Visible = false;
			LerpRotationTowards(0.0f, delta);
			return;
		}
		GlobalPosition = new Vector2(ownerPos.X, startY + groundOffset);

		var tex = _sprite.Texture;
		var texW = tex != null ? tex.GetWidth() : TextureSize;
		var texH = tex != null ? tex.GetHeight() : TextureSize;
		float k = 0.0f;
		Transform2D spriteXf;
		if (silhouette)
		{
			k = SunShear() * sx;
			// x-axis scaled, y-axis scaled + sheared: feet row (local y = 0)
			// stays welded while the far end of the shadow slides away from the sun.
			spriteXf = new Transform2D(
				new Vector2(sx, 0.0f),
				new Vector2(k, sy),
				Vector2.Zero);
			_sprite.Transform = spriteXf;
		}
		else
		{
			spriteXf = new Transform2D(new Vector2(sx, 0.0f), new Vector2(0.0f, sy), Vector2.Zero);
			_sprite.Scale = new Vector2(sx, sy);
		}

		_sprite.Modulate = new Color(0.0f, 0.0f, 0.0f, Mathf.Lerp(MaxAlpha, MinAlpha, t));

		// Show only where opaque ground art is under the shadow's extent.
		var halfWidth = texW * sx * 0.5f;
		var height = texH * sy;
		var shiftMax = Mathf.Abs(k) * texH;
		UpdateGroundClip(GlobalPosition, halfWidth, height, shiftMax, spriteXf);

		LerpRotationTowards(ComputeSlopeRotation(), delta);
	}

	private void UpdateGroundClip(Vector2 anchor, float halfWidth, float height, float shiftMax, Transform2D spriteXf)
	{
		if (_clipMat == null)
			return;

		// The clip tests world positions; the engine gives canvas_item shaders
		// no world matrix, so hand it the linear map we just composed on the
		// CPU (owner rotation + shadow slope rotation * sprite squash/shear).
		var ownerRot = _owner2D != null && IsInstanceValid(_owner2D) ? _owner2D.GlobalRotation : 0.0f;
		var lin = new Transform2D(ownerRot + Rotation, Vector2.Zero) * spriteXf;
		_clipMat.SetShaderParameter("u_origin", anchor);
		_clipMat.SetShaderParameter("u_col0", lin.X);
		_clipMat.SetShaderParameter("u_col1", lin.Y);

		// Pad x by the height too: slope/owner rotation can tilt the quad's
		// corners outside its unrotated width.
		var xPad = halfWidth + shiftMax + height + 2.0f;
		var rect = new Rect2(
			anchor.X - xPad,
			anchor.Y - 3.0f,
			xPad * 2.0f,
			height + 8.0f);
		GroundClip.Query(rect, _clipScratch);

		var count = Mathf.Min(_clipScratch.Count, MaxClipPolys);
		if (count > 1)
		{
			// Nearest first when the region has more polys than the shader fits.
			var cx = (rect.Position.X + rect.End.X) * 0.5f;
			var cy = (rect.Position.Y + rect.End.Y) * 0.5f;
			_clipScratch.Sort((a, b) =>
			{
				var da = (a.CenterX - cx) * (a.CenterX - cx) + (a.CenterY - cy) * (a.CenterY - cy);
				var db = (b.CenterX - cx) * (b.CenterX - cx) + (b.CenterY - cy) * (b.CenterY - cy);
				return da.CompareTo(db);
			});
		}

		var same = count == _clipLastCount;
		if (same)
		{
			for (var i = 0; i < count; i++)
			{
				if (_clipLastIds[i] != _clipScratch[i].Id)
				{
					same = false;
					break;
				}
			}
		}
		if (same)
			return;

		var edge = 0;
		for (var i = 0; i < count; i++)
		{
			_clipLastIds[i] = _clipScratch[i].Id;
			var pts = _clipScratch[i].World;
			var needed = pts.Length;
			if (edge + needed > MaxClipEdges)
			{
				count = i;
				break;
			}
			for (var j = 0; j < pts.Length; j++)
			{
				_clipEdgesA[edge] = pts[j];
				_clipEdgesB[edge] = pts[(j + 1) % pts.Length];
				edge++;
			}
			_clipEnds[i] = edge;
		}
		for (var i = count; i < MaxClipPolys; i++)
		{
			_clipEnds[i] = edge;
		}
		for (var i = edge; i < MaxClipEdges; i++)
		{
			_clipEdgesA[i] = Vector2.Zero;
			_clipEdgesB[i] = Vector2.Zero;
		}

		_clipMat.SetShaderParameter("edge_a", _clipEdgesA);
		_clipMat.SetShaderParameter("edge_b", _clipEdgesB);
		_clipMat.SetShaderParameter("poly_end", _clipEnds);
		_clipMat.SetShaderParameter("poly_count", count);
		_clipLastCount = count;
		ClipPolyCount = count;
	}

	private void UpdateTargetTexture()
	{
		if (_target != null && !IsInstanceValid(_target))
			_target = null;
		if (_polyTarget != null && !IsInstanceValid(_polyTarget))
			_polyTarget = null;
		if (_target == null && _polyTarget == null)
			ResolveTarget();

		if (_target == null && _polyTarget == null)
		{
			// Dynamic scenes create their visual after _Ready; retry next frame.
			return;
		}

		var visible =
			(_target != null && _target.IsVisibleInTree() && _target.Texture != null) ||
			(_polyTarget != null && _polyTarget.IsVisibleInTree() && _polyTarget.Polygon.Length >= 3);
		if (!visible)
		{
			// Tracked visual gone (e.g. crate shattered, body despawned).
			_targetLost = _lastSource != null;
			return;
		}
		_targetLost = false;

		if (_target != null)
		{
			if (_target.Texture == _lastSource)
				return;
			var bake = ShadowSilhouette.Get(_target.Texture);
			ApplySilhouette(bake?.Texture);
			_footLocalY = bake?.FootLocalY ?? 0.0f;
			_hasFoot = bake != null;
			_lastSource = _target.Texture;
		}
		else
		{
			// Polygon bakes are cached by node, so identity is stable after the first hit.
			var bake = ShadowSilhouette.Get(_polyTarget);
			if (bake == null || bake.Texture == null || bake.Texture == _lastSource)
				return;
			ApplySilhouette(bake.Texture);
			_footLocalY = bake.FootLocalY;
			_hasFoot = true;
			_lastSource = bake.Texture;
		}
	}

	private float ComputeFootWorldY()
	{
		if (!_hasFoot)
			return float.MinValue;
		if (_target != null)
			return _target.ToGlobal(new Vector2(0.0f, _footLocalY)).Y;
		if (_polyTarget != null)
			return _polyTarget.ToGlobal(new Vector2(0.0f, _footLocalY)).Y;
		return float.MinValue;
	}

	private void ApplySilhouette(Texture2D? silhouette)	{
		if (silhouette != null)
		{
			_sprite.Texture = silhouette;
			_sprite.Offset = new Vector2(-silhouette.GetWidth() * 0.5f, 0.0f);
			_hasSilhouette = true;
		}
		else
		{
			_sprite.Texture = _ellipse;
			_sprite.Offset = new Vector2(-TextureSize * 0.5f, -TextureSize * 0.5f);
			_hasSilhouette = false;
		}
	}

	private void ResolveTarget()
	{
		// Unassigned NodePath exports marshal as null, not empty.
		if (TargetSprite != null && !TargetSprite.IsEmpty)
		{
			_target = GetNodeOrNull<Sprite2D>(TargetSprite);
			return;
		}

		var root = GetParent();
		if (root == null)
			return;

		Polygon2D? firstPoly = null;
		var queue = new Queue<Node>();
		queue.Enqueue(root);
		while (queue.Count > 0)
		{
			foreach (var child in queue.Peek().GetChildren())
			{
				if (child is Sprite2D spr && spr != _sprite
					&& !IsFxName(spr.Name) && spr.Texture != null)
				{
					_target = spr;
					return;
				}
				if (firstPoly == null
					&& child is Polygon2D poly && !IsFxName(poly.Name)
					&& poly.Material == null // shader glows (RectGlow) are never the object body
					&& poly.Visible && poly.Polygon.Length >= 3)
				{
					firstPoly = poly;
				}
				queue.Enqueue(child);
			}
			queue.Dequeue();
		}
		_polyTarget = firstPoly;
	}

	private bool TryRayHit(PhysicsDirectSpaceState2D space, Vector2 ownerPos, Vector2 from, float length, out Vector2 hitPos)
	{
		var query = PhysicsRayQueryParameters2D.Create(
			from,
			from + new Vector2(0.0f, length),
			CollisionMask);
		if (_owner2D is PhysicsBody2D body)
			query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };
		var result = space.IntersectRay(query);
		if (result.Count == 0 || !result.ContainsKey("position"))
		{
			hitPos = ownerPos;
			return false;
		}
		hitPos = (Vector2)result["position"];
		return true;
	}

	private static bool IsFxName(StringName name)
	{
		foreach (var part in ExcludedNameParts)
		{
			if (name.ToString().Contains(part, System.StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}

	private float SunShear()
	{
		if (float.IsNaN(SunAngleDegrees))
			return WorldSun.Shear * ShadowSkewStrength;
		return WorldSun.ShearForAngle(SunAngleDegrees) * ShadowSkewStrength;
	}

	private void LerpRotationTowards(float target, double delta)
	{
		var lerpFactor = Mathf.Clamp(RotationLerpSpeed * (float)delta, 0.0f, 1.0f);
		_currentRotation = Mathf.LerpAngle(_currentRotation, target, lerpFactor);
		Rotation = _currentRotation;
	}

	private float ComputeSlopeRotation()
	{
		if (_owner2D is not CharacterBody2D body || !body.IsOnFloor())
			return 0.0f;

		var floorNormal = body.GetFloorNormal();
		if (floorNormal == Vector2.Zero)
			return 0.0f;

		var tangent = new Vector2(floorNormal.Y, -floorNormal.X);
		if (tangent.X < 0.0f) tangent = -tangent;
		if (tangent.LengthSquared() > 0.0f) tangent = tangent.Normalized();
		return tangent.Angle();
	}

	private static ImageTexture CreateSoftEllipseTexture(int size)
	{
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		var half = size * 0.5f;
		for (var x = 0; x < size; x++)
		{
			for (var y = 0; y < size; y++)
			{
				var dx = (x - half) / half;
				var dy = (y - half) / half;
				var dist = Mathf.Sqrt(dx * dx + dy * dy);
				var alpha = Mathf.Clamp(Mathf.Pow(1.0f - dist, 0.5f), 0.0f, 1.0f);
				img.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
			}
		}
		return ImageTexture.CreateFromImage(img);
	}
}
