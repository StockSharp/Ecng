namespace Ecng.Tests.Drawing;

#if NET10_0
using System.Diagnostics;
using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
[TestCategory("Integration")]
[TestCategory("DrawingBenchmark")]
[DoNotParallelize]
public class ImageCodecBenchmarkTests : BaseTestClass
{
	private const int _samples = 9;
	private const double _maximumTimeRatio = 1.25;
	private const double _timeAllowanceMilliseconds = 0.25;

	public static IEnumerable<object[]> CorpusCases()
	{
		foreach (var pattern in SyntheticImageCorpus.Patterns)
			foreach (var size in new[] { new Size(96, 64), new Size(960, 640) })
				foreach (var format in new[] { "png", "jpeg", "bmp", "gif", "webp-lossless", "webp-lossy" })
					yield return [pattern, size.Width, size.Height, format];
	}

	public static IEnumerable<object[]> QualityCases()
	{
		foreach (var pattern in SyntheticImageCorpus.Patterns.Where(p => p != "alpha"))
			foreach (var quality in new[] { 30, 50, 70, 85, 90, 95, 100 })
				yield return [pattern, quality];
	}

	public static IEnumerable<object[]> LosslessCases()
	{
		foreach (var pattern in SyntheticImageCorpus.Patterns)
			foreach (var format in new[] { ImageFormats.Png, ImageFormats.Bmp, ImageFormats.Webp })
				yield return [pattern, format];
	}

	public static IEnumerable<object[]> WebpQualityCases()
	{
		foreach (var pattern in SyntheticImageCorpus.Patterns)
			foreach (var quality in new[] { 30, 50, 70, 85, 90, 95, 100 }) yield return [pattern, quality];
	}

	public static IEnumerable<object[]> EncodingStageCases()
	{
		foreach (var pattern in SyntheticImageCorpus.Patterns)
			foreach (var format in pattern == "alpha" ? new[] { ImageFormats.Png } : new[] { ImageFormats.Png, ImageFormats.Jpeg })
				yield return [pattern, format];
	}

	public static IEnumerable<object[]> PipelineCases()
	{
		foreach (var pattern in SyntheticImageCorpus.Patterns)
			foreach (var input in pattern == "alpha" ? new[] { "png" } : new[] { "png", "jpeg" })
			{
				yield return [pattern, input, 0, ImageFormats.Png];
				foreach (var bounds in new[] { 100, 600 })
					foreach (var output in pattern == "alpha" ? new[] { ImageFormats.Png } : new[] { ImageFormats.Png, ImageFormats.Jpeg })
						yield return [pattern, input, bounds, output];
			}
	}

