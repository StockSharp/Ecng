namespace Ecng.Drawing;

using System;

/// <summary>Uncompressed, tightly packed RGBA8 pixels (top-to-bottom, left-to-right).</summary>
internal sealed class RasterImage
{
	public int Width { get; }
	public int Height { get; }
	public byte[] Pixels { get; }

	public RasterImage(int width, int height)
	{
		if (width < 1 || height < 1 || (long)width * height > 25_000_000)
			throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions exceed the supported 25-megapixel limit.");

		Width = width;
		Height = height;
		Pixels = new byte[checked(width * height * 4)];
	}

	/// <summary>Resizes with center-aligned bilinear interpolation, preserving alpha without dark halos.</summary>
	public RasterImage Downscale(int width, int height)
	{
		var result = new RasterImage(width, height);
		var sx = (double)Width / width;
		var sy = (double)Height / height;

		for (var y = 0; y < height; y++)
		{
			var fy = (y + 0.5) * sy - 0.5;
			var y0 = Math.Clamp((int)Math.Floor(fy), 0, Height - 1);
			var y1 = Math.Clamp(y0 + 1, 0, Height - 1);
			var ty = Math.Clamp(fy - y0, 0, 1);
			for (var x = 0; x < width; x++)
			{
				var fx = (x + 0.5) * sx - 0.5;
				var x0 = Math.Clamp((int)Math.Floor(fx), 0, Width - 1);
				var x1 = Math.Clamp(x0 + 1, 0, Width - 1);
				var tx = Math.Clamp(fx - x0, 0, 1);

				var a = (y0 * Width + x0) * 4;
				var b = (y0 * Width + x1) * 4;
				var c = (y1 * Width + x0) * 4;
				var d = (y1 * Width + x1) * 4;
				var dest = (y * width + x) * 4;

				var wa = (1 - tx) * (1 - ty);
				var wb = tx * (1 - ty);
				var wc = (1 - tx) * ty;
				var wd = tx * ty;
				var alpha = Pixels[a + 3] * wa + Pixels[b + 3] * wb + Pixels[c + 3] * wc + Pixels[d + 3] * wd;
				result.Pixels[dest + 3] = ToByte(alpha);

				for (var channel = 0; channel < 3; channel++)
				{
					var premultiplied =
						Pixels[a + channel] * Pixels[a + 3] * wa +
						Pixels[b + channel] * Pixels[b + 3] * wb +
						Pixels[c + channel] * Pixels[c + 3] * wc +
						Pixels[d + channel] * Pixels[d + 3] * wd;
					result.Pixels[dest + channel] = alpha > 0 ? ToByte(premultiplied / alpha) : (byte)0;
				}
			}
		}

		return result;
	}

	public void BlendPixel(int x, int y, byte red, byte green, byte blue, byte alpha)
	{
		if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || alpha == 0)
			return;

		var offset = (y * Width + x) * 4;
		var oldAlpha = Pixels[offset + 3];
		var a = alpha / 255.0;
		var oa = oldAlpha / 255.0;
		var newAlpha = a + oa * (1 - a);
		if (newAlpha == 0) return;

		Pixels[offset] = ToByte((red * a + Pixels[offset] * oa * (1 - a)) / newAlpha);
		Pixels[offset + 1] = ToByte((green * a + Pixels[offset + 1] * oa * (1 - a)) / newAlpha);
		Pixels[offset + 2] = ToByte((blue * a + Pixels[offset + 2] * oa * (1 - a)) / newAlpha);
		Pixels[offset + 3] = ToByte(newAlpha * 255);
	}

	public static byte ToByte(double value) => (byte)Math.Clamp((int)Math.Round(value), 0, 255);
}
