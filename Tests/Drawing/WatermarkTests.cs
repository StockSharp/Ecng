namespace Ecng.Tests.Drawing;

using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// A text watermark: the format it returns and the quality a JPEG is written with, the picture that
/// comes back when nothing is drawn, the checks of its text, the room the text and its ink are given,
/// and a font handed over as bytes.
/// </summary>
[TestClass]
public class WatermarkTests : BaseTestClass
{
	private const int _width = 64;
	private const int _height = 48;

	// A 64x48 gradient written by Pillow: baseline, quality 92, no chroma subsampling.
	private static readonly byte[] _jpeg = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgICAgMCAgIDAwMDBAYEBAQEBAgGBgUGCQgKCgkICQkKDA8MCgsOCwkJDRENDg8QEBEQCgwSExIQEw8QEBD/2wBDAQMDAwQDBAgEBAgQCwkLEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBD/wAARCAAwAEADAREAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD5pjsP9mv2uriT5CjXLUdh/s15tXEnr0K5bjsP9mvNq4k9ehXLUdh/s151XEnr0K5ajsP9mvNq4k9ehXLcdh/s15tXEnr0K5ajsPavNq4k9ehXLUdh/s15tXEnr0K5zUdh/s1+u1cSfyLQrlqOw/2a86riT16FctxWH+zXm1cSevQrlqOw/wBmvNq4k9ehXLUdh/s15tXEnr0a5bjsP9mvNq4k9ehXLUdh/s15tXEnr0K5ajsP9mvNq4k9ejXOajsP9mv12riT+RaNctx2HtXnVcSevQrlqOw/2a82riT16FctR2H+zXm1cSevRrluOw/2a82riT16FctR2H+zXm1cSevQrlqOw/2a82riT16Nctx2H+zXm1cSevQrnMx2H+zX69VxJ/ItCuW47D/ZrzauJPXoVy1HYf7NebVxJ69GuWo7D/ZrzauJPXoVy3HYf7NebVxJ69CuWo7D/ZrzauJPXo1y1HYf7NebVxJ69CuW47D/AGa82riT16Nc5mOw/wBmv16riT+RaFctx2H+zXm1cSevRrlqOw/2a82riT16Fctx2H+zXm1cSevRrlqOw/2a82riT16NctR2H+zXm1cSevQrluOw/wBmvOq4k9ehXLUdh/s15tXEnr0K5zUdh/s1+u1cSfyLRrlqOw/2a82riT16FctR2H+zXm1cSevQrluOw/2a82riT16FctR2H+zXm1cSevQrlqOw/wBmvNq4k9ejXLcdh/s151XEnr0K5ajsP9mvNq4k9ehXP//Z");

	[TestMethod]
	public void Jpeg_GetsItsWatermarkAndStaysAJpeg()
	{
		var marked = _jpeg.AddTextWatermark("I", fontSize: 18, opacity: 255, margin: 8, fontFilePath: FontFile());

		(marked[0] == 0xFF && marked[1] == 0xD8).AssertTrue("the result is not a JPEG");
		(marked[^2] == 0xFF && marked[^1] == 0xD9).AssertTrue("the result does not end with the end of image marker");
		marked[FindJpegFrame(marked) - 3].AssertEqual((byte)0xC0, "the marker of a baseline frame");
		marked.GetImageSize().AssertEqual(new Size(_width, _height));

		var after = ReadPng(marked.ConvertToPng(), _width, _height);

		// At 18 pt the glyph is a rectangle from (34.4, 16) to (53.6, 35.2): the 8x8 block at (40, 24) lies inside it.
		// White all over, that block is its DC term alone, which the default quality rounds to a step of 3:
		// three sixteenths of a level at most, and white stays white.
		for (var y = 24; y < 32; y++)
			for (var x = 40; x < 48; x++)
				Pixel(after, _width, x, y).AssertEqual(((byte)255, (byte)255, (byte)255, (byte)255), $"inside the glyph at ({x}, {y})");

		// A block the glyph does not reach is encoded from the pixels it had: it comes out the way it does
		// when the picture is written again without the text. The glyph reaches columns 32..55 of rows 16..39.
		var plain = ReadPng(JpegEncoder.Encode(JpegCodec.Decode(_jpeg), ImageHelper.DefaultJpegQuality, []).ConvertToPng(), _width, _height);

		AssertPixels(BesideTheGlyph(plain), BesideTheGlyph(after), _width);

		static byte[] BesideTheGlyph(byte[] rgba)
			=> Rgba(_width, _height, (x, y) => x is >= 32 and < 56 && y is >= 16 and < 40 ? default : Pixel(rgba, _width, x, y));
	}

