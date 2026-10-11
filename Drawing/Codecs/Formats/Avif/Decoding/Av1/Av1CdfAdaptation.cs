namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Av1CdfAdaptation
{
	public static void AdaptCdf(Span<ushort> cdf, int n, int symbol)
	{
		int count = cdf[n];
		int rate = 3 + (count > 15 ? 1 : 0) + (count > 31 ? 1 : 0) + Math.Min(FloorLog2((uint)n), 2);

		int tmp = 0;
		for (int i = 0; i < n - 1; i++)
		{
			tmp = i == symbol ? 1 << 15 : tmp;
			if (tmp < cdf[i])
			{
				cdf[i] -= (ushort)((cdf[i] - tmp) >> rate);
			}
			else
			{
				cdf[i] += (ushort)((tmp - cdf[i]) >> rate);
			}
		}

		if (count < 32)
		{
			cdf[n] = (ushort)(count + 1);
		}
	}

	public static int FloorLog2(uint x) => x == 0 ? -1 : System.Numerics.BitOperations.Log2(x);
}
