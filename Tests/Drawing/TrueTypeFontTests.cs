namespace Ecng.Tests.Drawing;

using System.Security.Cryptography;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// The glyph rasterizer and the font lookup, with fonts assembled by <see cref="TestFont"/>.
/// </summary>
[TestClass]
public class TrueTypeFontTests : BaseTestClass
{
	private const int _width = 64;
	private const int _height = 48;

	// One glyph with an advance of the whole em, drawn at 18 pt with a margin of 8 on a 64x48 picture:
	// 0.024 pixels to a font unit, the origin at x = 32, the baseline at y = 35.2.
	private const double _scale = 0.024;
	private const double _originX = 32;
	private const double _baseline = 35.2;

	[TestMethod]
	public void OverlappingContours_AreFilledWhereTheyCross()
	{
		// A horizontal and a vertical bar, both clockwise, crossing in the middle.
		var font = new TestFont();
		font.Map('I', font.AddGlyph(1000, TestFont.Rectangle(100, 300, 900, 500), TestFont.Rectangle(400, 0, 600, 800)));

		var image = Draw(font, "I");

		IsFilled(image, 500, 400).AssertTrue("the crossing");
		IsFilled(image, 200, 400).AssertTrue("the horizontal bar alone");
		IsFilled(image, 500, 650).AssertTrue("the vertical bar alone");
		IsFilled(image, 200, 650).AssertFalse("outside both bars");
	}

	[TestMethod]
	public void ContourOfTheOppositeDirection_CutsAHole()
	{
		var font = new TestFont();
		TestFont.Point[] inner = [new(300, 200), new(700, 200), new(700, 600), new(300, 600)];
		font.Map('I', font.AddGlyph(1000, TestFont.Rectangle(100, 0, 900, 800), inner));

		var image = Draw(font, "I");

		IsFilled(image, 200, 400).AssertTrue("the ring");
		IsFilled(image, 500, 400).AssertFalse("the hole");
	}

	[TestMethod]
	public void QuadraticCurves_RoundTheCornersOfTheirControlPolygon()
	{
		// Four off-curve points: four arcs that touch the square in the middle of each side.
		var font = new TestFont();
		TestFont.Point[] round = [new(0, 0, false), new(0, 800, false), new(800, 800, false), new(800, 0, false)];
		font.Map('I', font.AddGlyph(1000, round));

		var image = Draw(font, "I");

		IsFilled(image, 400, 400).AssertTrue("the center");
		IsFilled(image, 400, 30).AssertTrue("inside the middle of the bottom side");
		IsFilled(image, 30, 400).AssertTrue("inside the middle of the left side");
		IsFilled(image, 20, 30).AssertFalse("the bottom left corner");
		IsFilled(image, 780, 770).AssertFalse("the top right corner");
	}

	[TestMethod]
	public void Composite_PlacesAndScalesItsComponents()
	{
		var font = new TestFont();
		var square = font.AddGlyph(1000, TestFont.Rectangle(0, 0, 400, 400));

		// Moved; halved and moved; mirrored about the vertical axis and moved.
		font.Map('A', font.AddComposite(1000, new TestFont.Component(square, 500, 400)));
		font.Map('B', font.AddComposite(1000, new TestFont.Component(square, 500, 400, 0.5, 0, 0, 0.5)));
		font.Map('C', font.AddComposite(1000, new TestFont.Component(square, 900, 0, -1, 0, 0, 1)));

		var moved = Draw(font, "A");
		IsFilled(moved, 700, 600).AssertTrue("the moved square");
		IsFilled(moved, 200, 200).AssertFalse("where the square was");

		var halved = Draw(font, "B");
		IsFilled(halved, 600, 500).AssertTrue("the halved square");
		IsFilled(halved, 800, 700).AssertFalse("beyond the halved square");

		var mirrored = Draw(font, "C");
		IsFilled(mirrored, 700, 200).AssertTrue("the mirrored square");
		IsFilled(mirrored, 300, 200).AssertFalse("where the square was");
	}

