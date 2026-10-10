namespace Ecng.Drawing;

using System;
using System.Drawing;
using System.IO;

/// <summary>
/// Platform-independent image utilities implemented entirely in managed C#.
/// No GDI+, platform-native image libraries, or third-party NuGet packages are used.
/// </summary>
public static class ImageHelper
{
	/// <summary>
	/// Reads dimensions from the PNG or JPEG header without allocating a pixel buffer.
	/// </summary>
	public static Size GetImageSize(this byte[] data)
		=> GetImageSize((ReadOnlySpan<byte>)(data ?? throw new ArgumentNullException(nameof(data))));

	/// <inheritdoc cref="GetImageSize(byte[])"/>
	public static Size GetImageSize(this ReadOnlySpan<byte> data)
	{
		var (width, height) = PngCodec.IsPng(data) ? PngCodec.ReadSize(data) :
			JpegCodec.IsJpeg(data) ? JpegCodec.ReadSize(data) :
			throw new NotSupportedException("Only PNG and JPEG images are supported.");

		return new Size(width, height);
	}

	/// <summary>
	/// Reduces the image to fit within the specified dimensions while keeping aspect
	/// ratio and proper premultiplied-alpha interpolation. Does not enlarge images.
	/// Always returns PNG to avoid lossy double encoding of JPEG sources.
	/// </summary>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (maxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidth));
		if (maxHeight <= 0) throw new ArgumentOutOfRangeException(nameof(maxHeight));

		var image = Decode(data);
		var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
		if (ratio >= 1)
			return PngCodec.Encode(image);

		var width = Math.Max(1, (int)Math.Floor(image.Width * ratio));
		var height = Math.Max(1, (int)Math.Floor(image.Height * ratio));
		return PngCodec.Encode(image.Downscale(width, height));
	}

	/// <summary>
	/// Converts a PNG or 8-bit baseline JPEG picture to a standard RGBA8 PNG.
	/// </summary>
	public static byte[] ConvertToPng(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		return PngCodec.Encode(Decode(data));
	}

	/// <summary>
	/// Renders anti-aliased semi-transparent TrueType text into the bottom-right corner,
	/// then returns a PNG. The default font is Verdana; it must be installed as a .ttf
	/// on the host, or its path can be specified explicitly. No font is bundled.
	/// </summary>
	/// <param name="data">Encoded PNG or baseline JPEG image.</param>
	/// <param name="text">Single-line watermark text.</param>
	/// <param name="fontSize">Maximum font size in points.</param>
	/// <param name="opacity">White text opacity, 0..255.</param>
	/// <param name="margin">Margin around the text in pixels.</param>
	/// <param name="fontFamily">Font family name, Verdana by default.</param>
	/// <param name="fontFilePath">Optional path to a compatible TrueType .ttf file.</param>
	public static byte[] AddTextWatermark(
		this byte[] data, string text, float fontSize = 24, byte opacity = 160,
		int margin = 12, string fontFamily = "Verdana", string fontFilePath = null)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (string.IsNullOrWhiteSpace(text))
			throw new ArgumentException("Watermark text must not be empty.", nameof(text));
		if (fontSize <= 0 || float.IsNaN(fontSize) || float.IsInfinity(fontSize))
			throw new ArgumentOutOfRangeException(nameof(fontSize));
		if (margin < 0)
			throw new ArgumentOutOfRangeException(nameof(margin));
		if (string.IsNullOrWhiteSpace(fontFamily))
			throw new ArgumentException("A font family is required.", nameof(fontFamily));

		var image = Decode(data);
		if (2L * margin >= image.Width || 2L * margin >= image.Height)
			throw new ArgumentOutOfRangeException(nameof(margin), "Margins leave no room for text.");

		if (opacity != 0)
		{
			var font = new TrueTypeFont(TrueTypeFont.ResolveFontFile(fontFamily, fontFilePath));
			font.Draw(image, text, fontSize, opacity, margin);
		}

		return PngCodec.Encode(image);
	}

	private static RasterImage Decode(byte[] data)
	{
		if (data.Length == 0) throw new InvalidDataException("Image data is empty.");
		if (PngCodec.IsPng(data)) return PngCodec.Decode(data);
		if (JpegCodec.IsJpeg(data)) return JpegCodec.Decode(data);
		throw new NotSupportedException("Only PNG and baseline 8-bit JPEG images are supported.");
	}
}
