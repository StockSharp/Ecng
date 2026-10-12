namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class JpegQualityControlTests : BaseTestClass
{
	[TestMethod]
	[DataRow(512, 384)]
	[DataRow(1600, 960)]
	public void SmallAndLargeEncodesPreserveQualityAgainstIndependentDecoder(int width, int height)
	{
		var pixels = SyntheticImageCorpus.Pixels("gradient", width, height);
		var image = new RasterImage(width, height);
		pixels.CopyTo(image.Pixels, 0);
		var output = JpegEncoder.Encode(image, 100, []);
		var decoded = ImageResult.FromMemory(output, ColorComponents.RedGreenBlueAlpha);
		AreEqual(width, decoded.Width);
		AreEqual(height, decoded.Height);
		var scores = ImageQualityMetrics.Measure(pixels, decoded.Data, width, height);
		IsTrue(scores.RgbPsnr >= 48, $"JPEG quality 100: {scores.RgbPsnr:F2} dB.");
	}

	[TestMethod]
	[DataRow("bands")]
	[DataRow("gradient")]
	[DataRow("lines")]
	[DataRow("texture")]
	[DataRow("noise")]
	public void HigherQualityImprovesReconstructionOfKnownPixels(string pattern)
	{
		var pixels = SyntheticImageCorpus.Pixels(pattern, 128, 96);
		var source = CreatePng(128, 96, pixels);
		var low = source.ResizeImage(128, 96, ImageFormats.Jpeg, jpegQuality: 30);
		var high = source.ResizeImage(128, 96, ImageFormats.Jpeg, jpegQuality: 100);
		var lowScores = ImageQualityMetrics.Measure(pixels, ImageResult.FromMemory(low, ColorComponents.RedGreenBlueAlpha).Data, 128, 96);
		var highScores = ImageQualityMetrics.Measure(pixels, ImageResult.FromMemory(high, ColorComponents.RedGreenBlueAlpha).Data, 128, 96);
		IsTrue(highScores.RgbRmse <= lowScores.RgbRmse);
		IsTrue(highScores.LumaSsim >= lowScores.LumaSsim);
		IsTrue(highScores.RgbMae < 2);
		TestContext.WriteLine($"Q30: {low.Length} bytes, {lowScores.RgbPsnr:F2} dB; Q100: {high.Length} bytes, {highScores.RgbPsnr:F2} dB.");
	}
}
