namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class ImageProcessingLimitsTests : BaseTestClass
{
	[TestMethod]
	public void Webp_ExplicitImageBudgetCanExceedDefault()
	{
		const int width = 5200;
		const int height = 5000;
		using var bits = new WebpBitWriter();
		bits.Write(0x2f, 8);
		bits.Write(width - 1, 14);
		bits.Write(height - 1, 14);
		bits.Write(0, 4); // Opaque image, version zero.
		bits.Write(0, 3); // No transforms, color cache or entropy image.
		foreach (var symbol in new[] { 0u, 0u, 0u, 255u, 0u })
		{
			bits.Write(1, 1); // A simple Huffman code with one symbol, consuming no data bits.
			bits.Write(0, 1);
			bits.Write(symbol > 1 ? 1u : 0u, 1);
			bits.Write(symbol, symbol > 1 ? 8 : 1);
		}
		var payload = bits.Finish();
		using var file = new MemoryStream();
		using var writer = new BinaryWriter(file);
		writer.Write("RIFF"u8);
		writer.Write((uint)(12 + payload.Length + (payload.Length & 1)));
		writer.Write("WEBPVP8L"u8);
		writer.Write((uint)payload.Length);
		writer.Write(payload);
		if ((payload.Length & 1) != 0) writer.Write((byte)0);
		var input = file.ToArray();
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng());
		var output = input.ResizeImage(100, 100, ImageFormats.Png, new ImageProcessingLimits(30_000_000, 512, 100_000_000));
		AssertPixels(Rgba(100, 96, (_, _) => (0, 0, 0, 255)), ReadPng(output, 100, 96), 100);
	}

	[TestMethod]
	public void Tiff_ExplicitImageBudgetCanExceedDefault()
	{
		const int width = 5200;
		const int height = 5000;
		const int entries = 9;
		const int stripOffset = 8 + 2 + entries * 12 + 4;
		var input = new byte[stripOffset + width / 8 * height];
		input[0] = input[1] = (byte)'I';
		BinaryPrimitives.WriteUInt16LittleEndian(input.AsSpan(2), 42);
		BinaryPrimitives.WriteUInt32LittleEndian(input.AsSpan(4), 8);
		BinaryPrimitives.WriteUInt16LittleEndian(input.AsSpan(8), entries);
		(ushort tag, ushort type, uint value)[] tags =
		[
			(256, 4, width), (257, 4, height), (258, 3, 1), (259, 3, 1), (262, 3, 1),
			(273, 4, stripOffset), (277, 3, 1), (278, 4, height), (279, 4, (uint)(input.Length - stripOffset)),
		];
		for (var i = 0; i < tags.Length; i++)
		{
			var entry = input.AsSpan(10 + i * 12);
			BinaryPrimitives.WriteUInt16LittleEndian(entry, tags[i].tag);
			BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], tags[i].type);
			BinaryPrimitives.WriteUInt32LittleEndian(entry[4..], 1);
			BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], tags[i].value);
		}
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng());
		var output = input.ResizeImage(100, 100, new ImageProcessingLimits(30_000_000, 512, 100_000_000));
		AssertPixels(Rgba(100, 96, (_, _) => (0, 0, 0, 255)), ReadPng(output, 100, 96), 100);
	}

	[TestMethod]
	public void Conversion_EnforcesExplicitImagePixelBudget()
	{
		var input = CreatePng(32, 24, Rgba(32, 24, (_, _) => (10, 20, 30, 255)));
		var limits = new ImageProcessingLimits(700, 512, 100_000_000);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng(limits));
	}

	[TestMethod]
	[DataRow(ImageFormats.Gif)]
	[DataRow(ImageFormats.Png)]
	[DataRow(ImageFormats.Webp)]
	public void Animation_EnforcesExplicitTotalPixelBudget(ImageFormats format)
	{
		var builder = new GifBuilder(32, 24, GifBuilder.GrayPalette(2));
		for (var frame = 0; frame < 10; frame++) builder.Frame(0, 0, 1, 1, [0], delay: 1);
		var limits = new ImageProcessingLimits(25_000_000, 512, 7_000);
		var input = builder.Build().ResizeImage(32, 24, format);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng(limits));
	}

	[TestMethod]
	public void Animation_ExplicitFrameBudgetCanExceedDefault()
	{
		var builder = new GifBuilder(2, 2, GifBuilder.GrayPalette(2));
		for (var frame = 0; frame < 513; frame++) builder.Frame(0, 0, 1, 1, [(byte)(frame % 2)], delay: 1);
		var input = builder.Build();
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng());
		var limits = new ImageProcessingLimits(25_000_000, 600, 100_000_000);
		var output = input.ConvertToPng(limits);
		AreEqual(ImageFormats.Png, output.GetImageFormat());
		AreEqual(513, output.GetAnimationFrames(limits).Count);
	}
}
