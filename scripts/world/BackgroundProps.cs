using Godot;
using System.Collections.Generic;

public partial class BackgroundProps : Node2D
{
	private sealed class PlateLayer
	{
		public required string Path;
		public float Factor;
		public float Scale;
		public float BaselineScreenY;
		public float Wash;
		public float Alpha;
		public Color Tint;
		public Texture2D? Texture;
		public float BottomPixel;
		public readonly Dictionary<long, Sprite2D> Slots = new();
	}

	private Camera2D? _camera;
	private float _refCamY = 96f;
	private float _horizonScreenY;
	private readonly List<PlateLayer> _layers = new();

	public void Configure(Camera2D camera, float refCamY, float horizonScreenY)
	{
		_camera = camera;
		_refCamY = refCamY;
		_horizonScreenY = horizonScreenY;

		foreach (var layer in _layers)
		{
			foreach (var sprite in layer.Slots.Values)
			{
				sprite.QueueFree();
			}
			layer.Slots.Clear();
		}

		_layers.Clear();
		_layers.Add(new PlateLayer
		{
			Path = "res://assets/background/plates/far_refinery_skyline.png",
			Factor = 0.12f,
			Scale = 1.0f,
			BaselineScreenY = HorizonOffset(50f),
			Wash = 0.78f,
			Alpha = 0.38f,
		});
		_layers.Add(new PlateLayer
		{
			Path = "res://assets/background/plates/mid_industrial_yard.png",
			Factor = 0.28f,
			Scale = 1.08f,
			BaselineScreenY = HorizonOffset(140f),
			Wash = 0.52f,
			Alpha = 0.48f,
		});
		_layers.Add(new PlateLayer
		{
			Path = "res://assets/background/plates/near_understructure.png",
			Factor = 0.52f,
			Scale = 1.25f,
			BaselineScreenY = HorizonOffset(290f),
			Wash = 0.2f,
			Alpha = 0.58f,
		});

		foreach (var layer in _layers)
		{
			if (!ResourceLoader.Exists(layer.Path)) continue;
			layer.Texture = GD.Load<Texture2D>(layer.Path);
			layer.BottomPixel = FindOpaqueBottom(layer.Texture);
		}
	}

	public void SetPaletteData(LevelColorPalette palette, float bgDim)
	{
		var haze = palette.SecondaryLight;
		for (var li = 0; li < _layers.Count; li++)
		{
			var layer = _layers[li];
			var baseColor = li switch
			{
				0 => palette.PrimaryLight,
				1 => palette.PrimaryDark.Lerp(palette.PrimaryMedium, 0.45f),
				_ => palette.PrimaryDark,
			};
			var mixed = baseColor.Lerp(haze, layer.Wash);
			layer.Tint = new Color(mixed.R * bgDim, mixed.G * bgDim, mixed.B * bgDim, layer.Alpha);
			foreach (var sprite in layer.Slots.Values)
			{
				sprite.Modulate = layer.Tint;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_camera == null) return;

		var camX = _camera.GlobalPosition.X;
		var camY = _camera.GlobalPosition.Y;
		var view = GetViewportRect().Size;
		var halfW = view.X * 0.5f;
		var halfH = view.Y * 0.5f;

		for (var li = 0; li < _layers.Count; li++)
		{
			UpdateLayer(li, _layers[li], camX, camY, halfW, halfH);
		}
	}

	private void UpdateLayer(int li, PlateLayer layer, double camX, double camY, float halfW, float halfH)
	{
		if (layer.Texture == null) return;

		var plateScreenWidth = layer.Texture.GetWidth() * layer.Scale;
		var repeatWorldWidth = plateScreenWidth / layer.Factor;
		var halfSpan = (halfW + plateScreenWidth) / layer.Factor;
		var keyMin = (long)Mathf.Floor((camX - halfSpan) / repeatWorldWidth);
		var keyMax = (long)Mathf.Floor((camX + halfSpan) / repeatWorldWidth);

		for (var k = keyMin; k <= keyMax; k++)
		{
			if (!layer.Slots.ContainsKey(k))
			{
				layer.Slots[k] = CreatePlateSprite(li, layer);
			}
		}

		foreach (var kv in layer.Slots)
		{
			PlacePlate(layer, kv.Key, kv.Value, camX, camY, halfH, repeatWorldWidth);
		}

		var toRemove = new List<long>();
		foreach (var kv in layer.Slots)
		{
			if (kv.Key < keyMin || kv.Key > keyMax)
			{
				kv.Value.QueueFree();
				toRemove.Add(kv.Key);
			}
		}

		foreach (var key in toRemove)
		{
			layer.Slots.Remove(key);
		}
	}

	private Sprite2D CreatePlateSprite(int li, PlateLayer layer)
	{
		var sprite = new Sprite2D
		{
			Texture = layer.Texture,
			Centered = false,
			Modulate = layer.Tint,
			Scale = new Vector2(layer.Scale, layer.Scale),
			ZIndex = li,
		};
		AddChild(sprite);
		return sprite;
	}

	private void PlacePlate(PlateLayer layer, long k, Sprite2D sprite, double camX, double camY, float halfH, float repeatWorldWidth)
	{
		var wx = k * repeatWorldWidth;
		var wy = _refCamY + (layer.BaselineScreenY - halfH) / layer.Factor;
		var sx = camX + (wx - camX) * layer.Factor;
		var sy = camY + (wy - camY) * layer.Factor - layer.BottomPixel * layer.Scale;
		sprite.GlobalPosition = new Vector2((float)sx, (float)sy).Snapped(Vector2.One);
	}

	private float HorizonOffset(float y)
	{
		return _horizonScreenY + y;
	}

	private static float FindOpaqueBottom(Texture2D texture)
	{
		var image = texture.GetImage();
		if (image == null) return texture.GetHeight();

		for (var y = image.GetHeight() - 1; y >= 0; y--)
		{
			for (var x = 0; x < image.GetWidth(); x++)
			{
				if (image.GetPixel(x, y).A > 0.05f)
				{
					return y + 1;
				}
			}
		}

		return texture.GetHeight();
	}
}