	[TestMethod]
	public void Jpeg_IsWrittenWithTheQualityAskedFor()
	{
		var font = Font();

		var byDefault = _jpeg.AddTextWatermark("I", font, fontSize: 18, opacity: 255, margin: 8);
		var coarse = _jpeg.AddTextWatermark("I", font, fontSize: 18, opacity: 255, margin: 8, jpegQuality: 50);

		byDefault.AssertEqual(_jpeg.AddTextWatermark("I", font, fontSize: 18, opacity: 255, margin: 8, jpegQuality: ImageHelper.DefaultJpegQuality));
		(coarse.Length < byDefault.Length).AssertTrue($"{coarse.Length} bytes at 50, {byDefault.Length} by default");

		// The first step of the luminance table: 16 in the standard, which is a quality of 50, and 3 at 90.
		FirstStep(byDefault).AssertEqual((byte)3);
		FirstStep(coarse).AssertEqual((byte)16);

		// A font given by its file comes with the same quality.
		coarse.AssertEqual(_jpeg.AddTextWatermark("I", fontSize: 18, opacity: 255, margin: 8, fontFilePath: FontFile(), jpegQuality: 50));

		// The segment of the quantization tables: the number of the first table, then its steps.
		static byte FirstStep(byte[] jpeg)
			=> ReadJpegSegments(jpeg, 0xDB).Single()[1];
	}

	[TestMethod]
	public void QualityOutOfRange_IsAnArgumentErrorWhateverThePictureAndTheOpacity()
	{
		var font = Font();
		var file = FontFile();

		// A picture with no room for a text, one with room, a JPEG, and bytes that are no picture at all.
		byte[][] pictures = [Black(2, 2), Black(_width, _height), _jpeg, [1, 2, 3]];

		foreach (var quality in new[] { -1, 0, 101 })
			foreach (var picture in pictures)
				foreach (var opacity in new byte[] { 0, 255 })
				{
					ThrowsExactly<ArgumentOutOfRangeException>(() => picture.AddTextWatermark("I", font, opacity: opacity, jpegQuality: quality)).ParamName.AssertEqual("jpegQuality");
					ThrowsExactly<ArgumentOutOfRangeException>(() => picture.AddTextWatermark("I", opacity: opacity, fontFilePath: file, jpegQuality: quality)).ParamName.AssertEqual("jpegQuality");
				}
	}

	[TestMethod]
	public void Quality_IsOfNoConcernToAPictureOfAnotherFormat()
	{
		var font = Font();

		foreach (var (format, picture) in Pictures().Where(entry => entry.format is "PNG" or "APNG"))
		{
			var marked = picture.AddTextWatermark("I", font, fontSize: 18, opacity: 255, margin: 8, jpegQuality: 1);

			marked.SequenceEqual(picture).AssertFalse($"{format}: the text is not drawn");
			marked.AssertEqual(picture.AddTextWatermark("I", font, fontSize: 18, opacity: 255, margin: 8, jpegQuality: 100), format);
		}
	}

	[TestMethod]
	public void InvisibleWatermark_ReturnsThePictureAsItCame()
	{
		foreach (var (format, picture) in Pictures())
		{
			var result = picture.AddTextWatermark("text", opacity: 0, margin: 0);

			result.AssertEqual(picture, format);
			ReferenceEquals(result, picture).AssertFalse($"{format}: the caller's own array came back");
		}
	}

