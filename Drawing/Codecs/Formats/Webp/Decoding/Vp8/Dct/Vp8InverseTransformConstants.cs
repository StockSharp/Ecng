namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Dct;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8InverseTransformConstants
{
	public const int C1 = 20091;
	public const int C2 = 35468;

	public static int Mul1(int v) => v + ((v * C1) >> 16);

	public static int Mul2(int v) => (v * C2) >> 16;
}