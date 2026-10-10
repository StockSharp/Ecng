namespace Ecng.Drawing;

using System;

/// <summary>
/// The arithmetic both JPEG decoders share: the inverse DCT and the conversion of decoded components to RGBA.
/// </summary>
internal static class JpegTransforms
{
	// _basis[x * 8 + u] = C(u) * cos((2x + 1) * u * pi / 16) / 2: the two passes of the transform
	// give the 1/4 of its definition between them.
	private static readonly double[] _basis = CreateBasis();

	/// <summary>
	/// Position in a block of each coefficient, in the zigzag order coefficients are coded in.
	/// </summary>
	public static ReadOnlySpan<byte> ZigZag =>
	[
		0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5,
		12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28,
		35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51,
		58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63,
	];

	/// <summary>
	/// Turns the dequantized coefficients of one block into 8x8 samples.
	/// </summary>
	/// <param name="block">64 coefficients in natural order.</param>
	/// <param name="samples">The plane the samples go to.</param>
	/// <param name="stride">Length of a row of the plane.</param>
	/// <param name="left">Column of the block in the plane.</param>
	/// <param name="top">Row of the block in the plane.</param>
	public static void InverseDct(ReadOnlySpan<int> block, byte[] samples, int stride, int left, int top)
	{
		var flat = true;

		for (var i = 1; i < 64 && flat; i++)
			flat = block[i] == 0;

		if (flat)
		{
			// Nothing but the DC coefficient: every sample of the block is the same.
			var value = RasterImage.ToByte(block[0] / 8.0 + 128);

			for (var y = 0; y < 8; y++)
				samples.AsSpan((top + y) * stride + left, 8).Fill(value);

			return;
		}

		Span<double> columns = stackalloc double[64];

		for (var u = 0; u < 8; u++)
			for (var y = 0; y < 8; y++)
			{
				var sum = 0.0;

				for (var v = 0; v < 8; v++)
					sum += block[v * 8 + u] * _basis[y * 8 + v];

				columns[y * 8 + u] = sum;
			}

		for (var y = 0; y < 8; y++)
		{
			var row = (top + y) * stride + left;

			for (var x = 0; x < 8; x++)
			{
				var sum = 0.0;

				for (var u = 0; u < 8; u++)
					sum += columns[y * 8 + u] * _basis[x * 8 + u];

				samples[row + x] = RasterImage.ToByte(sum + 128);
			}
		}
	}

	/// <summary>
	/// Decides what the components of a frame stand for, the way libjpeg does.
	/// </summary>
	/// <param name="components">The components of the frame.</param>
	/// <param name="hasJfif">Whether the file has a JFIF segment.</param>
	/// <param name="adobeTransform">The transform flag of the Adobe segment, or -1 if the file has none.</param>
	public static JpegColorModels ResolveColorModel(JpegComponent[] components, bool hasJfif, int adobeTransform)
	{
		switch (components.Length)
		{
			case 1:
				return JpegColorModels.Gray;

			case 3:
				if (hasJfif)
					return JpegColorModels.YCbCr;

				if (adobeTransform >= 0)
					return adobeTransform == 0 ? JpegColorModels.Rgb : JpegColorModels.YCbCr;

				return components[0].Id == 'R' && components[1].Id == 'G' && components[2].Id == 'B'
					? JpegColorModels.Rgb
					: JpegColorModels.YCbCr;

			default:
				// Four components are stored inverted whether or not an Adobe segment says so:
				// that is what writers do and what the other decoders assume.
				return adobeTransform switch
				{
					2 => JpegColorModels.Ycck,
					0 or -1 => JpegColorModels.Cmyk,
					_ => throw new NotSupportedException("Unsupported Adobe JPEG color transform."),
				};
		}
	}

	/// <summary>
	/// Builds the picture out of the decoded sample planes.
	/// </summary>
	public static RasterImage ToImage(int width, int height, JpegComponent[] components, JpegColorModels model)
	{
		var maxH = 1;
		var maxV = 1;

		foreach (var component in components)
		{
			maxH = Math.Max(maxH, component.H);
			maxV = Math.Max(maxV, component.V);
		}

		// A subsampled component repeats each of its samples over the pixels it covers.
		var columns = new int[components.Length][];

		for (var i = 0; i < components.Length; i++)
		{
			columns[i] = new int[width];

			for (var x = 0; x < width; x++)
				columns[i][x] = x * components[i].H / maxH;
		}

		var image = new RasterImage(width, height);
		var pixels = image.Pixels;
		Span<int> rows = stackalloc int[components.Length];
		Span<int> values = stackalloc int[components.Length];

		for (var y = 0; y < height; y++)
		{
			for (var i = 0; i < components.Length; i++)
				rows[i] = y * components[i].V / maxV * components[i].Stride;

			for (var x = 0; x < width; x++)
			{
				for (var i = 0; i < components.Length; i++)
					values[i] = components[i].Samples[rows[i] + columns[i][x]];

				var at = (y * width + x) * 4;

				switch (model)
				{
					case JpegColorModels.Gray:
						pixels[at] = pixels[at + 1] = pixels[at + 2] = (byte)values[0];
						break;

					case JpegColorModels.Rgb:
						pixels[at] = (byte)values[0];
						pixels[at + 1] = (byte)values[1];
						pixels[at + 2] = (byte)values[2];
						break;

					case JpegColorModels.YCbCr:
						pixels[at] = Red(values[0], values[2]);
						pixels[at + 1] = Green(values[0], values[1], values[2]);
						pixels[at + 2] = Blue(values[0], values[1]);
						break;

					case JpegColorModels.Cmyk:
						pixels[at] = RasterImage.ToByte(values[0] * values[3] / 255.0);
						pixels[at + 1] = RasterImage.ToByte(values[1] * values[3] / 255.0);
						pixels[at + 2] = RasterImage.ToByte(values[2] * values[3] / 255.0);
						break;

					default:
						// The color transform was applied to cyan, magenta and yellow before they were inverted.
						pixels[at] = RasterImage.ToByte((255 - Red(values[0], values[2])) * values[3] / 255.0);
						pixels[at + 1] = RasterImage.ToByte((255 - Green(values[0], values[1], values[2])) * values[3] / 255.0);
						pixels[at + 2] = RasterImage.ToByte((255 - Blue(values[0], values[1])) * values[3] / 255.0);
						break;
				}

				pixels[at + 3] = 255;
			}
		}

		return image;
	}

	private static byte Red(int y, int cr)
		=> RasterImage.ToByte(y + 1.402 * (cr - 128));

	private static byte Green(int y, int cb, int cr)
		=> RasterImage.ToByte(y - 0.344136 * (cb - 128) - 0.714136 * (cr - 128));

	private static byte Blue(int y, int cb)
		=> RasterImage.ToByte(y + 1.772 * (cb - 128));

	private static double[] CreateBasis()
	{
		var basis = new double[64];

		for (var x = 0; x < 8; x++)
			for (var u = 0; u < 8; u++)
				basis[x * 8 + u] = (u == 0 ? 1 / Math.Sqrt(2) : 1) * Math.Cos((2 * x + 1) * u * Math.PI / 16) / 2;

		return basis;
	}
}
