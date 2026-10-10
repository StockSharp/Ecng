namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

/// <summary>
/// Platform-independent image utilities implemented entirely in managed C#.
/// No GDI+, platform-native image libraries, or third-party NuGet packages are used.
/// </summary>
public static class ImageHelper
{
	/// <summary>
	/// The quality a JPEG is written with unless another is asked for.
	/// </summary>
	public const int DefaultJpegQuality = 90;

	// The most characters a watermark may have.
	private const int _maxWatermarkLength = 1024;

	/// <summary>
	/// Reads the size of a PNG, JPEG, BMP or GIF picture off its header, without decoding the picture.
	/// </summary>
	/// <param name="data">The picture.</param>
	/// <returns>
	/// The size the picture is shown with, in pixels. For a JPEG whose Exif orientation lays it on its
	/// side that is the size after turning it upright.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The data is none of the four formats.</exception>
	/// <exception cref="InvalidDataException">The header is damaged.</exception>
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
	/// Reduces a picture to fit the given bounds, keeping its proportions. A picture that fits already is
	/// returned as it is; none is ever enlarged.
	/// </summary>
	/// <param name="data">A PNG, JPEG, BMP or GIF picture.</param>
	/// <param name="maxWidth">The most the result may be wide, in pixels.</param>
	/// <param name="maxHeight">The most the result may be high, in pixels.</param>
	/// <param name="jpegQuality">
	/// The quality a JPEG is written with, from 1 to 100 on the scale of libjpeg: the higher, the closer
	/// to the picture and the larger the file. A picture of another format takes no notice of it.
	/// </param>
	/// <returns>
	/// The picture as it came, if it fits already. Otherwise the picture in the format it came in: PNG,
	/// baseline JPEG, 32-bit BMP, or - every frame of it - an animated GIF or PNG with its timing and
	/// loop count. A JPEG that had an Exif orientation is then turned upright and has none. The ICC
	/// profile of a JPEG or a PNG stays with the picture if it is one for RGB; other metadata does not.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">A bound is not positive, or the quality is not from 1 to 100.</exception>
	/// <exception cref="NotSupportedException">The format, or a feature of it the picture uses, is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged, or larger than is decoded.</exception>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight, int jpegQuality = DefaultJpegQuality)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (maxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidth));
		if (maxHeight <= 0) throw new ArgumentOutOfRangeException(nameof(maxHeight));
		ValidateQuality(jpegQuality);

		if (OpenAnimation(data) is { } animation)
		{
			if (!TryFit(animation.Width, animation.Height, maxWidth, maxHeight, out var frameWidth, out var frameHeight))
			{
				Validate(animation);
				return (byte[])data.Clone();
			}

			return EncodeAnimation(data, animation.Select(frameWidth, frameHeight, frame => frame.Downscale(frameWidth, frameHeight)));
		}

		var image = Decode(data);
		if (!TryFit(image.Width, image.Height, maxWidth, maxHeight, out var width, out var height))
			return (byte[])data.Clone();

