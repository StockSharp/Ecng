namespace Ecng.Drawing;

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Ecng.Drawing.Codecs.Jpeg;

/// <summary>
/// The arithmetic both JPEG decoders share: the inverse DCT and the conversion of decoded components to RGBA.
/// </summary>
internal static class JpegTransforms
{
	// _basis[x * 8 + u] = C(u) * cos((2x + 1) * u * pi / 16) / 2: the two passes of the transform
	// give the 1/4 of its definition between them.
	private static readonly double[] _basis = CreateBasis();
	private static readonly double[][] _scaledBasis = [CreateScaledBasis(2), CreateScaledBasis(4)];

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
		=> InverseDct(block, samples, stride, left, top, 1, 8);

	public static void InverseDct(ReadOnlySpan<int> block, byte[] samples, int stride, int left, int top, int scale, int precision)
	{
		var size = 8 / scale;
		var divisor = 1 << (precision - 8);
		var center = 1 << (precision - 1);
		var flat = true;

		for (var i = 1; i < 64 && flat; i++)
			flat = block[i] == 0;

		if (flat || size == 1)
		{
			// Nothing but the DC coefficient: every sample of the block is the same.
			var value = RasterImage.ToByte((block[0] / 8.0 + center) / divisor);

			for (var y = 0; y < size; y++)
				samples.AsSpan((top + y) * stride + left, size).Fill(value);

			return;
		}

		if (scale == 1 && precision == 8)
		{
			Unsafe.SkipInit(out JpegBlock8x8F source);
			Unsafe.SkipInit(out JpegBlock8x8F decoded);
			Unsafe.SkipInit(out JpegBlock8x8F scratch);
			var input = MemoryMarshal.CreateSpan(ref source.V0L.X, 64);
			if (Avx.IsSupported)
			{
				ref int start = ref MemoryMarshal.GetReference(block);
				for (var i = 0; i < 64; i += 8)
				{
					var values = Unsafe.ReadUnaligned<Vector256<int>>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref start, i)));
					Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref input[i]), Avx.ConvertToVector256Single(values));
				}
			}
			else
				for (var i = 0; i < 64; i++) input[i] = block[i];
			FastFloatingPointDCT.TransformIDCT(ref source, ref decoded, ref scratch);
			var output = MemoryMarshal.CreateReadOnlySpan(ref decoded.V0L.X, 64);
			JpegRasterSamples.Write(output, samples, stride, left, top);
			return;
		}

		var basis = scale == 1 ? _basis : _scaledBasis[scale == 2 ? 0 : 1];
		Span<double> columns = stackalloc double[64];

		for (var u = 0; u < 8; u++)
			for (var y = 0; y < size; y++)
			{
				var sum = 0.0;

				for (var v = 0; v < 8; v++)
					sum += block[v * 8 + u] * basis[y * 8 + v];

				columns[y * 8 + u] = sum;
			}

		for (var y = 0; y < size; y++)
		{
			var row = (top + y) * stride + left;

			for (var x = 0; x < size; x++)
			{
				var sum = 0.0;

				for (var u = 0; u < 8; u++)
					sum += columns[y * 8 + u] * basis[x * 8 + u];

				samples[row + x] = RasterImage.ToByte((sum + center) / divisor);
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
		=> ToImage(width, height, components, model, ImageProcessingLimits.Default);

	public static RasterImage ToImage(int width, int height, JpegComponent[] components, JpegColorModels model, ImageProcessingLimits limits)
	{
		var image = new RasterImage(width, height, limits);
		var converter = new JpegColorConverter(width, components, model);
		for (var y = 0; y < height; y++)
			converter.WriteRow(y, image.Pixels.AsSpan(y * width * 4, width * 4));
		return image;
	}

	private static double[] CreateBasis()
	{
		var basis = new double[64];

		for (var x = 0; x < 8; x++)
			for (var u = 0; u < 8; u++)
				basis[x * 8 + u] = (u == 0 ? 1 / Math.Sqrt(2) : 1) * Math.Cos((2 * x + 1) * u * Math.PI / 16) / 2;

		return basis;
	}

	private static double[] CreateScaledBasis(int scale)
	{
		var basis = new double[8 / scale * 8];
		for (var x = 0; x < 8 / scale; x++)
			for (var u = 0; u < 8; u++)
				for (var i = 0; i < scale; i++)
					basis[x * 8 + u] += _basis[(x * scale + i) * 8 + u] / scale;
		return basis;
	}
}
