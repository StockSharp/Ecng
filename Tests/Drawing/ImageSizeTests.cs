namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;
using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// <see cref="ImageHelper.GetImageSize(byte[])"/> reads a header: it has no use for the limits of the decoders.
/// </summary>
[TestClass]
public class ImageSizeTests : BaseTestClass
{
	[TestMethod]
	public void PictureLargerThanTheDecodersTake_StillHasASize()
	{
		// 8000x6000 is what a 48-megapixel phone camera writes.
		var expected = new Size(8000, 6000);

		Png(8000, 6000).GetImageSize().AssertEqual(expected);
		Jpeg(8000, 6000).GetImageSize().AssertEqual(expected);
		Bmp(8000, 6000).GetImageSize().AssertEqual(expected);
		Bmp(8000, -6000).GetImageSize().AssertEqual(expected);
		Gif(8000, 6000).GetImageSize().AssertEqual(expected);
	}

	[TestMethod]
	public void SizeOfAPng_IsTheOnePngHelperReads()
	{
		var png = Png(70000, 3);

		png.GetImageSize().AssertEqual(png.GetPngSize());
	}

	[TestMethod]
	public void ZeroOrNegativeSize_IsInvalidData()
	{
		ThrowsExactly<InvalidDataException>(() => Png(0, 10).GetImageSize());
		ThrowsExactly<InvalidDataException>(() => Png(10, -1).GetImageSize());
		ThrowsExactly<InvalidDataException>(() => Jpeg(0, 10).GetImageSize());
		ThrowsExactly<InvalidDataException>(() => Bmp(-5, 10).GetImageSize());
		ThrowsExactly<InvalidDataException>(() => Bmp(5, 0).GetImageSize());
		ThrowsExactly<InvalidDataException>(() => Gif(0, 10).GetImageSize());
	}

	[TestMethod]
	public void PictureLargerThanTheDecodersTake_IsRefusedWhenItHasToBeDecoded()
	{
		ThrowsExactly<InvalidDataException>(() => Jpeg(8000, 6000).ResizeImage(100, 100));
		ThrowsExactly<InvalidDataException>(() => Gif(8000, 6000).ResizeImage(100, 100));
	}

	private static byte[] Png(int width, int height)
		=> WriteChunks([("IHDR", Header(width, height, 8, 2))]);

	private static byte[] Jpeg(int width, int height) =>
	[
		0xFF, 0xD8,
		0xFF, 0xC0, 0x00, 0x0B, 8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 1, 1, 0x11, 0,
	];

	private static byte[] Bmp(int width, int height)
	{
		var bmp = new byte[54];
		"BM"u8.CopyTo(bmp);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), width);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), height);
		BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26), 1);
		BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28), 24);
		return bmp;
	}

	private static byte[] Gif(int width, int height)
	{
		var gif = new byte[13];
		"GIF89a"u8.CopyTo(gif);
		BinaryPrimitives.WriteUInt16LittleEndian(gif.AsSpan(6), (ushort)width);
		BinaryPrimitives.WriteUInt16LittleEndian(gif.AsSpan(8), (ushort)height);
		return gif;
	}
}
