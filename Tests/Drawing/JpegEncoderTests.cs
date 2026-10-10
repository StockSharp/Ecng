namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// The JPEG encoder: its quality scale, the tables it writes, and its files next to what libjpeg-turbo
/// makes of the same pixels.
/// </summary>
[TestClass]
public class JpegEncoderTests : BaseTestClass
{
	private const int _width = 160;
	private const int _height = 120;

	private const byte _quantizationTables = 0xDB;

	// What libjpeg-turbo 3.1.3 makes of the picture below (through Pillow 12.1.1 with subsampling=0,
	// optimize=True): the size of the file, and how close to the picture it decodes, in dB.
	private static readonly (int quality, int bytes, double psnr)[] _libjpeg =
	[
		(30, 4334, 24.55),
		(50, 5827, 26.22),
		(75, 8485, 29.28),
		(90, 13474, 34.31),
		(100, 37561, 50.38),
	];

	// Tables K.1 and K.2 of the standard, row by row: what a quality of 50 leaves as it is.
	private static readonly byte[] _luminance =
	[
		16, 11, 10, 16, 24, 40, 51, 61,
		12, 12, 14, 19, 26, 58, 60, 55,
		14, 13, 16, 24, 40, 57, 69, 56,
		14, 17, 22, 29, 51, 87, 80, 62,
		18, 22, 37, 56, 68, 109, 103, 77,
		24, 35, 55, 64, 81, 104, 113, 92,
		49, 64, 78, 87, 103, 121, 120, 101,
		72, 92, 95, 98, 112, 100, 103, 99,
	];

	private static readonly byte[] _chrominance =
	[
		17, 18, 24, 47, 99, 99, 99, 99,
		18, 21, 26, 66, 99, 99, 99, 99,
		24, 26, 56, 99, 99, 99, 99, 99,
		47, 66, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
	];

	// Where the coefficients of a block are in the order a file lists them.
	private static readonly byte[] _zigzag =
	[
		0, 1, 8, 16, 9, 2, 3, 10,
		17, 24, 32, 25, 18, 11, 4, 5,
		12, 19, 26, 33, 40, 48, 41, 34,
		27, 20, 13, 6, 7, 14, 21, 28,
		35, 42, 49, 56, 57, 50, 43, 36,
		29, 22, 15, 23, 30, 37, 44, 51,
		58, 59, 52, 45, 38, 31, 39, 46,
		53, 60, 61, 54, 47, 55, 62, 63,
	];

	[TestMethod]
	public void File_IsWhatLibjpegMakesOfTheSamePixels()
	{
		var picture = Picture();

		// The numbers of libjpeg are for these very pixels.
		(picture.Pixels.Where((_, i) => i % 4 != 3).Sum(v => (long)v) % 1000003).AssertEqual(357458);

		foreach (var (quality, bytes, psnr) in _libjpeg)
		{
			var jpeg = JpegEncoder.Encode(picture, quality, []);
			var ours = Psnr(picture, JpegCodec.Decode(jpeg));

			// No larger than a hundredth more, no further from the picture than a tenth of a dB.
			(jpeg.Length <= bytes * 1.01).AssertTrue($"quality {quality}: {jpeg.Length} bytes, libjpeg {bytes}");
			(ours >= psnr - 0.1).AssertTrue($"quality {quality}: {ours:F2} dB, libjpeg {psnr}");
		}
	}

	[TestMethod]
	public void QuantizationTables_AreThoseOfTheStandardOnTheScaleOfLibjpeg()
	{
		var picture = Picture();

		(byte[] luminance, byte[] chrominance) Tables(int quality)
			=> ReadTables(JpegEncoder.Encode(picture, quality, []));

		// 50 leaves the tables as they are, 100 turns them into ones, 1 into the coarsest a byte holds.
		Tables(50).luminance.AssertEqual(_luminance);
		Tables(50).chrominance.AssertEqual(_chrominance);
		Tables(100).luminance.AssertEqual(Enumerable.Repeat((byte)1, 64).ToArray());
		Tables(1).chrominance.AssertEqual(Enumerable.Repeat((byte)255, 64).ToArray());

		// The first rows of the luminance tables libjpeg writes at 25, 75 and 90.
		Tables(25).luminance[..8].AssertEqual(new byte[] { 32, 22, 20, 32, 48, 80, 102, 122 });
		Tables(75).luminance[..8].AssertEqual(new byte[] { 8, 6, 5, 8, 12, 20, 26, 31 });
		Tables(90).luminance[..8].AssertEqual(new byte[] { 3, 2, 2, 3, 5, 8, 10, 12 });
	}

