namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;
using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class ImageFormatSafetyTests : BaseTestClass
{
	[TestMethod]
	[DataRow(1)]
	[DataRow(5)]
	[DataRow(32773)]
	public void Tiff_IncompleteStripIsRejected(int compression)
	{
		var complete = compression switch
		{
			1 => new byte[] { 10, 30, 50, 70 },
			5 => Lzw([256, 10, 30, 50, 70, 257]),
			_ => [3, 10, 30, 50, 70],
		};
		var truncated = compression switch
		{
			1 => new byte[] { 10, 30 },
			5 => Lzw([256, 10, 257]),
			_ => [3, 10, 30],
		};
		AssertPixels([10,10,10,255, 30,30,30,255, 50,50,50,255, 70,70,70,255],
			ReadPng(Tiff(compression, complete).ConvertToPng(), 4, 1), 4);
		ThrowsExactly<InvalidDataException>(() => Tiff(compression, truncated).ConvertToPng());
	}

	[TestMethod]
	[DataRow("lossy.webp", ImageFormats.Webp)]
	[DataRow("lossless.webp", ImageFormats.Webp)]
	[DataRow("animated.webp", ImageFormats.Webp)]
	[DataRow("black.tiff", ImageFormats.Tiff)]
	[DataRow("white-1x1.avif", ImageFormats.Avif)]
	[DataRow("arithmetic.jpg", ImageFormats.Jpeg)]
	public void DetectedFormatMatchesFile(string name, ImageFormats format)
	{
		var data = ImageDebtReproductionTests.Fixture(name);
		AreEqual(format, data.GetImageFormat());
		AreEqual(format, ((ReadOnlySpan<byte>)data).GetImageFormat());
		IsTrue(data.TryGetImageSize(out var size));
		AreEqual(data.GetImageSize(), size);
	}

	[TestMethod]
	public void TrySize_DoesNotClaimPixelsAreReadable()
	{
		var headerOnly = WriteChunks([("IHDR", Header(4, 3, 8, 6)), ("IEND", [])]);
		IsTrue(headerOnly.TryGetImageSize(out var size));
		AreEqual(new Size(4, 3), size);
		ThrowsExactly<InvalidDataException>(() => headerOnly.ConvertToPng());
		IsFalse(((byte[])null).TryGetImageSize(out size));
		AreEqual(Size.Empty, size);
		IsFalse(new byte[] { 1, 2, 3 }.TryGetImageSize(out size));
		AreEqual(ImageFormats.Unknown, new byte[] { 1, 2, 3 }.GetImageFormat());
	}

	[TestMethod]
	[DataRow(ImageFormats.Png)]
	[DataRow(ImageFormats.Jpeg)]
	[DataRow(ImageFormats.Webp)]
	public void ExplicitOutputFormat_IsUsedEvenWhenResizeFits(ImageFormats format)
	{
		var method = typeof(ImageHelper).GetMethod(nameof(ImageHelper.ResizeImage),
			[typeof(byte[]), typeof(int), typeof(int), typeof(ImageFormats), typeof(int)]);
		IsNotNull(method, "ResizeImage needs an explicit output-format overload.");
		var source = BmpCodec.Encode(PngCodec.Decode(CreatePng(20, 12, Rgba(20, 12, (_, _) => (20, 40, 90, 255)))));
		var output = (byte[])method.Invoke(null, [source, 20, 12, format, 100]);
		AreEqual(format, output.GetImageFormat());
		AreEqual(new Size(20, 12), output.GetImageSize());
		AssertPixels(Rgba(20, 12, (_, _) => (20, 40, 90, 255)), ReadPng(output.ConvertToPng(), 20, 12), 20,
			tolerance: format == ImageFormats.Jpeg ? 2 : 0);
		if (format == ImageFormats.Png) IsTrue(output.Length < source.Length / 5);
	}

	[TestMethod]
	public void ExplicitWatermarkOutput_IsUsedWhenOpacityIsZero()
	{
		var method = typeof(ImageHelper).GetMethod(nameof(ImageHelper.AddTextWatermark),
			[typeof(byte[]), typeof(string), typeof(byte[]), typeof(ImageFormats), typeof(float), typeof(byte), typeof(int), typeof(int)]);
		IsNotNull(method, "Watermark needs an explicit output-format overload.");
		var source = BmpCodec.Encode(PngCodec.Decode(CreatePng(4, 3, Rgba(4, 3, (_, _) => (80, 20, 30, 255)))));
		var output = (byte[])method.Invoke(null, [source, "I", new byte[] { 1 }, ImageFormats.Png, 8f, (byte)0, 0, 90]);
		AreEqual(ImageFormats.Png, output.GetImageFormat());
		AssertPixels(Rgba(4, 3, (_, _) => (80, 20, 30, 255)), ReadPng(output, 4, 3), 4);
	}

	[TestMethod]
	[DataRow("arithmetic.jpg")]
	[DataRow("lossless.jpg")]
	public void Jpeg_ExtendedCodingRequiresEndMarker(string name)
	{
		var data = ImageDebtReproductionTests.Fixture(name);
		AreEqual(new Size(32, 24), data.ConvertToPng().GetImageSize());
		ThrowsExactly<InvalidDataException>(() => data[..^2].ConvertToPng());
	}

	[TestMethod]
	public void ExplicitAnimationOutput_RetainsFramesTimingAndLoops()
	{
		var source = ImageDebtReproductionTests.Fixture("animated.webp");
		var converted = source.ResizeImage(32, 24, ImageFormats.Png);
		AreEqual(ImageFormats.Png, converted.GetImageFormat());
		AreEqual(3, converted.GetAnimationLoopCount());
		var frames = converted.GetAnimationFrames();
		AreEqual(2, frames.Count);
		AreEqual(70, frames[0].delayMilliseconds);
		AreEqual(110, frames[1].delayMilliseconds);
		AssertPixels(Rgba(32, 24, (_, _) => (0, 0, 0, 255)), ReadPng(frames[0].png, 32, 24), 32);
		AssertPixels(Rgba(32, 24, (_, _) => (255, 255, 255, 255)), ReadPng(frames[1].png, 32, 24), 32);
		ThrowsExactly<NotSupportedException>(() => source.ResizeImage(16, 12, ImageFormats.Jpeg));
	}

	[TestMethod]
	public void Webp_MalformedContainerAndVersionAreRejected()
	{
		var source = ImageDebtReproductionTests.Fixture("lossless.webp");
		ThrowsExactly<InvalidDataException>(() => source[..^1].ConvertToPng());
		source[24] |= 0xe0;
		ThrowsExactly<InvalidDataException>(() => source.ConvertToPng());
		IsFalse(source.TryGetImageSize(out _));
	}

	private static byte[] Tiff(int compression, byte[] strip)
	{
		const int entries = 9;
		const int stripOffset = 8 + 2 + entries * 12 + 4;
		var data = new byte[stripOffset + strip.Length];
		data[0] = data[1] = (byte)'I';
		BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2), 42);
		BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 8);
		BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(8), entries);
		(ushort tag, ushort type, uint value)[] tags =
		[
			(256, 4, 4), (257, 4, 1), (258, 3, 8), (259, 3, (uint)compression), (262, 3, 1),
			(273, 4, stripOffset), (277, 3, 1), (278, 4, 1), (279, 4, (uint)strip.Length),
		];
		for (var i = 0; i < tags.Length; i++)
		{
			var entry = data.AsSpan(10 + i * 12);
			BinaryPrimitives.WriteUInt16LittleEndian(entry, tags[i].tag);
			BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], tags[i].type);
			BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], 1);
			BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], tags[i].value);
		}
		strip.CopyTo(data, stripOffset);
		return data;
	}

	private static byte[] Lzw(int[] codes)
	{
		var bytes = new byte[(codes.Length * 9 + 7) / 8];
		var bit = 0;
		foreach (var code in codes)
			for (var index = 8; index >= 0; index--, bit++)
				bytes[bit / 8] |= (byte)(((code >> index) & 1) << (7 - bit % 8));
		return bytes;
	}
}
