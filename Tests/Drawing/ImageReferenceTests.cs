namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class ImageReferenceTests : BaseTestClass
{
	[TestMethod]
	[DataRow(1, 1)]
	[DataRow(3, 7)]
	[DataRow(31, 17)]
	[DataRow(65, 33)]
	public void Png_EncodedAndDecodedPixelsMatchIndependentDecoder(int width, int height)
	{
		var pixels = new byte[width * height * 4];
		new Random(width * 1000 + height).NextBytes(pixels);
		var fixture = CreatePng(width, height, pixels);
		var image = PngCodec.Decode(fixture);
		AssertReference(fixture, image, pixels);
		AssertReference(PngCodec.Encode(image, []), image, pixels);
	}

	[TestMethod]
	[DataRow(1, 1)]
	[DataRow(3, 7)]
	[DataRow(31, 17)]
	public void Bmp_EncodedPixelsMatchIndependentDecoderIncludingAlpha(int width, int height)
	{
		var image = new RasterImage(width, height);
		Rgba(width, height, (x, y) => ((byte)(x * 37), (byte)(y * 19), (byte)(x * y * 11),
			(byte)((x + y) % 3 == 0 ? 255 : (x + y) % 3 == 1 ? 128 : 0))).CopyTo(image.Pixels, 0);
		var encoded = BmpCodec.Encode(image);
		AssertReference(encoded, BmpCodec.Decode(encoded), image.Pixels);
	}

	[TestMethod]
	[DataRow(50)]
	[DataRow(90)]
	[DataRow(100)]
	public void Jpeg_EncoderOutputMatchesIndependentDecoderWithIdctRoundingTolerance(int quality)
	{
		var image = new RasterImage(35, 19);
		Rgba(image.Width, image.Height, (x, y) =>
			((byte)(x * 7), (byte)(y * 13), (byte)((x * 17 + y * 11) % 256), 255)).CopyTo(image.Pixels, 0);
		var jpeg = JpegEncoder.Encode(image, quality, []);
		var reference = ImageResult.FromMemory(jpeg, ColorComponents.RedGreenBlueAlpha);
		var actual = JpegCodec.Decode(jpeg);

		AreEqual(image.Width, reference.Width);
		AreEqual(image.Height, reference.Height);
		// Independent integer and floating-point inverse DCTs can differ by one sample level.
		AssertPixels(reference.Data, actual.Pixels, image.Width, tolerance: 2);
	}

	[TestMethod]
	public void Gif_EncoderOutputMatchesIndependentDecoderForEveryFrameAndDelay()
	{
		const int width = 129;
		const int height = 67;
		var frames = new AnimationFrame[4];
		for (var frame = 0; frame < frames.Length; frame++)
		{
			var image = new RasterImage(width, height);
			Rgba(width, height, (x, y) =>
			{
				var value = (byte)((x * 13 + y * 7 + (x >= frame * 20 ? frame : 0)) % 16 * 17);
				return (value, (byte)(255 - value), (byte)(value ^ 0x55), 255);
			}).CopyTo(image.Pixels, 0);
			frames[frame] = new(image, frame + 1, 100);
		}

		AssertGifFrames(GifCodec.Encode(new(width, height, 0, frames.Length, frames)), frames);
	}

	[TestMethod]
	public void Gif_IdenticalFramesKeepTheirOwnPixelsAndDelaysInIndependentDecoder()
	{
		var image = new RasterImage(17, 13);
		Rgba(image.Width, image.Height, (_, _) => (40, 80, 120, 255)).CopyTo(image.Pixels, 0);
		AnimationFrame[] frames = [new(image, 3, 100), new(image, 7, 100), new(image, 11, 100)];
		AssertGifFrames(GifCodec.Encode(new(image.Width, image.Height, 2, frames.Length, frames)), frames);
	}

	[TestMethod]
	public void Apng_EncodedFramesMatchOriginalRgbaAndTimingWithoutExternalProcesses()
	{
		const int width = 19;
		const int height = 11;
		var frames = new AnimationFrame[3];
		for (var frame = 0; frame < frames.Length; frame++)
		{
			var image = new RasterImage(width, height);
			new Random(739 + frame).NextBytes(image.Pixels);
			frames[frame] = new(image, frame + 1, 30);
		}

		var apng = ApngEncoder.Encode(new(width, height, 4, frames.Length, frames), []);
		var reference = ReadApng(apng);
		AreEqual(4, reference.Plays);
		AreEqual(frames.Length, reference.Frames.Count);
		var actual = ApngDecoder.Open(apng).Frames.ToArray();
		for (var frame = 0; frame < frames.Length; frame++)
		{
			AreEqual((ushort)(frame + 1), reference.Frames[frame].DelayNumerator);
			AreEqual((ushort)30, reference.Frames[frame].DelayDenominator);
			AssertPixels(frames[frame].Image.Pixels, reference.Frames[frame].Rgba, width);
			AssertPixels(frames[frame].Image.Pixels, actual[frame].Image.Pixels, width);
		}
	}

	[TestMethod]
	public void Apng_SubrectanglesBlendAndDisposeToExpectedCSharpPixelBuffers()
	{
		byte[] red = [255, 0, 0, 255];
		byte[] blue = [0, 0, 255, 255];
		byte[] green = [0, 255, 0, 255];
		byte[] none = [0, 0, 0, 0];
		var control = new byte[8];
		BinaryPrimitives.WriteInt32BigEndian(control, 5);
		BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(4), 2);
		var apng = WriteChunks(
		[
			("IHDR", Header(3, 1, 8, 6)),
			("acTL", control),
			("fcTL", FrameControl(0, 3, 1, 0, 0, 1, 10, 0, 0)),
			("IDAT", FrameData(3, 1, [.. red, .. red, .. red])),
			("fcTL", FrameControl(1, 1, 1, 1, 0, 2, 10, 2, 0)),
			("fdAT", Sequenced(2, FrameData(1, 1, blue))),
			("fcTL", FrameControl(3, 1, 1, 2, 0, 3, 10, 1, 0)),
			("fdAT", Sequenced(4, FrameData(1, 1, green))),
			("fcTL", FrameControl(5, 1, 1, 0, 0, 4, 10, 0, 1)),
			("fdAT", Sequenced(6, FrameData(1, 1, [0, 0, 255, 128]))),
			("fcTL", FrameControl(7, 1, 1, 1, 0, 5, 10, 0, 0)),
			("fdAT", Sequenced(8, FrameData(1, 1, green))),
			("IEND", []),
		]);
		byte[][] expected =
		[
			[.. red, .. red, .. red],
			[.. red, .. blue, .. red],
			[.. red, .. red, .. green],
			[127, 0, 128, 255, .. red, .. none],
			[127, 0, 128, 255, .. green, .. none],
		];

		var frames = apng.GetAnimationFrames();
		AreEqual(expected.Length, frames.Count);
		AreEqual(2, apng.GetAnimationLoopCount());
		for (var frame = 0; frame < frames.Count; frame++)
		{
			AreEqual((frame + 1) * 100, frames[frame].delayMilliseconds);
			var reference = ImageResult.FromMemory(frames[frame].png, ColorComponents.RedGreenBlueAlpha);
			AssertPixels(expected[frame], reference.Data, 3);
		}
	}

	private static void AssertReference(byte[] encoded, RasterImage actual, byte[] expected)
	{
		var reference = ImageResult.FromMemory(encoded, ColorComponents.RedGreenBlueAlpha);
		AreEqual(actual.Width, reference.Width);
		AreEqual(actual.Height, reference.Height);
		AssertPixels(expected, reference.Data, actual.Width);
		AssertPixels(reference.Data, actual.Pixels, actual.Width);
	}

	private static void AssertGifFrames(byte[] encoded, AnimationFrame[] expected)
	{
		using var stream = new MemoryStream(encoded);
		using var reference = ImageResult.AnimatedGifFramesFromStream(stream, ColorComponents.RedGreenBlueAlpha).GetEnumerator();
		for (var frame = 0; frame < expected.Length; frame++)
		{
			IsTrue(reference.MoveNext(), $"Missing GIF frame {frame}.");
			var image = reference.Current;
			AreEqual(expected[frame].Image.Width, image.Width);
			AreEqual(expected[frame].Image.Height, image.Height);
			AreEqual(expected[frame].DelayMilliseconds, image.DelayInMs);
			AssertPixels(expected[frame].Image.Pixels, image.Data, image.Width);
		}
		IsFalse(reference.MoveNext(), "Unexpected extra GIF frame.");
	}
}
