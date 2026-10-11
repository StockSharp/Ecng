namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;
using System.Drawing;

using Ecng.Drawing;
using Ecng.Net;

using StbImageSharp;

using static ImageTestHelper;

/// <summary>
/// Regressions for the capabilities and compatibility cases recorded in configs commit ff010f6.
/// Full-resolution decoding and animation resource limits remain explicit positive controls.
/// </summary>
[TestClass]
[TestCategory("DrawingDebt")]
public class ImageDebtReproductionTests : BaseTestClass
{
	public enum ImageOperations
	{
		Size,
		Resize,
		Convert,
		Watermark,
	}

	public static IEnumerable<object[]> MissingFormatCases()
	{
		foreach (var name in new[] { "lossy.webp", "lossless.webp", "alpha.webp", "animated.webp", "png.ico", "dib.ico", "black.tiff" })
			foreach (var operation in Enum.GetValues<ImageOperations>())
				yield return [name, operation];
	}

	public static IEnumerable<object[]> JpegModeCases()
	{
		foreach (var name in new[] { "baseline.jpg", "extended.jpg", "multiscan.jpg", "arithmetic.jpg", "lossless.jpg", "12bit.jpg", "12bit-progressive.jpg" })
			foreach (var operation in Enum.GetValues<ImageOperations>())
				yield return [name, operation];
	}

	[TestMethod]
	[DynamicData(nameof(MissingFormatCases))]
	public void RecognizedImageFormats_SupportPublicImageOperations(string name, ImageOperations operation)
	{
		IsTrue(name.IsImage(), $"The network layer must recognize {name} as an image.");
		var data = name.EndsWith(".ico", StringComparison.Ordinal) ? Icon(name == "png.ico") : Fixture(name);
		AssertOperation(data, operation, name == "alpha.webp" ? (byte)64 : (byte)255,
			name == "animated.webp");
	}

	[TestMethod]
	[DataRow(ImageOperations.Size)]
	[DataRow(ImageOperations.Resize)]
	[DataRow(ImageOperations.Convert)]
	[DataRow(ImageOperations.Watermark)]
	public void Avif_OptionalFormatProposal_SupportsPublicImageOperations(ImageOperations operation)
	{
		// TIFF and AVIF support is an owner decision in the debt log, not an existing API promise.
		var data = Fixture("white-1x1.avif");
		IsTrue("picture.avif".IsImage());
		var output = operation switch
		{
			ImageOperations.Size => data,
			ImageOperations.Resize => data.ResizeImage(1, 1),
			ImageOperations.Convert => data.ConvertToPng(),
			ImageOperations.Watermark => data.AddTextWatermark("I", ImagePixelGoldenTests.BuildTestTrueType(), fontSize: 1, margin: 0),
			_ => throw new ArgumentOutOfRangeException(nameof(operation)),
		};
		AreEqual(new Size(1, 1), output.GetImageSize());
		if (operation != ImageOperations.Size)
			AssertPixels(ImageResult.FromMemory(Fixture("white-1x1.png"), ColorComponents.RedGreenBlueAlpha).Data,
				ImageResult.FromMemory(output.ConvertToPng(), ColorComponents.RedGreenBlueAlpha).Data, 1);
	}

	[TestMethod]
	[DynamicData(nameof(JpegModeCases))]
	public void ValidJpegModes_SupportPublicImageOperations(string name, ImageOperations operation)
		=> AssertOperation(Fixture(name), operation, 255, false);

	[TestMethod]
	[DataRow(ImageOperations.Resize)]
	[DataRow(ImageOperations.Convert)]
	[DataRow(ImageOperations.Watermark)]
	public void Png_BytesAfterIend_DoNotPreventImageOperations(ImageOperations operation)
	{
		var pixels = Rgba(32, 24, (_, _) => (0, 0, 0, 255));
		var png = CreatePng(32, 24, pixels);
		// The control proves that the same operation and font work before the single-byte mutation.
		AssertOperation(png, operation, 255, false);
		byte[] withTail = [.. png, 0x42];
		var reference = ImageResult.FromMemory(withTail, ColorComponents.RedGreenBlueAlpha);
		AssertPixels(pixels, reference.Data, 32);
		AreEqual(new Size(32, 24), withTail.GetImageSize());
		AssertOperation(withTail, operation, 255, false);
	}