		return Encode(data, image.Downscale(width, height), jpegQuality);
	}

	/// <summary>
	/// Converts a picture to PNG.
	/// </summary>
	/// <param name="data">A PNG, JPEG, BMP or GIF picture.</param>
	/// <returns>
	/// An RGBA8 PNG. A GIF becomes an animated PNG with all its frames, delays and as many plays; a PNG
	/// that is animated already is checked and returned as it is. The ICC profile of a JPEG or a PNG
	/// stays with the picture if it is one for RGB.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The format, or a feature of it the picture uses, is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged, or larger than is decoded.</exception>
	public static byte[] ConvertToPng(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (GifCodec.IsGif(data)) return ApngEncoder.Encode(GifCodec.Open(data), []);
		if (ApngDecoder.IsApng(data))
		{
			Validate(ApngDecoder.Open(data));
			return (byte[])data.Clone();
		}
		return PngCodec.Encode(Decode(data), ColorChunks(data));
	}

	/// <summary>
	/// Renders anti-aliased semi-transparent white text into the bottom-right corner of a picture - of
	/// every frame, if it is animated. The text is laid out left to right in one line and is made smaller
	/// if it does not fit between the margins, down to 6 pixels to the em. A text that has no room even
	/// then is not drawn, and neither is one that would lay no opacity on any pixel: too small to reach
	/// a point a pixel is sampled at, or too faint for the part of a pixel it covers. The picture is then
	/// returned as it came. A text that is laid on and cannot be told from what is under it - white on
	/// white - is drawn, and the picture is written again.
	/// </summary>
	/// <param name="data">A PNG, JPEG, BMP or GIF picture.</param>
	/// <param name="text">Single-line watermark text, 1024 characters at most.</param>
	/// <param name="fontSize">Maximum font size in points. A size below 6 pixels to the em is kept as it is.</param>
	/// <param name="opacity">
	/// Text opacity, 0..255. With 0 nothing is drawn, no font is looked for and the picture is returned as it came.
	/// </param>
	/// <param name="margin">Margin around the text in pixels; a quarter of the smaller side of the picture at most.</param>
	/// <param name="fontFamily">
	/// Font family name, Verdana by default. If it is not installed, the first installed of a list of
	/// common sans-serif families is taken instead, and failing those the font built into the library,
	/// a subset of DejaVu Sans. "DejaVu Sans" itself is always there: installed or built in.
	/// </param>
	/// <param name="fontFilePath">Optional path to a TrueType .ttf file to use instead of an installed font.</param>
	/// <param name="jpegQuality">
	/// The quality a JPEG is written with, from 1 to 100 on the scale of libjpeg: the higher, the closer
	/// to the picture and the larger the file. A picture of another format takes no notice of it.
	/// </param>
	/// <returns>
	/// The picture as it came, if nothing is drawn on it. Otherwise the picture in the format it came in:
	/// PNG, baseline JPEG, 32-bit BMP, or - every frame of it - an animated GIF or PNG with its timing
	/// and loop count. A JPEG that had an Exif orientation is then turned upright and has none. The ICC
	/// profile of a JPEG or a PNG stays with the picture if it is one for RGB; other metadata does not.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">
	/// The text is empty, has a line break or more than 1024 characters, or none of its characters has
	/// an outline in the font; or the font family is empty.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// The font size is not a positive number, the margin is negative, or the quality is not from 1 to 100.
	/// </exception>
	/// <exception cref="FileNotFoundException"><paramref name="fontFilePath"/> does not exist.</exception>
	/// <exception cref="NotSupportedException">The format of the picture or of the font, or a feature of it, is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture or the font is damaged, or the picture is larger than is decoded.</exception>
	public static byte[] AddTextWatermark(
		this byte[] data, string text, float fontSize = 24, byte opacity = 160,
		int margin = 12, string fontFamily = "Verdana", string fontFilePath = null, int jpegQuality = DefaultJpegQuality)
	{
		ArgumentNullException.ThrowIfNull(data);
		ValidateWatermark(text, fontSize, margin, jpegQuality);
		if (string.IsNullOrWhiteSpace(fontFamily))
			throw new ArgumentException("A font family is required.", nameof(fontFamily));

		// The installed fonts are listed only if one of them is asked for.
		return Watermark(data, text, fontSize, opacity, margin, jpegQuality,
			() => fontFilePath is null ? TrueTypeFont.OfFamily(FontCatalog.Installed, fontFamily) : TrueTypeFont.FromFile(fontFilePath));
	}

	/// <summary>
	/// Renders a text watermark with a font given as the bytes of its file - one an application keeps in
	/// its resources, for instance - where the other overload looks for a font by family or by path.
	/// The text is placed, made smaller and left out the same way.
	/// </summary>
	/// <param name="data">A PNG, JPEG, BMP or GIF picture.</param>
	/// <param name="text">Single-line watermark text, 1024 characters at most.</param>
	/// <param name="font">A TrueType .ttf file. It is only read, and not at all with an opacity of 0.</param>
	/// <param name="fontSize">Maximum font size in points. A size below 6 pixels to the em is kept as it is.</param>
	/// <param name="opacity">Text opacity, 0..255. With 0 nothing is drawn and the picture is returned as it came.</param>
	/// <param name="margin">Margin around the text in pixels; a quarter of the smaller side of the picture at most.</param>
	/// <param name="jpegQuality">
	/// The quality a JPEG is written with, from 1 to 100 on the scale of libjpeg: the higher, the closer
	/// to the picture and the larger the file. A picture of another format takes no notice of it.
	/// </param>
	/// <returns>
	/// The picture as it came, if nothing is drawn on it. Otherwise the picture in the format it came in:
	/// PNG, baseline JPEG, 32-bit BMP, or - every frame of it - an animated GIF or PNG with its timing
	/// and loop count. A JPEG that had an Exif orientation is then turned upright and has none. The ICC
	/// profile of a JPEG or a PNG stays with the picture if it is one for RGB; other metadata does not.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> or <paramref name="font"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">
	/// The text is empty, has a line break or more than 1024 characters, or none of its characters has
	/// an outline in the font.
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// The font size is not a positive number, the margin is negative, or the quality is not from 1 to 100.
	/// </exception>
	/// <exception cref="NotSupportedException">The format of the picture or of the font, or a feature of it, is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture or the font is damaged, or the picture is larger than is decoded.</exception>
	public static byte[] AddTextWatermark(
		this byte[] data, string text, byte[] font, float fontSize = 24, byte opacity = 160, int margin = 12,
		int jpegQuality = DefaultJpegQuality)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(font);
		ValidateWatermark(text, fontSize, margin, jpegQuality);

		return Watermark(data, text, fontSize, opacity, margin, jpegQuality, () => new TrueTypeFont(font));
	}

	/// <summary>
	/// Decodes every frame of a GIF.
	/// </summary>
	/// <param name="data">A GIF picture.</param>
	/// <returns>The frames as they are shown - the whole canvas, as an RGBA8 PNG - each with the time it is shown for.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The data is not a GIF, or uses a feature that is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged, or larger than is decoded.</exception>
	public static IReadOnlyList<(byte[] png, int delayMilliseconds)> GetGifFrames(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (!GifCodec.IsGif(data)) throw new NotSupportedException("Expected GIF87a/GIF89a data.");
		return ToFrames(data, GifCodec.Open(data));
	}

	/// <summary>
	/// Decodes every frame of a GIF or of an animated PNG.
	/// </summary>
	/// <param name="data">A GIF or an animated PNG.</param>
	/// <returns>The frames as they are shown - the whole canvas, as an RGBA8 PNG - each with the time it is shown for.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The data is neither, or uses a feature that is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged, or larger than is decoded.</exception>
	public static IReadOnlyList<(byte[] png, int delayMilliseconds)> GetAnimationFrames(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		return ToFrames(data, OpenAnimation(data) ?? throw new NotSupportedException("Only GIF and APNG animations are supported."));
	}

	/// <summary>
	/// Reads how many times a GIF or an animated PNG plays, without decoding its frames.
	/// </summary>
	/// <param name="data">A GIF or an animated PNG.</param>
	/// <returns>The number of plays, 0 for endlessly. A GIF without a loop extension plays once.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The data is neither.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged.</exception>
	public static int GetAnimationLoopCount(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);

		if (GifCodec.IsGif(data))
		{
			// The extension counts repetitions after the first run.
			var loops = GifCodec.ReadLoopCount(data);
			return loops < 0 ? 1 : loops == 0 ? 0 : loops + 1;
		}

		if (ApngDecoder.IsApng(data)) return ApngDecoder.ReadPlays(data);
		throw new NotSupportedException("Only GIF and APNG animations are supported.");
	}

	/// <summary>
	/// Reads the loop count of the NETSCAPE2.0 extension of a GIF, without decoding its frames.
	/// </summary>
	/// <param name="data">A GIF picture.</param>
	/// <returns>
	/// How many times the animation is repeated after its first run, 0 for endlessly; -1 if the file has
	/// no such extension. <see cref="GetAnimationLoopCount"/> gives the number of plays instead.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The data is not a GIF.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged.</exception>
	public static int GetGifLoopCount(this byte[] data)
	{
		ArgumentNullException.ThrowIfNull(data);
		if (!GifCodec.IsGif(data)) throw new NotSupportedException("Expected GIF87a/GIF89a data.");
		return GifCodec.ReadLoopCount(data);
	}

	private static void ValidateQuality(int jpegQuality)
	{
		if (jpegQuality is < JpegEncoder.MinQuality or > JpegEncoder.MaxQuality)
			throw new ArgumentOutOfRangeException(nameof(jpegQuality));
	}

	private static void ValidateWatermark(string text, float fontSize, int margin, int jpegQuality)
	{
		if (string.IsNullOrWhiteSpace(text))
			throw new ArgumentException("Watermark text must not be empty.", nameof(text));
		if (text.Any(IsLineBreak) || text.EnumerateRunes().Count() > _maxWatermarkLength)
			throw new ArgumentException($"Watermark must be one line of {_maxWatermarkLength} characters at most.", nameof(text));
		if (fontSize <= 0 || float.IsNaN(fontSize) || float.IsInfinity(fontSize))
			throw new ArgumentOutOfRangeException(nameof(fontSize));
		if (margin < 0)
			throw new ArgumentOutOfRangeException(nameof(margin));

		ValidateQuality(jpegQuality);
	}

	// The controls from the line feed to the carriage return, the next-line control and the line and
	// paragraph separators.
	private static bool IsLineBreak(char character)
		=> character is >= '\n' and <= '\r' or '\u0085' or '\u2028' or '\u2029';

	// The font is opened only if there is a text to draw with it.
	private static byte[] Watermark(byte[] data, string text, float fontSize, byte opacity, int margin, int jpegQuality, Func<TrueTypeFont> openFont)
	{
		// False if nothing is to be drawn: the text is invisible, or a picture of that size has no room for it
		// or would show no pixel of it.
		bool TryRender(int width, int height, out TextMask rendered)
		{
			rendered = null;

			return opacity != 0 && openFont().TryRender(width, height, text, fontSize, margin, opacity, out rendered);
		}

		if (OpenAnimation(data) is { } animation)
		{
			if (!TryRender(animation.Width, animation.Height, out var frameMask))
			{
				Validate(animation);
				return (byte[])data.Clone();
			}

			return EncodeAnimation(data, animation.Select(animation.Width, animation.Height, frame =>
			{
				frameMask.Draw(frame);
				return frame;
			}));
		}

		var image = Decode(data);

		if (!TryRender(image.Width, image.Height, out var mask))
			return (byte[])data.Clone();

		mask.Draw(image);

		return Encode(data, image, jpegQuality);
	}

	// Null if the picture is neither a GIF nor an animated PNG.
	private static Animation OpenAnimation(byte[] data)
		=> GifCodec.IsGif(data) ? GifCodec.Open(data) : ApngDecoder.IsApng(data) ? ApngDecoder.Open(data) : null;

	// Writes a picture in the format its source came in, with the color profile the source has.
	private static byte[] Encode(byte[] source, RasterImage image, int jpegQuality)
		=> JpegCodec.IsJpeg(source) ? JpegEncoder.Encode(image, jpegQuality, JpegSegments.ReadProfile(source)) :
			BmpCodec.IsBmp(source) ? BmpCodec.Encode(image) : PngCodec.Encode(image, ColorChunks(source));

	// Writes an animation in the format its source came in.
	private static byte[] EncodeAnimation(byte[] source, Animation animation)
		=> GifCodec.IsGif(source) ? GifCodec.Encode(animation) : ApngEncoder.Encode(animation, ColorChunks(source));

	// What a PNG made of a picture says of its colors: what the picture says of them itself.
	private static byte[] ColorChunks(byte[] source)
		=> PngCodec.IsPng(source) ? PngCodec.ReadColorChunks(source) :
			JpegCodec.IsJpeg(source) ? PngCodec.ProfileChunk(JpegSegments.ReadProfile(source)) : [];

	// Frames are decoded as they are asked for: asking for all of them is what proves the file sound.
	private static void Validate(Animation animation)
	{
		foreach (var _ in animation.Frames)
		{
		}
	}

	private static IReadOnlyList<(byte[] png, int delayMilliseconds)> ToFrames(byte[] source, Animation animation)
	{
		var frames = new List<(byte[] png, int delayMilliseconds)>(animation.FrameCount);
		var colorChunks = ColorChunks(source);

		foreach (var frame in animation.Frames)
			frames.Add((PngCodec.Encode(frame.Image, colorChunks), frame.DelayMilliseconds));

		return frames;
	}

	// Works out the size a picture takes inside the bounds, keeping its proportions.
	// Returns false if the picture fits as it is.
	private static bool TryFit(int width, int height, int maxWidth, int maxHeight, out int newWidth, out int newHeight)
	{
		newWidth = width;
		newHeight = height;

		if (width <= maxWidth && height <= maxHeight)
			return false;

		// The tighter bound decides. Whole numbers: a ratio taken in floating point can land
		// a hair under the bound and lose a pixel.
		if ((long)width * maxHeight >= (long)height * maxWidth)
		{
			newWidth = maxWidth;
			newHeight = (int)Math.Max(1, (long)height * maxWidth / width);
		}
		else
		{
			newHeight = maxHeight;
			newWidth = (int)Math.Max(1, (long)width * maxHeight / height);
		}

		return true;
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
