namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1TxSize
{
	public const int Tx4x4 = 0;
	public const int Tx8x8 = 1;
	public const int Tx16x16 = 2;
	public const int Tx32x32 = 3;
	public const int Tx64x64 = 4;
	public const int Tx4x8 = 5;
	public const int Tx8x4 = 6;
	public const int Tx8x16 = 7;
	public const int Tx16x8 = 8;
	public const int Tx16x32 = 9;
	public const int Tx32x16 = 10;
	public const int Tx32x64 = 11;
	public const int Tx64x32 = 12;
	public const int Tx4x16 = 13;
	public const int Tx16x4 = 14;
	public const int Tx8x32 = 15;
	public const int Tx32x8 = 16;
	public const int Tx16x64 = 17;
	public const int Tx64x16 = 18;

	public const int Count = 19;
}
