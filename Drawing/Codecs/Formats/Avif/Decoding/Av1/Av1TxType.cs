namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1TxType
{
	public const int DctDct = 0;
	public const int AdstDct = 1;
	public const int DctAdst = 2;
	public const int AdstAdst = 3;
	public const int FlipadstDct = 4;
	public const int DctFlipadst = 5;
	public const int FlipadstFlipadst = 6;
	public const int AdstFlipadst = 7;
	public const int FlipadstAdst = 8;
	public const int Idtx = 9;
	public const int VDct = 10;
	public const int HDct = 11;
	public const int VAdst = 12;
	public const int HAdst = 13;
	public const int VFlipadst = 14;
	public const int HFlipadst = 15;

	public const int Count = 16;
}
