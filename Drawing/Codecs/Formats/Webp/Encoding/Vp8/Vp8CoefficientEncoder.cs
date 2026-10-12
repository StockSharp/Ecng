namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

internal static class Vp8CoefficientEncoder
{
	public static void EncodeBlock(
		Vp8BoolEncoder bw,
		byte[] probabilities,
		int planeType,
		int firstContext,
		int first,
		ReadOnlySpan<short> quantized,
		int last)
	{
		int ctx = firstContext;
		int n = first;

		while (n < 16)
		{
			int band = Vp8CoefficientTrees.PositionToBand[n];
			var p = ProbabilitiesFor(probabilities, planeType, band, ctx);

			if (n >= last)
			{
				bw.PutBit(0, p[0]); // End of block: every remaining position is zero.
				return;
			}

			bw.PutBit(1, p[0]); // Not end of block: at least one more nonzero coefficient follows.

			while (quantized[n] == 0)
			{
				bw.PutBit(0, p[1]); // This position is zero; continue the run.
				n++;
				ctx = 0;
				band = Vp8CoefficientTrees.PositionToBand[n];
				p = ProbabilitiesFor(probabilities, planeType, band, ctx);
			}

			bw.PutBit(1, p[1]); // Not zero.

			int magnitude = Math.Abs((int)quantized[n]);
			int nextCtx;
			if (magnitude == 1)
			{
				bw.PutBit(0, p[2]);
				nextCtx = 1;
			}
			else
			{
				bw.PutBit(1, p[2]);
				EncodeLargeValue(bw, p, magnitude);
				nextCtx = 2;
			}

			bw.PutFlag(quantized[n] < 0);

			n++;
			ctx = nextCtx;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ReadOnlySpan<byte> ProbabilitiesFor(byte[] probabilities, int planeType, int band, int ctx) =>
		probabilities.AsSpan(
			Vp8CoefficientProbabilities.FlatOffset(planeType, band, ctx),
			Vp8CoefficientProbabilities.NumProbabilities);

	private static void EncodeLargeValue(Vp8BoolEncoder bw, ReadOnlySpan<byte> p, int magnitude)
	{
		if (magnitude < 5)
		{
			bw.PutBit(0, p[3]);
			if (magnitude == 2)
			{
				bw.PutBit(0, p[4]);
			}
			else
			{
				bw.PutBit(1, p[4]);
				bw.PutBit(magnitude - 3, p[5]); // magnitude 3 -> 0, magnitude 4 -> 1.
			}

			return;
		}

		bw.PutBit(1, p[3]);

		if (magnitude < 11)
		{
			bw.PutBit(0, p[6]);
			if (magnitude < 7)
			{
				bw.PutBit(0, p[7]);
				bw.PutBit(magnitude - 5, 159);
			}
			else
			{
				bw.PutBit(1, p[7]);
				int rem = magnitude - 7;
				bw.PutBit((rem >> 1) & 1, 165);
				bw.PutBit(rem & 1, 145);
			}

			return;
		}

		bw.PutBit(1, p[6]);

		int cat;
		byte[] table;
		if (magnitude < 19)
		{
			cat = 0;
			table = Vp8CoefficientTrees.Cat3;
		}
		else if (magnitude < 35)
		{
			cat = 1;
			table = Vp8CoefficientTrees.Cat4;
		}
		else if (magnitude < 67)
		{
			cat = 2;
			table = Vp8CoefficientTrees.Cat5;
		}
		else
		{
			cat = 3;
			table = Vp8CoefficientTrees.Cat6;
		}

		int bit1 = cat >> 1;
		int bit0 = cat & 1;
		bw.PutBit(bit1, p[8]);
		bw.PutBit(bit0, p[9 + bit1]);

		int extra = magnitude - (3 + (8 << cat));
		int numBits = table.Length;
		for (int k = 0; k < numBits; k++)
		{
			int bitPos = numBits - 1 - k;
			int bit = (extra >> bitPos) & 1;
			bw.PutBit(bit, table[k]);
		}
	}
}
