namespace Ecng.Tests.Drawing;

internal static class ImageResizeReference
{
	public static byte[] AreaAverage(byte[] source, int sourceWidth, int sourceHeight, int width, int height)
	{
		var output = new byte[width * height * 4];
		Span<double> colors = stackalloc double[3];
		for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var alpha = 0.0;
				colors.Clear();
				for (var sy = y * sourceHeight / height; sy < ((y + 1) * sourceHeight + height - 1) / height; sy++)
					for (var sx = x * sourceWidth / width; sx < ((x + 1) * sourceWidth + width - 1) / width; sx++)
					{
						var weightX = Math.Min((x + 1) * sourceWidth, (sx + 1) * width) - Math.Max(x * sourceWidth, sx * width);
						var weightY = Math.Min((y + 1) * sourceHeight, (sy + 1) * height) - Math.Max(y * sourceHeight, sy * height);
						var offset = (sy * sourceWidth + sx) * 4;
						var weightedAlpha = source[offset + 3] * (double)weightX * weightY;
						alpha += weightedAlpha;
						for (var channel = 0; channel < 3; channel++) colors[channel] += source[offset + channel] * weightedAlpha;
					}
				var destination = (y * width + x) * 4;
				output[destination + 3] = (byte)Math.Clamp((int)Math.Round(alpha / (sourceWidth * (double)sourceHeight)), 0, 255);
				if (alpha > 0)
					for (var channel = 0; channel < 3; channel++) output[destination + channel] = (byte)Math.Clamp((int)Math.Round(colors[channel] / alpha), 0, 255);
			}
		return output;
	}
}