	[TestMethod]
	public void Composite_TurnsAComponentTheWayItsTwoByTwoTransformSays()
	{
		// A bar lying along the baseline; x' = xscale * x + scale10 * y, y' = scale01 * x + yscale * y
		// with (0, 1, -1, 0) stands it up to the left of where it started.
		var font = new TestFont();
		var bar = font.AddGlyph(1000, TestFont.Rectangle(0, 0, 800, 200));
		font.Map('I', font.AddComposite(1000, new TestFont.Component(bar, 300, 0, 0, 1, -1, 0)));

		var image = Draw(font, "I");

		IsFilled(image, 200, 400).AssertTrue("the bar standing up");
		IsFilled(image, 600, 100).AssertFalse("the bar lying down");
	}

	[TestMethod]
	public void Composite_ThatMultipliesItsOutlines_IsRefused()
	{
		// Twelve levels of ten components each come to a million million rectangles.
		var font = new TestFont();
		var glyph = font.AddGlyph(1000, TestFont.Rectangle(0, 0, 400, 400));

		for (var level = 0; level < 12; level++)
			glyph = font.AddComposite(1000, [.. Enumerable.Repeat(new TestFont.Component(glyph, 0, 0), 10)]);

		font.Map('I', glyph);

		ThrowsExactly<InvalidDataException>(() => Draw(font, "I"));
	}

	[TestMethod]
	public void Composite_ThatRefersToItself_IsRefused()
	{
		var font = new TestFont();

		// The glyph added next gets index 1.
		font.Map('I', font.AddComposite(1000, new TestFont.Component(1, 0, 0)));

		ThrowsExactly<InvalidDataException>(() => Draw(font, "I"));
	}

	[TestMethod]
	public void CharacterBeyondTheBasicPlane_IsFoundThroughAFormatTwelveMap()
	{
		var font = new TestFont { IsFormat12 = true };
		font.Map(0x1F600, font.AddGlyph(1000, TestFont.Rectangle(100, 0, 900, 800)));

		var image = Draw(font, "\U0001F600");

		IsFilled(image, 500, 400).AssertTrue();
	}

	[TestMethod]
	public void SeveralGlyphs_AreLaidOutByTheirAdvances()
	{
		var font = new TestFont();
		font.Map('I', font.AddGlyph(500, TestFont.Rectangle(100, 0, 400, 800)));

		// Three glyphs of half an em each end at the right margin: the first starts at x = 20.
		var image = Draw(font, "III");

		foreach (var origin in new[] { 20.0, 32.0, 44.0 })
		{
			Pixel(image, _width, (int)(origin + 250 * _scale), 25).r.AssertEqual((byte)255, $"the glyph at {origin}");
			Pixel(image, _width, (int)(origin + 450 * _scale), 25).r.AssertEqual((byte)0, $"the gap after the glyph at {origin}");
		}
	}

	[TestMethod]
	public void Glyphs_ThatMeetOrOverlap_MakeOneShapeOfOneOpacity()
	{
		// Three glyphs that fill their advance of 525 units, 10.5 pixels at 15 pt: the second and the third
		// meet in the middle of a pixel, and each covers its half of it. The origin is at x = 24.5, the baseline at y = 36.
		AssertLine(TestFont.Rectangle(0, 0, 525, 800), (24.5, 20, 35, 36), (35, 20, 45.5, 36), (45.5, 20, 56, 36));

		// Glyphs that reach 100 units, two pixels, into both their neighbors: the line is as opaque where
		// two of them lie over one another as where one lies. The origin is at x = 22.5.
		AssertLine(TestFont.Rectangle(-100, 0, 625, 800), (20.5, 20, 35, 36), (31, 20, 45.5, 36), (41.5, 20, 56, 36));

		static void AssertLine(TestFont.Point[] outline, params (double, double, double, double)[] boxes)
		{
			var font = new TestFont();
			font.Map('I', font.AddGlyph(525, outline));

			var marked = CreatePng(_width, _height, BlackRgba(_width, _height)).AddTextWatermark("III", font.Build(), fontSize: 15, opacity: 160, margin: 8);

			AssertPixels(WhiteBoxes(BlackRgba(_width, _height), _width, 160, boxes), ReadPng(marked, _width, _height), _width);
		}
	}

