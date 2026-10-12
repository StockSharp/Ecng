namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

internal static class Vp8ModeDecision
{
	private static readonly int[] WholeBlockModes =
	[
		Vp8PredictionModes.DcPred,
		Vp8PredictionModes.VPred,
		Vp8PredictionModes.HPred,
		Vp8PredictionModes.TmPred,
	];

	public static int SelectWholeBlockMode(
		Span<byte> recon, int origin, int stride, int size, bool hasAbove, bool hasLeft,
		ReadOnlySpan<byte> source, int sourceOrigin, int sourceStride,
		out int bestSad)
	{
		int bestMode = WholeBlockModes[0];
		bestSad = int.MaxValue;
		int lastTried = bestMode;

		foreach (int mode in WholeBlockModes)
		{
			Vp8IntraPredictionWholeBlock.PredictModeWholeBlock(mode, recon, origin, stride, size, hasAbove, hasLeft);
			lastTried = mode;
			int sad = Sad(source, sourceOrigin, sourceStride, recon, origin, stride, size);
			if (sad < bestSad)
			{
				bestSad = sad;
				bestMode = mode;
			}
		}

		if (bestMode != lastTried)
		{
			Vp8IntraPredictionWholeBlock.PredictModeWholeBlock(bestMode, recon, origin, stride, size, hasAbove, hasLeft);
		}

		return bestMode;
	}

	public static int SelectSubblockMode(
		Span<byte> recon, int origin, int stride, ReadOnlySpan<byte> aboveRight,
		ReadOnlySpan<byte> source, int sourceOrigin, int sourceStride,
		out int bestSad)
	{
		int bestMode = 0;
		bestSad = int.MaxValue;

		for (int mode = 0; mode < Vp8PredictionModes.NumBModes; mode++)
		{
			Vp8IntraPrediction4x4.Predict(mode, recon, origin, stride, aboveRight);
			int sad = Sad(source, sourceOrigin, sourceStride, recon, origin, stride, 4);
			if (sad < bestSad)
			{
				bestSad = sad;
				bestMode = mode;
			}
		}

		Vp8IntraPrediction4x4.Predict(bestMode, recon, origin, stride, aboveRight);
		return bestMode;
	}

	private static int Sad(ReadOnlySpan<byte> a, int aOffset, int aStride, ReadOnlySpan<byte> b, int bOffset, int bStride, int size)
	{
#if NET7_0_OR_GREATER
		if (Vector128.IsHardwareAccelerated)
		{
			switch (size)
			{
				case 16:
					return Sad16(a, aOffset, aStride, b, bOffset, bStride);
				case 8:
					return Sad8(a, aOffset, aStride, b, bOffset, bStride);
				case 4:
					return Sad4(a, aOffset, aStride, b, bOffset, bStride);
			}
		}
#endif

		int sad = 0;
		for (int y = 0; y < size; y++)
		{
			int ao = aOffset + (y * aStride);
			int bo = bOffset + (y * bStride);
			for (int x = 0; x < size; x++)
			{
				sad += Math.Abs(a[ao + x] - b[bo + x]);
			}
		}

		return sad;
	}

#if NET7_0_OR_GREATER
	private static int Sad16(ReadOnlySpan<byte> a, int aOffset, int aStride, ReadOnlySpan<byte> b, int bOffset, int bStride)
	{
		var acc = Vector128<ushort>.Zero;
		for (int y = 0; y < 16; y++)
		{
			var diff = AbsDiff(Load16(a, aOffset + (y * aStride)), Load16(b, bOffset + (y * bStride)));
			acc += Vector128.WidenLower(diff) + Vector128.WidenUpper(diff);
		}

		return HorizontalSum(acc);
	}

	private static int Sad8(ReadOnlySpan<byte> a, int aOffset, int aStride, ReadOnlySpan<byte> b, int bOffset, int bStride)
	{
		var acc = Vector128<ushort>.Zero;
		for (int y = 0; y < 8; y++)
		{
			var diff = AbsDiff(Load8(a, aOffset + (y * aStride)), Load8(b, bOffset + (y * bStride)));
			acc += Vector128.WidenLower(diff) + Vector128.WidenUpper(diff);
		}

		return HorizontalSum(acc);
	}

	private static int Sad4(ReadOnlySpan<byte> a, int aOffset, int aStride, ReadOnlySpan<byte> b, int bOffset, int bStride)
	{
		var diff = AbsDiff(Load4Rows(a, aOffset, aStride), Load4Rows(b, bOffset, bStride));
		return HorizontalSum(Vector128.WidenLower(diff) + Vector128.WidenUpper(diff));
	}

	private static int HorizontalSum(Vector128<ushort> lanes) =>
		(int)Vector128.Sum(Vector128.WidenLower(lanes) + Vector128.WidenUpper(lanes));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> AbsDiff(Vector128<byte> x, Vector128<byte> y) => Vector128.Max(x, y) - Vector128.Min(x, y);

	private static Vector128<byte> Load16(ReadOnlySpan<byte> span, int offset)
	{
		_ = span[offset + 15];
		return Vector128.LoadUnsafe(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), offset));
	}

	private static Vector128<byte> Load8(ReadOnlySpan<byte> span, int offset) =>
		Vector128.CreateScalar(MemoryMarshal.Read<ulong>(span.Slice(offset, 8))).AsByte();

	private static Vector128<byte> Load4Rows(ReadOnlySpan<byte> span, int origin, int stride) =>
		Vector128.Create(
			MemoryMarshal.Read<uint>(span.Slice(origin, 4)),
			MemoryMarshal.Read<uint>(span.Slice(origin + stride, 4)),
			MemoryMarshal.Read<uint>(span.Slice(origin + (2 * stride), 4)),
			MemoryMarshal.Read<uint>(span.Slice(origin + (3 * stride), 4))).AsByte();
#endif
}
