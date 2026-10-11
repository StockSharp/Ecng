namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Vp8PredictionModes
{
	public const int BDcPred = 0;
	public const int BTmPred = 1;
	public const int BVePred = 2;
	public const int BHePred = 3;
	public const int BRdPred = 4;
	public const int BVrPred = 5;
	public const int BLdPred = 6;
	public const int BVlPred = 7;
	public const int BHdPred = 8;
	public const int BHuPred = 9;
	public const int NumBModes = 10;

	public const int DcPred = BDcPred;
	public const int VPred = BVePred;
	public const int HPred = BHePred;
	public const int TmPred = BTmPred;
	public const int BPred = NumBModes;
}