	[TestMethod]
	public void OutlineEdges_ThatLieOnThePointsPixelsAreSampledAt_AreHalfOpen()
	{
		// An em of 1024 units at 24 pt is a 32nd of a pixel to a unit, exactly: the origin is at x = 56 and
		// the baseline at y = 48. The rectangle runs from (58.25, 30.25) to (70.75, 40.75), every edge
		// through points pixels are sampled at: its left and upper edges are inside, its right and lower
		// ones are not.
		var font = new TestFont { UnitsPerEm = 1024, Ascent = 768, Descent = -256 };
		font.Map('I', font.AddGlyph(1024, TestFont.Rectangle(72, 232, 472, 568)));

		var marked = CreatePng(96, 64, BlackRgba(96, 64)).AddTextWatermark("I", font.Build(), fontSize: 24, opacity: 255, margin: 8);

		AssertPixels(WhiteBoxes(BlackRgba(96, 64), 96, 255, (58.25, 30.25, 70.75, 40.75)), ReadPng(marked, 96, 64), 96);
	}

	[TestMethod]
	public void Watermark_IsDrawnOnEveryFrameOfAnAnimatedPng()
	{
		var black = BlackRgba(_width, _height);
		var gray = Rgba(_width, _height, (_, _) => (90, 90, 90, 255));

		var apng = WriteChunks(
		[
			("IHDR", Header(_width, _height, 8, 6)),
			("acTL", [0, 0, 0, 2, 0, 0, 0, 0]),
			("fcTL", FrameControl(0, _width, _height, 0, 0, 1, 25, 0, 0)),
			("IDAT", FrameData(_width, _height, black)),
			("fcTL", FrameControl(1, _width, _height, 0, 0, 3, 25, 0, 0)),
			("fdAT", Sequenced(2, FrameData(_width, _height, gray))),
			("IEND", []),
		]);

		var result = ReadApng(apng.AddTextWatermark("I", fontSize: 18, opacity: 255, margin: 8, fontFilePath: TempFile("font.ttf", new TestFont().MapLetterI().Build())));

		result.Frames.Count.AssertEqual(2);
		result.Frames.Select(f => (f.DelayNumerator, f.DelayDenominator)).ToArray().AssertEqual(new (ushort, ushort)[] { (1, 25), (3, 25) });

		// Inside the glyph both frames are white; beside it each keeps its own color.
		Pixel(result.Frames[0].Rgba, _width, 44, 25).AssertEqual(((byte)255, (byte)255, (byte)255, (byte)255));
		Pixel(result.Frames[1].Rgba, _width, 44, 25).AssertEqual(((byte)255, (byte)255, (byte)255, (byte)255));
		Pixel(result.Frames[0].Rgba, _width, 5, 5).AssertEqual(((byte)0, (byte)0, (byte)0, (byte)255));
		Pixel(result.Frames[1].Rgba, _width, 5, 5).AssertEqual(((byte)90, (byte)90, (byte)90, (byte)255));
	}

	[TestMethod]
	public void FontFamily_IsFoundByTheNameTheFontCarries()
	{
		var directory = TempDirectory(
			("one.ttf", Named("Alpha Sans", "Bold")),
			("two.ttf", Named("Alpha Sans", "Regular")),
			("Beta.ttf", Named("Gamma Mono", "Regular")),
			("broken.ttf", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]));

