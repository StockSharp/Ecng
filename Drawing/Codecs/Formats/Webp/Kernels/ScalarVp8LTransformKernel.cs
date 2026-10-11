namespace Ecng.Drawing.Codecs.Formats.Webp.Kernels;

using System;

internal sealed class ScalarVp8LTransformKernel : IVp8LTransformKernel
{
	public void SubtractGreenInverse(Span<uint> pixels)
	{
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = SubtractGreenInversePixel(pixels[i]);
		}
	}

	public void PredictorTopInverse(Span<byte> row, ReadOnlySpan<byte> topRow)
	{
		for (int i = 0; i < row.Length; i++)
		{
			row[i] = (byte)(row[i] + topRow[i]);
		}
	}

	public void ColorTransformInverse(Span<uint> pixels, sbyte greenToRed, sbyte greenToBlue, sbyte redToBlue)
	{
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = ColorTransformInversePixel(pixels[i], greenToRed, greenToBlue, redToBlue);
		}
	}

	internal static uint SubtractGreenInversePixel(uint argb)
	{
		byte green = (byte)(argb >> 8);
		byte red = (byte)((byte)(argb >> 16) + green);
		byte blue = (byte)((byte)argb + green);
		return (argb & 0xFF00FF00u) | ((uint)red << 16) | blue;
	}

	internal static uint ColorTransformInversePixel(uint argb, sbyte greenToRed, sbyte greenToBlue, sbyte redToBlue)
	{
		sbyte green = (sbyte)(argb >> 8);
		int red = (int)(argb >> 16) & 0xFF;
		int blue = (int)argb & 0xFF;

		red = (red + ColorTransformDelta(greenToRed, green)) & 0xFF;
		blue = (blue + ColorTransformDelta(greenToBlue, green)) & 0xFF;
		blue = (blue + ColorTransformDelta(redToBlue, (sbyte)red)) & 0xFF;

		return (argb & 0xFF00FF00u) | ((uint)red << 16) | (uint)blue;
	}

	private static int ColorTransformDelta(sbyte multiplier, sbyte color) => (multiplier * color) >> 5;
}