	[TestMethod]
	public void InvisibleWatermark_StillRefusesADamagedPicture()
	{
		foreach (var (format, picture) in Pictures())
		{
			// Without its last bytes no format has its end: the IEND chunk, the end of image marker,
			// the last row of pixels, the trailer.
			var damaged = picture[..^4];

			ThrowsExactly<InvalidDataException>(() => damaged.AddTextWatermark("text", opacity: 0, margin: 0), format);
		}

		foreach (var (format, picture) in PicturesWithADamagedFrame())
			ThrowsExactly<InvalidDataException>(() => picture.AddTextWatermark("text", opacity: 0, margin: 0), format);
	}

	[TestMethod]
	public void Margin_GivesWayOnAPictureTooSmallForIt()
	{
		// A margin of 12 on a picture 20 high: a quarter of the smaller side, 5, is what is left of it.
		// The text shrinks into the 10 rows between the margins: 0.0095 pixels to a font unit,
		// the origin at x = 49.5, the baseline at y = 13.1.
		var marked = Black(64, 20).AddTextWatermark("I", fontSize: 18, opacity: 255, margin: 12, fontFilePath: FontFile());

		AssertPixels(WhiteBoxes(BlackRgba(64, 20), 64, 255, (50.45, 5.5, 58.05, 13.1)), ReadPng(marked, 64, 20), 64);

		// The smaller side is the width here: the same 5, the origin at x = 5.5, the baseline at y = 57.1.
		marked = Black(20, 64).AddTextWatermark("I", fontSize: 18, opacity: 255, margin: 12, fontFilePath: FontFile());

		AssertPixels(WhiteBoxes(BlackRgba(20, 64), 20, 255, (6.45, 49.5, 14.05, 57.1)), ReadPng(marked, 20, 64), 20);

		// A quarter of 23 is 5 as well: the 13 rows that leaves hold the text at 0.01235 pixels to a font unit,
		// the origin at x = 46.65, the baseline at y = 15.53.
		marked = Black(64, 23).AddTextWatermark("I", fontSize: 18, opacity: 255, margin: 12, fontFilePath: FontFile());

		AssertPixels(WhiteBoxes(BlackRgba(64, 23), 64, 255, (47.885, 5.65, 57.765, 15.53)), ReadPng(marked, 64, 23), 64);
	}

	[TestMethod]
	public void Text_IsNotMadeSmallerThanSixPixelsOfEm()
	{
		// Two glyphs of an em each and no margin: 13 pixels of width leave 6.175 to the em, 12 leave 5.7.
		var marked = Black(13, 32).AddTextWatermark("II", fontSize: 18, opacity: 255, margin: 0, fontFilePath: FontFile());

		AssertPixels(WhiteBoxes(BlackRgba(13, 32), 13, 255, (1.2675, 25.825, 6.2075, 30.765), (7.4425, 25.825, 12.3825, 30.765)), ReadPng(marked, 13, 32), 13);

		var narrow = Black(12, 32);

		narrow.AddTextWatermark("II", fontSize: 18, opacity: 255, margin: 0, fontFilePath: FontFile()).AssertEqual(narrow);

		// The limit itself, on those 12 pixels: a glyph 1899 units wide is left 6.003 pixels to an em of 1000 units,
		// one of 1901 units 5.997. An em of 2048 units has the same limit at 3889 and 3893 units.
		IsDrawn(1000, 1899).AssertTrue("6.003 pixels to the em");
		IsDrawn(1000, 1901).AssertFalse("5.997 pixels to the em");
		IsDrawn(2048, 3889).AssertTrue("6.003 pixels to an em of 2048 units");
		IsDrawn(2048, 3893).AssertFalse("5.997 pixels to an em of 2048 units");

		bool IsDrawn(int unitsPerEm, int advance)
		{
			var font = new TestFont { UnitsPerEm = unitsPerEm };
			font.Map('I', font.AddGlyph(advance, TestFont.Rectangle(100, 0, 900, 800)));

			return !narrow.AddTextWatermark("I", font.Build(), fontSize: 18, opacity: 255, margin: 0).SequenceEqual(narrow);
		}
	}

