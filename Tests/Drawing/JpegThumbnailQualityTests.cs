namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class JpegThumbnailQualityTests : BaseTestClass
{
	[TestMethod]
	[DataRow("lines", 100)]
	[DataRow("noise", 100)]
	[DataRow("gradient", 100)]
	[DataRow("texture", 100)]
	[DataRow("lines", 600)]
	public void Thumbnail_MatchesDirectAreaAverageOfIndependentlyDecodedPixels(string pattern, int bounds)
	{
		const int width = 960;
		const int height = 640;
		var input = CreatePng(width, height, SyntheticImageCorpus.Pixels(pattern, width, height)).ResizeImage(width, height, ImageFormats.Jpeg);
		var expected = ImageResult.FromMemory(input, ColorComponents.RedGreenBlueAlpha).Data;
		var outputHeight = height * bounds / width;
		expected = ImageResizeReference.AreaAverage(expected, width, height, bounds, outputHeight);
		var actual = ReadPng(input.ResizeImage(bounds, bounds, ImageFormats.Png), bounds, outputHeight);
		var scores = ImageQualityMetrics.Measure(expected, actual, bounds, outputHeight);
		IsTrue(scores.MaxRgbError <= 2, $"{pattern}: maximum error {scores.MaxRgbError}, SSIM {scores.LumaSsim}.");
	}
}
