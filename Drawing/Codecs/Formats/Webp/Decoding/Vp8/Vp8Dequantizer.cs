namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Vp8Dequantizer
{
	private static readonly int[] KDcTable =
	[
		4, 5, 6, 7, 8, 9, 10, 10, 11, 12, 13, 14, 15, 16, 17,
		17, 18, 19, 20, 20, 21, 21, 22, 22, 23, 23, 24, 25, 25, 26,
		27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 37, 38, 39, 40,
		41, 42, 43, 44, 45, 46, 46, 47, 48, 49, 50, 51, 52, 53, 54,
		55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
		70, 71, 72, 73, 74, 75, 76, 76, 77, 78, 79, 80, 81, 82, 83,
		84, 85, 86, 87, 88, 89, 91, 93, 95, 96, 98, 100, 101, 102, 104,
		106, 108, 110, 112, 114, 116, 118, 122, 124, 126, 128, 130, 132, 134, 136,
		138, 140, 143, 145, 148, 151, 154, 157,
	];

	private static readonly int[] KAcTable =
	[
		4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18,
		19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33,
		34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
		49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 62, 64, 66, 68,
		70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94, 96, 98,
		100, 102, 104, 106, 108, 110, 112, 114, 116, 119, 122, 125, 128, 131, 134,
		137, 140, 143, 146, 149, 152, 155, 158, 161, 164, 167, 170, 173, 177, 181,
		185, 189, 193, 197, 201, 205, 209, 213, 217, 221, 225, 229, 234, 239, 245,
		249, 254, 259, 264, 269, 274, 279, 284,
	];

	private static int Clip(int v, int max) => v < 0 ? 0 : v > max ? max : v;
	public static Vp8QuantMatrix[] Resolve(Vp8QuantIndices indices, Vp8SegmentHeader segmentHeader)
	{
		var result = new Vp8QuantMatrix[Vp8SegmentHeader.NumSegments];

		for (int i = 0; i < Vp8SegmentHeader.NumSegments; i++)
		{
			int q;
			if (segmentHeader.UseSegment)
			{
				q = segmentHeader.Quantizer[i];
				if (!segmentHeader.AbsoluteDelta)
				{
					q += indices.BaseQ0;
				}
			}
			else if (i > 0)
			{
				result[i] = result[0];
				continue;
			}
			else
			{
				q = indices.BaseQ0;
			}

			int y1Dc = KDcTable[Clip(q + indices.Y1DcDelta, 127)];
			int y1Ac = KAcTable[Clip(q, 127)];

			int y2Dc = KDcTable[Clip(q + indices.Y2DcDelta, 127)] * 2;
			// For all x in [0,284], x*155/100 is bitwise equal to (x*101581)>>16 (libwebp's own comment).
			int y2Ac = (KAcTable[Clip(q + indices.Y2AcDelta, 127)] * 101581) >> 16;
			if (y2Ac < 8)
			{
				y2Ac = 8;
			}

			int uvDc = KDcTable[Clip(q + indices.UvDcDelta, 117)];
			int uvAc = KAcTable[Clip(q + indices.UvAcDelta, 127)];

			result[i] = new Vp8QuantMatrix
			{
				Y1Dc = y1Dc,
				Y1Ac = y1Ac,
				Y2Dc = y2Dc,
				Y2Ac = y2Ac,
				UvDc = uvDc,
				UvAc = uvAc,
			};
		}

		return result;
	}
}