	[TestMethod]
	[DataRow(ImageOperations.Resize)]
	[DataRow(ImageOperations.Convert)]
	[DataRow(ImageOperations.Watermark)]
	public void ReadableHeader_DoesNotRemoveTheNeedToHandleMalformedPixelData(ImageOperations operation)
	{
		// A TryGetImageSize API could succeed here. It cannot guarantee that later decoding will succeed.
		var noPixels = WriteChunks([("IHDR", Header(32, 24, 8, 6)), ("IEND", [])]);
		AreEqual(new Size(32, 24), noPixels.GetImageSize());
		ThrowsExactly<InvalidDataException>(() =>
		{
			_ = operation switch
			{
				ImageOperations.Resize => noPixels.ResizeImage(16, 12),
				ImageOperations.Convert => noPixels.ConvertToPng(),
				ImageOperations.Watermark => noPixels.AddTextWatermark("I", ImagePixelGoldenTests.BuildTestTrueType(), fontSize: 8, margin: 1),
				_ => throw new ArgumentOutOfRangeException(nameof(operation)),
			};
		});
	}

	[TestMethod]
	[DataRow("png")]
	[DataRow("jpeg")]
	public void LargeValidImage_CanBeReducedWithoutAllocatingItsFullCanvas(string format)
	{
		var data = format == "png" ? LargePng() : Fixture("large-48mp.jpg");
		AreEqual(new Size(8000, 6000), data.GetImageSize());
		var before = GC.GetAllocatedBytesForCurrentThread();
		var thumbnail = data.ResizeImage(200, 200);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		AreEqual(new Size(200, 150), thumbnail.GetImageSize());
		AssertPixels(Rgba(200, 150, (_, _) => (0, 0, 0, 255)),
			ImageResult.FromMemory(thumbnail, ColorComponents.RedGreenBlueAlpha).Data, 200);
		// Full RGBA alone would cost 192 MB. A thumbnail should not need that allocation.
		IsTrue(allocated < 48_000_000, $"Thumbnail allocated {allocated:N0} bytes.");
	}

