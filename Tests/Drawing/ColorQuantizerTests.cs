namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

/// <summary>
/// The palette an indexed frame gets.
/// </summary>
[TestClass]
public class ColorQuantizerTests : BaseTestClass
{
	[TestMethod]
	public void ColorsThatFitThePalette_AreKeptExactly()
	{
		// 16x16 with a color of its own in every pixel, and room for all of them.
		var image = Fill(16, 16, (x, y) => ((byte)(x * 17), (byte)(y * 17), (byte)(x * 16 + y)));
		var indices = new byte[16 * 16];

		var palette = ColorQuantizer.Quantize(image, 256, indices, 0);

		(palette.Length / 3).AssertEqual(256);

		for (var i = 0; i < indices.Length; i++)
			for (var channel = 0; channel < 3; channel++)
				palette[indices[i] * 3 + channel].AssertEqual(image.Pixels[i * 4 + channel], $"pixel {i}, channel {channel}");
	}

	[TestMethod]
	public void TransparentPixels_TakeNoColorAndKeepTheirIndex()
	{
		var image = Fill(4, 1, (x, _) => ((byte)(x * 60), 0, 0));
		image.Pixels[3] = 0;
		image.Pixels[7] = 127;

		var indices = new byte[] { 9, 9, 9, 9 };
		var palette = ColorQuantizer.Quantize(image, 255, indices, 1);

		(palette.Length / 3).AssertEqual(2);
		indices.AssertEqual(new byte[] { 9, 9, 1, 2 });
	}

	[TestMethod]
	public void PictureWithoutAnOpaquePixel_HasNoPalette()
	{
		var image = new RasterImage(3, 3);
		var indices = new byte[9];

		ColorQuantizer.Quantize(image, 256, indices, 0).Length.AssertEqual(0);
	}

	[TestMethod]
	public void SmoothPictureOfManyColors_StaysCloseAfterReduction()
	{
		// Three gradients across 512x512: far more colors than are counted one by one.
		const int side = 512;
		var image = Fill(side, side, (x, y) => ((byte)(x / 2), (byte)(y / 2), (byte)((x + y) / 4)));

		var distinct = new HashSet<int>();

		for (var i = 0; i < image.Pixels.Length; i += 4)
			distinct.Add(image.Pixels[i] << 16 | image.Pixels[i + 1] << 8 | image.Pixels[i + 2]);

		(distinct.Count > 1 << 16).AssertTrue($"{distinct.Count} colors");

		var indices = new byte[side * side];
		var palette = ColorQuantizer.Quantize(image, 256, indices, 0);

		(palette.Length / 3).AssertEqual(256);

		var worst = 0;
		var total = 0L;

		for (var i = 0; i < indices.Length; i++)
			for (var channel = 0; channel < 3; channel++)
			{
				var error = Math.Abs(palette[indices[i] * 3 + channel] - image.Pixels[i * 4 + channel]);
				worst = Math.Max(worst, error);
				total += error;
			}

		// 256 colors over a plane of 256x256: a cell of about 16 levels a side, so half of that at most.
		var mean = (double)total / (indices.Length * 3);
		(mean < 5).AssertTrue($"mean error {mean:F2}");
		(worst <= 24).AssertTrue($"largest error {worst}");
	}

	private static RasterImage Fill(int width, int height, Func<int, int, (byte r, byte g, byte b)> color)
	{
		var image = new RasterImage(width, height);

		for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var (r, g, b) = color(x, y);
				var at = (y * width + x) * 4;
				image.Pixels[at] = r;
				image.Pixels[at + 1] = g;
				image.Pixels[at + 2] = b;
				image.Pixels[at + 3] = 255;
			}

		return image;
	}
}
