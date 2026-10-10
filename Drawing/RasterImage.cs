namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>Uncompressed, tightly packed RGBA8 pixels (top-to-bottom, left-to-right).</summary>
internal sealed class RasterImage
{
	/// <summary>
	/// The largest picture the decoders take: its pixels alone are 100 MB.
	/// </summary>
	public const int MaxPixels = 25_000_000;

	public int Width { get; }
	public int Height { get; }
	public byte[] Pixels { get; }

	public RasterImage(int width, int height)
	{
		if (width < 1 || height < 1 || (long)width * height > MaxPixels)
			throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions exceed the supported 25-megapixel limit.");

		Width = width;
		Height = height;
		Pixels = new byte[checked(width * height * 4)];
	}

	/// <summary>
	/// Refuses a picture a decoder is about to allocate buffers for if it is larger than <see cref="MaxPixels"/>.
	/// </summary>
	/// <exception cref="InvalidDataException">The picture is too large to be decoded.</exception>
	public static void EnsureDecodable(int width, int height)
	{
		if ((long)width * height > MaxPixels)
			throw new InvalidDataException($"The picture is {width}x{height}: more than the 25 megapixels that can be decoded.");
	}

	/// <summary>
	/// Reduces the picture. Every pixel of the result is the mean of the source area it covers,
	/// with colors weighted by alpha, so that transparent pixels lend no color to their neighbors.
	/// </summary>
	public RasterImage Downscale(int width, int height)
	{
		var result = new RasterImage(width, height);

		var scaleX = (double)Width / width;
		var scaleY = (double)Height / height;
		var area = scaleX * scaleY;

		// Sums over the source area of each pixel of the row being built: alpha, then colors times alpha.
		var sums = new double[width * 4];

		for (var y = 0; y < height; y++)
		{
			var top = y * scaleY;
			var bottom = Math.Min(Height, (y + 1) * scaleY);

			Array.Clear(sums);

			for (var sourceY = (int)top; sourceY < bottom; sourceY++)
			{
				var weightY = Math.Min(bottom, sourceY + 1) - Math.Max(top, sourceY);
				var row = sourceY * Width * 4;

				for (var x = 0; x < width; x++)
				{
					var left = x * scaleX;
					var right = Math.Min(Width, (x + 1) * scaleX);

					for (var sourceX = (int)left; sourceX < right; sourceX++)
					{
						var at = row + sourceX * 4;
						var alpha = Pixels[at + 3] * weightY * (Math.Min(right, sourceX + 1) - Math.Max(left, sourceX));

						sums[x * 4] += alpha;
						sums[x * 4 + 1] += Pixels[at] * alpha;
						sums[x * 4 + 2] += Pixels[at + 1] * alpha;
						sums[x * 4 + 3] += Pixels[at + 2] * alpha;
					}
				}
			}

			for (var x = 0; x < width; x++)
			{
				var alpha = sums[x * 4];
				var to = (y * width + x) * 4;

				if (alpha > 0)
				{
					result.Pixels[to] = ToByte(sums[x * 4 + 1] / alpha);
					result.Pixels[to + 1] = ToByte(sums[x * 4 + 2] / alpha);
					result.Pixels[to + 2] = ToByte(sums[x * 4 + 3] / alpha);
				}

				result.Pixels[to + 3] = ToByte(alpha / area);
			}
		}

		return result;
	}

	/// <summary>
	/// Turns and mirrors the picture the way an Exif orientation asks for it to be shown.
	/// </summary>
	/// <param name="orientation">The Exif orientation, 1 to 8; 1 leaves the picture as it is.</param>
	public RasterImage Orient(int orientation)
	{
		if (orientation is < 2 or > 8)
			return this;

		// Orientations 5 to 8 lay the rows of the stored picture along the vertical of the shown one.
		var result = orientation >= 5 ? new RasterImage(Height, Width) : new RasterImage(Width, Height);

		for (var y = 0; y < result.Height; y++)
			for (var x = 0; x < result.Width; x++)
			{
				var (sourceX, sourceY) = orientation switch
				{
					2 => (Width - 1 - x, y),
					3 => (Width - 1 - x, Height - 1 - y),
					4 => (x, Height - 1 - y),
					5 => (y, x),
					6 => (y, Height - 1 - x),
					7 => (Width - 1 - y, Height - 1 - x),
					_ => (Width - 1 - y, x),
				};

				Pixels.AsSpan((sourceY * Width + sourceX) * 4, 4).CopyTo(result.Pixels.AsSpan((y * result.Width + x) * 4));
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
