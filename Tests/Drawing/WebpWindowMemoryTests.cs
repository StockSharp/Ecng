namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class WebpWindowMemoryTests : BaseTestClass
{
	[TestMethod]
	[DataRow("tall-alpha-lossless", 4 * 1024 * 1024, 0)]
	[DataRow("tall-filtered-lossy", 128 * 1024, 2)]
	[DataRow("tall-filtered-alpha", 4 * 1024 * 1024, 2)]
	public void TallThumbnail_UsesBoundedHistoryAndMatchesLibwebp(string name, int budget, int tolerance)
	{
		var input = ImageDebtReproductionTests.Fixture(name + ".webp");
		var reference = ImageResult.FromMemory(ImageDebtReproductionTests.Fixture(name + ".png"), ColorComponents.RedGreenBlueAlpha).Data;
		var expected = ImageResizeReference.AreaAverage(reference, 512, 4096, 32, 256);
		var limits = new ImageProcessingLimits(8192, 512, 100_000_000, budget);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng(limits));
		var thumbnail = input.ResizeImage(32, 256, ImageFormats.Png, limits);
		var scores = ImageQualityMetrics.Measure(expected, ReadPng(thumbnail, 32, 256), 32, 256);
		IsTrue(scores.MaxRgbError <= tolerance, $"Maximum color error {scores.MaxRgbError}.");
		IsTrue(scores.AlphaMae == 0, $"Alpha error {scores.AlphaMae}.");
	}
}