	[TestMethod]
	public void Frame_IsBaselineWithEveryComponentAtFullResolution()
	{
		var jpeg = JpegEncoder.Encode(Picture(), 90, []);

		(jpeg[0] == 0xFF && jpeg[1] == 0xD8).AssertTrue("start of image");
		(jpeg[^2] == 0xFF && jpeg[^1] == 0xD9).AssertTrue("end of image");

		// A frame header: the marker, its length, the sample precision, the height and the width, the
		// number of components, and three bytes for each: its number, its sampling, its quantization table.
		var frame = FindJpegFrame(jpeg);

		jpeg[frame - 3].AssertEqual((byte)0xC0, "the marker of a baseline frame");
		jpeg.AsSpan(frame, 6).ToArray().AssertEqual(new byte[] { 8, 0, _height, 0, _width, 3 });

		// One sample a pixel in all three: Y with the first table, Cb and Cr with the second.
		jpeg.AsSpan(frame + 6, 9).ToArray().AssertEqual(new byte[] { 1, 0x11, 0, 2, 0x11, 1, 3, 0x11, 1 });
	}

	[TestMethod]
	public void Quality_BuysFidelityWithBytes()
	{
		var picture = Picture();
		var bytes = 0;
		var psnr = 0.0;

		foreach (var quality in new[] { 1, 10, 30, 50, 70, 85, 95, 100 })
		{
			var jpeg = JpegEncoder.Encode(picture, quality, []);
			var fidelity = Psnr(picture, JpegCodec.Decode(jpeg));

			(jpeg.Length > bytes && fidelity > psnr).AssertTrue($"quality {quality}: {jpeg.Length} bytes and {fidelity:F2} dB after {bytes} bytes and {psnr:F2} dB");

			bytes = jpeg.Length;
			psnr = fidelity;
		}
	}

	[TestMethod]
	public void FlatPicture_TakesABitASymbol()
	{
		const int side = 256;
		var picture = Fill(side, side, (_, _) => (30, 60, 90, 255));

		var jpeg = JpegEncoder.Encode(picture, 90, []);

		// A block that is the color of the one before it says so and that it has nothing else: two
		// symbols, each the only frequent one of its table and so a bit long. 1024 blocks of three
		// components make 768 bytes, and the segments before them less than 300.
		(jpeg.Length < 768 + 300).AssertTrue($"{jpeg.Length} bytes");

		var decoded = JpegCodec.Decode(jpeg);
		AssertPixels(picture.Pixels, decoded.Pixels, side, tolerance: 1);
	}

	[TestMethod]
	public void PictureThatIsNotWholeBlocks_KeepsItsSizeAndItsEdges()
	{
		// 13x7: the last column and the last row of blocks reach beyond the picture.
		var picture = Fill(13, 7, (x, y) => ((byte)(x * 19), (byte)(y * 36), (byte)(255 - x * 19), 255));

		var decoded = JpegCodec.Decode(JpegEncoder.Encode(picture, 100, []));

		(decoded.Width, decoded.Height).AssertEqual((13, 7));
		AssertPixels(picture.Pixels, decoded.Pixels, 13, tolerance: 3);
	}

	[TestMethod]
	public void TransparentPixels_AreShownOverWhite()
	{
		// Black, from opaque on the left to nothing on the right.
		var picture = Fill(16, 16, (x, _) => (0, 0, 0, (byte)(x < 8 ? 255 : 0)));

		var decoded = JpegCodec.Decode(JpegEncoder.Encode(picture, 100, []));

		Pixel(decoded.Pixels, 16, 2, 8).AssertEqual(((byte)0, (byte)0, (byte)0, (byte)255));
		Pixel(decoded.Pixels, 16, 13, 8).AssertEqual(((byte)255, (byte)255, (byte)255, (byte)255));
	}

	[TestMethod]
	public void QualityOutOfRange_IsRefusedWhateverThePictureIs()
	{
		var jpeg = JpegEncoder.Encode(Picture(), 90, []);

		// A JPEG to reduce, one that fits as it is, a picture of another format, and bytes that are no picture.
		(byte[] picture, int bound)[] cases = [(jpeg, 10), (jpeg, 1000), (CreatePng(2, 2, new byte[16]), 1), ([1, 2, 3], 10)];

		foreach (var quality in new[] { int.MinValue, -1, 0, 101, int.MaxValue })
		{
			foreach (var (picture, bound) in cases)
				ThrowsExactly<ArgumentOutOfRangeException>(() => picture.ResizeImage(bound, bound, quality)).ParamName.AssertEqual("jpegQuality");

			ThrowsExactly<ArgumentOutOfRangeException>(() => JpegEncoder.Encode(new RasterImage(8, 8), quality, []));
		}

		// The ends of the range are qualities.
		jpeg.ResizeImage(10, 10, 1).GetImageSize().Width.AssertEqual(10);
		jpeg.ResizeImage(10, 10, 100).GetImageSize().Width.AssertEqual(10);
	}