	[TestMethod]
	public void Text_AskedForSmallerThanSixPixelsOfEm_IsDrawnAtItsSize()
	{
		// 3 pt are 4 pixels to the em: the origin at x = 52, the baseline at y = 39.2.
		var marked = Black(_width, _height).AddTextWatermark("I", fontSize: 3, opacity: 255, margin: 8, fontFilePath: FontFile());

		AssertPixels(WhiteBoxes(BlackRgba(_width, _height), _width, 255, (52.4, 36, 55.6, 39.2)), ReadPng(marked, _width, _height), _width);

		// It is not made smaller still to fit.
		var narrow = Black(12, _height);

		narrow.AddTextWatermark("IIII", fontSize: 3, opacity: 255, margin: 0, fontFilePath: FontFile()).AssertEqual(narrow);
	}

	[TestMethod]
	public void TextWithNoRoom_LeavesThePictureAsItCame()
	{
		// Sixteen letters and the default margin: none of the pictures has the room, the smallest least of all.
		var text = new string('I', 16);
		var font = FontFile();

		foreach (var (format, picture) in Pictures())
		{
			var result = picture.AddTextWatermark(text, fontFilePath: font);

			result.AssertEqual(picture, format);
			ReferenceEquals(result, picture).AssertFalse($"{format}: the caller's own array came back");

			ThrowsExactly<InvalidDataException>(() => picture[..^4].AddTextWatermark(text, fontFilePath: font), format);
		}

		foreach (var (format, picture) in PicturesWithADamagedFrame())
			ThrowsExactly<InvalidDataException>(() => picture.AddTextWatermark(text, fontFilePath: font), format);
	}

	[TestMethod]
	public void TextThatIsTooLongOrNotOneLine_IsAnArgumentErrorWhateverThePictureAndTheOpacity()
	{
		var font = Font();
		var file = FontFile();

		// A picture with no room for a text, one with room, and bytes that are no picture at all.
		byte[][] pictures = [Black(2, 2), Black(_width, _height), [1, 2, 3]];

		// A line feed, a carriage return, the vertical tab and the form feed between them, the next-line
		// control and the two separators of Unicode all end a line.
		foreach (var text in new[] { "I\nI", "I\rI", "II\r\n", "I\vI", "I\fI", "I\u0085I", "I\u2028I", "I\u2029I", new string('I', 1025) })
			foreach (var picture in pictures)
				foreach (var opacity in new byte[] { 0, 255 })
				{
					ThrowsExactly<ArgumentException>(() => picture.AddTextWatermark(text, font, opacity: opacity));
					ThrowsExactly<ArgumentException>(() => picture.AddTextWatermark(text, opacity: opacity, fontFilePath: file));
				}

		// 1024 characters are a line still, and a character beyond the basic plane is one of them, not two.
		var black = Black(_width, _height);

		black.AddTextWatermark(new string('I', 1023) + "\U0001F600", font).AssertEqual(black);
	}

	[TestMethod]
	public void TextWithNothingToPrint_IsAnArgumentError()
	{
		var picture = Black(200, _height);

		// The font has 'A' alone, and what it shows for any other character has a width and no outline.
		var font = new TestFont().SetMissingGlyph(600);
		font.Map('A', font.AddGlyph(1000, TestFont.Rectangle(100, 0, 900, 800)));

		ThrowsExactly<ArgumentException>(() => picture.AddTextWatermark("StockSharp", font.Build()));

		// A left-to-right mark and a space: characters that take room and leave no ink.
		ThrowsExactly<ArgumentException>(() => picture.AddTextWatermark("\u200E ", BuiltInFont.Data));

		// One character that has an outline is a text to print.
		picture.AddTextWatermark("StockSharp A", font.Build()).SequenceEqual(picture).AssertFalse("the letter was not drawn");
	}

