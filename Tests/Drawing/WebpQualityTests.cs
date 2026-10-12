namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class WebpQualityTests : BaseTestClass
{
	[TestMethod]
	public void LossyQuality_ControlsSizeAndFidelity()
	{
		const int width = 512;
		const int height = 384;
		var pixels = SyntheticImageCorpus.Pixels("texture", width, height);
		var input = CreatePng(width, height, pixels);
		var low = input.ResizeImage(width, height, ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, 10));
		var high = input.ResizeImage(width, height, ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, 95));
		var lowScores = ImageQualityMetrics.Measure(pixels, ReadPng(low.ConvertToPng(), width, height), width, height);
		var highScores = ImageQualityMetrics.Measure(pixels, ReadPng(high.ConvertToPng(), width, height), width, height);
		IsTrue(high.Length > low.Length, $"Q10 {low.Length} bytes, Q95 {high.Length} bytes.");
		IsTrue(highScores.LumaSsim > lowScores.LumaSsim);
		IsTrue(highScores.RgbPsnr > lowScores.RgbPsnr + 2);
		TestContext.WriteLine($"Q10 {low.Length} bytes / {lowScores.RgbPsnr:F2} dB; Q95 {high.Length} bytes / {highScores.RgbPsnr:F2} dB.");
	}

	[TestMethod]
	public void LossyAlpha_PreservesEveryAlphaByteIncludingOddDimensions()
	{
		const int width = 163;
		const int height = 117;
		var pixels = SyntheticImageCorpus.Pixels("alpha", width, height);
		var input = CreatePng(width, height, pixels);
		var output = input.ResizeImage(width, height, ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, 85));
		var actual = ReadPng(output.ConvertToPng(), width, height);
		for (var i = 3; i < pixels.Length; i += 4) AreEqual(pixels[i], actual[i]);
	}

	[TestMethod]
	public void ExplicitLossyOptions_ReencodeAWebpThatAlreadyFits()
	{
		var input = CreatePng(64, 48, SyntheticImageCorpus.Pixels("texture", 64, 48)).ResizeImage(64, 48, ImageFormats.Webp);
		var output = input.ResizeImage(64, 48, ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, 50));
		AreNotEqual(Convert.ToHexString(input), Convert.ToHexString(output));
		AreEqual(input.GetImageSize(), output.GetImageSize());
	}

	[TestMethod]
	[DataRow(-1)]
	[DataRow(101)]
	public void InvalidQuality_IsRejected(int quality)
		=> ThrowsExactly<ArgumentOutOfRangeException>(() => new WebpEncodingOptions(WebpCompressionModes.Lossy, quality));

	[TestMethod]
	public void WatermarkOptions_ConvertToLossyEvenWhenTextIsInvisible()
	{
		var input = CreatePng(64, 48, SyntheticImageCorpus.Pixels("texture", 64, 48)).ResizeImage(64, 48, ImageFormats.Webp);
		var output = input.AddTextWatermark("I", ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, 75),
			opacity: 0, fontFamily: "A font that does not exist");
		AreNotEqual(Convert.ToHexString(input), Convert.ToHexString(output));
		AreEqual(input.GetImageSize(), output.GetImageSize());
	}

	[TestMethod]
	public void LossyAnimation_PreservesEveryFrameDelayAndLoopCount()
	{
		var input = AnimationSource();
		var output = input.ResizeImage(16, 12, ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, 90));
		var frames = output.GetAnimationFrames();
		AreEqual(2, frames.Count);
		AreEqual(70, frames[0].delayMilliseconds);
		AreEqual(110, frames[1].delayMilliseconds);
		AreEqual(3, output.GetAnimationLoopCount());
		for (var frame = 0; frame < frames.Count; frame++)
		{
			var pixels = ReadPng(frames[frame].png, 16, 12);
			var expected = frame == 0 ? 0 : 255;
			for (var i = 0; i < pixels.Length; i++)
				if (i % 4 == 3) AreEqual((byte)255, pixels[i]);
				else IsTrue(Math.Abs(pixels[i] - expected) <= 4);
		}
	}

	private static byte[] AnimationSource()
	{
		var builder = new GifBuilder(32, 24, GifBuilder.GrayPalette(2)).Loop(2);
		builder.Frame(0, 0, 32, 24, new byte[32 * 24], delay: 7);
		builder.Frame(0, 0, 32, 24, Enumerable.Repeat((byte)1, 32 * 24).ToArray(), delay: 11);
		return builder.Build();
	}

#if NET10_0
	[TestMethod]
	[TestCategory("Integration")]
	public void LossyAnimation_MatchesAnIndependentDecoder()
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		var output = AnimationSource().ResizeImage(16, 12, ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, 90));
		var actual = output.GetAnimationFrames();
		var expected = reference.DecodeFrames(output);
		AreEqual(2, expected.Count);
		for (var frame = 0; frame < expected.Count; frame++)
			AssertPixels(expected[frame], ReadPng(actual[frame].png, 16, 12), 16);
	}

	[TestMethod]
	[TestCategory("Integration")]
	[DataRow("texture", 10)]
	[DataRow("texture", 85)]
	[DataRow("gradient", 100)]
	[DataRow("alpha", 75)]
	public void LossyPixels_MatchAnIndependentWebpDecoder(string pattern, int quality)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		const int width = 163;
		const int height = 117;
		var input = CreatePng(width, height, SyntheticImageCorpus.Pixels(pattern, width, height));
		var output = input.ResizeImage(width, height, ImageFormats.Webp, new WebpEncodingOptions(WebpCompressionModes.Lossy, quality));
		File.WriteAllBytes(Path.Combine(ImageBenchmarkArtifacts.DirectoryFor(TestContext), $"webp-{pattern}-q{quality}.webp"), output);
		var expected = reference.Decode(output, out var size);
		AreEqual(input.GetImageSize(), size);
		var actual = ReadPng(output.ConvertToPng(), width, height);
		var scores = ImageQualityMetrics.Measure(expected, actual, width, height);
		IsTrue(scores.MaxRgbError <= 2, $"Maximum decoded color error {scores.MaxRgbError}.");
		AreEqual(0.0, scores.AlphaMae);
	}
#endif
}
