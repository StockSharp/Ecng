namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Dct;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8ScalarInverseDct
{
	public static void TransformAndAdd(ReadOnlySpan<short> coefficients, Span<byte> dst, int offset, int stride)
	{
		bool anyAc = false;
		for (int i = 1; i < 16; i++)
		{
			if (coefficients[i] != 0)
			{
				anyAc = true;
				break;
			}
		}

		if (!anyAc)
		{
			if (coefficients[0] == 0)
			{
				return;
			}

			TransformDcOnlyAndAdd(coefficients, dst, offset, stride);
			return;
		}

		// Bit-identical to the scalar butterfly (Vp8VectorInverseDctTests holds it to exactly that), so callers
		// that don't pick a kernel themselves -- the encoder's reconstruction pass -- get the faster one.
#if NET7_0_OR_GREATER
		if (Vp8VectorInverseDct.CanTransform)
		{
			Vp8VectorInverseDct.TransformFullAndAdd(coefficients, dst, offset, stride);
			return;
		}
#endif

		TransformFullAndAdd(coefficients, dst, offset, stride);
	}
	public static void TransformDcOnlyAndAdd(ReadOnlySpan<short> coefficients, Span<byte> dst, int offset, int stride)
	{
		int dc = (coefficients[0] + 4) >> 3;
		for (int y = 0; y < 4; y++)
		{
			int rowOffset = offset + (y * stride);
			for (int x = 0; x < 4; x++)
			{
				dst[rowOffset + x] = ClipAdd(dst[rowOffset + x], dc);
			}
		}
	}
	public static void TransformFullAndAdd(ReadOnlySpan<short> coefficients, Span<byte> dst, int offset, int stride)
	{
		Span<int> tmp = stackalloc int[16];
		for (int i = 0; i < 4; i++)
		{
			int in0 = coefficients[i];
			int in4 = coefficients[4 + i];
			int in8 = coefficients[8 + i];
			int in12 = coefficients[12 + i];

			int a = in0 + in8;
			int b = in0 - in8;
			int c = Vp8InverseTransformConstants.Mul2(in4) - Vp8InverseTransformConstants.Mul1(in12);
			int d = Vp8InverseTransformConstants.Mul1(in4) + Vp8InverseTransformConstants.Mul2(in12);

			tmp[(i * 4) + 0] = a + d;
			tmp[(i * 4) + 1] = b + c;
			tmp[(i * 4) + 2] = b - c;
			tmp[(i * 4) + 3] = a - d;
		}

		for (int i = 0; i < 4; i++)
		{
			int t0 = tmp[(0 * 4) + i];
			int t4 = tmp[(1 * 4) + i];
			int t8 = tmp[(2 * 4) + i];
			int t12 = tmp[(3 * 4) + i];

			int dc = t0 + 4;
			int a = dc + t8;
			int b = dc - t8;
			int c = Vp8InverseTransformConstants.Mul2(t4) - Vp8InverseTransformConstants.Mul1(t12);
			int d = Vp8InverseTransformConstants.Mul1(t4) + Vp8InverseTransformConstants.Mul2(t12);

			int rowOffset = offset + (i * stride);
			dst[rowOffset + 0] = ClipAdd(dst[rowOffset + 0], (a + d) >> 3);
			dst[rowOffset + 1] = ClipAdd(dst[rowOffset + 1], (b + c) >> 3);
			dst[rowOffset + 2] = ClipAdd(dst[rowOffset + 2], (b - c) >> 3);
			dst[rowOffset + 3] = ClipAdd(dst[rowOffset + 3], (a - d) >> 3);
		}
	}

	private static byte ClipAdd(byte baseValue, int delta)
	{
		int v = baseValue + delta;
		return (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
	}
}