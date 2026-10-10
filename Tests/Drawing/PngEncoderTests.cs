namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// How the PNG encoder packs scanlines: filtered where that makes the file smaller, and never larger
/// than with the rows left as they are.
/// </summary>
[TestClass]
public class PngEncoderTests : BaseTestClass
{
	[TestMethod]
	public void SmoothPicture_IsPackedWithFilteredRows()
	{
		// Three ramps: no two pixels of a row are alike, but every one differs from its neighbor by the same step.
		const int side = 128;
		var rgba = Rgba(side, side, (x, y) => ((byte)(x * 2), (byte)(y * 2), (byte)(x + y), 255));

		var png = Encode(side, side, rgba);

		AssertPixels(rgba, ReadPng(png, side, side), side);
		ReadFilters(png).All(filter => filter != 0).AssertTrue("every row is filtered");
		(Packed(png) * 4 < Packed(CreatePng(side, side, rgba))).AssertTrue($"{Packed(png)} bytes filtered, {Packed(CreatePng(side, side, rgba))} unfiltered");
	}

	[TestMethod]
	public void PictureOfTwoColorsAtRandom_IsLeftUnfiltered()
	{
		// A filter is chosen for a row by the size of what it leaves, and here every filter leaves less
		// than the row itself: differences of ten where the pixels are ten or twenty. But two values
		// pack tighter than the three their differences take.
		const int side = 96;
		var random = new Random(77);
		var rgba = Rgba(side, side, (_, _) => random.Next(2) == 0 ? ((byte)10, (byte)10, (byte)10, (byte)255) : ((byte)20, (byte)20, (byte)20, (byte)255));

		var png = Encode(side, side, rgba);

		AssertPixels(rgba, ReadPng(png, side, side), side);
		ReadFilters(png).All(filter => filter == 0).AssertTrue("no row is filtered");
		Packed(png).AssertEqual(Packed(CreatePng(side, side, rgba)));
	}

	[TestMethod]
	public void Picture_IsNeverLargerThanWithItsRowsLeftAsTheyAre()
	{
		const int width = 64;
		const int height = 48;
		var random = new Random(31);

		var pictures = new (string name, Func<int, int, (byte r, byte g, byte b, byte a)> pixel)[]
		{
			("one color", (_, _) => (30, 60, 90, 255)),
			("ramps", (x, y) => ((byte)(x * 4), (byte)(y * 5), (byte)(x + y), 255)),
			("ramp of transparency", (x, y) => (200, 100, 50, (byte)(x * 4))),
			("stripes", (x, y) => y % 2 == 0 ? ((byte)255, (byte)255, (byte)255, (byte)255) : ((byte)0, (byte)0, (byte)0, (byte)255)),
			("checkers", (x, y) => (x / 4 + y / 4) % 2 == 0 ? ((byte)250, (byte)250, (byte)250, (byte)255) : ((byte)40, (byte)40, (byte)40, (byte)255)),
			("noise", (_, _) => ((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256))),
			("a tile repeated", (x, y) => ((byte)(x % 8 * 30), (byte)(y % 6 * 40), (byte)((x % 8 + y % 6) * 17), 255)),
		};

		foreach (var (name, pixel) in pictures)
		{
			var rgba = Rgba(width, height, pixel);
			var plain = Packed(CreatePng(width, height, rgba));

			var png = Encode(width, height, rgba);

			AssertPixels(rgba, ReadPng(png, width, height), width);
			(Packed(png) <= plain).AssertTrue($"{name}: {Packed(png)} bytes, {plain} with unfiltered rows");
		}
	}

	[TestMethod]
	public void PictureOfSeveralBlocksOfScanlines_AndOneOfRowsLongerThanABlock_KeepTheirPixels()
	{
		// Scanlines go to the packer 64 KB at a time: here 300 KB of them in rows of 2 KB, and rows of 80 KB.
		foreach (var (width, height) in new[] { (500, 150), (20_000, 3) })
		{
			var random = new Random(width);
			var noise = new byte[width * height];
			random.NextBytes(noise);

			// Half of the picture smooth, half of it noise, so that neither packing is all zeros.
			var rgba = Rgba(width, height, (x, y) => x % 2 == 0 ? ((byte)x, (byte)y, (byte)(x + y), (byte)255) : (noise[y * width + x], (byte)x, (byte)0, (byte)200));

			AssertPixels(rgba, ReadPng(Encode(width, height, rgba), width, height), width);
		}
	}

