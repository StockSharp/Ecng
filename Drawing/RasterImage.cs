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
	public ImageProcessingLimits Limits { get; }

	public RasterImage(int width, int height)
		: this(width, height, ImageProcessingLimits.Default)
	{
	}

	public RasterImage(int width, int height, ImageProcessingLimits limits)
	{
		ArgumentNullException.ThrowIfNull(limits);
		if (width < 1 || height < 1 || (long)width * height > limits.MaxImagePixels)
			throw new ArgumentOutOfRangeException(nameof(width), $"Image dimensions exceed the {limits.MaxImagePixels}-pixel limit.");

		Width = width;
		Height = height;
		Limits = limits;
		Pixels = new byte[checked(width * height * 4)];
	}

	/// <summary>
	/// Refuses a picture a decoder is about to allocate buffers for if it is larger than <see cref="MaxPixels"/>.
	/// </summary>
	/// <exception cref="InvalidDataException">The picture is too large to be decoded.</exception>
	public static void EnsureDecodable(int width, int height)
		=> EnsureDecodable(width, height, ImageProcessingLimits.Default);

	public static void EnsureDecodable(int width, int height, ImageProcessingLimits limits)
	{
		limits.EnsureWorkingMemory((long)width * height * 4);
		if ((long)width * height > limits.MaxImagePixels)
			throw new InvalidDataException($"The picture is {width}x{height}: more than the {limits.MaxImagePixels} pixels that can be decoded.");
	}

	/// <summary>
	/// Reduces the picture. Every pixel of the result is the mean of the source area it covers,
	/// with colors weighted by alpha, so that transparent pixels lend no color to their neighbors.
	/// </summary>
	public RasterImage Downscale(int width, int height)
	{
		var downscaler = new ImageDownscaler(Width, Height, width, height, Limits);
		for (var y = 0; y < Height; y++)
			downscaler.AddRow(Pixels.AsSpan(y * Width * 4, Width * 4));
		return downscaler.Image;
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
		var result = orientation >= 5 ? new RasterImage(Height, Width, Limits) : new RasterImage(Width, Height, Limits);

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
