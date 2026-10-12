namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class ImageAreaTileWindowTests : BaseTestClass
{
	[TestMethod]
	[DataRow(64, 96, 16, 24)]
	[DataRow(49, 41, 13, 13)]
	public void TileRows_PreserveFractionalCoverageAcrossWindowWraps(int width, int height, int outputWidth, int outputHeight)
	{
		var source = SyntheticImageCorpus.Pixels("alpha", width, height);
		const int tileHeight = 13;
		var retained = Math.Min(outputHeight, (tileHeight * outputHeight + height - 1) / height + 1);
		var accumulator = new ImageAreaAccumulator(width, height, outputWidth, outputHeight, retained, ImageProcessingLimits.Default);
		for (var top = 0; top < height; top += tileHeight)
		{
			var bottom = Math.Min(height, top + tileHeight);
			for (var left = 0; left < width; left += 11)
			{
				var columns = Math.Min(11, width - left);
				for (var y = top; y < bottom; y++)
					accumulator.AddRow(source.AsSpan((y * width + left) * 4, columns * 4), left, y, 1);
			}
			accumulator.CompleteThrough(bottom);
		}
		accumulator.Complete();
		var expected = ImageResizeReference.AreaAverage(source, width, height, outputWidth, outputHeight);
		var scores = ImageQualityMetrics.Measure(expected, accumulator.Image.Pixels, outputWidth, outputHeight);
		IsTrue(scores.MaxRgbError <= 1);
		IsTrue(scores.AlphaMae <= 1);
	}

	[TestMethod]
	public void TileRows_RefuseToOverwriteUnfinishedOrCompletedRows()
	{
		var accumulator = new ImageAreaAccumulator(4, 4, 4, 4, 1, ImageProcessingLimits.Default);
		var row = Rgba(4, 1, (_, _) => (10, 20, 30, 255));
		ThrowsExactly<InvalidDataException>(() => accumulator.AddRow(row, 0, 1, 1));
		accumulator.AddRow(row, 0, 0, 1);
		accumulator.CompleteThrough(1);
		ThrowsExactly<InvalidDataException>(() => accumulator.AddRow(row, 0, 0, 1));
	}
}
