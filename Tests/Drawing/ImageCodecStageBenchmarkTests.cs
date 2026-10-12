namespace Ecng.Tests.Drawing;

#if NET10_0
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
[TestCategory("Integration")]
[TestCategory("DrawingBenchmark")]
[DoNotParallelize]
public class ImageCodecStageBenchmarkTests : BaseTestClass
{
	[TestMethod]
	public void EncodingStages_ReportTheirIndependentMedianTimes()
	{
		var image = new RasterImage(512, 384);
		SyntheticImageCorpus.Pixels("texture", image.Width, image.Height).CopyTo(image.Pixels, 0);
		var png = CreatePng(image.Width, image.Height, image.Pixels);
		TestContext.WriteLine($"AVX={Avx.IsSupported}; AVX2={Avx2.IsSupported}; Vector128={Vector128.IsHardwareAccelerated}; Vector256={Vector256.IsHardwareAccelerated}.");
		Measure("PNG decode", () => _ = PngCodec.Decode(png));
		Measure("JPEG encode Q90", () => _ = JpegEncoder.Encode(image, 90, []));
		Measure("JPEG color conversion", () => ReadColorBlocks(image));
		Measure("JPEG transform and quantization", () => QuantizeBlocks(image));
		Measure("PNG encode", () => _ = PngCodec.Encode(image, []));
		var large = new RasterImage(960, 640);
		SyntheticImageCorpus.Pixels("texture", large.Width, large.Height).CopyTo(large.Pixels, 0);
		using var reference = new ImageSharpCodecReference(TestContext);
		var jpeg = reference.Processor(reference.Encoder(ImageFormats.Jpeg, 90))(CreatePng(large.Width, large.Height, large.Pixels));
		Measure("JPEG decode 960x640", () => _ = JpegCodec.Decode(jpeg));
		Measure("JPEG area thumbnail 600x400", () => _ = JpegSequentialCodec.DecodeRows(jpeg, 600, 400, ImageProcessingLimits.Default));
		Measure("RGBA area thumbnail 600x400", () => _ = large.Downscale(600, 400));
	}

	[TestMethod]
	[DataRow("bands")]
	[DataRow("gradient")]
	[DataRow("lines")]
	[DataRow("texture")]
	[DataRow("noise")]
	[DataRow("alpha")]
	public void PatternStages_ReportDecodeEncodeAndThumbnailCosts(string pattern)
	{
		var image = new RasterImage(512, 384);
		SyntheticImageCorpus.Pixels(pattern, image.Width, image.Height).CopyTo(image.Pixels, 0);
		var png = CreatePng(image.Width, image.Height, image.Pixels);
		Measure("PNG decode", () => _ = PngCodec.Decode(png));
		Measure("JPEG encode Q90", () => _ = JpegEncoder.Encode(image, 90, []));
		Measure("PNG encode", () => _ = PngCodec.Encode(image, []));
		Measure("JPEG transform and quantization", () => QuantizeBlocks(image));
		using var reference = new ImageSharpCodecReference(TestContext);
		var large = new RasterImage(960, 640);
		SyntheticImageCorpus.Pixels(pattern, large.Width, large.Height).CopyTo(large.Pixels, 0);
		var jpeg = reference.Processor(reference.Encoder(ImageFormats.Jpeg, 90))(CreatePng(large.Width, large.Height, large.Pixels));
		Measure("JPEG decode 960x640", () => _ = JpegCodec.Decode(jpeg));
		Measure("JPEG area thumbnail 100x66", () => _ = JpegSequentialCodec.DecodeRows(jpeg, 100, 66, ImageProcessingLimits.Default));
		Measure("RGBA area thumbnail 600x400", () => _ = large.Downscale(600, 400));
	}

	private void Measure(string name, Action action)
	{
		for (var i = 0; i < 32; i++) action();
		var samples = new double[5];
		for (var i = 0; i < samples.Length; i++)
		{
			CancellationToken.ThrowIfCancellationRequested();
			var start = Stopwatch.GetTimestamp();
			action();
			samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
		}
		TestContext.WriteLine($"{name}: {samples.Order().ElementAt(2):F3} ms.");
	}

	private static void ReadColorBlocks(RasterImage image)
	{
		Span<double> y = stackalloc double[64];
		Span<double> cb = stackalloc double[64];
		Span<double> cr = stackalloc double[64];
		for (var top = 0; top < image.Height; top += 8)
			for (var left = 0; left < image.Width; left += 8) JpegEncoder.ReadBlock(image, left, top, y, cb, cr);
	}

	private static void QuantizeBlocks(RasterImage image)
	{
		Span<double> y = stackalloc double[64];
		Span<double> cb = stackalloc double[64];
		Span<double> cr = stackalloc double[64];
		Span<short> quantized = stackalloc short[64];
		var factors = Enumerable.Repeat(1.0 / 12, 64).ToArray();
		for (var top = 0; top < image.Height; top += 8)
			for (var left = 0; left < image.Width; left += 8)
			{
				JpegEncoder.ReadBlock(image, left, top, y, cb, cr);
				JpegEncoder.QuantizeBlock(y, factors, quantized);
				JpegEncoder.QuantizeBlock(cb, factors, quantized);
				JpegEncoder.QuantizeBlock(cr, factors, quantized);
			}
	}
}
#endif
