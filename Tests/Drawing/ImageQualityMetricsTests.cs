namespace Ecng.Tests.Drawing;

using static ImageTestHelper;

[TestClass]
public class ImageQualityMetricsTests : BaseTestClass
{
	[TestMethod]
	public void IdenticalPixelsHaveNoErrorAndPerfectSimilarity()
	{
		var pixels = Rgba(32, 24, (x, y) => ((byte)(x * 7), (byte)(y * 9), 80, 255));
		var result = ImageQualityMetrics.Measure(pixels, pixels.ToArray(), 32, 24);
		AreEqual(0.0, result.RgbMae);
		AreEqual(0.0, result.RgbRmse);
		IsTrue(double.IsPositiveInfinity(result.RgbPsnr));
		IsTrue(Math.Abs(result.LumaSsim - 1) < 1e-12);
	}

	[TestMethod]
	public void BlackAndWhiteMatchAnalyticalErrorsAndSsim()
	{
		var result = ImageQualityMetrics.Measure(Rgba(16, 16, (_, _) => (0, 0, 0, 255)), Rgba(16, 16, (_, _) => (255, 255, 255, 255)), 16, 16);
		AreEqual(255.0, result.RgbMae);
		AreEqual(255.0, result.RgbRmse);
		AreEqual(0.0, result.RgbPsnr);
		IsTrue(Math.Abs(result.LumaSsim - 6.5025 / (65025 + 6.5025)) < 1e-12);
	}

	[TestMethod]
	public void SingleChannelErrorUsesThreeRgbChannels()
	{
		var result = ImageQualityMetrics.Measure(Rgba(16, 16, (_, _) => (0, 0, 0, 255)), Rgba(16, 16, (_, _) => (255, 0, 0, 255)), 16, 16);
		AreEqual(85.0, result.RgbMae);
		IsTrue(Math.Abs(result.RgbPsnr - 10 * Math.Log10(3)) < 1e-12);
		AreEqual(0.0, result.AlphaMae);
	}

	[TestMethod]
	public void TransparentRgbIsIgnoredButAlphaErrorIsMeasured()
	{
		var first = Rgba(16, 16, (_, _) => (255, 0, 0, 0));
		var second = Rgba(16, 16, (_, _) => (0, 255, 0, 0));
		AreEqual(0.0, ImageQualityMetrics.Measure(first, second, 16, 16).RgbMae);
		AreEqual(128.0, ImageQualityMetrics.Measure(first, Rgba(16, 16, (_, _) => (0, 0, 0, 128)), 16, 16).AlphaMae);
	}
}
