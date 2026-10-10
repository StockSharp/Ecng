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
	/// Reads PNG, JPEG, BMP and GIF dimensions without allocating a pixel buffer.
	/// </summary>
	public static Size GetImageSize(this byte[] data)
		=> GetImageSize((ReadOnlySpan<byte>)(data ?? throw new ArgumentNullException(nameof(data))));

	/// <inheritdoc cref="GetImageSize(byte[])"/>
	public static Size GetImageSize(this ReadOnlySpan<byte> data)
	{
		var (width, height) = PngCodec.IsPng(data) ? PngCodec.ReadSize(data) :
			JpegCodec.IsJpeg(data) ? JpegCodec.ReadSize(data) :
			BmpCodec.IsBmp(data) ? BmpCodec.ReadSize(data) :
			GifCodec.IsGif(data) ? GifCodec.ReadSize(data) :
			throw new NotSupportedException("Only PNG, JPEG, BMP and GIF images are supported.");

		return new Size(width, height);
	}

	/// <summary>
	/// Reduces the image to fit within the specified dimensions while keeping aspect
	/// ratio and proper premultiplied-alpha interpolation. Does not enlarge images.
	/// Preserves the source format: PNG remains lossless PNG; JPEG is re-encoded
	/// as baseline JPEG only when its dimensions actually change.
	/// </summary>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (maxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidth));
		if (maxHeight <= 0) throw new ArgumentOutOfRangeException(nameof(maxHeight));

		if (GifCodec.IsGif(data) || ApngDecoder.IsApng(data))
		{
			var isGif = GifCodec.IsGif(data);
			var animation = isGif ? GifCodec.Decode(data) : ApngDecoder.Decode(data);
			var gifRatio = Math.Min((double)maxWidth / animation.Width, (double)maxHeight / animation.Height);
			if (gifRatio >= 1) return (byte[])data.Clone();
			var gifWidth = Math.Max(1, (int)Math.Floor(animation.Width * gifRatio));
			var gifHeight = Math.Max(1, (int)Math.Floor(animation.Height * gifRatio));
			var resizedAnimation = new GifAnimation(gifWidth, gifHeight) { LoopCount = animation.LoopCount };
			foreach (var frame in animation.Frames)
				resizedAnimation.Frames.Add(new GifFrame(frame.Image.Downscale(gifWidth, gifHeight),
					frame.DelayCentiseconds));
			return isGif ? GifCodec.Encode(resizedAnimation) : ApngEncoder.Encode(resizedAnimation);
		}

		var image = Decode(data);
		var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
		if (ratio >= 1)
			return (byte[])data.Clone();

		var width = Math.Max(1, (int)Math.Floor(image.Width * ratio));
		var height = Math.Max(1, (int)Math.Floor(image.Height * ratio));
		var resized = image.Downscale(width, height);
		return JpegCodec.IsJpeg(data) ? JpegEncoder.Encode(resized) :
			BmpCodec.IsBmp(data) ? BmpCodec.Encode(resized) : PngCodec.Encode(resized);
	}

	/// <summary>
	/// Converts a PNG or 8-bit baseline/progressive JPEG (including grayscale and CMYK) to RGBA8 PNG.
	/// </summary>
	public static byte[] ConvertToPng(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (GifCodec.IsGif(data)) return ApngEncoder.Encode(GifCodec.Decode(data));
		if (ApngDecoder.IsApng(data))
		{
			_ = ApngDecoder.Decode(data); // validate every frame instead of silently discarding animation
			return (byte[])data.Clone();
		}
		return PngCodec.Encode(Decode(data));
	}

	/// <summary>
	/// Renders anti-aliased semi-transparent TrueType text into the bottom-right corner,
	/// then returns a PNG. Prefers Verdana, falls back to installed TrueType fonts,
	/// or accepts an explicit .ttf path. No licensed font is bundled.
	/// </summary>
	/// <param name="data">Encoded PNG or supported 8-bit JPEG image.</param>
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

		if (GifCodec.IsGif(data))
		{
			var animation = GifCodec.Decode(data);
			if (2L * margin >= animation.Width || 2L * margin >= animation.Height)
				throw new ArgumentOutOfRangeException(nameof(margin), "Margins leave no room for text.");
			if (opacity == 0) return isGif ? GifCodec.Encode(animation) : ApngEncoder.Encode(animation);
			var font = new TrueTypeFont(TrueTypeFont.ResolveFontFile(fontFamily, fontFilePath));
			foreach (var frame in animation.Frames)
				font.Draw(frame.Image, text, fontSize, opacity, margin);
			return isGif ? GifCodec.Encode(animation) : ApngEncoder.Encode(animation);
		}

		var image = Decode(data);
		if (2L * margin >= image.Width || 2L * margin >= image.Height)
			throw new ArgumentOutOfRangeException(nameof(margin), "Margins leave no room for text.");

		if (opacity != 0)
		{
			var font = new TrueTypeFont(TrueTypeFont.ResolveFontFile(fontFamily, fontFilePath));
			font.Draw(image, text, fontSize, opacity, margin);
		}

		return BmpCodec.IsBmp(data) ? BmpCodec.Encode(image) : PngCodec.Encode(image);
	}

	/// <summary>All displayed GIF frames as full-canvas RGBA8 PNG images with their delays.</summary>
	public static System.Collections.Generic.IReadOnlyList<(byte[] png, int delayMilliseconds)> GetGifFrames(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (!GifCodec.IsGif(data)) throw new NotSupportedException("Expected GIF87a/GIF89a data.");
		var animation = GifCodec.Decode(data);
		var frames = new (byte[] png, int delayMilliseconds)[animation.Frames.Count];
		for (var i = 0; i < frames.Length; i++)
			frames[i] = (PngCodec.Encode(animation.Frames[i].Image), animation.Frames[i].DelayCentiseconds * 10);
		return frames;
	}

	/// <summary>All displayed frames of GIF or APNG as full-canvas RGBA8 PNG images.</summary>
	public static System.Collections.Generic.IReadOnlyList<(byte[] png, int delayMilliseconds)> GetAnimationFrames(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		var animation = GifCodec.IsGif(data) ? GifCodec.Decode(data) :
			ApngDecoder.IsApng(data) ? ApngDecoder.Decode(data) :
			throw new NotSupportedException("Only GIF and APNG animations are supported.");
		var frames = new (byte[] png, int delayMilliseconds)[animation.Frames.Count];
		for (var i = 0; i < frames.Length; i++)
			frames[i] = (PngCodec.Encode(animation.Frames[i].Image), animation.Frames[i].DelayCentiseconds * 10);
		return frames;
	}

	/// <summary>Return animation loop count (0=infinite) for GIF or APNG.</summary>
	public static int GetAnimationLoopCount(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (GifCodec.IsGif(data)) return GifCodec.Decode(data).LoopCount;
		if (ApngDecoder.IsApng(data)) return ApngDecoder.Decode(data).LoopCount;
		throw new NotSupportedException("Only GIF and APNG animations are supported.");
	}

	/// <summary>NETSCAPE loop count: 0 for infinite; -1 if no loop extension exists.</summary>
	public static int GetGifLoopCount(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (!GifCodec.IsGif(data)) throw new NotSupportedException("Expected GIF87a/GIF89a data.");
		return GifCodec.Decode(data).LoopCount;
	}

	private static RasterImage Decode(byte[] data)
	{
		if (data.Length == 0) throw new InvalidDataException("Image data is empty.");
		if (PngCodec.IsPng(data)) return PngCodec.Decode(data);
		if (JpegCodec.IsJpeg(data)) return JpegCodec.Decode(data);
		if (BmpCodec.IsBmp(data)) return BmpCodec.Decode(data);
		throw new NotSupportedException("Only PNG, supported 8-bit JPEG, BMP and GIF are supported.");
	}
}
