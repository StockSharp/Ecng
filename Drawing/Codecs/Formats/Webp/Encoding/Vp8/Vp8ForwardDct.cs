namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Dct;

internal static class Vp8ForwardDct
{
	public static void Transform(
		ReadOnlySpan<byte> source, int sourceOffset, int sourceStride,
		ReadOnlySpan<byte> prediction, int predictionOffset, int predictionStride,
		Span<short> output)
	{
#if NET7_0_OR_GREATER
		if (Vp8Interleave.IsSupported)
		{
			TransformVector(source, sourceOffset, sourceStride, prediction, predictionOffset, predictionStride, output);
			return;
		}
#endif

		TransformScalar(source, sourceOffset, sourceStride, prediction, predictionOffset, predictionStride, output);
	}

#if NET7_0_OR_GREATER
	internal static void TransformVector(
		ReadOnlySpan<byte> source, int sourceOffset, int sourceStride,
		ReadOnlySpan<byte> prediction, int predictionOffset, int predictionStride,
		Span<short> output)
	{
		var row0 = ResidualRow(source, sourceOffset, prediction, predictionOffset);
		var row1 = ResidualRow(source, sourceOffset + sourceStride, prediction, predictionOffset + predictionStride);
		var row2 = ResidualRow(source, sourceOffset + (2 * sourceStride), prediction, predictionOffset + (2 * predictionStride));
		var row3 = ResidualRow(source, sourceOffset + (3 * sourceStride), prediction, predictionOffset + (3 * predictionStride));

		Vp8VectorInverseDct.Transpose4x4(row0, row1, row2, row3, out var d0, out var d1, out var d2, out var d3);

		var sin = Vector128.Create(Vp8ForwardTransformConstants.Sin);
		var cos = Vector128.Create(Vp8ForwardTransformConstants.Cos);

		var a0 = d0 + d3;
		var a1 = d1 + d2;
		var a2 = d1 - d2;
		var a3 = d0 - d3;

		var t0 = (a0 + a1) * 8;
		var t1 = Vector128.ShiftRightArithmetic((a2 * sin) + (a3 * cos) + Vector128.Create(Vp8ForwardTransformConstants.FirstPassBiasOdd1), 9);
		var t2 = (a0 - a1) * 8;
		var t3 = Vector128.ShiftRightArithmetic((a3 * sin) - (a2 * cos) + Vector128.Create(Vp8ForwardTransformConstants.FirstPassBiasOdd3), 9);

		Vp8VectorInverseDct.Transpose4x4(t0, t1, t2, t3, out var r0, out var r1, out var r2, out var r3);

		var b0 = r0 + r3;
		var b1 = r1 + r2;
		var b2 = r1 - r2;
		var b3 = r0 - r3;

		var seven = Vector128.Create(7);
		var out0 = Vector128.ShiftRightArithmetic(b0 + b1 + seven, 4);
		var out2 = Vector128.ShiftRightArithmetic(b0 - b1 + seven, 4);
		var out3 = Vector128.ShiftRightArithmetic((b3 * sin) - (b2 * cos) + Vector128.Create(Vp8ForwardTransformConstants.SecondPassBiasOdd3), 16);

		var nonZeroB3 = Vector128.Equals(b3, Vector128<int>.Zero) + Vector128.Create(1);
		var out1 = Vector128.ShiftRightArithmetic((b2 * sin) + (b3 * cos) + Vector128.Create(Vp8ForwardTransformConstants.SecondPassBiasOdd1), 16) + nonZeroB3;

		ref short dst = ref MemoryMarshal.GetReference(output.Slice(0, 16));
		Vector128.Narrow(out0, out1).StoreUnsafe(ref dst, 0);
		Vector128.Narrow(out2, out3).StoreUnsafe(ref dst, 8);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<int> ResidualRow(ReadOnlySpan<byte> source, int sourceOffset, ReadOnlySpan<byte> prediction, int predictionOffset)
	{
		var s = Vector128.WidenLower(Vector128.WidenLower(Vector128.CreateScalar(MemoryMarshal.Read<uint>(source.Slice(sourceOffset, 4))).AsByte())).AsInt32();
		var p = Vector128.WidenLower(Vector128.WidenLower(Vector128.CreateScalar(MemoryMarshal.Read<uint>(prediction.Slice(predictionOffset, 4))).AsByte())).AsInt32();
		return s - p;
	}

#endif
	internal static void TransformScalar(
		ReadOnlySpan<byte> source, int sourceOffset, int sourceStride,
		ReadOnlySpan<byte> prediction, int predictionOffset, int predictionStride,
		Span<short> output)
	{
		Span<int> tmp = stackalloc int[16];

		for (int i = 0; i < 4; i++)
		{
			int srcRow = sourceOffset + (i * sourceStride);
			int predRow = predictionOffset + (i * predictionStride);

			int d0 = source[srcRow + 0] - prediction[predRow + 0];
			int d1 = source[srcRow + 1] - prediction[predRow + 1];
			int d2 = source[srcRow + 2] - prediction[predRow + 2];
			int d3 = source[srcRow + 3] - prediction[predRow + 3];

			int a0 = d0 + d3;
			int a1 = d1 + d2;
			int a2 = d1 - d2;
			int a3 = d0 - d3;

			tmp[(i * 4) + 0] = (a0 + a1) * 8;
			tmp[(i * 4) + 1] = ((a2 * Vp8ForwardTransformConstants.Sin) + (a3 * Vp8ForwardTransformConstants.Cos) + Vp8ForwardTransformConstants.FirstPassBiasOdd1) >> 9;
			tmp[(i * 4) + 2] = (a0 - a1) * 8;
			tmp[(i * 4) + 3] = ((a3 * Vp8ForwardTransformConstants.Sin) - (a2 * Vp8ForwardTransformConstants.Cos) + Vp8ForwardTransformConstants.FirstPassBiasOdd3) >> 9;
		}

		for (int i = 0; i < 4; i++)
		{
			int a0 = tmp[0 + i] + tmp[12 + i];
			int a1 = tmp[4 + i] + tmp[8 + i];
			int a2 = tmp[4 + i] - tmp[8 + i];
			int a3 = tmp[0 + i] - tmp[12 + i];

			output[0 + i] = (short)((a0 + a1 + 7) >> 4);
			output[4 + i] = (short)((((a2 * Vp8ForwardTransformConstants.Sin) + (a3 * Vp8ForwardTransformConstants.Cos) + Vp8ForwardTransformConstants.SecondPassBiasOdd1) >> 16) + (a3 != 0 ? 1 : 0));
			output[8 + i] = (short)((a0 - a1 + 7) >> 4);
			output[12 + i] = (short)(((a3 * Vp8ForwardTransformConstants.Sin) - (a2 * Vp8ForwardTransformConstants.Cos) + Vp8ForwardTransformConstants.SecondPassBiasOdd3) >> 16);
		}
	}
}
