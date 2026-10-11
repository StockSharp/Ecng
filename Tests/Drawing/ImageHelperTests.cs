namespace Ecng.Tests.Drawing;

using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class ImageHelperTests : BaseTestClass
{
	// Fixtures encoded by an independent encoder (Pillow), so the tests do not merely
	// round-trip our encoder and decoder through the same implementation.
	private static readonly byte[] _rgba = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAABAAAAAICAYAAADwdn+XAAAAIUlEQVR42mNkYAhg4GdgQMb/0fh4MQuDJANFYNSAwWAAABDMAv2PDdyuAAAAAElFTkSuQmCC");

	private static readonly byte[] _jpeg = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAAIABADASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAb/xAAZEAABBQAAAAAAAAAAAAAAAAAABAYjMaH/xAAVAQEBAAAAAAAAAAAAAAAAAAACBf/EABkRAAIDAQAAAAAAAAAAAAAAAAAEAwUhUf/aAAwDAQACEQMRAD8Ai0LSqPCkQtKo8AJrTs3Q0Nixmn//2Q==");

	private static readonly byte[] _gray = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAAAsAAAAHCAAAAAD7BeiBAAAAEElEQVR42mOsZIADJgbqsAEp2wCHVwHWfwAAAABJRU5ErkJggg==");

	private static readonly byte[] _palette = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAAAgAAAAFAQMAAABCXz8WAAAABlBMVEUAAAD/AAAb/40iAAAAAnRSTlMA/1uRIrUAAAASSURBVHjaYwhlCmVmYAplZgAABokBCjJzwdAAAAAASUVORK5CYII=");

	// An opaque black canvas, built without the codecs under test.
	private static readonly byte[] _large = CreatePng(320, 160, Rgba(320, 160, (_, _) => (0, 0, 0, 255)));

	[TestMethod]
	public void GetImageSize_PngAndJpegAcrossOperatingSystems()
	{
		_rgba.GetImageSize().AssertEqual(new Size(16, 8));
		_jpeg.GetImageSize().AssertEqual(new Size(16, 8));
		ReadOnlySpan<byte> span = _gray;
		span.GetImageSize().AssertEqual(new Size(11, 7));
	}

	[TestMethod]
	public void GetImageSize_RejectsBadHeadersAndHugeDimensions()
	{
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.GetImageSize());
		ThrowsExactly<NotSupportedException>(() => new byte[] { 1, 2, 3 }.GetImageSize());
		var damaged = (byte[])_rgba.Clone();
		damaged[16] = damaged[17] = damaged[18] = damaged[19] = 0;
		ThrowsExactly<InvalidDataException>(() => damaged.GetImageSize());
	}

	[TestMethod]
	public void ConvertToPng_TrueAlphaAndSourceUnchanged()
	{
		var before = (byte[])_rgba.Clone();
		var result = _rgba.ConvertToPng();
		result.IsPng().AssertTrue();
		result.GetPngSize().AssertEqual(new Size(16, 8));
		_rgba.SequenceEqual(before).AssertTrue();

		// The fixture is RGBA8 as the result is: handed back as it came it would read right all the same,
		// so the decoder is asked itself, and the result has to hold what it gave.
		var pixels = PngCodec.Decode(_rgba).Pixels;
		Pixel(pixels, 16, 0, 0).a.AssertEqual((byte)0);
		Pixel(pixels, 16, 3, 4).a.AssertEqual((byte)0);
		Pixel(pixels, 16, 4, 4).a.AssertEqual((byte)255);
		AssertPixels(pixels, ReadPng(result, 16, 8), 16);
	}

	[TestMethod]
	public void ConvertToPng_DecodesGrayscaleAndPaletteWithTransparency()
	{
		var grayscale = _gray.ConvertToPng();
		var grayPixels = ReadPng(grayscale).rgba;
		grayPixels[0].AssertEqual((byte)121);
		grayPixels[1].AssertEqual((byte)121);
		grayPixels[2].AssertEqual((byte)121);
		grayPixels[3].AssertEqual((byte)255);

		var indexed = _palette.ConvertToPng();
		indexed.GetPngSize().AssertEqual(new Size(8, 5));
		var colors = ReadPng(indexed).rgba;
		Pixel(colors, 8, 0, 0).a.AssertEqual((byte)0);
		Pixel(colors, 8, 1, 0).a.AssertEqual((byte)255);
		colors[(0 * 8 + 1) * 4].AssertEqual((byte)255);
	}

	[TestMethod]
	public void ConvertToPng_DecodesBaselineJpegAndPixelColors()
	{
		var result = _jpeg.ConvertToPng();
		result.IsPng().AssertTrue();
		result.GetPngSize().AssertEqual(new Size(16, 8));
		var pixels = ReadPng(result).rgba;
		var index = (4 * 16 + 8) * 4;
		(Math.Abs(pixels[index] - 120) < 45).AssertTrue();
		(Math.Abs(pixels[index + 1] - 100) < 45).AssertTrue();
		(Math.Abs(pixels[index + 2] - 80) < 45).AssertTrue();
		pixels[index + 3].AssertEqual((byte)255);
	}

	[TestMethod]
	public void ResizeImage_RespectsBothBoundsWithoutUpscaling()
	{
		_rgba.ResizeImage(8, 8).GetPngSize().AssertEqual(new Size(8, 4));
		_rgba.ResizeImage(4, 1).GetPngSize().AssertEqual(new Size(2, 1));
		_rgba.ResizeImage(100, 100).GetPngSize().AssertEqual(new Size(16, 8));
		_jpeg.ResizeImage(4, 4).GetImageSize().AssertEqual(new Size(4, 2));
	}

	[TestMethod]
	public void ResizeImage_PreservesAlphaAndDoesNotModifyInput()
	{
		var before = (byte[])_rgba.Clone();
		var output = _rgba.ResizeImage(8, 8);
		var pixels = ReadPng(output).rgba;
		Pixel(pixels, 8, 0, 0).a.AssertEqual((byte)0);
		Pixel(pixels, 8, 7, 1).a.AssertEqual((byte)255);
		_rgba.SequenceEqual(before).AssertTrue();
	}

	[TestMethod]
	public void ResizeImage_RejectsInvalidDimensions()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.ResizeImage(0, 8));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.ResizeImage(8, -1));
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.ResizeImage(2, 2));
	}

	[TestMethod]
	public void CorruptPng_CrcIsNotIgnored()
	{
		var broken = (byte[])_rgba.Clone();
		broken[broken.Length - 18] ^= 0x10;
		ThrowsExactly<InvalidDataException>(() => broken.ConvertToPng());
	}

	[TestMethod]
	public void NoJpegOrTiffSilentlyGuessedFromArbitraryBytes()
	{
		ThrowsExactly<NotSupportedException>(() => new byte[] { 3, 2, 1 }.ConvertToPng());
		ThrowsExactly<InvalidDataException>(() => new byte[] { (byte)'I', (byte)'I', 42, 0 }.ResizeImage(5, 5));
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.ConvertToPng());
	}

	[TestMethod]
	public void Watermark_IsRealRenderedTextAndKeepsOriginalPixels()
	{
		var source = _large.ConvertToPng();
		var copy = (byte[])source.Clone();
		var before = ReadPng(source, 320, 160);
		var result = source.AddTextWatermark("StockSharp", BuiltInFont.Data, fontSize: 24, margin: 12, opacity: 255);
		var after = ReadPng(result, 320, 160);

		source.AssertEqual(copy);

		int left = 320, top = 160, right = -1, bottom = -1;
		var white = 0;

		for (var y = 0; y < 160; y++)
			for (var x = 0; x < 320; x++)
			{
				var pixel = Pixel(after, 320, x, y);

				if (pixel == Pixel(before, 320, x, y))
					continue;

				left = Math.Min(left, x);
				right = Math.Max(right, x);
				top = Math.Min(top, y);
				bottom = Math.Max(bottom, y);

				if (pixel == (255, 255, 255, 255))
					white++;
			}

		// 24 pt are 32 pixels to the em of 2048 units. The ten letters advance 11663 units, 182.2 pixels,
		// and the font sinks 483 units below its baseline: the line starts at x = 125.8 and has its baseline
		// at y = 140.5. Its ink runs from the 'S', 135 units in, to the 'p', 11551 units in, and from the top
		// of the 'k', 1556 units up, to the foot of the 'p', 426 units down: from (127.9, 116.1) to (306.3, 147.1).
		// Nothing outside that is touched.
		(left, top, right, bottom).AssertEqual((128, 116, 305, 146));
		(white > 500).AssertTrue($"{white} pixels are white");
	}

	[TestMethod]
	public void Watermark_UnicodeText()
	{
		var input = _large.ConvertToPng();
		var output = input.AddTextWatermark("S# Тест", BuiltInFont.Data, fontSize: 24, margin: 12);
		output.IsPng().AssertTrue();
		output.GetPngSize().AssertEqual(new Size(320, 160));

		// The letters are there: the picture is not the one that came, nor the one with the boxes
		// a font draws for letters it lacks.
		var drawn = ReadPng(output, 320, 160);

		drawn.SequenceEqual(ReadPng(input, 320, 160)).AssertFalse("nothing was drawn");
		drawn.SequenceEqual(ReadPng(input.AddTextWatermark("S# \u4E2D\u4E2D\u4E2D\u4E2D", BuiltInFont.Data, fontSize: 24, margin: 12), 320, 160)).AssertFalse("the Cyrillic letters are drawn as missing");
	}

	[TestMethod]
	public void Watermark_ZeroOpacityKeepsPixels()
	{
		var before = ReadPng(_rgba.ConvertToPng()).rgba;
		var result = _rgba.AddTextWatermark("No Change", opacity: 0, margin: 0);
		// Nothing is drawn, so the fixture comes back as Pillow wrote it.
		ReadPng(result).rgba.SequenceEqual(before).AssertTrue();
	}

	[TestMethod]
	public void Watermark_ValidatesArguments()
	{
		ThrowsExactly<ArgumentException>(() => _rgba.AddTextWatermark(" "));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", fontSize: 0));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", fontSize: -1));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", fontSize: float.NaN));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", margin: -1));
		ThrowsExactly<ArgumentException>(() => _rgba.AddTextWatermark("test", fontFamily: " "));
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.AddTextWatermark("test"));
	}

	[TestMethod]
	public void Watermark_NoSilentFontSubstitution()
	{
		ThrowsExactly<FileNotFoundException>(() =>
			_rgba.AddTextWatermark("Text", fontSize: 5, margin: 1,
				fontFilePath: TempPath("missing.ttf")));
	}
}
