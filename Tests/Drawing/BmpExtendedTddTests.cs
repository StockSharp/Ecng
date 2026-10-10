namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// Independent BMP fixture generation for all indexed depths, BI_BITFIELDS,
/// 16bpp RGB565, OS/2 BITMAPCORE and RLE4/RLE8, plus malformed data tests.
/// </summary>
[TestClass]
public class BmpExtendedTddTests : BaseTestClass
{
	private static readonly byte[][] _colors =
	[
		[255,0,0,255], [0,255,0,255], [0,0,255,255], [255,255,0,255]
	];

	[TestMethod]
	public void Bmp_AllIndexedDepths_TopDownAndBottomUp_ArePixelExact()
	{
		foreach (var depth in new[] { 1, 4, 8 })
			foreach (var topDown in new[] { false, true })
			{
				var colors = depth == 1 ? 2 : 4;
				var bmp = Indexed(depth, 5, 3, topDown);
				var pixels = ReadPng(bmp.ConvertToPng(), 5, 3);
				for (var y = 0; y < 3; y++)
					for (var x = 0; x < 5; x++)
					{
						var expected = _colors[(x + y) % colors];
						for (var c = 0; c < 4; c++)
							pixels[(y * 5 + x) * 4 + c].AssertEqual(expected[c]);
					}
			}
	}

	[TestMethod]
	public void Bmp_BitfieldsRgb565_PreservesPrimaryColorsWithoutSwappingRedBlue()
	{
		var bmp = CreateBmp(2, 2, 16, 3, [0x00, 0xF8, 0xE0, 0x07, 0x1F, 0x00, 0xFF, 0xFF],
			masks: [0xF800, 0x07E0, 0x001F], topDown: true);
		var actual = ReadPng(bmp.ConvertToPng(), 2, 2);
		AssertPixels([255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255], actual, 2);
	}

	[TestMethod]
	public void Bmp_Os2Core24bpp_UsesThreeBytePaletteOrNoPaletteAndCorrectRows()
	{
		var bmp = new byte[26 + 16];
		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		Put(bmp, 2, (uint)bmp.Length);
		Put(bmp, 10, 26);
		Put(bmp, 14, 12);
		Short(bmp, 18, 2);
		Short(bmp, 20, 2);
		Short(bmp, 22, 1);
		Short(bmp, 24, 24);
		// Bottom row: red, green; top row: blue, white.
		Array.Copy(new byte[] { 0, 0, 255, 0, 255, 0, 0, 0, 255, 0, 0, 255, 255, 255, 0, 0 }, 0, bmp, 26, 16);
		var actual = ReadPng(bmp.ConvertToPng(), 2, 2);
		AssertPixels([0, 0, 255, 255, 255, 255, 255, 255, 255, 0, 0, 255, 0, 255, 0, 255], actual, 2);
	}

	[TestMethod]
	public void Bmp_Rle8_AbsoluteAndEncodedRuns_ReconstructPixelGrid()
	{
		byte[] commands = [4, 1, 0, 0, 0, 4, 3, 2, 1, 0, 0, 0, 0, 4, 0, 1, 2, 3, 0, 0, 0, 1];
		var bmp = CreateBmp(4, 3, 8, 1, commands, numberOfColors: 4);
		ExpectedGrid(ReadPng(bmp.ConvertToPng(), 4, 3));
	}

	[TestMethod]
	public void Bmp_Rle4_NibbleRunsAndAbsolutePixels_ReconstructPixelGrid()
	{
		byte[] commands = [4, 0x11, 0, 0, 0, 4, 0x32, 0x10, 0, 0, 0, 4, 0x01, 0x23, 0, 0, 0, 1];
		var bmp = CreateBmp(4, 3, 4, 2, commands, numberOfColors: 4);
		ExpectedGrid(ReadPng(bmp.ConvertToPng(), 4, 3));
	}

	[TestMethod]
	public void Bmp_Rle8_DeltaAndEarlyEnd_AreHandledWithoutReadingOutsideImage()
	{
		byte[] commands = [0, 2, 1, 0, 2, 2, 0, 0, 4, 3, 0, 0, 0, 1];
		var bmp = CreateBmp(4, 2, 8, 1, commands, numberOfColors: 4);
		var output = ReadPng(bmp.ConvertToPng(), 4, 2);
		// Top line is index 3, bottom line is index 0 apart from indices x=1..2 = blue.
		var top = (byte[])_colors[3].Clone();
		for (var x = 0; x < 4; x++)
			for (var c = 0; c < 4; c++)
				output[x * 4 + c].AssertEqual(top[c]);
		for (var x = 0; x < 4; x++)
			for (var c = 0; c < 4; c++)
				output[(4 + x) * 4 + c].AssertEqual(_colors[x is 1 or 2 ? 2 : 0][c]);
	}

	[TestMethod]
	public void Bmp_32bitRgbReservedZeroAlphaIsTreatedAsOpaque()
	{
		byte[] bgra = [0, 0, 255, 0, 0, 255, 0, 0, 255, 0, 0, 0, 255, 255, 255, 0];
		var bmp = CreateBmp(2, 2, 32, 0, bgra, topDown: true);
		var actual = ReadPng(bmp.ConvertToPng(), 2, 2);
		for (var i = 3; i < actual.Length; i += 4)
			actual[i].AssertEqual((byte)255);
	}

