namespace Ecng.Tests.Drawing;

[TestClass]
public class ImageResizeReferenceTests : BaseTestClass
{
	[TestMethod]
	public void AreaAverage_EqualOpaqueCoverage()
	{
		var pixels = ImageResizeReference.AreaAverage([255, 0, 0, 255, 0, 0, 255, 255], 2, 1, 1, 1);
		AreEqual((byte)128, pixels[0]);
		AreEqual((byte)0, pixels[1]);
		AreEqual((byte)128, pixels[2]);
		AreEqual((byte)255, pixels[3]);
	}

	[TestMethod]
	public void AreaAverage_FractionalCoverage()
	{
		var pixels = ImageResizeReference.AreaAverage([0, 0, 0, 255, 90, 0, 0, 255, 240, 0, 0, 255], 3, 1, 2, 1);
		AreEqual((byte)30, pixels[0]);
		AreEqual((byte)190, pixels[4]);
		AreEqual((byte)255, pixels[3]);
		AreEqual((byte)255, pixels[7]);
	}

	[TestMethod]
	public void AreaAverage_TransparentCoverageDoesNotTintOpaqueColor()
	{
		var pixels = ImageResizeReference.AreaAverage([255, 0, 0, 255, 0, 0, 255, 0], 2, 1, 1, 1);
		AreEqual((byte)255, pixels[0]);
		AreEqual((byte)0, pixels[1]);
		AreEqual((byte)0, pixels[2]);
		AreEqual((byte)128, pixels[3]);
	}
}
