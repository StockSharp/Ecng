namespace Ecng.Tests.Drawing;

internal static class ImageQualityMetrics
{
	private static readonly double[] _gaussian = CreateGaussian();

	public static ImageQualityScores Measure(byte[] expected, byte[] actual, int width, int height)
	{
		if (width < 11 || height < 11 || expected.Length != checked(width * height * 4) || actual.Length != expected.Length)
			throw new ArgumentException("Metrics require equally sized RGBA images of at least 11x11 pixels.");

		var count = width * height;
		var expectedLuma = new double[count];
		var actualLuma = new double[count];
		var absolute = 0.0;
		var squares = 0.0;
		var alpha = 0.0;
		var maximum = 0.0;

		for (var pixel = 0; pixel < count; pixel++)
		{
			var offset = pixel * 4;
			alpha += Math.Abs(expected[offset + 3] - actual[offset + 3]);
			for (var channel = 0; channel < 3; channel++)
			{
				var first = expected[offset + channel] * expected[offset + 3] / 255.0;
				var second = actual[offset + channel] * actual[offset + 3] / 255.0;
				var difference = Math.Abs(first - second);
				absolute += difference;
				squares += difference * difference;
				maximum = Math.Max(maximum, difference);
				var weight = channel switch { 0 => 0.299, 1 => 0.587, _ => 0.114 };
				expectedLuma[pixel] += first * weight;
				actualLuma[pixel] += second * weight;
			}
		}

		var mse = squares / (count * 3.0);
		return new(absolute / (count * 3.0), Math.Sqrt(mse), mse == 0 ? double.PositiveInfinity : 10 * Math.Log10(255 * 255 / mse),
			Ssim(expectedLuma, actualLuma, width, height), alpha / count, maximum);
	}

	private static double Ssim(double[] first, double[] second, int width, int height)
	{
		const double c1 = 6.5025;
		const double c2 = 58.5225;
		var columns = width - 10;
		var horizontal = new double[checked(columns * height * 5)];
		for (var y = 0; y < height; y++)
			for (var x = 0; x < columns; x++)
			{
				var destination = (y * columns + x) * 5;
				for (var k = 0; k < 11; k++)
				{
					var a = first[y * width + x + k];
					var b = second[y * width + x + k];
					var weight = _gaussian[k];
					horizontal[destination] += a * weight;
					horizontal[destination + 1] += b * weight;
					horizontal[destination + 2] += a * a * weight;
					horizontal[destination + 3] += b * b * weight;
					horizontal[destination + 4] += a * b * weight;
				}
			}

		var total = 0.0;
		Span<double> moments = stackalloc double[5];
		for (var y = 0; y < height - 10; y++)
			for (var x = 0; x < columns; x++)
			{
				moments.Clear();
				for (var k = 0; k < 11; k++)
					for (var moment = 0; moment < 5; moment++)
						moments[moment] += horizontal[((y + k) * columns + x) * 5 + moment] * _gaussian[k];

				var aa = moments[0] * moments[0];
				var bb = moments[1] * moments[1];
				var ab = moments[0] * moments[1];
				total += ((2 * ab + c1) * (2 * (moments[4] - ab) + c2)) /
					((aa + bb + c1) * (Math.Max(0, moments[2] - aa) + Math.Max(0, moments[3] - bb) + c2));
			}

		return total / (columns * (double)(height - 10));
	}

	private static double[] CreateGaussian()
	{
		var weights = Enumerable.Range(-5, 11).Select(i => Math.Exp(-i * i / (2 * 1.5 * 1.5))).ToArray();
		var total = weights.Sum();
		for (var i = 0; i < weights.Length; i++) weights[i] /= total;
		return weights;
	}
}
