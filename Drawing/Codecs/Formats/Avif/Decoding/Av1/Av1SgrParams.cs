namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Av1SgrParams
{
	public static readonly int[][] Table =
	[
		[2, 12, 1, 4],
		[2, 15, 1, 6],
		[2, 18, 1, 8],
		[2, 21, 1, 9],
		[2, 24, 1, 10],
		[2, 29, 1, 11],
		[2, 36, 1, 12],
		[2, 45, 1, 13],
		[2, 56, 1, 14],
		[2, 68, 1, 15],
		[0, 0, 1, 5],
		[0, 0, 1, 8],
		[0, 0, 1, 11],
		[0, 0, 1, 14],
		[2, 30, 0, 0],
		[2, 75, 0, 0],
	];
}