	[TestMethod]
	public void Resize_AlreadyFittingValidPng_DoesNotDecodeTheFullImage()
	{
		var png = CreatePng(1024, 768, Rgba(1024, 768, (_, _) => (20, 40, 90, 255)));
		CreatePng(1, 1, [0, 0, 0, 255]).ResizeImage(1, 1);
		var before = GC.GetAllocatedBytesForCurrentThread();
		var result = png.ResizeImage(1024, 768);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		IsTrue(png.AsSpan().SequenceEqual(result));
		IsTrue(allocated < png.Length + 256_000,
			$"An already fitting {png.Length:N0}-byte PNG allocated {allocated:N0} bytes.");
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void TryGetImageSize_ReportsReadableAndUnknownDataWithoutThrowing(bool readable)
	{
		// Check the conventional Try signature without inventing a production API merely to compile a test.
		var method = typeof(ImageHelper).GetMethod("TryGetImageSize",
			[typeof(byte[]), typeof(Size).MakeByRefType()]);
		IsNotNull(method, "ImageHelper has no TryGetImageSize(byte[], out Size). This is an API proposal.");
		object[] arguments = [readable ? CreatePng(32, 24, new byte[32 * 24 * 4]) : new byte[] { 1, 2, 3 }, default(Size)];
		AreEqual(readable, (bool)method.Invoke(null, arguments));
		AreEqual(readable ? new Size(32, 24) : Size.Empty, (Size)arguments[1]);
	}

	[TestMethod]
	public void Bmp_ThumbnailUsesUncompressedOriginalFormat_AndPngIsSmaller()
	{
		// This is an observation of the existing contract, not a claim that its format is incorrect.
		var image = new RasterImage(200, 200);
		Rgba(200, 200, (_, _) => (20, 40, 90, 255)).CopyTo(image.Pixels, 0);
		var thumbnail = BmpCodec.Encode(image).ResizeImage(100, 100);
		AreEqual(new Size(100, 100), thumbnail.GetImageSize());
		AreEqual(14 + 108 + 100 * 100 * 4, thumbnail.Length);
		var png = thumbnail.ConvertToPng();
		IsTrue(png.Length < thumbnail.Length / 10);
		AssertPixels(ImageResult.FromMemory(thumbnail, ColorComponents.RedGreenBlueAlpha).Data,
			ImageResult.FromMemory(png, ColorComponents.RedGreenBlueAlpha).Data, 100);
		TestContext.WriteLine($"100x100 BMP: {thumbnail.Length} bytes; PNG: {png.Length} bytes.");
	}

	[TestMethod]
	[DataRow(513, 1, 1)]
	[DataRow(5, 5000, 5000)]
	public void ValidGif_ExistingAnimationBudgetsAreExplicitlyEnforced(int frames, int width, int height)
	{
		var builder = new GifBuilder(width, height, GifBuilder.GrayPalette(2));
		for (var frame = 0; frame < frames; frame++)
			builder.Frame(0, 0, 1, 1, [0], delay: 1);
		var gif = builder.Build();
		AreEqual(new Size(width, height), gif.GetImageSize());
		var before = GC.GetAllocatedBytesForCurrentThread();
		var error = ThrowsExactly<InvalidDataException>(() => gif.ResizeImage(1, 1));
		IsTrue(error.Message.Contains(frames > 512 ? "frames" : "pixels", StringComparison.Ordinal));
		IsTrue(GC.GetAllocatedBytesForCurrentThread() - before < 1_000_000,
			"Refusing an animation must happen before allocating its canvas.");
	}

	internal static byte[] Fixture(string name)
	{
		using var stream = typeof(ImageDebtReproductionTests).Assembly.GetManifestResourceStream($"Drawing.Fixtures.{name}");
		IsNotNull(stream, $"Missing embedded fixture: {name}");
		using var output = new MemoryStream();
		stream.CopyTo(output);
		return output.ToArray();
	}

	private static void AssertOperation(byte[] data, ImageOperations operation, byte alpha, bool animated)
	{
		if (operation == ImageOperations.Size)
		{
			AreEqual(new Size(32, 24), data.GetImageSize());
			return;
		}

		var output = operation switch
		{
			ImageOperations.Resize => data.ResizeImage(16, 12),
			ImageOperations.Convert => data.ConvertToPng(),
			ImageOperations.Watermark => data.AddTextWatermark("I", ImagePixelGoldenTests.BuildTestTrueType(), fontSize: 8, margin: 1),
			_ => throw new ArgumentOutOfRangeException(nameof(operation)),
		};
		var width = operation == ImageOperations.Resize ? 16 : 32;
		var height = operation == ImageOperations.Resize ? 12 : 24;
		AreEqual(new Size(width, height), output.GetImageSize());
		if (animated)
		{
			AreEqual(3, output.GetAnimationLoopCount());
			var frames = output.GetAnimationFrames();
			AreEqual(2, frames.Count);
			for (var frame = 0; frame < frames.Count; frame++)
			{
				AreEqual(frame == 0 ? 70 : 110, frames[frame].delayMilliseconds);
				AssertOutput(frames[frame].png, width, height, frame == 0 ? (byte)0 : (byte)255, alpha, operation);
			}
		}
		else
			AssertOutput(output.ConvertToPng(), width, height, 0, alpha, operation);
	}

	private static void AssertOutput(byte[] png, int width, int height, byte color, byte alpha, ImageOperations operation)
	{
		var actual = ImageResult.FromMemory(png, ColorComponents.RedGreenBlueAlpha);
		AreEqual(width, actual.Width);
		AreEqual(height, actual.Height);
		var expected = Rgba(width, height, (_, _) => (color, color, color, alpha));
		if (operation == ImageOperations.Watermark)
		{
			// The bottom-right glyph must change some pixels and leave the top-left background alone.
			if (color != 255 || alpha != 255)
				IsFalse(expected.AsSpan().SequenceEqual(actual.Data));
			else
				AssertPixels(expected, actual.Data, width, tolerance: 2);
			AssertPixels(expected[..4], actual.Data[..4], 1, tolerance: 2);
		}
		else
			AssertPixels(expected, actual.Data, width, tolerance: 2);
	}

	internal static byte[] LargePng()
		=> WriteChunks([("IHDR", Header(8000, 6000, 1, 0)), ("IDAT", Deflate(new byte[6000 * 1001])), ("IEND", [])]);

	internal static byte[] Icon(bool pngFrame)
	{
		const int width = 32;
		const int height = 24;
		byte[] frame;
		if (pngFrame)
			frame = CreatePng(width, height, Rgba(width, height, (_, _) => (0, 0, 0, 255)));
		else
		{
			// ICO DIB height includes the XOR bitmap and its 1-bit AND mask.
			frame = new byte[40 + width * height * 4 + height * 4];
			BinaryPrimitives.WriteInt32LittleEndian(frame, 40);
			BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4), width);
			BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(8), height * 2);
			BinaryPrimitives.WriteInt16LittleEndian(frame.AsSpan(12), 1);
			BinaryPrimitives.WriteInt16LittleEndian(frame.AsSpan(14), 32);
			BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(20), width * height * 4);
			for (var pixel = 0; pixel < width * height; pixel++)
				frame[40 + pixel * 4 + 3] = 255;
		}
		var data = new byte[22 + frame.Length];
		BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(2), 1);
		BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(4), 1);
		data[6] = width;
		data[7] = height;
		BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(10), 1);
		BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(12), 32);
		BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(14), frame.Length);
		BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(18), 22);
		frame.CopyTo(data, 22);
		return data;
	}
}
