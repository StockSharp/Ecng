namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// BMP decoding against files written here byte by byte.
/// </summary>
[TestClass]
public class BmpCodecTests : BaseTestClass
{
	private const int _rgb = 0;
	private const int _rle8 = 1;
	private const int _rle4 = 2;

	// Index 0 white, 1 red, 2 blue, 3 black: stored blue, green, red, reserved.
	private static readonly byte[] _palette = [255, 255, 255, 0, 0, 0, 255, 0, 255, 0, 0, 0, 0, 0, 0, 0];

	private static readonly (byte r, byte g, byte b, byte a) _white = (255, 255, 255, 255);
	private static readonly (byte r, byte g, byte b, byte a) _red = (255, 0, 0, 255);

	[TestMethod]
	public void PixelOffsetThatDoesNotFitAnInt_IsInvalidData()
	{
		var bmp = Bmp(2, 2, 24, _rgb, [], new byte[16]);

		foreach (var offset in new uint[] { 0x8000_0000, 0x8000_0036, 0xFFFF_FFFF })
		{
			BinaryPrimitives.WriteUInt32LittleEndian(bmp.AsSpan(10), offset);

			ThrowsExactly<InvalidDataException>(() => bmp.ConvertToPng());
			ThrowsExactly<InvalidDataException>(() => bmp.ResizeImage(1, 1));
		}
	}

	[TestMethod]
	public void FourthByteOfUncompressedThirtyTwoBitPixels_IsNotAlpha()
	{
		// The format reserves the byte, and writers leave anything in it.
		var bmp = Bmp(2, 1, 32, _rgb, [], [10, 20, 30, 0, 40, 50, 60, 128]);

		var rgba = ReadPng(bmp.ConvertToPng(), 2, 1);

		AssertPixels([30, 20, 10, 255, 60, 50, 40, 255], rgba, 2);
	}

	[TestMethod]
	public void Rle_RunLongerThanTheRow_IsCutAtItsEnd()
	{
		// Six pixels of color 1 on a row of four, end of line, end of bitmap.
		var rle8 = Bmp(4, 1, 8, _rle8, _palette, [6, 1, 0, 0, 0, 1]);
		AssertPixels(Rgba(4, 1, (_, _) => _red), ReadPng(rle8.ConvertToPng(), 4, 1), 4);

		// The same in four bits a pixel: a run alternates the two halves of its byte.
		var rle4 = Bmp(4, 1, 4, _rle4, _palette, [6, 0x11, 0, 0, 0, 1]);
		AssertPixels(Rgba(4, 1, (_, _) => _red), ReadPng(rle4.ConvertToPng(), 4, 1), 4);

		// Literal pixels that run over the edge: five of them, padded to an even count of bytes.
		var literal = Bmp(4, 1, 8, _rle8, _palette, [0, 5, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1]);
		AssertPixels(Rgba(4, 1, (_, _) => _red), ReadPng(literal.ConvertToPng(), 4, 1), 4);
	}

	[TestMethod]
	public void Rle_StreamWithoutTheEndOfBitmapMarker_IsAccepted()
	{
		// Rows are stored bottom up: the first run is the lower row.
		var bmp = Bmp(2, 2, 8, _rle8, _palette, [2, 1, 0, 0, 2, 0]);

		var rgba = ReadPng(bmp.ConvertToPng(), 2, 2);

		AssertPixels(Rgba(2, 2, (_, y) => y == 0 ? _white : _red), rgba, 2);
	}

	[TestMethod]
	public void Rle_CommandCutInTheMiddle_IsInvalidData()
	{
		// A run count without its color; a literal run announcing more bytes than there are.
		ThrowsExactly<InvalidDataException>(() => Bmp(2, 1, 8, _rle8, _palette, [2]).ConvertToPng());
		ThrowsExactly<InvalidDataException>(() => Bmp(4, 1, 8, _rle8, _palette, [0, 4, 1, 1]).ConvertToPng());
	}

	[TestMethod]
	public void WrittenBmp_KeepsTheAlphaOfItsPixels()
	{
		var image = new RasterImage(2, 2);
		byte[] pixels = [10, 20, 30, 255, 40, 50, 60, 128, 70, 80, 90, 1, 100, 110, 120, 0];

		pixels.CopyTo(image.Pixels, 0);

		AssertPixels(pixels, ReadPng(BmpCodec.Encode(image).ConvertToPng(), 2, 2), 2);
	}

	// A BITMAPINFOHEADER file: 14 bytes of file header, 40 of information header, the palette, the pixels.
	private static byte[] Bmp(int width, int height, int bits, int compression, byte[] palette, byte[] pixels)
	{
		var offset = 54 + palette.Length;
		var bmp = new byte[offset + pixels.Length];

		"BM"u8.CopyTo(bmp);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), offset);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), width);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), height);
		BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26), 1);
		BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28), (short)bits);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(30), compression);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(34), pixels.Length);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(46), palette.Length / 4);

		palette.CopyTo(bmp, 54);
		pixels.CopyTo(bmp, offset);
		return bmp;
	}
}