	[TestMethod]
	[DynamicData(nameof(CorpusCases))]
	public void GeneratedCorpus_DecodedPixelsMatchIndependentDecoder(string pattern, int width, int height, string format)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		var original = SyntheticImageCorpus.Pixels(pattern, width, height);
		var source = Source(reference, pattern, width, height, format);
		var expected = reference.Decode(source, out var size);
		AreEqual(new Size(width, height), size);
		AreEqual(size, source.GetImageSize());
		var decoded = ReadPng(source.ConvertToPng(), width, height);
		var tolerance = format is "jpeg" or "webp-lossy" ? 2 : 0;
		AssertPixels(expected, decoded, width, tolerance: tolerance);
		var directory = ImageBenchmarkArtifacts.DirectoryFor(TestContext);
		var name = $"corpus-{pattern}-{width}x{height}-{format}";
		File.WriteAllBytes(Path.Combine(directory, name + ".png"), CreatePng(width, height, original));
		File.WriteAllBytes(Path.Combine(directory, name + "-encoded." + (format.StartsWith("webp") ? "webp" : format)), source);
		TestContext.WriteLine($"{name}: {source.Length} bytes, independently decoded {width * height} RGBA pixels; ImageSharp {reference.Version}.");
	}

	[TestMethod]
	[TestCategory("DrawingPerformance")]
	[DynamicData(nameof(QualityCases))]
	public void JpegQuality_ReportsSizeFidelityAndSerialEncodingTime(string pattern, int quality)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		const int width = 512;
		const int height = 384;
		var expected = SyntheticImageCorpus.Pixels(pattern, width, height);
		var source = CreatePng(width, height, expected);
		var row = Run(reference, $"quality-{pattern}-jpeg-q{quality}", pattern, "png", "Encode", ImageFormats.Jpeg, quality, source, expected, width, height,
			() => source.ResizeImage(width, height, ImageFormats.Jpeg, quality), reference.Processor(reference.Encoder(ImageFormats.Jpeg, quality)));
		AssertPerformance(row);
	}

	[TestMethod]
	[TestCategory("DrawingPerformance")]
	[DynamicData(nameof(LosslessCases))]
	public void LosslessEncoding_ReportsSizeAndSpeedWithoutPixelLoss(string pattern, ImageFormats format)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		const int width = 512;
		const int height = 384;
		var expected = SyntheticImageCorpus.Pixels(pattern, width, height);
		var source = CreatePng(width, height, expected);
		var row = Run(reference, $"lossless-{pattern}-{format.ToString().ToLowerInvariant()}", pattern, "png", "Encode", format, 0, source, expected, width, height,
			() => format == ImageFormats.Png ? source.ConvertToPng() : source.ResizeImage(width, height, format), reference.Processor(reference.Encoder(format)));
		AreEqual(0.0, row.EcngQuality.RgbMae);
		AreEqual(0.0, row.ReferenceQuality.RgbMae);
		AreEqual(0.0, row.EcngQuality.AlphaMae);
		AreEqual(0.0, row.ReferenceQuality.AlphaMae);
		AssertPerformance(row);
	}

	[TestMethod]
	[TestCategory("DrawingPerformance")]
	[DynamicData(nameof(WebpQualityCases))]
	public void WebpQuality_ReportsSizeFidelityAndSerialEncodingTime(string pattern, int quality)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		const int width = 512;
		const int height = 384;
		var expected = SyntheticImageCorpus.Pixels(pattern, width, height);
		var source = CreatePng(width, height, expected);
		var options = new WebpEncodingOptions(WebpCompressionModes.Lossy, quality);
		var row = Run(reference, $"quality-{pattern}-webp-q{quality}", pattern, "png", "EncodeLossy", ImageFormats.Webp, quality, source, expected, width, height,
			() => source.ResizeImage(width, height, ImageFormats.Webp, options), reference.Processor(reference.Encoder(ImageFormats.Webp, quality, false)));
		AssertPerformance(row);
	}

	[TestMethod]
	[TestCategory("DrawingPerformance")]
	[DynamicData(nameof(PipelineCases))]
	public void ImagePipeline_UsesMatchingOutputFormatGeometryAndQuality(string pattern, string input, int bounds, ImageFormats output)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		const int sourceWidth = 960;
		const int sourceHeight = 640;
		var source = Source(reference, pattern, sourceWidth, sourceHeight, input);
		var pixels = reference.Decode(source, out _);
		var width = bounds == 0 ? sourceWidth : bounds;
		var height = bounds == 0 ? sourceHeight : sourceHeight * bounds / sourceWidth;
		var ideal = bounds == 0 ? pixels : ImageResizeReference.AreaAverage(pixels, sourceWidth, sourceHeight, width, height);
		var row = Run(reference, $"pipeline-{pattern}-{input}-{bounds}-{output.ToString().ToLowerInvariant()}", pattern, input,
			bounds == 0 ? "Normalize" : "Resize", output, output == ImageFormats.Jpeg ? 90 : 0, source, ideal, width, height,
			() => bounds == 0 ? source.ConvertToPng() : source.ResizeImage(bounds, bounds, output, 90), reference.Processor(reference.Encoder(output), bounds == 0 ? 0 : width, height));
		AssertPerformance(row);
	}

	[TestMethod]
	[TestCategory("DrawingPerformance")]
	[DataRow(256, 192)]
	[DataRow(512, 384)]
	public void AnimationConversion_MeasuresAllDisplayedFramesInBothImplementations(int width, int height)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		var builder = new GifBuilder(width, height, GifBuilder.GrayPalette(2)).Loop(2);
		builder.Frame(0, 0, width, height, new byte[width * height], delay: 4);
		for (var frame = 1; frame < 20; frame++)
			builder.Frame(frame * 11 % (width - 4), frame * 7 % (height - 4), 4, 4, Enumerable.Repeat((byte)1, 16).ToArray(), delay: frame + 4);
		var source = builder.Build();
		var previous = reference.Processor(reference.Encoder(ImageFormats.Png));
		var row = Run(reference, $"animation-{width}x{height}-gif-png", "animation", "gif", "ConvertAnimation", ImageFormats.Png, 0,
			source, Rgba(width, height, (_, _) => (0, 0, 0, 255)), width, height, () => source.ConvertToPng(), previous);
		var current = source.ConvertToPng();
		var independent = previous(source);
		AreEqual(20, reference.GetFrameCount(current));
		AreEqual(20, reference.GetFrameCount(independent));
		AreEqual(3, current.GetAnimationLoopCount());
		var expected = reference.DecodeFrames(source);
		var currentFrames = reference.DecodeFrames(current);
		var independentFrames = reference.DecodeFrames(independent);
		for (var frame = 0; frame < expected.Count; frame++)
		{
			AssertPixels(expected[frame], currentFrames[frame], width);
			AssertPixels(expected[frame], independentFrames[frame], width);
		}
		AssertPerformance(row);
	}

	[TestMethod]
	[TestCategory("DrawingPerformance")]
	[DynamicData(nameof(EncodingStageCases))]
	public void EncodingStages_StayWithinTheReferenceTimeBudget(string pattern, ImageFormats format)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		const int width = 512;
		const int height = 384;
		var image = new RasterImage(width, height);
		SyntheticImageCorpus.Pixels(pattern, width, height).CopyTo(image.Pixels, 0);
		var source = CreatePng(width, height, image.Pixels);
		var encoder = reference.PreparedEncoder(source, reference.Encoder(format));
		var row = Run(reference, $"stage-{pattern}-encode-{format.ToString().ToLowerInvariant()}", pattern, "rgba", "EncodeStage",
			format, format == ImageFormats.Jpeg ? 90 : 0, source, image.Pixels, width, height,
			() => format == ImageFormats.Jpeg ? JpegEncoder.Encode(image, 90, []) : PngCodec.Encode(image, []), _ => encoder());
		if (format == ImageFormats.Png)
		{
			AreEqual(0.0, row.EcngQuality.RgbMae);
			AreEqual(0.0, row.EcngQuality.AlphaMae);
		}
		AssertPerformance(row);
	}

	private byte[] Source(ImageSharpCodecReference reference, string pattern, int width, int height, string format)
	{
		var png = CreatePng(width, height, SyntheticImageCorpus.Pixels(pattern, width, height));
		if (format == "png") return png;
		var target = format switch { "jpeg" => ImageFormats.Jpeg, "bmp" => ImageFormats.Bmp, "gif" => ImageFormats.Gif, _ => ImageFormats.Webp };
		return reference.Processor(reference.Encoder(target, 90, format != "webp-lossy"))(png);
	}

	private ImageBenchmarkRow Run(ImageSharpCodecReference reference, string name, string pattern, string input, string operation, ImageFormats format, int quality,
		byte[] source, byte[] expected, int width, int height, Func<byte[]> current, Func<byte[], byte[]> previous)
	{
		CancellationToken.ThrowIfCancellationRequested();
		var started = Stopwatch.GetTimestamp();
		var currentElapsed = 0.0;
		var referenceElapsed = 0.0;
		Func<byte[]> referenceAction = () => previous(source);
		for (var warmup = 0; warmup < 32; warmup++)
		{
			CancellationToken.ThrowIfCancellationRequested();
			currentElapsed = Measure(current, 1, out _).Milliseconds;
			referenceElapsed = Measure(referenceAction, 1, out _).Milliseconds;
			if (warmup >= 3 && Stopwatch.GetElapsedTime(started).TotalMilliseconds >= 250)
				break;
		}
		var iterations = Math.Clamp((int)Math.Ceiling(100 / Math.Max(0.001, Math.Min(currentElapsed, referenceElapsed))), 1, 64);
		var ours = new Sample[_samples];
		var theirs = new Sample[_samples];
		byte[] currentBytes = null;
		byte[] previousBytes = null;
		for (var sample = 0; sample < _samples; sample++)
		{
			CancellationToken.ThrowIfCancellationRequested();
			if (sample % 2 == 0)
			{
				ours[sample] = Measure(current, iterations, out currentBytes);
				theirs[sample] = Measure(referenceAction, iterations, out previousBytes);
			}
			else
			{
				theirs[sample] = Measure(referenceAction, iterations, out previousBytes);
				ours[sample] = Measure(current, iterations, out currentBytes);
			}
		}
		var currentPixels = reference.Decode(currentBytes, out var currentSize);
		var previousPixels = reference.Decode(previousBytes, out var previousSize);
		AreEqual(new Size(width, height), currentSize);
		AreEqual(currentSize, previousSize);
		AreEqual(format, currentBytes.GetImageFormat());
		AreEqual(format, previousBytes.GetImageFormat());
		var row = new ImageBenchmarkRow(name, pattern, input, operation, format.ToString(), quality, width, height,
			source.Length, currentBytes.Length, previousBytes.Length, Median(ours.Select(s => s.Milliseconds)), Median(theirs.Select(s => s.Milliseconds)),
			ours.Max(s => s.Milliseconds), theirs.Max(s => s.Milliseconds), (long)Median(ours.Select(s => (double)s.AllocatedBytes)), (long)Median(theirs.Select(s => (double)s.AllocatedBytes)),
			ImageQualityMetrics.Measure(expected, currentPixels, width, height), ImageQualityMetrics.Measure(expected, previousPixels, width, height), reference.Version);
		ImageBenchmarkArtifacts.Write(TestContext, row, source, currentBytes, previousBytes);
		return row;
	}

	private void AssertPerformance(ImageBenchmarkRow row)
	{
		var maximum = row.ReferenceMedianMs * _maximumTimeRatio + _timeAllowanceMilliseconds;
		IsTrue(row.EcngMedianMs <= maximum,
			$"{row.Case}: Ecng {row.EcngMedianMs:F4} ms; ImageSharp {row.ReferenceMedianMs:F4} ms; " +
			$"budget {maximum:F4} ms (reference x {_maximumTimeRatio} + {_timeAllowanceMilliseconds} ms). " +
			"Nine alternating batches after warmup; metrics and outputs are saved before this assertion.");
	}

	private static Sample Measure(Func<byte[]> action, int iterations, out byte[] result)
	{
		result = null;
		var allocated = GC.GetAllocatedBytesForCurrentThread();
		var start = Stopwatch.GetTimestamp();
		for (var iteration = 0; iteration < iterations; iteration++)
			result = action();
		var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds / iterations;
		return new(elapsed, (GC.GetAllocatedBytesForCurrentThread() - allocated) / iterations);
	}

	private static double Median(IEnumerable<double> values) => values.Order().ElementAt(_samples / 2);

	private sealed record Sample(double Milliseconds, long AllocatedBytes);
}
#endif
