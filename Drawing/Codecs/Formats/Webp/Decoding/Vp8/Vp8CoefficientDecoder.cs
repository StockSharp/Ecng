namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.CompilerServices;

internal static class Vp8CoefficientDecoder
{
	public static byte[] ParseProbabilityUpdates(Vp8BoolDecoder br)
	{
		var probabilities = new byte[Vp8CoefficientProbabilities.FlatLength];
		Vp8CoefficientProbabilities.DefaultFlat.CopyTo(probabilities, 0);
		var updateProbabilities = Vp8CoefficientProbabilities.UpdateProbabilityFlat;

		for (int i = 0; i < Vp8CoefficientProbabilities.FlatLength; i++)
		{
			if (br.GetBit(updateProbabilities[i]) != 0)
			{
				probabilities[i] = (byte)br.GetValue(8);
			}
		}

		return probabilities;
	}
	public static int DecodeBlock(
		Vp8BoolDecoder br,
		byte[] probabilities,
		int planeType,
		int firstContext,
		int first,
		int dcQuant,
		int acQuant,
		Span<short> output)
	{
		int ctx = firstContext;
		int n = first;

		while (n < 16)
		{
			int band = Vp8CoefficientTrees.PositionToBand[n];
			var p = ProbabilitiesFor(probabilities, planeType, band, ctx);
			if (br.GetBit(p[0]) == 0)
			{
				return n;
			}

			while (br.GetBit(p[1]) == 0)
			{
				n++;
				if (n == 16)
				{
					return 16;
				}

				ctx = 0;
				band = Vp8CoefficientTrees.PositionToBand[n];
				p = ProbabilitiesFor(probabilities, planeType, band, ctx);
			}

			int magnitude;
			int nextCtx;
			if (br.GetBit(p[2]) == 0)
			{
				magnitude = 1;
				nextCtx = 1;
			}
			else
			{
				magnitude = DecodeLargeValue(br, p);
				nextCtx = 2;
			}

			int value = br.GetFlag() ? -magnitude : magnitude;
			int quant = n == 0 ? dcQuant : acQuant;
			output[Vp8ZigZag.Order[n]] = (short)(value * quant);

			n++;
			ctx = nextCtx;
		}

		return 16;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ReadOnlySpan<byte> ProbabilitiesFor(byte[] probabilities, int planeType, int band, int ctx) =>
		probabilities.AsSpan(
			Vp8CoefficientProbabilities.FlatOffset(planeType, band, ctx),
			Vp8CoefficientProbabilities.NumProbabilities);

	private static int DecodeLargeValue(Vp8BoolDecoder br, ReadOnlySpan<byte> p)
	{
		int v;
		if (br.GetBit(p[3]) == 0)
		{
			v = br.GetBit(p[4]) == 0 ? 2 : 3 + br.GetBit(p[5]);
		}
		else if (br.GetBit(p[6]) == 0)
		{
			if (br.GetBit(p[7]) == 0)
			{
				v = 5 + br.GetBit(159);
			}
			else
			{
				v = 7 + (2 * br.GetBit(165));
				v += br.GetBit(145);
			}
		}
		else
		{
			int bit1 = br.GetBit(p[8]);
			int bit0 = br.GetBit(p[9 + bit1]);
			int cat = (2 * bit1) + bit0;
			byte[] table = cat switch
			{
				0 => Vp8CoefficientTrees.Cat3,
				1 => Vp8CoefficientTrees.Cat4,
				2 => Vp8CoefficientTrees.Cat5,
				_ => Vp8CoefficientTrees.Cat6,
			};

			v = 0;
			foreach (byte prob in table)
			{
				v += v + br.GetBit(prob);
			}

			v += 3 + (8 << cat);
		}

		return v;
	}
}