	[TestMethod]
	public void TextNoPixelOfWhichShows_LeavesThePictureAsItCame()
	{
		var font = FontFile();

		// At a tenth of a point the glyph is a tenth of a pixel wide and lies between the points pixels are sampled at.
		foreach (var (format, picture) in Pictures())
			picture.AddTextWatermark("I", fontSize: 0.1f, fontFilePath: font).AssertEqual(picture, format);

		// At the smallest size there is the line has no extent at all, not even at the edge of the picture.
		foreach (var (format, picture) in Pictures())
			picture.AddTextWatermark("I", fontSize: float.Epsilon, margin: 0, fontFilePath: font).AssertEqual(picture, format);

		// A stroke 0.8 pixels wide covers two of the four points of a pixel at most:
		// at an opacity of 1 that is nothing, at an opacity of 2 the faintest white there is.
		var thin = new TestFont();
		thin.Map('I', thin.AddGlyph(1000, TestFont.Rectangle(100, 0, 300, 800)));

		foreach (var (format, picture) in Pictures())
			picture.AddTextWatermark("I", thin.Build(), fontSize: 3, opacity: 1, margin: 8).AssertEqual(picture, format);

		var marked = Black(_width, _height).AddTextWatermark("I", thin.Build(), fontSize: 3, opacity: 2, margin: 8);

		AssertPixels(WhiteBoxes(BlackRgba(_width, _height), _width, 2, (52.4, 36, 53.2, 39.2)), ReadPng(marked, _width, _height), _width);
	}

	[TestMethod]
	public void Ink_ThatReachesOutOfTheAdvanceOrTheHeightOfTheFont_StaysBetweenTheMargins()
	{
		// The outline ends 400 units to the right of the advance, and the line is that much wider:
		// 0.024 pixels to a font unit, the origin at x = 22.4, the baseline at y = 35.2.
		AssertMarked(64, 48, 8, 1000, TestFont.Rectangle(100, 0, 1400, 800), (24.8, 16, 56, 35.2));

		// It starts 300 units to the left of the origin: 26 pixels of width hold the 1300 units
		// at 0.019 pixels to a unit, the origin at x = 7, the baseline at y = 44.2.
		AssertMarked(26, 48, 0, 1000, TestFont.Rectangle(-300, 0, 700, 800), (1.3, 29, 20.3, 44.2));

		// It rises 300 units above the ascent of the font and sinks 200 below its descent: the 30 rows between
		// the margins hold the 1500 units at 0.019 pixels to a unit, the origin at x = 41, the baseline at y = 26.4.
		AssertMarked(64, 38, 4, 1000, TestFont.Rectangle(100, -400, 900, 1100), (42.9, 5.5, 58.1, 34));

		// An accent set over the letter before it has no advance and lies to the left of the origin:
		// the line is the 600 units it reaches back, the origin at x = 56, the baseline at y = 35.2.
		AssertMarked(64, 48, 8, 0, TestFont.Rectangle(-600, 500, -200, 700), (41.6, 18.4, 51.2, 23.2));

		static void AssertMarked(int width, int height, int margin, int advance, TestFont.Point[] outline, (double, double, double, double) box)
		{
			var font = new TestFont();
			font.Map('I', font.AddGlyph(advance, outline));

			var marked = Black(width, height).AddTextWatermark("I", font.Build(), fontSize: 18, opacity: 255, margin: margin);

			AssertPixels(WhiteBoxes(BlackRgba(width, height), width, 255, box), ReadPng(marked, width, height), width);
		}
	}

	[TestMethod]
	public void Glyph_ThatReachesBackOfItsOriginInsideTheLine_DoesNotWidenTheLine()
	{
		// The second glyph starts 300 units before its origin, which is 1000 units into the line: the line
		// is its 2000 units of advance still. 31 pixels hold them at 0.014725 pixels to a unit, the origin
		// at x = 1.55, the baseline at y = 45.055.
		var font = new TestFont().MapLetterI();
		font.Map('J', font.AddGlyph(1000, TestFont.Rectangle(-300, 0, 700, 800)));

		var marked = Black(31, 48).AddTextWatermark("IJ", font.Build(), fontSize: 18, opacity: 255, margin: 0);

		AssertPixels(WhiteBoxes(BlackRgba(31, 48), 31, 255, (3.0225, 33.275, 14.8025, 45.055), (11.8575, 33.275, 26.5825, 45.055)), ReadPng(marked, 31, 48), 31);
	}

