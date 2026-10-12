namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class WebpCompressionTests : BaseTestClass
{
	public static IEnumerable<object[]> PackingCases()
	{
		yield return [1, 1, 1];
		yield return [1, 17, 2];
		yield return [17, 1, 2];
		yield return [13, 7, 4];
		yield return [13, 19, 16];
		yield return [17, 17, 256];
		yield return [17, 17, 257];
	}

	[TestMethod]
	[DynamicData(nameof(PackingCases))]
	public void PaletteAndPredictorEdgesPreserveEveryRgbaByte(int width, int height, int colors)
	{
		var pixels = PackingPixels(width, height, colors);
		var input = CreatePng(width, height, pixels);
		var encoded = input.ResizeImage(width, height, ImageFormats.Webp);
		AssertPixels(pixels, ReadPng(encoded.ConvertToPng(), width, height), width);
	}

#if NET10_0
	[TestMethod]
	[TestCategory("Integration")]
	[DynamicData(nameof(PackingCases))]
	public void PaletteAndPredictorEdgesMatchIndependentWebpDecoder(int width, int height, int colors)
	{
		using var reference = new ImageSharpCodecReference(TestContext);
		var pixels = PackingPixels(width, height, colors);
		var input = CreatePng(width, height, pixels);
		var encoded = input.ResizeImage(width, height, ImageFormats.Webp);
		AssertPixels(pixels, reference.Decode(encoded, out var size), width);
		AreEqual(new System.Drawing.Size(width, height), size);
	}
#endif

	private static byte[] PackingPixels(int width, int height, int colors)
		=> Rgba(width, height, (x, y) =>
		{
			var color = (y * width + x) % colors;
			return ((byte)color, (byte)(color >> 8), (byte)(color * 37), (byte)(color % 3 == 0 ? 0 : color * 13 + 17));
		});

	[TestMethod]
	[DataRow("bands")]
	[DataRow("gradient")]
	[DataRow("lines")]
	[DataRow("texture")]
	[DataRow("noise")]
	[DataRow("alpha")]
	public void LosslessWebp_CompressesKnownPixelsAtLeastAsWellAsPng(string pattern)
	{
		const int width = 512;
		const int height = 384;
		var pixels = SyntheticImageCorpus.Pixels(pattern, width, height);
		var png = CreatePng(width, height, pixels);
		var encoded = png.ResizeImage(width, height, ImageFormats.Webp);
		AssertPixels(pixels, ReadPng(encoded.ConvertToPng(), width, height), width);
		var compressedPng = png.ConvertToPng();
		IsTrue(encoded.Length <= compressedPng.Length, $"{pattern}: lossless WebP {encoded.Length} bytes exceeds PNG {compressedPng.Length} bytes.");
	}
}
