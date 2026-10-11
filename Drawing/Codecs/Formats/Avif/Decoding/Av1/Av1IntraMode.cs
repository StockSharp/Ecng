namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1IntraMode
{
	public const int DcPred = 0;
	public const int VPred = 1;
	public const int HPred = 2;
	public const int D45Pred = 3;
	public const int D135Pred = 4;
	public const int D113Pred = 5;
	public const int D157Pred = 6;
	public const int D203Pred = 7;
	public const int D67Pred = 8;
	public const int SmoothPred = 9;
	public const int SmoothVPred = 10;
	public const int SmoothHPred = 11;
	public const int PaethPred = 12;
	public const int UvCflPred = 13;

	public const int Count = 13;
	public static bool IsDirectional(int mode) => mode is >= VPred and <= D67Pred;
}
