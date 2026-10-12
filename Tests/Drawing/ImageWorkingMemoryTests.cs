namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class ImageWorkingMemoryTests : BaseTestClass
{
	[TestMethod]
	public void FullDecode_EnforcesWorkingBytesIndependentlyOfPixelBudget()
	{
		var input = CreatePng(32, 24, Rgba(32, 24, (_, _) => (1, 2, 3, 255)));
		var limits = new ImageProcessingLimits(25_000_000, 512, 100_000_000, 1024);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng(limits));
	}

	[TestMethod]
	public void PngThumbnail_AccountsForSourceScanlinesEvenWhenTheDestinationFits()
	{
		var input = CreatePng(32, 24, Rgba(32, 24, (_, _) => (1, 2, 3, 255)));
		var limits = new ImageProcessingLimits(256, 512, 100_000_000, 256);
		ThrowsExactly<InvalidDataException>(() => input.ResizeImage(1, 1, ImageFormats.Png, limits));
	}

	[TestMethod]
	[DataRow("lossless.webp")]
	[DataRow("lossy.webp")]
	[DataRow("alpha.webp")]
	[DataRow("color-avif-8.avif")]
	[DataRow("color-avif-10.avif")]
	[DataRow("multiscan.jpg")]
	[DataRow("12bit-progressive.jpg")]
	[DataRow("arithmetic.jpg")]
	public void Thumbnail_RejectsSourceWorkingBuffersAboveTheExplicitBudget(string fixture)
	{
		var input = ImageDebtReproductionTests.Fixture(fixture);
		var limits = new ImageProcessingLimits(256, 512, 100_000_000, 256);
		ThrowsExactly<InvalidDataException>(() => input.ResizeImage(1, 1, ImageFormats.Png, limits));
	}

	[TestMethod]
	[DataRow(0L)]
	[DataRow(-1L)]
	public void NonPositiveWorkingBudget_IsRejected(long bytes)
		=> ThrowsExactly<ArgumentOutOfRangeException>(() => new ImageProcessingLimits(25_000_000, 512, 100_000_000, bytes));
}
