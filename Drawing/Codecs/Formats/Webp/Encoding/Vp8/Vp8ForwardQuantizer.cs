namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

internal static class Vp8ForwardQuantizer
{
	public static int Quantize(ReadOnlySpan<short> coefficients, int dcQuant, int acQuant, Span<short> quantized)
	{
#if NET7_0_OR_GREATER
		if (Vector128.IsHardwareAccelerated)
		{
			return QuantizeVector(coefficients, dcQuant, acQuant, quantized);
		}
#endif

		return QuantizeScalar(coefficients, dcQuant, acQuant, quantized);
	}

#if NET7_0_OR_GREATER
	internal static int QuantizeVector(ReadOnlySpan<short> coefficients, int dcQuant, int acQuant, Span<short> quantized)
	{
		ref short src = ref MemoryMarshal.GetReference(coefficients.Slice(0, 16));
		var ac = Vector128.Create((float)acQuant);
		var acHalf = Vector128.Create(acQuant / 2);

		var divisor0 = ac.WithElement(0, dcQuant);
		var half0 = acHalf.WithElement(0, dcQuant / 2);

		var l0 = QuantizeRow(Vector128.LoadUnsafe(ref src, 0), half0, divisor0);
		var l1 = QuantizeRow(Vector128.LoadUnsafe(ref src, 4), acHalf, ac);
		var l2 = QuantizeRow(Vector128.LoadUnsafe(ref src, 8), acHalf, ac);
		var l3 = QuantizeRow(Vector128.LoadUnsafe(ref src, 12), acHalf, ac);

		var natural0 = Vector128.Narrow(l0, l1);
		var natural1 = Vector128.Narrow(l2, l3);

		var scan0 = Vector128.Shuffle(natural0, ZigZagLow0) | Vector128.Shuffle(natural1, ZigZagLow1);
		var scan1 = Vector128.Shuffle(natural0, ZigZagHigh0) | Vector128.Shuffle(natural1, ZigZagHigh1);

		ref short dst = ref MemoryMarshal.GetReference(quantized.Slice(0, 16));
		scan0.StoreUnsafe(ref dst, 0);
		scan1.StoreUnsafe(ref dst, 8);

		var isZero = Vector128.Narrow(Vector128.Equals(scan0, Vector128<short>.Zero), Vector128.Equals(scan1, Vector128<short>.Zero));
		uint nonZero = ~isZero.ExtractMostSignificantBits() & 0xFFFF;
		return 32 - BitOperations.LeadingZeroCount(nonZero);
	}

	private static Vector128<int> QuantizeRow(Vector128<short> coefficients, Vector128<int> half, Vector128<float> divisor)
	{
		var value = Vector128.WidenLower(coefficients);
		var sign = Vector128.ShiftRightArithmetic(value, 31);
		var magnitude = Vector128.Abs(value);
		var level = Vector128.ConvertToInt32(Vector128.ConvertToSingle(magnitude + half) / divisor);

		return (level ^ sign) - sign;
	}

#endif
	internal static int QuantizeScalar(ReadOnlySpan<short> coefficients, int dcQuant, int acQuant, Span<short> quantized)
	{
		int last = 0;
		for (int scan = 0; scan < 16; scan++)
		{
			int naturalPos = Vp8ZigZag.Order[scan];
			int coeff = coefficients[naturalPos];
			int quant = scan == 0 ? dcQuant : acQuant;
			int level = QuantizeOne(coeff, quant);
			quantized[scan] = (short)level;
			if (level != 0)
			{
				last = scan + 1;
			}
		}

		return last;
	}

	public static void Dequantize(ReadOnlySpan<short> quantized, int dcQuant, int acQuant, Span<short> output)
	{
#if NET7_0_OR_GREATER
		if (Vector128.IsHardwareAccelerated)
		{
			DequantizeVector(quantized, dcQuant, acQuant, output);
			return;
		}
#endif

		DequantizeScalar(quantized, dcQuant, acQuant, output);
	}

#if NET7_0_OR_GREATER
	internal static void DequantizeVector(ReadOnlySpan<short> quantized, int dcQuant, int acQuant, Span<short> output)
	{
		ref short src = ref MemoryMarshal.GetReference(quantized.Slice(0, 16));
		var scan0 = Vector128.LoadUnsafe(ref src, 0);
		var scan1 = Vector128.LoadUnsafe(ref src, 8);

		var natural0 = Vector128.Shuffle(scan0, UnZigZagLow0) | Vector128.Shuffle(scan1, UnZigZagLow1);
		var natural1 = Vector128.Shuffle(scan0, UnZigZagHigh0) | Vector128.Shuffle(scan1, UnZigZagHigh1);

		var acStep = Vector128.Create((short)acQuant);
		ref short dst = ref MemoryMarshal.GetReference(output.Slice(0, 16));
		(natural0 * acStep.WithElement(0, (short)dcQuant)).StoreUnsafe(ref dst, 0);
		(natural1 * acStep).StoreUnsafe(ref dst, 8);
	}

	private static readonly Vector128<short> ZigZagLow0 = ShuffleTable(scanBase: 0, fromNaturalHalf: 0, forward: true);
	private static readonly Vector128<short> ZigZagLow1 = ShuffleTable(scanBase: 0, fromNaturalHalf: 1, forward: true);
	private static readonly Vector128<short> ZigZagHigh0 = ShuffleTable(scanBase: 8, fromNaturalHalf: 0, forward: true);
	private static readonly Vector128<short> ZigZagHigh1 = ShuffleTable(scanBase: 8, fromNaturalHalf: 1, forward: true);
	private static readonly Vector128<short> UnZigZagLow0 = ShuffleTable(scanBase: 0, fromNaturalHalf: 0, forward: false);
	private static readonly Vector128<short> UnZigZagLow1 = ShuffleTable(scanBase: 0, fromNaturalHalf: 1, forward: false);
	private static readonly Vector128<short> UnZigZagHigh0 = ShuffleTable(scanBase: 8, fromNaturalHalf: 0, forward: false);
	private static readonly Vector128<short> UnZigZagHigh1 = ShuffleTable(scanBase: 8, fromNaturalHalf: 1, forward: false);

	private static Vector128<short> ShuffleTable(int scanBase, int fromNaturalHalf, bool forward)
	{
		var indices = new short[8];
		for (int lane = 0; lane < 8; lane++)
		{
			int outputPosition = scanBase + lane;
			int source = forward ? Vp8ZigZag.Order[outputPosition] : Array.IndexOf(Vp8ZigZag.Order, outputPosition);
			indices[lane] = source / 8 == fromNaturalHalf ? (short)(source % 8) : (short)255;
		}

		return Vector128.Create(indices);
	}

#endif
	internal static void DequantizeScalar(ReadOnlySpan<short> quantized, int dcQuant, int acQuant, Span<short> output)
	{
		output.Clear();
		for (int scan = 0; scan < 16; scan++)
		{
			int level = quantized[scan];
			if (level == 0)
			{
				continue;
			}

			int naturalPos = Vp8ZigZag.Order[scan];
			int quant = scan == 0 ? dcQuant : acQuant;
			output[naturalPos] = (short)(level * quant);
		}
	}

	private static int QuantizeOne(int coeff, int quant)
	{
		int magnitude = Math.Abs(coeff);
		int level = (magnitude + (quant / 2)) / quant;
		return coeff < 0 ? -level : level;
	}
}
