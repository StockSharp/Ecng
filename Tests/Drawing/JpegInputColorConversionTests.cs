namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

[TestClass]
public class JpegInputColorConversionTests : BaseTestClass
{
	[TestMethod]
	[DataRow(0, 64)]
	[DataRow(64, 128)]
	[DataRow(128, 192)]
	[DataRow(192, 256)]
	public void EveryOpaqueRgbValue_MatchesTheScalarColorEquations(int firstRed, int lastRed)
	{
		var image = new RasterImage(256, 256);
		Span<double> y = stackalloc double[64];
		Span<double> cb = stackalloc double[64];
		Span<double> cr = stackalloc double[64];
		for (var red = firstRed; red < lastRed; red++)
		{
			for (var blue = 0; blue < 256; blue++)
				for (var green = 0; green < 256; green++)
				{
					var at = (blue * 256 + green) * 4;
					image.Pixels[at] = (byte)red;
					image.Pixels[at + 1] = (byte)green;
					image.Pixels[at + 2] = (byte)blue;
					image.Pixels[at + 3] = 255;
				}
			for (var top = 0; top < 256; top += 8)
				for (var left = 0; left < 256; left += 8)
				{
					JpegEncoder.ReadBlock(image, left, top, y, cb, cr);
					for (var row = 0; row < 8; row++)
						for (var column = 0; column < 8; column++)
						{
							var green = left + column;
							var blue = top + row;
							var at = row * 8 + column;
							if (y[at] != 0.299 * red + 0.587 * green + 0.114 * blue - 128 ||
								cb[at] != -0.168736 * red - 0.331264 * green + 0.5 * blue ||
								cr[at] != 0.5 * red - 0.418688 * green - 0.081312 * blue)
								Fail($"RGB ({red},{green},{blue}) differs from the scalar color equations.");
						}
				}
		}
	}

	[TestMethod]
	[DataRow(8, 8)]
	[DataRow(7, 9)]
	[DataRow(1, 1)]
	public void AlphaAndReplicatedEdges_MatchWhiteCompositing(int width, int height)
	{
		var image = new RasterImage(width, height);
		for (var i = 0; i < image.Pixels.Length; i++) image.Pixels[i] = (byte)(i * 137 + 19);
		Span<double> y = stackalloc double[64];
		Span<double> cb = stackalloc double[64];
		Span<double> cr = stackalloc double[64];
		JpegEncoder.ReadBlock(image, 0, 0, y, cb, cr);
		for (var row = 0; row < 8; row++)
			for (var column = 0; column < 8; column++)
			{
				var at = (Math.Min(height - 1, row) * width + Math.Min(width - 1, column)) * 4;
				var alpha = image.Pixels[at + 3];
				var red = (image.Pixels[at] * (double)alpha + 255.0 * (255 - alpha)) / 255;
				var green = (image.Pixels[at + 1] * (double)alpha + 255.0 * (255 - alpha)) / 255;
				var blue = (image.Pixels[at + 2] * (double)alpha + 255.0 * (255 - alpha)) / 255;
				var destination = row * 8 + column;
				AreEqual(0.299 * red + 0.587 * green + 0.114 * blue - 128, y[destination]);
				AreEqual(-0.168736 * red - 0.331264 * green + 0.5 * blue, cb[destination]);
				AreEqual(0.5 * red - 0.418688 * green - 0.081312 * blue, cr[destination]);
			}
	}
}
