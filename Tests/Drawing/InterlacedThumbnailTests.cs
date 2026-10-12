namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

[TestClass]
public class InterlacedThumbnailTests : BaseTestClass
{
	[TestMethod]
	[DataRow(17, 13, 7, 5)]
	[DataRow(3, 2, 2, 1)]
	[DataRow(1, 13, 1, 6)]
	[DataRow(13, 1, 6, 1)]
	public void Adam7_ThumbnailMatchesIndependentAreaAverageIncludingAlpha(int width, int height, int targetWidth, int targetHeight)
	{
		var pixels = Rgba(width, height, (x, y) => ((byte)(x * 37), (byte)(y * 51), (byte)((x + y) * 29), (byte)((x * 23 + y * 41) % 256)));
		using var scanlines = new MemoryStream();
		foreach (var (px, py, dx, dy) in new[] { (0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4), (0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2) })
		{
			if (px >= width || py >= height) continue;
			for (var y = py; y < height; y += dy)
			{
				scanlines.WriteByte(0);
				for (var x = px; x < width; x += dx) scanlines.Write(pixels.AsSpan((y * width + x) * 4, 4));
			}
		}
		var header = Header(width, height, 8, 6);
		header[12] = 1;
		var input = WriteChunks([("IHDR", header), ("IDAT", Deflate(scanlines.ToArray())), ("IEND", [])]);
		var output = input.ResizeImage(Math.Max(targetWidth, targetHeight), Math.Max(targetWidth, targetHeight));
		AssertPixels(ImageResizeReference.AreaAverage(pixels, width, height, targetWidth, targetHeight), ReadPng(output, targetWidth, targetHeight), targetWidth);
	}

	[TestMethod]
	public void Adam7_48MegapixelThumbnailDoesNotAllocateTheFullCanvas()
	{
		const int width = 8000;
		const int height = 6000;
		using var scanlines = new MemoryStream();
		foreach (var (x, y, dx, dy) in new[] { (0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4), (0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2) })
		{
			var passWidth = (width - x + dx - 1) / dx;
			var passHeight = (height - y + dy - 1) / dy;
			var row = new byte[1 + (passWidth + 7) / 8];
			for (var line = 0; line < passHeight; line++) scanlines.Write(row);
		}
		var header = Header(width, height, 1, 0);
		header[12] = 1;
		var input = WriteChunks([("IHDR", header), ("IDAT", Deflate(scanlines.ToArray())), ("IEND", [])]);
		ThrowsExactly<InvalidDataException>(() => input.ConvertToPng());
		var output = input.ResizeImage(100, 100);
		AssertPixels(Rgba(100, 75, (_, _) => (0, 0, 0, 255)), ReadPng(output, 100, 75), 100);
	}
}
