namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class ImageAdditionalCodecGoldenTests : BaseTestClass
{
	[TestMethod]
	[DataRow("color-lossless-1")]
	[DataRow("color-lossless-2")]
	[DataRow("color-lossless-3")]
	[DataRow("color-lossless-4")]
	[DataRow("color-lossless-5")]
	[DataRow("color-lossless-6")]
	[DataRow("color-lossless-7")]
	[DataRow("color-lossless-point")]
	[DataRow("color-lossless-restart")]
	public void LosslessJpeg_ColorMatchesLibjpegExactly(string name)
		=> AssertPixels(Reference(name + ".png"), ReadPng(Fixture(name + ".jpg").ConvertToPng(), 32, 24), 32);

	[TestMethod]
	[DataRow("color-arithmetic")]
	[DataRow("color-arithmetic-progressive")]
	[DataRow("color-12bit")]
	[DataRow("color-12bit-progressive")]
	public void DctJpeg_ColorMatchesLibjpeg(string name)
		=> AssertPixels(Reference(name + ".png"), ReadPng(Fixture(name + ".jpg").ConvertToPng(), 32, 24), 32, tolerance: 2);

	[TestMethod]
	[DataRow("lossless")]
	[DataRow("lossy")]
	public void Webp_ColorAndAlphaMatchLibwebp(string kind)
		=> AssertPixels(Reference("color-" + kind + "-webp.png"), ReadPng(Fixture("color-" + kind + ".webp").ConvertToPng(), 32, 24), 32,
			tolerance: kind == "lossy" ? 2 : 0);

	[TestMethod]
	[DataRow(8)]
	[DataRow(10)]
	public void Avif_ColorAndAlphaMatchLibavif(int depth)
		=> AssertPixels(Reference($"color-avif-{depth}.png"), ReadPng(Fixture($"color-avif-{depth}.avif").ConvertToPng(), 32, 24), 32, tolerance: depth == 8 ? 0 : 1);

	[TestMethod]
	public void Tiff_ColorAndAlphaMatchGdi()
		=> AssertPixels(Reference("color-tiff.png"), ReadPng(Fixture("color.tiff").ConvertToPng(), 32, 24), 32);

	[TestMethod]
	public void Webp_EncoderPreservesEveryRgbaByte()
	{
		var expected = Reference("color-lossless-webp.png");
		var source = CreatePng(32, 24, expected);
		var encoded = source.ResizeImage(32, 24, ImageFormats.Webp);
		AreEqual(ImageFormats.Webp, encoded.GetImageFormat());
		AssertPixels(expected, ReadPng(encoded.ConvertToPng(), 32, 24), 32);
		var path = Path.Combine(TestContext.ResultsDirectory, "encoded-color.webp");
		File.WriteAllBytes(path, encoded);
		TestContext.AddResultFile(path);
	}

	private static byte[] Fixture(string name) => ImageDebtReproductionTests.Fixture(name);
	private static byte[] Reference(string name) => ImageResult.FromMemory(Fixture(name), ColorComponents.RedGreenBlueAlpha).Data;
}