	[TestMethod]
	public void PictureWiderOrHigherThanAFrameHeaderHolds_IsRefused()
	{
		// A side is two bytes there.
		ThrowsExactly<ArgumentOutOfRangeException>(() => JpegEncoder.Encode(new RasterImage(65536, 1), 90, []));
		ThrowsExactly<ArgumentOutOfRangeException>(() => JpegEncoder.Encode(new RasterImage(1, 65536), 90, []));

		JpegCodec.Decode(JpegEncoder.Encode(new RasterImage(65535, 1), 90, [])).Width.AssertEqual(65535);
	}

	[TestMethod]
	public void ResizedJpeg_IsWrittenWithTheQualityAskedFor()
	{
		var jpeg = JpegEncoder.Encode(Picture(), 95, []);

		var byDefault = jpeg.ResizeImage(80, 60);
		var coarse = jpeg.ResizeImage(80, 60, 50);

		byDefault.AssertEqual(jpeg.ResizeImage(80, 60, ImageHelper.DefaultJpegQuality));
		ReadTables(byDefault).luminance[..8].AssertEqual(new byte[] { 3, 2, 2, 3, 5, 8, 10, 12 }, "the default is a quality of 90");
		ReadTables(coarse).luminance.AssertEqual(_luminance);
		(coarse.Length < byDefault.Length).AssertTrue($"{coarse.Length} bytes at 50, {byDefault.Length} by default");
	}

	[TestMethod]
	public void Quality_IsOfNoConcernToAPictureOfAnotherFormat()
	{
		var rgba = Rgba(16, 12, (x, y) => ((byte)(x * 16), (byte)(y * 20), 90, 255));
		var png = CreatePng(16, 12, rgba);

		var gif = new GifBuilder(4, 4, GifBuilder.GrayPalette(4))
			.Frame(0, 0, 4, 4, Enumerable.Range(0, 16).Select(i => (byte)(i % 4)).ToArray(), delay: 5)
			.Frame(0, 0, 4, 4, Enumerable.Range(0, 16).Select(i => (byte)(i / 4)).ToArray(), delay: 5)
			.Build();

		foreach (var picture in new[] { png, gif, gif.ConvertToPng() })
			picture.ResizeImage(2, 2, 1).AssertEqual(picture.ResizeImage(2, 2, 100));
	}

	[TestMethod]
	public void HuffmanTable_GivesTheShortestScanThereIs()
	{
		int[] used = [5, 9, 12, 13, 16, 45, 1, 1, 2, 3, 400, 7];
		var frequencies = new int[256];

		for (var i = 0; i < used.Length; i++)
			frequencies[i * 3] = used[i];

		var counts = new byte[16];
		var symbols = JpegHuffmanTable.Optimize(frequencies, counts);
		var lengths = Lengths(counts, symbols);

		lengths.Count.AssertEqual(used.Length);

		// One code is left free, the one of all ones: as long as the longest of the others, it takes as
		// many of the 65536 codes of sixteen bits as one of them does.
		var longest = lengths.Values.Max();
		(65536 - Taken(counts)).AssertEqual(1 << (16 - longest));

		// Counted as a symbol that occurs once, it makes the set whole, and the whole costs what the
		// best code there is costs.
		var cost = lengths.Sum(pair => (long)frequencies[pair.Key] * pair.Value) + longest;
		cost.AssertEqual(BestCost([.. used, 1]));

		foreach (var (a, lengthA) in lengths)
			foreach (var (b, lengthB) in lengths)
				(frequencies[a] <= frequencies[b] || lengthA <= lengthB).AssertTrue($"symbol {a} occurs more often than {b} and has a longer code");
	}

	[TestMethod]
	public void HuffmanTable_KeepsCodesWithinSixteenBitsAndTheCodeOfAllOnesFree()
	{
		// Every symbol occurs three times as often as the one before it: more often than all before it
		// together, so the best code gives each a bit more than the next. Twenty symbols, twenty bits.
		var frequencies = new int[256];
		var frequency = 1;

		for (var i = 0; i < 20; i++, frequency *= 3)
			frequencies[i] = frequency;

		var counts = new byte[16];
		var symbols = JpegHuffmanTable.Optimize(frequencies, counts);

		// All of them have codes, the more frequent the earlier.
		symbols.AssertEqual(Enumerable.Range(0, 20).Select(i => (byte)(19 - i)).ToArray());
		counts.Sum(count => count).AssertEqual(20);

		(Taken(counts) < 65536).AssertTrue($"{Taken(counts)} of 65536 codes are taken: none is left for all ones");

		// What is written is what the decoder builds its table from.
		_ = new JpegHuffmanTable(counts, symbols);
	}