	[TestMethod]
	public void FramesOfAnAnimatedPng_AreFilteredAsAStillPictureIs()
	{
		// Two frames that swell from a corner, the second the first turned over: no row is like another,
		// but every one is close to the row above it.
		const int side = 64;
		var first = new byte[side * side];
		var second = new byte[side * side];

		for (var y = 0; y < side; y++)
			for (var x = 0; x < side; x++)
			{
				first[y * side + x] = (byte)((x * x + y * y * 2) / 48);
				second[y * side + x] = (byte)((y * y + x * x * 2) / 48);
			}

		var gif = new GifBuilder(side, side, GifBuilder.GrayPalette(256))
			.Frame(0, 0, side, side, first)
			.Frame(0, 0, side, side, second)
			.Build();

		var png = gif.ConvertToPng();
		var apng = ReadApng(png);

		AssertPixels(GrayToRgba(first), apng.Frames[0].Rgba, side);
		AssertPixels(GrayToRgba(second), apng.Frames[1].Rgba, side);

		// The first frame is in IDAT chunks, the second in fdAT ones, each of which starts with its number.
		var chunks = ReadChunks(png);
		var packedFirst = chunks.Where(chunk => chunk.tag == "IDAT").Sum(chunk => chunk.data.Length);
		var packedSecond = chunks.Where(chunk => chunk.tag == "fdAT").Sum(chunk => chunk.data.Length - 4);

		(packedFirst * 2 < Packed(CreatePng(side, side, GrayToRgba(first)))).AssertTrue($"first frame: {packedFirst} bytes");
		(packedSecond * 2 < Packed(CreatePng(side, side, GrayToRgba(second)))).AssertTrue($"second frame: {packedSecond} bytes");
	}

	[TestMethod]
	public void FramesOfAnAnimatedPng_AreEachPackedTheWayThatSuitsThem()
	{
		// A frame of two colors at random, which packs better left as it is, and a frame that swells from
		// a corner, which packs better filtered.
		const int side = 96;
		var random = new Random(77);
		var noise = new byte[side * side];

		for (var i = 0; i < noise.Length; i++)
			noise[i] = (byte)(10 + random.Next(2) * 10);

		var smooth = new byte[side * side];

		for (var y = 0; y < side; y++)
			for (var x = 0; x < side; x++)
				smooth[y * side + x] = (byte)((x * x + y * y * 2) / 108);

		var gif = new GifBuilder(side, side, GifBuilder.GrayPalette(256))
			.Frame(0, 0, side, side, noise)
			.Frame(0, 0, side, side, smooth)
			.Build();

		var chunks = ReadChunks(gif.ConvertToPng());
		var packedFirst = chunks.Where(chunk => chunk.tag == "IDAT").Sum(chunk => chunk.data.Length);
		var packedSecond = chunks.Where(chunk => chunk.tag == "fdAT").Sum(chunk => chunk.data.Length - 4);

		// The first is as large as it is with its rows left alone; the second less than half of that.
		packedFirst.AssertEqual(Packed(CreatePng(side, side, GrayToRgba(noise))));
		(packedSecond * 2 < Packed(CreatePng(side, side, GrayToRgba(smooth)))).AssertTrue($"second frame: {packedSecond} bytes, {Packed(CreatePng(side, side, GrayToRgba(smooth)))} with unfiltered rows");
	}

	private static byte[] Encode(int width, int height, byte[] rgba)
	{
		var image = new RasterImage(width, height);
		rgba.CopyTo(image.Pixels, 0);
		return PngCodec.Encode(image, []);
	}

	// The size of the packed scanlines of a PNG.
	private static int Packed(byte[] png)
		=> ReadChunks(png).Where(chunk => chunk.tag == "IDAT").Sum(chunk => chunk.data.Length);
}
