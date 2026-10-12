namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

[TestClass]
public class ImageAreaRowTests : BaseTestClass
{
	[TestMethod]
	[DataRow(1237, 600, 0, 1, 1237)]
	[DataRow(83, 23, 0, 1, 21)]
	[DataRow(83, 23, 17, 1, 31)]
	[DataRow(83, 23, 0, 4, 21)]
	public void HorizontalContributions_MatchAnIndependentIntegerIntegral(int width, int outputWidth, int start, int step, int count)
	{
		var pixels = SyntheticImageCorpus.Pixels("alpha", count, 1);
		var row = new ImageAreaRow(width, outputWidth);
		row.Sum(pixels, start, step);
		for (var x = 0; x < outputWidth; x++)
		{
			var sums = new long[4];
			for (var index = 0; index < count; index++)
			{
				var sourceX = start + index * step;
				var weight = Math.Max(0, Math.Min((x + 1L) * width, (sourceX + 1L) * outputWidth) - Math.Max((long)x * width, (long)sourceX * outputWidth));
				var alpha = pixels[index * 4 + 3] * weight;
				sums[0] += alpha;
				for (var channel = 0; channel < 3; channel++)
					sums[channel + 1] += pixels[index * 4 + channel] * alpha;
			}
			for (var channel = 0; channel < 4; channel++)
				AreEqual((double)sums[channel], row.Sums[x * 4 + channel]);
		}
	}
}
