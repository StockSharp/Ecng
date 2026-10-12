namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class AvifGridMemoryTests : BaseTestClass
{
	[TestMethod]
	[DataRow("grid-alpha-512x384", 384, 24)]
	[DataRow("grid-alpha-512x4096", 4096, 256)]
	[DataRow("grid-alpha-420-10bit", 384, 24)]
	public void GridThumbnail_ReadsTilesWithinOneMegabyteAndPreservesIndependentAlphaPixels(string name, int height, int outputHeight)
	{
		var input = ImageDebtReproductionTests.Fixture(name + ".avif");
		var reference = ImageResult.FromMemory(ImageDebtReproductionTests.Fixture(name + ".png"), ColorComponents.RedGreenBlueAlpha).Data;
		var expected = ImageResizeReference.AreaAverage(reference, 512, height, 32, outputHeight);
		var limits = new ImageProcessingLimits(32 * outputHeight, 512, 100_000_000, 1024 * 1024);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng(limits));
		var thumbnail = input.ResizeImage(32, outputHeight, ImageFormats.Png, limits);
		var scores = ImageQualityMetrics.Measure(expected, ReadPng(thumbnail, 32, outputHeight), 32, outputHeight);
		IsTrue(scores.MaxRgbError <= 2, $"Maximum color error {scores.MaxRgbError}.");
		IsTrue(scores.AlphaMae <= 1, $"Alpha error {scores.AlphaMae}.");
	}

	[TestMethod]
	public void GridThumbnail_RefusesWhenEvenOneTileExceedsTheWorkingBudget()
	{
		var input = ImageDebtReproductionTests.Fixture("grid-alpha-512x384.avif");
		var limits = new ImageProcessingLimits(768, 512, 100_000_000, 128 * 1024);
		ThrowsExactly<InvalidDataException>(() => input.ResizeImage(32, 24, ImageFormats.Png, limits));
	}
}
