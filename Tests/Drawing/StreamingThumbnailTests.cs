namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class StreamingThumbnailTests : BaseTestClass
{
	[TestMethod]
	[DataRow("multiscan.jpg")]
	[DataRow("arithmetic.jpg")]
	[DataRow("lossless.jpg")]
	[DataRow("12bit-progressive.jpg")]
	[DataRow("black.tiff")]
	[DataRow("lossless.webp")]
	[DataRow("lossy.webp")]
	[DataRow("alpha.webp")]
	public void Thumbnail_BudgetsTheDestinationWithoutAllocatingTheSourceCanvas(string fixture)
	{
		var input = ImageDebtReproductionTests.Fixture(fixture);
		var limits = new ImageProcessingLimits(256, 512, 100_000_000);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng(limits));
		var output = input.ResizeImage(16, 12, ImageFormats.Png, limits);
		AssertPixels(Rgba(16, 12, (_, _) => (0, 0, 0, fixture == "alpha.webp" ? (byte)64 : (byte)255)), ReadPng(output, 16, 12), 16);
	}

	[TestMethod]
	public void BmpThumbnail_MatchesAnIndependentAreaAverageUnderTheDestinationBudget()
	{
		const int width = 64;
		const int height = 48;
		var pixels = SyntheticImageCorpus.Pixels("alpha", width, height);
		var source = CreatePng(width, height, pixels).ResizeImage(width, height, ImageFormats.Bmp);
		var limits = new ImageProcessingLimits(256, 512, 100_000_000);
		ThrowsExactly<InvalidDataException>(() => source.ConvertToPng(limits));
		var actual = ReadPng(source.ResizeImage(16, 12, ImageFormats.Png, limits), 16, 12);
		AssertPixels(ImageResizeReference.AreaAverage(pixels, width, height, 16, 12), actual, 16);
	}

	[TestMethod]
	[DataRow("color-arithmetic")]
	[DataRow("color-arithmetic-progressive")]
	[DataRow("color-12bit-progressive")]
	public void ColoredJpegThumbnail_MatchesFrozenIndependentPixels(string name)
	{
		var input = ImageDebtReproductionTests.Fixture(name + ".jpg");
		var reference = ImageDebtReproductionTests.Fixture(name + ".png");
		var expected = ImageResult.FromMemory(reference, ColorComponents.RedGreenBlueAlpha).Data;
		expected = ImageResizeReference.AreaAverage(expected, 32, 24, 16, 12);
		var output = input.ResizeImage(16, 12, ImageFormats.Png, new ImageProcessingLimits(256, 512, 100_000_000));
		var actual = ReadPng(output, 16, 12);
		var scores = ImageQualityMetrics.Measure(expected, actual, 16, 12);
		IsTrue(scores.MaxRgbError <= 2, $"{name}: maximum error {scores.MaxRgbError}.");
	}

	[TestMethod]
	[DataRow("color-arithmetic-multiscan-tall")]
	[DataRow("color-lossless-multiscan-tall")]
	[DataRow("color-arithmetic-multiscan-restart-tall")]
	[DataRow("color-lossless-multiscan-restart-tall")]
	public void ComponentSeparatedJpeg_MatchesIndependentPixelsAcrossBufferWindows(string name)
	{
		var input = ImageDebtReproductionTests.Fixture(name + ".jpg");
		var expected = ImageResult.FromMemory(ImageDebtReproductionTests.Fixture(name + ".png"), ColorComponents.RedGreenBlueAlpha).Data;
		var full = ReadPng(input.ConvertToPng(), 64, 96);
		IsTrue(ImageQualityMetrics.Measure(expected, full, 64, 96).MaxRgbError <= 2);
		expected = ImageResizeReference.AreaAverage(expected, 64, 96, 16, 24);
		var limits = new ImageProcessingLimits(384, 512, 100_000_000, 16_384);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng(limits));
		var output = input.ResizeImage(16, 24, ImageFormats.Png, limits);
		var scores = ImageQualityMetrics.Measure(expected, ReadPng(output, 16, 24), 16, 24);
		IsTrue(scores.MaxRgbError <= 2, $"Maximum error {scores.MaxRgbError}.");
	}

	[TestMethod]
	public void LargeProgressiveJpeg_UsesRowStorageAndRetainsTheFullDecodeLimit()
	{
		var input = ImageDebtReproductionTests.Fixture("large-progressive-48mp.jpg");
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng());
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		var output = input.ResizeImage(100, 100, ImageFormats.Png);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
		AssertPixels(Rgba(100, 75, (_, _) => (0, 0, 0, 255)), ReadPng(output, 100, 75), 100);
		IsTrue(allocated < 8 * 1024 * 1024, $"Thumbnail allocated {allocated} bytes.");
	}

	[TestMethod]
	[DataRow("color-avif-8")]
	[DataRow("color-avif-10")]
	public void AvifThumbnail_MatchesFrozenIndependentPixelsUnderTheDestinationBudget(string name)
	{
		var input = ImageDebtReproductionTests.Fixture(name + ".avif");
		var source = ImageResult.FromMemory(ImageDebtReproductionTests.Fixture(name + ".png"), ColorComponents.RedGreenBlueAlpha).Data;
		var expected = ImageResizeReference.AreaAverage(source, 32, 24, 16, 12);
		var output = input.ResizeImage(16, 12, ImageFormats.Png, new ImageProcessingLimits(256, 512, 100_000_000));
		var scores = ImageQualityMetrics.Measure(expected, ReadPng(output, 16, 12), 16, 12);
		IsTrue(scores.MaxRgbError <= 2, $"{name}: maximum error {scores.MaxRgbError}.");
	}
}