	[TestMethod]
	public void HuffmanTable_OfOneSymbol_IsOneCodeOfOneBit()
	{
		var frequencies = new int[256];
		frequencies[0xF0] = 1000;

		var counts = new byte[16];
		var symbols = JpegHuffmanTable.Optimize(frequencies, counts);

		symbols.AssertEqual(new byte[] { 0xF0 });
		counts.AssertEqual(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
	}

	// A picture made by whole-number arithmetic, so that the script the numbers of libjpeg come from
	// makes the same one: smooth waves, and blocks with hard edges the way text and lines have them.
	private static RasterImage Picture()
		=> Fill(_width, _height, (x, y) =>
		{
			var red = (Wave(x, 64) + Wave(y, 48)) / 2;
			var green = (Wave(x + y, 80) + Wave(3 * x + 2 * y, 56)) / 2;
			var blue = Wave(x * y / 16, 97);

			return (x / 5 + y / 7) % 11 == 0
				? ((byte)(255 - red), (byte)(255 - green), (byte)(255 - blue), (byte)255)
				: ((byte)red, (byte)green, (byte)blue, (byte)255);
		});

	// A triangle wave from 0 to 255.
	private static int Wave(int at, int period)
		=> Math.Abs(at % period - period / 2) * 255 / (period / 2);

	private static RasterImage Fill(int width, int height, Func<int, int, (byte r, byte g, byte b, byte a)> pixel)
	{
		var image = new RasterImage(width, height);
		Rgba(width, height, pixel).CopyTo(image.Pixels, 0);
		return image;
	}

	// How close the colors of two pictures are, in dB.
	private static double Psnr(RasterImage expected, RasterImage actual)
	{
		(actual.Width, actual.Height).AssertEqual((expected.Width, expected.Height));

		var squares = 0L;

		for (var i = 0; i < expected.Pixels.Length; i++)
		{
			if (i % 4 == 3)
				continue;

			var difference = expected.Pixels[i] - actual.Pixels[i];
			squares += difference * difference;
		}

		return 10 * Math.Log10(255.0 * 255 * expected.Width * expected.Height * 3 / squares);
	}

	// The two quantization tables of a file, row by row.
	private static (byte[] luminance, byte[] chrominance) ReadTables(byte[] jpeg)
	{
		var segment = ReadJpegSegments(jpeg, _quantizationTables).Single();

		// A table is a byte with its number and 64 values in zigzag order.
		segment.Length.AssertEqual(2 * 65);
		segment[0].AssertEqual((byte)0);
		segment[65].AssertEqual((byte)1);

		var luminance = new byte[64];
		var chrominance = new byte[64];

		for (var i = 0; i < 64; i++)
		{
			luminance[_zigzag[i]] = segment[1 + i];
			chrominance[_zigzag[i]] = segment[66 + i];
		}

		return (luminance, chrominance);
	}

	// How many of the 65536 codes of sixteen bits the codes of a table take: one of n bits takes 2^(16-n).
	private static int Taken(byte[] counts)
	{
		var taken = 0;

		for (var length = 1; length <= counts.Length; length++)
			taken += counts[length - 1] << (16 - length);

		return taken;
	}

	// The length of the code of every symbol of a table.
	private static Dictionary<int, int> Lengths(byte[] counts, byte[] symbols)
	{
		var lengths = new Dictionary<int, int>();
		var at = 0;

		for (var length = 1; length <= counts.Length; length++)
			for (var i = 0; i < counts[length - 1]; i++)
				lengths.Add(symbols[at++], length);

		at.AssertEqual(symbols.Length);
		return lengths;
	}

	// What the best prefix code costs: every time the two lightest nodes are joined, their weight is paid once more.
	private static long BestCost(int[] weights)
	{
		var nodes = weights.Select(weight => (long)weight).ToList();
		var cost = 0L;

		while (nodes.Count > 1)
		{
			nodes.Sort();

			var joined = nodes[0] + nodes[1];
			cost += joined;

			nodes.RemoveRange(0, 2);
			nodes.Add(joined);
		}

		return cost;
	}
}