		var regular = Path.Combine(directory, "two.ttf");
		var other = Path.Combine(directory, "Beta.ttf");
		var fonts = FontCatalog.Scan([directory]);

		// The family as the font names it, whatever the case, and its regular face before the bold one.
		fonts.Resolve("Alpha Sans").AssertEqual(regular);
		fonts.Resolve("alpha sans").AssertEqual(regular);
		fonts.Resolve("Gamma Mono").AssertEqual(other);

		// The name of the file still does.
		fonts.Resolve("beta").AssertEqual(other);

		// Neither of these families stands in for another one.
		fonts.Resolve("No Such Family").AssertNull();
	}

	[TestMethod]
	public void FontFamily_ThatIsNotInstalled_IsReplacedByAUsualSansSerifOne()
	{
		(string, byte[])[] files = [("a.ttf", Named("Gamma Mono", "Regular")), ("b.ttf", Named("Arial", "Regular"))];

		var directory = TempDirectory(files);
		var fonts = FontCatalog.Scan([directory]);

		fonts.Resolve("No Such Family").AssertEqual(Path.Combine(directory, "b.ttf"));

		// The built-in font is not replaced: asked for by the family it carries or by the name
		// its file has where it is installed, it is that font.
		fonts.Resolve(BuiltInFont.Family).AssertNull();
		fonts.Resolve(BuiltInFont.FileName).AssertNull();
		fonts.Resolve(BuiltInFont.FileName.ToLowerInvariant()).AssertNull();

		// Installed, it is taken from its file like any other, and it stands in for a family before Arial does.
		directory = TempDirectory([.. files, ("c.ttf", Named(BuiltInFont.Family, "Book"))]);
		fonts = FontCatalog.Scan([directory]);

		var dejaVu = Path.Combine(directory, "c.ttf");

		fonts.Resolve(BuiltInFont.Family).AssertEqual(dejaVu);
		fonts.Resolve(BuiltInFont.FileName).AssertEqual(dejaVu);
		fonts.Resolve("No Such Family").AssertEqual(dejaVu);
	}

	[TestMethod]
	public void FontFamily_ThatIsNotInstalled_IsReplacedInTheOrderOfTheUsualFamilies()
	{
		string[] families = ["Verdana", BuiltInFont.Family, "Arial", "Liberation Sans", "Noto Sans", "FreeSans", "Helvetica"];

		// With the families before it gone, each is the one taken. The files are named so that
		// the first of them by path is the last of the families.
		for (var first = 0; first < families.Length; first++)
		{
			var directory = TempDirectory([.. families.Skip(first).Select((family, i) => ($"{(char)('z' - i)}.ttf", Named(family, "Regular")))]);

			FontCatalog.Scan([directory]).Resolve("No Such Family").AssertEqual(Path.Combine(directory, "z.ttf"), families[first]);
		}

		// A font that carries no name is found by the name its file usually has.
		foreach (var name in new[] { BuiltInFont.FileName, "LiberationSans-Regular", "NotoSans-Regular" })
		{
			var directory = TempDirectory(("a.ttf", Named("Gamma Mono", "Regular")), ($"{name}.ttf", new TestFont().MapLetterI().Build()));

			FontCatalog.Scan([directory]).Resolve("No Such Family").AssertEqual(Path.Combine(directory, $"{name}.ttf"), name);
		}
	}

	[TestMethod]
	public void FontFamily_WithNoFontsAtAll_IsNotFound()
	{
		var directory = TempDirectory(("readme.txt", [1, 2, 3]));

		FontCatalog.Scan([directory]).Resolve("Verdana").AssertNull();
	}

	[TestMethod]
	public void WithNoFontInstalled_TheBuiltInOneIsDrawnWith()
	{
		const int width = 200;

		var directory = TempDirectory(("readme.txt", [1, 2, 3]));
		var canvas = new RasterImage(width, _height);

		TrueTypeFont.OfFamily(FontCatalog.Scan([directory]), "Verdana").TryRender(width, _height, "Watermark", 18, 8, 255, out var mask).AssertTrue("no room");
		mask.Draw(canvas);

		var builtIn = CreatePng(width, _height, new byte[width * _height * 4]).AddTextWatermark("Watermark", BuiltInFont.Data, fontSize: 18, opacity: 255, margin: 8);

		canvas.Pixels.Any(value => value != 0).AssertTrue("nothing was drawn");
		AssertPixels(ReadPng(builtIn, width, _height), canvas.Pixels, width);
	}

	[TestMethod]
	public void BuiltInFont_IsTheRecordedSubsetOfDejaVuSans()
	{
		// The command that makes the file and this hash of it are in Drawing/Fonts/README.md.
		Convert.ToHexString(SHA256.HashData(BuiltInFont.Data)).AssertEqual("E1B35496DDB70A17FF5649F3E78203345197CEC46E4D3AC012FF669331BD903B");

		// It names itself the way it is asked for.
		var file = TempFile("built-in.ttf", BuiltInFont.Data);

		FontCatalog.Scan([Path.GetDirectoryName(file)]).Resolve(BuiltInFont.Family).AssertEqual(file);

		// The notices its license wants in every copy are in its name table.
		string[] notices =
		[
			"Copyright (c) 2003 by Bitstream, Inc. All Rights Reserved.",
			"Copyright (c) 2006 by Tavmjong Bah. All Rights Reserved.",
			"Bitstream Vera Fonts Copyright",
			"Arev Fonts Copyright",
		];

		foreach (var notice in notices)
			(BuiltInFont.Data.AsSpan().IndexOf(Encoding.BigEndianUnicode.GetBytes(notice)) >= 0).AssertTrue(notice);
	}

	[TestMethod]
	public void BuiltInFont_HasTheLettersAndSignsAWatermarkIsWrittenWith()
	{
		// A CJK ideograph is not in the font: it is drawn as the box of the missing glyph.
		var missing = DrawBuiltIn("\u4E2D");
		var blank = BlackRgba(_width, _height);

		const string sample = "AZaz09 ÄßŁșțệ ΑΩαω АЯаяЁёЇїҐґЎўӘәҢң €$£¥₽₴₸₺₹ ©®™№ «»„“”—–…•·×÷±−≈≠≤≥∞ →←↑↓ ★✓ .,:;!?@#%&/()[]";

		foreach (var rune in sample.EnumerateRunes())
		{
			if (Rune.IsWhiteSpace(rune))
				continue;

			var drawn = DrawBuiltIn(rune.ToString());

			drawn.SequenceEqual(missing).AssertFalse($"U+{rune.Value:X4} has no glyph");
			drawn.SequenceEqual(blank).AssertFalse($"U+{rune.Value:X4} leaves no ink");
		}
	}

	private static byte[] Named(string family, string subfamily)
		=> new TestFont { Family = family, Subfamily = subfamily }.MapLetterI().Build();

	// Draws white text at full opacity on an opaque black 64x48 picture.
	private static byte[] Draw(TestFont font, string text)
	{
		var png = CreatePng(_width, _height, BlackRgba(_width, _height));

		return ReadPng(png.AddTextWatermark(text, fontSize: 18, opacity: 255, margin: 8, fontFilePath: TempFile("font.ttf", font.Build())), _width, _height);
	}

	private static byte[] DrawBuiltIn(string text)
	{
		var png = CreatePng(_width, _height, BlackRgba(_width, _height));

		return ReadPng(png.AddTextWatermark(text, BuiltInFont.Data, fontSize: 18, opacity: 255, margin: 8), _width, _height);
	}

	// Whether the pixel that holds a point given in font units is covered entirely.
	private static bool IsFilled(byte[] image, int fontX, int fontY)
		=> Pixel(image, _width, (int)(_originX + fontX * _scale), (int)(_baseline - fontY * _scale)).r == 255;
}