	[TestMethod]
	public void Curve_ThatReachesOutOfTheAdvance_StaysBetweenTheMargins()
	{
		// Four points that only steer the curve: an oval 1600 units wide on an advance of 1000, which touches
		// the middle of every side of their rectangle. At 18 pt the line is those 1600 units, 38.4 pixels:
		// the origin at x = 17.6, the baseline at y = 35.2, the oval from (17.6, 16) to (56, 35.2).
		var font = new TestFont();
		TestFont.Point[] oval = [new(0, 0, false), new(0, 800, false), new(1600, 800, false), new(1600, 0, false)];
		font.Map('I', font.AddGlyph(1000, oval));

		var marked = ReadPng(Black(_width, _height).AddTextWatermark("I", font.Build(), fontSize: 18, opacity: 255, margin: 8), _width, _height);

		int left = _width, top = _height, right = -1, bottom = -1;

		for (var y = 0; y < _height; y++)
			for (var x = 0; x < _width; x++)
			{
				if (Pixel(marked, _width, x, y).r == 0)
					continue;

				left = Math.Min(left, x);
				right = Math.Max(right, x);
				top = Math.Min(top, y);
				bottom = Math.Max(bottom, y);
			}

		(left, top, right, bottom).AssertEqual((17, 16, 55, 34));
	}

	[TestMethod]
	public void FontGivenAsBytes_DrawsWhatTheSameFontDrawsFromItsFile()
	{
		var font = Font();
		var copy = (byte[])font.Clone();
		var picture = Black(_width, _height);

		var marked = picture.AddTextWatermark("I", font, fontSize: 18, opacity: 255, margin: 8);

		// 0.024 pixels to a font unit, the origin at x = 32, the baseline at y = 35.2.
		AssertPixels(WhiteBoxes(BlackRgba(_width, _height), _width, 255, (34.4, 16, 53.6, 35.2)), ReadPng(marked, _width, _height), _width);
		marked.AssertEqual(picture.AddTextWatermark("I", fontSize: 18, opacity: 255, margin: 8, fontFilePath: FontFile()));
		font.AssertEqual(copy);
	}

	[TestMethod]
	public void FontGivenAsBytes_ThatIsNoTrueTypeFont_IsRefused()
	{
		var picture = Black(_width, _height);
		byte[] none = null;

		ThrowsExactly<ArgumentNullException>(() => picture.AddTextWatermark("I", none));
		ThrowsExactly<ArgumentNullException>(() => picture.AddTextWatermark("I", none, opacity: 0));

		// Too short to be a font file, and an OpenType file whose outlines are not TrueType ones.
		ThrowsExactly<InvalidDataException>(() => picture.AddTextWatermark("I", [1, 2, 3]));
		ThrowsExactly<NotSupportedException>(() => picture.AddTextWatermark("I", [.. "OTTO"u8, 0, 1, 0, 0, 0, 0, 0, 0]));
	}

	[TestMethod]
	public void FontGivenAsBytes_IsNotLookedAtForAnInvisibleText()
	{
		var picture = Black(_width, _height);

		picture.AddTextWatermark("I", [1, 2, 3], opacity: 0).AssertEqual(picture);
	}

	[TestMethod]
	public void Defaults_AreTheSameWhicheverWayTheFontIsGiven()
	{
		var picture = Black(96, 64);

		var marked = picture.AddTextWatermark("I", Font());

		// 24 pt at an opacity of 160 with a margin of 12: 0.032 pixels to a font unit,
		// the origin at x = 52, the baseline at y = 45.6.
		AssertPixels(WhiteBoxes(BlackRgba(96, 64), 96, 160, (55.2, 20, 80.8, 45.6)), ReadPng(marked, 96, 64), 96);
		marked.AssertEqual(picture.AddTextWatermark("I", fontFilePath: FontFile()));
	}

	[TestMethod]
	public void FontGivenAsBytes_ComesWithTheSameChecksOfTheOtherArguments()
	{
		var picture = Black(_width, _height);
		var font = Font();
		byte[] none = null;

		ThrowsExactly<ArgumentNullException>(() => none.AddTextWatermark("I", font));
		ThrowsExactly<ArgumentException>(() => picture.AddTextWatermark(" ", font));
		ThrowsExactly<ArgumentException>(() => picture.AddTextWatermark("I\nI", font));
		ThrowsExactly<ArgumentOutOfRangeException>(() => picture.AddTextWatermark("I", font, fontSize: 0));
		ThrowsExactly<ArgumentOutOfRangeException>(() => picture.AddTextWatermark("I", font, fontSize: -1));
		ThrowsExactly<ArgumentOutOfRangeException>(() => picture.AddTextWatermark("I", font, fontSize: float.PositiveInfinity));
		ThrowsExactly<ArgumentOutOfRangeException>(() => picture.AddTextWatermark("I", font, margin: -1));
	}

