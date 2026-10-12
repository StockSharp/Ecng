namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

[TestClass]
public class JpegEncoderCacheTests : BaseTestClass
{
	[TestMethod]
	[DataRow("bands", 513, 385)]
	[DataRow("bands", 1025, 769)]
	[DataRow("bands", 2049, 769)]
	[DataRow("noise", 513, 385)]
	[DataRow("noise", 1025, 769)]
	[DataRow("noise", 2049, 769)]
	public void EncoderStorageSizes_PreserveIndependentPixelsAndDeterministicReuse(string pattern, int width, int height)
	{
		var image = new RasterImage(width, height);
		SyntheticImageCorpus.Pixels(pattern, width, height).CopyTo(image.Pixels, 0);
		var first = JpegEncoder.Encode(image, 100, []);
		var decoded = ImageResult.FromMemory(first, ColorComponents.RedGreenBlueAlpha);
		AreEqual(width, decoded.Width);
		AreEqual(height, decoded.Height);
		// Quality 100 still quantizes transformed coefficients; noise can differ by four RGB levels.
		AssertPixels(image.Pixels, decoded.Data, width, tolerance: pattern == "noise" ? 4 : 2);
		AssertPixels(JpegCodec.Decode(first).Pixels, decoded.Data, width, tolerance: pattern == "noise" ? 3 : 2);
		var other = new RasterImage(width, height);
		Rgba(width, height, (_, _) => (80, 140, 200, 255)).CopyTo(other.Pixels, 0);
		JpegEncoder.Encode(other, 30, []);
		IsTrue(first.AsSpan().SequenceEqual(JpegEncoder.Encode(image, 100, [])), "Reusing an encoder buffer changed the encoded JPEG.");
	}
}