	[TestMethod]
	public void Bmp_WatermarkRealTrueTypeIsAppliedAndEveryOtherPixelIsKept()
	{
		const int width = 64, height = 48;

		// A color of its own in every row and column: pixels written in another order would show.
		var rgba = Rgba(width, height, (x, y) => ((byte)(x * 3), (byte)(y * 5), 60, 255));
		var bgra = new byte[rgba.Length];
		for (var p = 0; p < width * height; p++)
		{
			bgra[p * 4 + 0] = rgba[p * 4 + 2];
			bgra[p * 4 + 1] = rgba[p * 4 + 1];
			bgra[p * 4 + 2] = rgba[p * 4 + 0];
			bgra[p * 4 + 3] = 255;
		}
		var bmp = CreateBmp(width, height, 32, 0, bgra, topDown: true);
		var path = TempFile("font.ttf", ImagePixelGoldenTests.BuildTestTrueType());
		var output = bmp.AddTextWatermark("I", fontFilePath: path, fontSize: 18, opacity: 200, margin: 8);
		(output[0] == 'B' && output[1] == 'M').AssertTrue();

		// At 18 pt the glyph is the rectangle from (34.4, 16) to (53.6, 35.2).
		AssertPixels(WhiteBoxes(rgba, width, 200, (34.4, 16, 53.6, 35.2)), ReadPng(output.ConvertToPng(), width, height), width);
	}

	[TestMethod]
	public void Bmp_120BitFlippedAndTruncatedInputs_RejectWithoutRuntimeCrashes()
	{
		var original = Indexed(8, 5, 4, false);
		var random = new Random(105912);
		for (var length = 1; length < original.Length; length += Math.Max(1, original.Length / 24))
		{
			try
			{
				original[..length].ConvertToPng();
				Fail($"BMP truncated after byte {length} was accepted.");
			}
			catch (InvalidDataException) { }
			catch (NotSupportedException) { }
		}
		for (var n = 0; n < 120; n++)
		{
			var mutated = (byte[])original.Clone();
			var at = random.Next(mutated.Length);
			mutated[at] ^= (byte)(1 << random.Next(8));
			try
			{
				var png = mutated.ConvertToPng();
				(png.GetImageSize().Width is > 0 and <= 100).AssertTrue();
			}
			catch (InvalidDataException) { }
			catch (NotSupportedException) { }
		}
	}

	private static void ExpectedGrid(byte[] rgba)
	{
		int[] indices = [0, 1, 2, 3, 3, 2, 1, 0, 1, 1, 1, 1];
		for (var i = 0; i < 12; i++)
			for (var c = 0; c < 4; c++)
				rgba[4 * i + c].AssertEqual(_colors[indices[i]][c]);
	}

	private static byte[] Indexed(int depth, int width, int height, bool topDown)
	{
		var count = depth == 1 ? 2 : 4;
		var stride = (width * depth + 31) / 32 * 4;
		var rows = new byte[stride * height];
		for (var y = 0; y < height; y++)
		{
			var row = topDown ? y : height - y - 1;
			for (var x = 0; x < width; x++)
			{
				var sample = (x + y) % count;
				var bit = x * depth;
				rows[row * stride + bit / 8] |= (byte)(sample << (8 - depth - bit % 8));
			}
		}
		return CreateBmp(width, height, depth, 0, rows, topDown: topDown, numberOfColors: count);
	}

	private static byte[] CreateBmp(int width, int height, int depth, int compression, byte[] pixelData,
		bool topDown = false, int numberOfColors = 0, uint[] masks = null)
	{
		var colors = numberOfColors;
		var masksSize = masks?.Length * 4 ?? 0;
		var offset = 54 + masksSize + colors * 4;
		var bytes = new byte[offset + pixelData.Length];
		bytes[0] = (byte)'B';
		bytes[1] = (byte)'M';
		Put(bytes, 2, (uint)bytes.Length);
		Put(bytes, 10, (uint)offset);
		Put(bytes, 14, 40);
		Put(bytes, 18, (uint)width);
		Put(bytes, 22, unchecked((uint)(topDown ? -height : height)));
		Short(bytes, 26, 1);
		Short(bytes, 28, (ushort)depth);
		Put(bytes, 30, (uint)compression);
		Put(bytes, 34, (uint)pixelData.Length);
		Put(bytes, 46, (uint)colors);
		if (masks != null)
			for (var i = 0; i < masks.Length; i++)
				Put(bytes, 54 + i * 4, masks[i]);
		for (var i = 0; i < colors; i++)
		{
			var c = _colors[i];
			var at = 54 + masksSize + i * 4;
			bytes[at] = c[2];
			bytes[at + 1] = c[1];
			bytes[at + 2] = c[0];
		}
		Array.Copy(pixelData, 0, bytes, offset, pixelData.Length);
		return bytes;
	}

	private static void Put(byte[] data, int offset, uint value) =>
		BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);

	private static void Short(byte[] data, int offset, int value) =>
		BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), (ushort)value);
}