	private static byte[] Black(int width, int height)
		=> CreatePng(width, height, BlackRgba(width, height));

	private static byte[] Gradient()
		=> Rgba(_width, _height, (x, y) => ((byte)(x * 4), (byte)(y * 5), 90, 255));

	// One picture of every format a watermark takes, none of them such as the library writes it.
	private static (string format, byte[] picture)[] Pictures()
	{
		var rgba = Gradient();

		// A chunk the library does not write: a PNG it wrote again would lack it.
		(string, byte[]) comment = ("tEXt", "Comment\0as it came"u8.ToArray());

		var png = WriteChunks(
		[
			("IHDR", Header(_width, _height, 8, 6)),
			comment,
			("IDAT", FrameData(_width, _height, rgba)),
			("IEND", []),
		]);

		var gif = new GifBuilder(2, 2, [0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255])
			.Frame(0, 0, 2, 2, [1, 2, 3, 1], delay: 5)
			.Frame(0, 0, 2, 2, [3, 1, 2, 3], delay: 7)
			.Build();

		var apng = WriteChunks(
		[
			("IHDR", Header(_width, _height, 8, 6)),
			comment,
			("acTL", [0, 0, 0, 2, 0, 0, 0, 0]),
			("fcTL", FrameControl(0, _width, _height, 0, 0, 1, 25, 0, 0)),
			("IDAT", FrameData(_width, _height, rgba)),
			("fcTL", FrameControl(1, _width, _height, 0, 0, 3, 25, 0, 0)),
			("fdAT", Sequenced(2, FrameData(_width, _height, rgba))),
			("IEND", []),
		]);

		return
		[
			("PNG", png),
			("JPEG", _jpeg),
			("BMP", Bmp()),
			("GIF", gif),
			("APNG", apng),
		];
	}

	// Animations whose file is sound and one of whose frames is not: only decoding the frames finds that.
	private static (string format, byte[] picture)[] PicturesWithADamagedFrame()
	{
		// Its frame is declared three pixels wide and has the data of two. The image descriptor follows
		// the 13-byte header, the 12-byte color table and the 8-byte control extension.
		var gif = new GifBuilder(3, 1, [0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255])
			.Frame(0, 0, 2, 1, [1, 3])
			.Build();

		gif[13 + 12 + 8].AssertEqual((byte)0x2C);
		gif[13 + 12 + 8 + 5] = 3;

		// Its second frame is a row short.
		var rgba = Gradient();

		var apng = WriteChunks(
		[
			("IHDR", Header(_width, _height, 8, 6)),
			("acTL", [0, 0, 0, 2, 0, 0, 0, 0]),
			("fcTL", FrameControl(0, _width, _height, 0, 0, 1, 25, 0, 0)),
			("IDAT", FrameData(_width, _height, rgba)),
			("fcTL", FrameControl(1, _width, _height, 0, 0, 3, 25, 0, 0)),
			("fdAT", Sequenced(2, FrameData(_width, _height - 1, rgba))),
			("IEND", []),
		]);

		return [("GIF", gif), ("APNG", apng)];
	}

	// A 2x2 picture of 24 bits a pixel: the file header, a BITMAPINFOHEADER, two rows padded to four bytes.
	private static byte[] Bmp()
	{
		var bmp = new byte[54 + 16];

		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		bmp[2] = (byte)bmp.Length;
		bmp[10] = 54;
		bmp[14] = 40;
		bmp[18] = 2;
		bmp[22] = 2;
		bmp[26] = 1;
		bmp[28] = 24;
		bmp[34] = 16;

		for (var i = 54; i < bmp.Length; i++)
			bmp[i] = (byte)(i * 3);

		return bmp;
	}

	// One glyph for 'I': a rectangle from (100, 0) to (900, 800) on an em of 1000 units.
	private static byte[] Font()
		=> new TestFont().MapLetterI().Build();

	private static string FontFile()
		=> TempFile("font.ttf", Font());
}
