namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Dct;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8ScalarInverseWht
{
	public static void Transform(ReadOnlySpan<short> input, Span<short> blockDc)
	{
		Span<int> tmp = stackalloc int[16];

		for (int i = 0; i < 4; i++)
		{
			int a0 = input[0 + i] + input[12 + i];
			int a1 = input[4 + i] + input[8 + i];
			int a2 = input[4 + i] - input[8 + i];
			int a3 = input[0 + i] - input[12 + i];

			tmp[0 + i] = a0 + a1;
			tmp[8 + i] = a0 - a1;
			tmp[4 + i] = a3 + a2;
			tmp[12 + i] = a3 - a2;
		}

		for (int i = 0; i < 4; i++)
		{
			int dc = tmp[(i * 4) + 0] + 3;
			int a0 = dc + tmp[(i * 4) + 3];
			int a1 = tmp[(i * 4) + 1] + tmp[(i * 4) + 2];
			int a2 = tmp[(i * 4) + 1] - tmp[(i * 4) + 2];
			int a3 = dc - tmp[(i * 4) + 3];

			blockDc[(4 * i) + 0] = (short)((a0 + a1) >> 3);
			blockDc[(4 * i) + 1] = (short)((a3 + a2) >> 3);
			blockDc[(4 * i) + 2] = (short)((a0 - a1) >> 3);
			blockDc[(4 * i) + 3] = (short)((a3 - a2) >> 3);
		}
	}
}