namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1TxClass
{
	public const int Class2D = 0;
	public const int ClassHoriz = 1;
	public const int ClassVert = 2;
	public static int Get(int txType) => txType switch
	{
		Av1TxType.VDct or Av1TxType.VAdst or Av1TxType.VFlipadst => ClassVert,
		Av1TxType.HDct or Av1TxType.HAdst or Av1TxType.HFlipadst => ClassHoriz,
		_ => Class2D,
	};
}
