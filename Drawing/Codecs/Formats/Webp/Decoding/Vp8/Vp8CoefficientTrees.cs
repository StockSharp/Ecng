namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8CoefficientTrees
{
	public static readonly int[] PositionToBand =
	[
		0, 1, 2, 3, 6, 4, 5, 6, 6, 6, 6, 6, 6, 6, 6, 7,
		0, // sentinel, never actually indexed at position 16
	];
	public static readonly byte[] Cat3 = [173, 148, 140];
	public static readonly byte[] Cat4 = [176, 155, 140, 135];
	public static readonly byte[] Cat5 = [180, 157, 141, 134, 130];
	public static readonly byte[] Cat6 = [254, 254, 243, 230, 196, 177, 153, 140, 133, 130, 129];
}