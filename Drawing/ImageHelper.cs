namespace Ecng.Drawing;

using System;
using System.Drawing;
using System.IO;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using ImageSharpColor = SixLabors.ImageSharp.Color;
using ImageSharpPointF = SixLabors.ImageSharp.PointF;
using ImageSharpSize = SixLabors.ImageSharp.Size;

/// <summary>
/// Cross-platform operations on encoded images. Input buffers are never changed.
/// </summary>
public static class ImageHelper
{
	/// <summary>
	/// Reads the image dimensions without decoding pixel data. Supports all image formats recognized by ImageSharp.
	/// For a PNG-only fast header check see <see cref="PngHelper.GetPngSize(byte[])"/>.
	/// </summary>
	public static Size GetImageSize(this byte[] data)
		=> GetImageSize((ReadOnlySpan<byte>)(data ?? throw new ArgumentNullException(nameof(data))));

	/// <inheritdoc cref="GetImageSize(byte[])"/>
	public static Size GetImageSize(this ReadOnlySpan<byte> data)
	{
		var info = Image.Identify(data);
		return new(info.Width, info.Height);
	}

	/// <summary>
	/// Reduces an image to fit the provided maximum dimensions, keeping its aspect ratio
	/// and encoding format. Does not enlarge small images. Returns a separate byte array.
	/// </summary>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight)
	{
		ArgumentNullException.ThrowIfNull(data);

		if (maxWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxWidth));

		if (maxHeight <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxHeight));

		using var image = Image.Load(data);
		var scale = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);

		if (scale >= 1)
			return (byte[])data.Clone();

		var width = Math.Max(1, (int)Math.Floor(image.Width * scale));
		var height = Math.Max(1, (int)Math.Floor(image.Height * scale));

		image.Mutate(ctx => ctx.Resize(new ImageSharpSize(width, height), KnownResamplers.Lanczos3));

		IImageFormat format = image.Metadata.DecodedImageFormat
			?? throw new InvalidDataException("The decoded image format is unknown.");

		using var output = new MemoryStream();
		image.Save(output, format);
		return output.ToArray();
	}

	/// <summary>
	/// Converts the image to PNG while preserving alpha. For animated images only the first frame is used.
	/// </summary>
	public static byte[] ConvertToPng(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);

		using var image = Image.Load<Rgba32>(data);
		KeepFirstFrame(image);

		using var output = new MemoryStream();
		image.SaveAsPng(output);
		return output.ToArray();
	}

	/// <summary>
	/// Adds semi-transparent white text at the bottom-right, returning a PNG.
	/// The default family is Verdana. It must be installed, otherwise explicitly supply another installed font.
	/// The text is automatically reduced in size, when necessary, to fit within the margins.
	/// </summary>
	/// <param name="data">Original encoded image.</param>
	/// <param name="text">Watermark text, nonempty and visible.</param>
	/// <param name="fontSize">Maximum text size in points, finite and positive.</param>
	/// <param name="opacity">Text alpha, 0-255.</param>
	/// <param name="margin">Minimum edge margin in pixels.</param>
	/// <param name="fontFamily">Installed font family, Verdana by default.</param>
	public static byte[] AddTextWatermark(
		this byte[] data,
		string text,
		float fontSize = 24,
		byte opacity = 160,
		int margin = 12,
		string fontFamily = "Verdana")
	{
		ArgumentNullException.ThrowIfNull(data);

		if (string.IsNullOrWhiteSpace(text))
			throw new ArgumentException("Watermark text must not be blank.", nameof(text));

		if (fontSize <= 0 || float.IsNaN(fontSize) || float.IsInfinity(fontSize))
			throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be finite and positive.");

		if (margin < 0)
			throw new ArgumentOutOfRangeException(nameof(margin));

		if (string.IsNullOrWhiteSpace(fontFamily))
			throw new ArgumentException("Font family is required.", nameof(fontFamily));

		if (!SystemFonts.TryGet(fontFamily, out var family))
			throw new InvalidOperationException($"Font '{fontFamily}' is not installed. Install it or specify another font family.");

		using var image = Image.Load<Rgba32>(data);
		KeepFirstFrame(image);

		// Use long arithmetic for margins to prevent integer overflow.
		var availableWidth = (long)image.Width - 2L * margin;
		var availableHeight = (long)image.Height - 2L * margin;

		if (availableWidth <= 0 || availableHeight <= 0)
			throw new ArgumentOutOfRangeException(nameof(margin), "There is no room for text inside the margins.");

		var size = fontSize;
		Font font = null;
		FontRectangle bounds = default;

		for (var attempt = 0; attempt < 8; attempt++)
		{
			font = family.CreateFont(size);
			bounds = TextMeasurer.MeasureBounds(text, new TextOptions(font));

			if (bounds.Width <= 0 || bounds.Height <= 0)
				throw new ArgumentException("Text contains no drawable glyphs in the selected font.", nameof(text));

			if (bounds.Width <= availableWidth && bounds.Height <= availableHeight)
				break;

			// A slight safety factor prevents glyphs being clipped by rasterizer rounding.
			var factor = Math.Min((double)availableWidth / bounds.Width, (double)availableHeight / bounds.Height);
			size = (float)(size * Math.Min(factor * 0.98, 0.98));

			if (size < 1)
				throw new ArgumentException("Watermark cannot fit within the image.", nameof(text));
		}

		if (bounds.Width > availableWidth || bounds.Height > availableHeight)
			throw new ArgumentException("Watermark cannot fit within the image.", nameof(text));

		// Position using measured ink bounds (including glyph bearings), not line advance.
		var x = (float)(image.Width - margin - bounds.X - bounds.Width);
		var y = (float)(image.Height - margin - bounds.Y - bounds.Height);
		var color = ImageSharpColor.FromRgba(255, 255, 255, opacity);

		image.Mutate(ctx => ctx.DrawText(text, font, color, new ImageSharpPointF(x, y)));

		using var output = new MemoryStream();
		image.SaveAsPng(output);
		return output.ToArray();
	}

	private static void KeepFirstFrame(Image<Rgba32> image)
	{
		while (image.Frames.Count > 1)
			image.Frames.RemoveFrame(image.Frames.Count - 1);
	}
}
