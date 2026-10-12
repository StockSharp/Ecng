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
	/// Reads the display size of a supported image without decoding pixels.
	/// </summary>
	/// <param name="data">The picture.</param>
	/// <returns>
	/// The size the picture is shown with, in pixels. For a JPEG whose Exif orientation lays it on its
	/// side that is the size after turning it upright.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The format or header feature is unsupported.</exception>
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
			IconCodec.IsIcon(data) ? IconCodec.ReadSize(data) :
			WebpCodec.IsWebp(data) ? WebpCodec.ReadSize(data) :
			TiffCodec.IsTiff(data) ? TiffCodec.ReadSize(data) :
			AvifCodec.IsAvif(data) ? AvifCodec.ReadSize(data) :
			throw new NotSupportedException("Supported images are PNG, JPEG, BMP, GIF, WebP, ICO, TIFF and AVIF.");

		return new Size(width, height);
	}

	/// <summary>
	/// Identifies a supported file signature without decoding pixels or validating the complete file.
	/// </summary>
	/// <param name="data">The image bytes.</param>
	/// <returns>The detected format, or <see cref="ImageFormats.Unknown"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
	public static ImageFormats GetImageFormat(this byte[] data)
		=> GetImageFormat((ReadOnlySpan<byte>)(data ?? throw new ArgumentNullException(nameof(data))));

	/// <inheritdoc cref="GetImageFormat(byte[])"/>
	public static ImageFormats GetImageFormat(this ReadOnlySpan<byte> data)
		=> PngCodec.IsPng(data) ? ImageFormats.Png :
			JpegCodec.IsJpeg(data) ? ImageFormats.Jpeg :
			BmpCodec.IsBmp(data) ? ImageFormats.Bmp :
			GifCodec.IsGif(data) ? ImageFormats.Gif :
			IconCodec.IsIcon(data) ? ImageFormats.Icon :
			WebpCodec.IsWebp(data) ? ImageFormats.Webp :
			TiffCodec.IsTiff(data) ? ImageFormats.Tiff :
			AvifCodec.IsAvif(data) ? ImageFormats.Avif : ImageFormats.Unknown;

	/// <summary>
	/// Tries to read the display dimensions from an image header without decoding pixels.
	/// A readable header does not guarantee that pixel decoding will succeed.
	/// </summary>
	/// <param name="data">The image bytes; null is treated as unreadable.</param>
	/// <param name="size">The display size, or <see cref="Size.Empty"/> on failure.</param>
	/// <returns>Whether the format and dimensions could be read.</returns>
	public static bool TryGetImageSize(this byte[] data, out Size size)
		=> TryGetImageSize((ReadOnlySpan<byte>)data, out size);

	/// <inheritdoc cref="TryGetImageSize(byte[], out Size)"/>
	public static bool TryGetImageSize(this ReadOnlySpan<byte> data, out Size size)
	{
		size = Size.Empty;
		try
		{
			size = GetImageSize(data);
			return true;
		}
		catch (Exception error) when (error is InvalidDataException or NotSupportedException)
		{
			return false;
		}
	}

	/// <summary>
	/// Reduces a picture to fit the given bounds, keeping its proportions. A picture that fits already is
	/// returned as it is; none is ever enlarged.
	/// </summary>
	/// <param name="data">A PNG, JPEG, BMP, GIF, WebP, ICO, TIFF or AVIF picture.</param>
	/// <param name="maxWidth">The most the result may be wide, in pixels.</param>
	/// <param name="maxHeight">The most the result may be high, in pixels.</param>
	/// <param name="jpegQuality">
	/// The quality a JPEG is written with, from 1 to 100 on the scale of libjpeg: the higher, the closer
	/// to the picture and the larger the file. A picture of another format takes no notice of it.
	/// </param>
	/// <returns>
	/// The picture as it came, if it fits already. Otherwise the picture in the format it came in: PNG,
	/// baseline JPEG, 32-bit BMP, ICO, WebP, or all frames of an animated GIF or PNG with its timing and
	/// loop count. A JPEG that had an Exif orientation is then turned upright and has none. The ICC
	/// profile of a JPEG or a PNG stays with the picture if it is one for RGB; other metadata does not.
	/// Modified TIFF and AVIF inputs are written as PNG.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">A bound is not positive, or the quality is not from 1 to 100.</exception>
	/// <exception cref="NotSupportedException">The format, or a feature of it the picture uses, is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged, or larger than is decoded.</exception>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight, int jpegQuality = DefaultJpegQuality)
		=> Resize(data, maxWidth, maxHeight, null, jpegQuality, ImageProcessingLimits.Default);

	/// <summary>Reduces a picture to fit the bounds and writes it in the requested format.</summary>
	/// <param name="data">The source image.</param>
	/// <param name="maxWidth">Maximum output width.</param>
	/// <param name="maxHeight">Maximum output height.</param>
	/// <param name="outputFormat">PNG, JPEG, BMP, GIF, WebP or ICO. Animated output requires PNG, GIF or WebP.</param>
	/// <param name="jpegQuality">JPEG quality from 1 to 100.</param>
	/// <returns>The resized or converted image, retaining all animation frames.</returns>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight, ImageFormats outputFormat, int jpegQuality = DefaultJpegQuality)
		=> Resize(data, maxWidth, maxHeight, outputFormat, jpegQuality, ImageProcessingLimits.Default);

	/// <summary>Reduces an image or animation with explicit limits and retains its format.</summary>
	/// <param name="data">Encoded source image.</param>
	/// <param name="maxWidth">Positive maximum width.</param>
	/// <param name="maxHeight">Positive maximum height.</param>
	/// <param name="limits">Limits on decoded images and animation work.</param>
	/// <param name="jpegQuality">JPEG encoding quality, 1 through 100.</param>
	/// <returns>Encoded output retaining animation frames.</returns>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight, ImageProcessingLimits limits, int jpegQuality = DefaultJpegQuality)
		=> Resize(data, maxWidth, maxHeight, null, jpegQuality, limits);

	/// <summary>Reduces an image or animation with explicit limits and an explicit output format.</summary>
	/// <param name="data">Encoded source image.</param>
	/// <param name="maxWidth">Positive maximum width.</param>
	/// <param name="maxHeight">Positive maximum height.</param>
	/// <param name="outputFormat">PNG, JPEG, BMP, GIF, WebP or ICO.</param>
	/// <param name="limits">Limits on decoded images and animation work.</param>
	/// <param name="jpegQuality">JPEG encoding quality, 1 through 100.</param>
	/// <returns>Encoded output retaining animation when the format supports it.</returns>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight, ImageFormats outputFormat, ImageProcessingLimits limits, int jpegQuality = DefaultJpegQuality)
		=> Resize(data, maxWidth, maxHeight, outputFormat, jpegQuality, limits);

	private static byte[] Resize(byte[] data, int maxWidth, int maxHeight, ImageFormats? outputFormat, int jpegQuality, ImageProcessingLimits limits)
		=> Resize(data, maxWidth, maxHeight, outputFormat, jpegQuality, limits, WebpEncodingOptions.Lossless);

	/// <summary>Reduces or converts a picture with explicit WebP compression settings.</summary>
	/// <param name="data">Encoded source image or animation.</param>
	/// <param name="maxWidth">Positive maximum output width.</param>
	/// <param name="maxHeight">Positive maximum output height.</param>
	/// <param name="outputFormat">Requested output format.</param>
	/// <param name="webpOptions">WebP mode and quality, used for WebP output.</param>
	/// <param name="jpegQuality">JPEG quality, 1 through 100.</param>
	/// <returns>Encoded output retaining all animation frames.</returns>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight, ImageFormats outputFormat,
		WebpEncodingOptions webpOptions, int jpegQuality = DefaultJpegQuality)
		=> Resize(data, maxWidth, maxHeight, outputFormat, jpegQuality, ImageProcessingLimits.Default, webpOptions);

	/// <summary>Reduces or converts a picture with explicit processing limits and WebP settings.</summary>
	/// <param name="data">Encoded source image or animation.</param>
	/// <param name="maxWidth">Positive maximum output width.</param>
	/// <param name="maxHeight">Positive maximum output height.</param>
	/// <param name="outputFormat">Requested output format.</param>
	/// <param name="limits">Explicit image and animation processing budgets.</param>
	/// <param name="webpOptions">WebP mode and quality, used for WebP output.</param>
	/// <param name="jpegQuality">JPEG quality, 1 through 100.</param>
	/// <returns>Encoded output retaining all animation frames.</returns>
	public static byte[] ResizeImage(this byte[] data, int maxWidth, int maxHeight, ImageFormats outputFormat,
		ImageProcessingLimits limits, WebpEncodingOptions webpOptions, int jpegQuality = DefaultJpegQuality)
		=> Resize(data, maxWidth, maxHeight, outputFormat, jpegQuality, limits, webpOptions);

	private static byte[] Resize(byte[] data, int maxWidth, int maxHeight, ImageFormats? outputFormat, int jpegQuality,
		ImageProcessingLimits limits, WebpEncodingOptions webpOptions)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(limits);
		ArgumentNullException.ThrowIfNull(webpOptions);
		if (maxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidth));
		if (maxHeight <= 0) throw new ArgumentOutOfRangeException(nameof(maxHeight));
		ValidateQuality(jpegQuality);
		ValidateOutput(outputFormat);
		var rewriteWebp = (outputFormat ?? DefaultOutput(data)) == ImageFormats.Webp && webpOptions.Mode == WebpCompressionModes.Lossy;

		if (OpenAnimation(data, limits) is { } animation)
		{
			if (!TryFit(animation.Width, animation.Height, maxWidth, maxHeight, out var frameWidth, out var frameHeight))
			{
				if (!IsSameFormat(data, outputFormat) || rewriteWebp) return EncodeAnimation(data, animation, outputFormat, jpegQuality, webpOptions);
				Validate(animation);
				return (byte[])data.Clone();
			}

			return EncodeAnimation(data, animation.Select(frameWidth, frameHeight, frame => frame.Downscale(frameWidth, frameHeight)), outputFormat, jpegQuality, webpOptions);
		}

		var size = data.GetImageSize();
		if (JpegCodec.IsJpeg(data))
			JpegCodec.ValidateScanHeader(data);
		if (!TryFit(size.Width, size.Height, maxWidth, maxHeight, out var width, out var height))
			return IsSameFormat(data, outputFormat) && !rewriteWebp ? (byte[])data.Clone() : Encode(data, Decode(data, limits), jpegQuality, outputFormat, webpOptions);

		var image = PngCodec.IsPng(data) ? PngCodec.DecodeDownscaled(data, width, height, limits) :
			JpegCodec.IsJpeg(data) ? JpegCodec.DecodeDownscaled(data, width, height, limits) :
			BmpCodec.IsBmp(data) ? BmpCodec.DecodeDownscaled(data, width, height, limits) :
			TiffCodec.IsTiff(data) ? TiffCodec.DecodeDownscaled(data, width, height, limits) :
			WebpCodec.IsWebp(data) ? WebpCodec.DecodeDownscaled(data, width, height, limits) :
			AvifCodec.IsAvif(data) ? AvifCodec.DecodeDownscaled(data, width, height, limits) : Decode(data, limits).Downscale(width, height);
		return Encode(data, image, jpegQuality, outputFormat, webpOptions);
	}

	/// <summary>
	/// Converts a picture to PNG.
	/// </summary>
	/// <param name="data">A PNG, JPEG, BMP, GIF, WebP, ICO, TIFF or AVIF picture.</param>
	/// <returns>
	/// An RGBA8 PNG. A GIF becomes an animated PNG with all its frames, delays and as many plays; a PNG
	/// that is animated already is checked and returned as it is. The ICC profile of a JPEG or a PNG
	/// stays with the picture if it is one for RGB.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The format, or a feature of it the picture uses, is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged, or larger than is decoded.</exception>
	public static byte[] ConvertToPng(this byte[] data)
		=> ConvertToPng(data, ImageProcessingLimits.Default);

	/// <summary>Converts every displayed animation frame, or a static picture, to PNG under explicit limits.</summary>
	/// <param name="data">Encoded image data.</param>
	/// <param name="limits">Limits on decoded images and animation work.</param>
	/// <returns>RGBA8 PNG or APNG, preserving frame timing and supported color profiles.</returns>
	/// <exception cref="ArgumentNullException">Data or limits are null.</exception>
	/// <exception cref="InvalidDataException">The image is damaged or exceeds a processing limit.</exception>
	/// <exception cref="NotSupportedException">The image uses an unsupported format or feature.</exception>
	public static byte[] ConvertToPng(this byte[] data, ImageProcessingLimits limits)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(limits);
		if (GifCodec.IsGif(data)) return ApngEncoder.Encode(GifCodec.Open(data, limits), []);
		if (ApngDecoder.IsApng(data))
		{
			Validate(ApngDecoder.Open(data, limits));
			return (byte[])data.Clone();
		}
		if (WebpCodec.IsWebp(data) && WebpCodec.Open(data, limits) is { } animation)
			return ApngEncoder.Encode(animation, []);
		return PngCodec.Encode(Decode(data, limits), ColorChunks(data));
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
	/// <param name="data">A PNG, JPEG, BMP, GIF, WebP, ICO, TIFF or AVIF picture.</param>
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
	/// PNG, baseline JPEG, 32-bit BMP, ICO, WebP, or all frames of an animated GIF or PNG with its timing
	/// and loop count. A JPEG that had an Exif orientation is then turned upright and has none. The ICC
	/// profile of a JPEG or a PNG stays with the picture if it is one for RGB; other metadata does not.
	/// Modified TIFF and AVIF inputs are written as PNG.
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
			() => fontFilePath is null ? TrueTypeFont.OfFamily(FontCatalog.Installed, fontFamily) : TrueTypeFont.FromFile(fontFilePath), ImageProcessingLimits.Default);
	}

	/// <summary>
	/// Renders a text watermark with a font given as the bytes of its file - one an application keeps in
	/// its resources, for instance - where the other overload looks for a font by family or by path.
	/// The text is placed, made smaller and left out the same way.
	/// </summary>
	/// <param name="data">A PNG, JPEG, BMP, GIF, WebP, ICO, TIFF or AVIF picture.</param>
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
	/// PNG, baseline JPEG, 32-bit BMP, ICO, WebP, or all frames of an animated GIF or PNG with its timing
	/// and loop count. A JPEG that had an Exif orientation is then turned upright and has none. The ICC
	/// profile of a JPEG or a PNG stays with the picture if it is one for RGB; other metadata does not.
	/// Modified TIFF and AVIF inputs are written as PNG.
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

		return Watermark(data, text, fontSize, opacity, margin, jpegQuality, () => new TrueTypeFont(font), ImageProcessingLimits.Default);
	}

	/// <summary>Renders a watermark and writes the requested image format.</summary>
	/// <param name="data">The source image.</param>
	/// <param name="text">Single-line watermark text.</param>
	/// <param name="outputFormat">PNG, JPEG, BMP, GIF, WebP or ICO.</param>
	/// <param name="fontSize">Maximum font size.</param>
	/// <param name="opacity">Text opacity from 0 to 255.</param>
	/// <param name="margin">Margin around the text.</param>
	/// <param name="fontFamily">Installed font family.</param>
	/// <param name="fontFilePath">Optional TrueType font file.</param>
	/// <param name="jpegQuality">JPEG quality from 1 to 100.</param>
	/// <returns>The image in the requested format, retaining all animation frames.</returns>
	public static byte[] AddTextWatermark(this byte[] data, string text, ImageFormats outputFormat,
		float fontSize = 24, byte opacity = 160, int margin = 12, string fontFamily = "Verdana", string fontFilePath = null, int jpegQuality = DefaultJpegQuality)
	{
		ArgumentNullException.ThrowIfNull(data);
		ValidateWatermark(text, fontSize, margin, jpegQuality);
		ValidateOutput(outputFormat);
		if (string.IsNullOrWhiteSpace(fontFamily)) throw new ArgumentException("A font family is required.", nameof(fontFamily));
		return Watermark(data, text, fontSize, opacity, margin, jpegQuality,
			() => fontFilePath is null ? TrueTypeFont.OfFamily(FontCatalog.Installed, fontFamily) : TrueTypeFont.FromFile(fontFilePath), ImageProcessingLimits.Default, outputFormat);
	}

	/// <summary>Renders a watermark with supplied font bytes and writes the requested image format.</summary>
	/// <param name="data">The source image.</param>
	/// <param name="text">Single-line watermark text.</param>
	/// <param name="font">TrueType font bytes; not read when opacity is zero.</param>
	/// <param name="outputFormat">PNG, JPEG, BMP, GIF, WebP or ICO.</param>
	/// <param name="fontSize">Maximum font size.</param>
	/// <param name="opacity">Text opacity from 0 to 255.</param>
	/// <param name="margin">Margin around the text.</param>
	/// <param name="jpegQuality">JPEG quality from 1 to 100.</param>
	/// <returns>The image in the requested format, retaining all animation frames.</returns>
	public static byte[] AddTextWatermark(this byte[] data, string text, byte[] font, ImageFormats outputFormat,
		float fontSize = 24, byte opacity = 160, int margin = 12, int jpegQuality = DefaultJpegQuality)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(font);
		ValidateWatermark(text, fontSize, margin, jpegQuality);
		ValidateOutput(outputFormat);
		return Watermark(data, text, fontSize, opacity, margin, jpegQuality, () => new TrueTypeFont(font), ImageProcessingLimits.Default, outputFormat);
	}

	/// <summary>Renders a text watermark under explicit image and animation limits.</summary>
	/// <param name="data">Encoded source image.</param>
	/// <param name="text">Single-line text.</param>
	/// <param name="limits">Limits on decoded images and animation work.</param>
	/// <param name="fontSize">Positive maximum font size.</param>
	/// <param name="opacity">Text opacity, 0 through 255.</param>
	/// <param name="margin">Nonnegative margin.</param>
	/// <param name="fontFamily">Installed font family or the built-in DejaVu Sans.</param>
	/// <param name="jpegQuality">JPEG encoding quality, 1 through 100.</param>
	/// <returns>Encoded output retaining animation frames and timing.</returns>
	public static byte[] AddTextWatermark(this byte[] data, string text, ImageProcessingLimits limits,
		float fontSize = 24, byte opacity = 160, int margin = 12, string fontFamily = "Verdana", int jpegQuality = DefaultJpegQuality)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(limits);
		ValidateWatermark(text, fontSize, margin, jpegQuality);
		if (string.IsNullOrWhiteSpace(fontFamily)) throw new ArgumentException("A font family is required.", nameof(fontFamily));
		return Watermark(data, text, fontSize, opacity, margin, jpegQuality, () => TrueTypeFont.OfFamily(FontCatalog.Installed, fontFamily), limits);
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
	/// Decodes every displayed frame of a GIF, APNG or animated WebP.
	/// </summary>
	/// <param name="data">A GIF, APNG or animated WebP.</param>
	/// <returns>The frames as they are shown - the whole canvas, as an RGBA8 PNG - each with the time it is shown for.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
	/// <exception cref="NotSupportedException">The data is neither, or uses a feature that is not supported.</exception>
	/// <exception cref="InvalidDataException">The picture is damaged, or larger than is decoded.</exception>
	public static IReadOnlyList<(byte[] png, int delayMilliseconds)> GetAnimationFrames(this byte[] data)
		=> GetAnimationFrames(data, ImageProcessingLimits.Default);

	/// <summary>Decodes every displayed animation frame under explicit processing limits.</summary>
	/// <param name="data">GIF, APNG or animated WebP bytes.</param>
	/// <param name="limits">Limits on decoded images and animation work.</param>
	/// <returns>Full displayed frames as PNG, each with its delay in milliseconds.</returns>
	public static IReadOnlyList<(byte[] png, int delayMilliseconds)> GetAnimationFrames(this byte[] data, ImageProcessingLimits limits)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(limits);
		return ToFrames(data, OpenAnimation(data, limits) ?? throw new NotSupportedException("Supported animations are GIF, APNG and WebP."));
	}

	/// <summary>
	/// Reads how many times a GIF, APNG or animated WebP plays, without decoding pixels.
	/// </summary>
	/// <param name="data">A GIF, APNG or animated WebP.</param>
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
		if (WebpCodec.IsWebp(data)) return WebpCodec.ReadPlays(data);
		throw new NotSupportedException("Supported animations are GIF, APNG and WebP.");
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

	/// <summary>Renders a watermark with explicit WebP mode and quality.</summary>
	/// <param name="data">Encoded source image or animation.</param>
	/// <param name="text">Single-line watermark text.</param>
	/// <param name="outputFormat">Requested output format.</param>
	/// <param name="webpOptions">WebP mode and quality.</param>
	/// <param name="fontSize">Positive maximum font size.</param>
	/// <param name="opacity">Text opacity, 0 through 255.</param>
	/// <param name="margin">Nonnegative margin.</param>
	/// <param name="fontFamily">Font family, including the bundled DejaVu Sans.</param>
	/// <param name="jpegQuality">JPEG quality, 1 through 100.</param>
	/// <returns>Encoded output retaining all animation frames.</returns>
	public static byte[] AddTextWatermark(this byte[] data, string text, ImageFormats outputFormat, WebpEncodingOptions webpOptions,
		float fontSize = 24, byte opacity = 160, int margin = 12, string fontFamily = "Verdana", int jpegQuality = DefaultJpegQuality)
	{
		ArgumentNullException.ThrowIfNull(data);
		ArgumentNullException.ThrowIfNull(webpOptions);
		ValidateWatermark(text, fontSize, margin, jpegQuality);
		ValidateOutput(outputFormat);
		if (string.IsNullOrWhiteSpace(fontFamily)) throw new ArgumentException("A font family is required.", nameof(fontFamily));
		return Watermark(data, text, fontSize, opacity, margin, jpegQuality, () => TrueTypeFont.OfFamily(FontCatalog.Installed, fontFamily),
			ImageProcessingLimits.Default, outputFormat, webpOptions);
	}

	// The font is opened only if there is a text to draw with it.
	private static byte[] Watermark(byte[] data, string text, float fontSize, byte opacity, int margin, int jpegQuality, Func<TrueTypeFont> openFont, ImageProcessingLimits limits, ImageFormats? outputFormat = null)
		=> Watermark(data, text, fontSize, opacity, margin, jpegQuality, openFont, limits, outputFormat, WebpEncodingOptions.Lossless);

	private static byte[] Watermark(byte[] data, string text, float fontSize, byte opacity, int margin, int jpegQuality,
		Func<TrueTypeFont> openFont, ImageProcessingLimits limits, ImageFormats? outputFormat, WebpEncodingOptions webpOptions)
	{
		var rewriteWebp = (outputFormat ?? DefaultOutput(data)) == ImageFormats.Webp && webpOptions.Mode == WebpCompressionModes.Lossy;
		// False if nothing is to be drawn: the text is invisible, or a picture of that size has no room for it
		// or would show no pixel of it.
		bool TryRender(int width, int height, out TextMask rendered)
		{
			rendered = null;

			return opacity != 0 && openFont().TryRender(width, height, text, fontSize, margin, opacity, out rendered);
		}

		if (OpenAnimation(data, limits) is { } animation)
		{
			if (!TryRender(animation.Width, animation.Height, out var frameMask))
			{
				if (!IsSameFormat(data, outputFormat) || rewriteWebp) return EncodeAnimation(data, animation, outputFormat, jpegQuality, webpOptions);
				Validate(animation);
				return (byte[])data.Clone();
			}

			return EncodeAnimation(data, animation.Select(animation.Width, animation.Height, frame =>
			{
				frameMask.Draw(frame);
				return frame;
			}), outputFormat, jpegQuality, webpOptions);
		}

		var image = Decode(data, limits);

		if (!TryRender(image.Width, image.Height, out var mask))
			return IsSameFormat(data, outputFormat) && !rewriteWebp ? (byte[])data.Clone() : Encode(data, image, jpegQuality, outputFormat, webpOptions);

		mask.Draw(image);

		return Encode(data, image, jpegQuality, outputFormat, webpOptions);
	}

	// Null for static images.
	private static Animation OpenAnimation(byte[] data)
		=> OpenAnimation(data, ImageProcessingLimits.Default);

	private static Animation OpenAnimation(byte[] data, ImageProcessingLimits limits)
		=> GifCodec.IsGif(data) ? GifCodec.Open(data, limits) : ApngDecoder.IsApng(data) ? ApngDecoder.Open(data, limits) : WebpCodec.IsWebp(data) ? WebpCodec.Open(data, limits) : null;

	// Writes a picture in the format its source came in, with the color profile the source has.
	private static byte[] Encode(byte[] source, RasterImage image, int jpegQuality, ImageFormats? outputFormat = null)
		=> Encode(source, image, jpegQuality, outputFormat, WebpEncodingOptions.Lossless);

	private static byte[] Encode(byte[] source, RasterImage image, int jpegQuality, ImageFormats? outputFormat, WebpEncodingOptions webpOptions)
		=> (outputFormat ?? DefaultOutput(source)) switch
		{
			ImageFormats.Jpeg => JpegEncoder.Encode(image, jpegQuality, JpegCodec.IsJpeg(source) ? JpegSegments.ReadProfile(source) : []),
			ImageFormats.Bmp => BmpCodec.Encode(image),
			ImageFormats.Icon => IconCodec.Encode(image),
			ImageFormats.Webp => WebpEncoder.Encode(image, webpOptions),
			ImageFormats.Gif => GifCodec.Encode(new(image.Width, image.Height, 1, 1, [new(image, 0, 100)])),
			_ => PngCodec.Encode(image, ColorChunks(source)),
		};

	// Writes an animation in the format its source came in.
	private static byte[] EncodeAnimation(byte[] source, Animation animation, ImageFormats? outputFormat = null, int jpegQuality = DefaultJpegQuality)
		=> EncodeAnimation(source, animation, outputFormat, jpegQuality, WebpEncodingOptions.Lossless);

	private static byte[] EncodeAnimation(byte[] source, Animation animation, ImageFormats? outputFormat, int jpegQuality, WebpEncodingOptions webpOptions)
	{
		var format = outputFormat ?? DefaultOutput(source);
		if (format == ImageFormats.Gif) return GifCodec.Encode(animation);
		if (format == ImageFormats.Webp) return WebpEncoder.Encode(animation, webpOptions);
		if (format == ImageFormats.Png) return ApngEncoder.Encode(animation, ColorChunks(source));
		if (animation.FrameCount != 1) throw new NotSupportedException("The output format cannot retain animation frames.");
		foreach (var frame in animation.Frames) return Encode(source, frame.Image, jpegQuality, format, webpOptions);
		throw new InvalidDataException("Animation has no frames.");
	}

	private static bool IsSameFormat(byte[] source, ImageFormats? outputFormat)
		=> !outputFormat.HasValue || outputFormat.Value == source.GetImageFormat();

	private static ImageFormats DefaultOutput(byte[] source)
		=> source.GetImageFormat() switch
		{
			ImageFormats.Avif or ImageFormats.Tiff => ImageFormats.Png,
			var format => format,
		};

	private static void ValidateOutput(ImageFormats? outputFormat)
	{
		if (!outputFormat.HasValue) return;
		if (outputFormat is ImageFormats.Avif or ImageFormats.Tiff) throw new NotSupportedException("TIFF and AVIF encoding is not supported. Choose PNG, JPEG, BMP, GIF, WebP or ICO.");
		if (outputFormat < ImageFormats.Png || outputFormat > ImageFormats.Icon) throw new ArgumentOutOfRangeException(nameof(outputFormat));
	}

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
		=> Decode(data, ImageProcessingLimits.Default);

	private static RasterImage Decode(byte[] data, ImageProcessingLimits limits)
	{
		if (data.Length == 0) throw new InvalidDataException("Image data is empty.");
		if (PngCodec.IsPng(data)) return PngCodec.Decode(data, limits);
		var size = data.GetImageSize();
		RasterImage.EnsureDecodable(size.Width, size.Height, limits);
		if (JpegCodec.IsJpeg(data)) return JpegCodec.Decode(data, limits);
		if (BmpCodec.IsBmp(data)) return BmpCodec.Decode(data, limits);
		if (IconCodec.IsIcon(data)) return IconCodec.Decode(data, limits);
		if (WebpCodec.IsWebp(data)) return WebpCodec.Decode(data, limits);
		if (TiffCodec.IsTiff(data)) return TiffCodec.Decode(data, limits);
		if (AvifCodec.IsAvif(data)) return AvifCodec.Decode(data, limits);
		throw new NotSupportedException("Supported images are PNG, JPEG, BMP, GIF, WebP, ICO, TIFF and AVIF.");
	}
}
