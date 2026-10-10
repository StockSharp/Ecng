namespace Ecng.Tests.Drawing;

using System.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// What <see cref="ImageHelper.ResizeImage"/> does to the pixels and to the size.
/// </summary>
[TestClass]
public class ImageResizeTests : BaseTestClass
{
	[TestMethod]
	public void ReducingFourTimes_AveragesEverySourcePixel()
	{
		// A white ring around a black 2x2 core: 12 of 16 pixels are white.
		var source = Rgba(4, 4, (x, y) => x is 1 or 2 && y is 1 or 2 ? ((byte)0, (byte)0, (byte)0, (byte)255) : ((byte)255, (byte)255, (byte)255, (byte)255));

		var result = ReadPng(CreatePng(4, 4, source).ResizeImage(1, 1), 1, 1);

		AssertPixels([191, 191, 191, 255], result, 1, tolerance: 1);
	}

	[TestMethod]
	public void ThinLines_KeepTheirShareOfBrightnessWhereverTheyFall()
	{
		// One white column in every sixteen: a sixteenth of full brightness after a 16:1 reduction.
		foreach (var phase in new[] { 0, 3, 7, 8, 15 })
		{
			var source = Rgba(160, 2, (x, _) => x % 16 == phase ? ((byte)255, (byte)255, (byte)255, (byte)255) : ((byte)0, (byte)0, (byte)0, (byte)255));

			var result = ReadPng(CreatePng(160, 2, source).ResizeImage(10, 1), 10, 1);

			AssertPixels(Rgba(10, 1, (_, _) => (16, 16, 16, 255)), result, 10, tolerance: 1);
		}
	}

	[TestMethod]
	public void AnyRatio_MatchesTheAreaAverage()
	{
		var random = new Random(4127);

		// Whole and fractional ratios, equal and - where a side is rounded down - different along the two axes.
		foreach (var (width, height, maxWidth, maxHeight) in new[] { (10, 10, 2, 2), (10, 10, 4, 4), (5, 5, 2, 2), (7, 3, 3, 1), (9, 6, 3, 2), (11, 13, 4, 5), (6, 6, 5, 5) })
		{
			var source = new byte[width * height * 4];
			random.NextBytes(source);

			var (toWidth, toHeight, result) = ReadPng(CreatePng(width, height, source).ResizeImage(maxWidth, maxHeight));

			AssertPixels(AreaAverage(source, width, height, toWidth, toHeight), result, toWidth, tolerance: 1);
		}
	}

	[TestMethod]
	public void TransparentPixels_GiveNoColorToTheirNeighbors()
	{
		// One opaque red pixel beside three transparent green ones.
		var source = Rgba(4, 1, (x, _) => x == 0 ? ((byte)255, (byte)0, (byte)0, (byte)255) : ((byte)0, (byte)255, (byte)0, (byte)0));

		var result = ReadPng(CreatePng(4, 1, source).ResizeImage(1, 1), 1, 1);

		AssertPixels([255, 0, 0, 64], result, 1, tolerance: 1);
	}

	[TestMethod]
	public void TargetSize_ReachesTheBoundExactly()
	{
		// Sizes whose ratio to the bound is not a binary fraction.
		foreach (var (width, height, maxWidth, maxHeight, expected) in new[]
		{
			(98, 10, 2, 1000, new Size(2, 1)),
			(4624, 3, 800, 800, new Size(800, 1)),
			(3, 4624, 800, 800, new Size(1, 800)),
			(4900, 7, 100, 100, new Size(100, 1)),
			(300, 200, 100, 100, new Size(100, 66)),
			(200, 300, 100, 100, new Size(66, 100)),
		})
		{
			var png = CreatePng(width, height, new byte[width * height * 4]);

			png.ResizeImage(maxWidth, maxHeight).GetImageSize().AssertEqual(expected, $"{width}x{height} into {maxWidth}x{maxHeight}");
		}
	}

	/// <summary>
	/// Every destination pixel is the mean of the source area it covers, colors weighted by alpha.
	/// </summary>
	private static byte[] AreaAverage(byte[] source, int width, int height, int toWidth, int toHeight)
	{
		var result = new byte[toWidth * toHeight * 4];

		for (var y = 0; y < toHeight; y++)
			for (var x = 0; x < toWidth; x++)
			{
				double left = (double)x * width / toWidth, right = (double)(x + 1) * width / toWidth;
				double top = (double)y * height / toHeight, bottom = (double)(y + 1) * height / toHeight;
				double alpha = 0, red = 0, green = 0, blue = 0, area = 0;

				for (var sy = 0; sy < height; sy++)
					for (var sx = 0; sx < width; sx++)
					{
						var weight = Math.Max(0, Math.Min(right, sx + 1) - Math.Max(left, sx)) * Math.Max(0, Math.Min(bottom, sy + 1) - Math.Max(top, sy));

						if (weight == 0)
							continue;

						var at = (sy * width + sx) * 4;
						var a = source[at + 3] * weight;
						area += weight;
						alpha += a;
						red += source[at] * a;
						green += source[at + 1] * a;
						blue += source[at + 2] * a;
					}

				var to = (y * toWidth + x) * 4;

				if (alpha > 0)
				{
					result[to] = (byte)Math.Round(red / alpha);
					result[to + 1] = (byte)Math.Round(green / alpha);
					result[to + 2] = (byte)Math.Round(blue / alpha);
				}

				result[to + 3] = (byte)Math.Round(alpha / area);
			}

		return result;
	}
}
