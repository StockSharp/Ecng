namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1BlockSize
{
	public const int Block4x4 = 0;
	public const int Block4x8 = 1;
	public const int Block8x4 = 2;
	public const int Block8x8 = 3;
	public const int Block8x16 = 4;
	public const int Block16x8 = 5;
	public const int Block16x16 = 6;
	public const int Block16x32 = 7;
	public const int Block32x16 = 8;
	public const int Block32x32 = 9;
	public const int Block32x64 = 10;
	public const int Block64x32 = 11;
	public const int Block64x64 = 12;
	public const int Block64x128 = 13;
	public const int Block128x64 = 14;
	public const int Block128x128 = 15;
	public const int Block4x16 = 16;
	public const int Block16x4 = 17;
	public const int Block8x32 = 18;
	public const int Block32x8 = 19;
	public const int Block16x64 = 20;
	public const int Block64x16 = 21;
	public const int Invalid = -1;

	public const int Count = 22;
}
