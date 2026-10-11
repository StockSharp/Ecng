namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1PartitionType
{
	public const int None = 0;
	public const int Horz = 1;
	public const int Vert = 2;
	public const int Split = 3;
	public const int HorzA = 4;
	public const int HorzB = 5;
	public const int VertA = 6;
	public const int VertB = 7;
	public const int Horz4 = 8;
	public const int Vert4 = 9;
}
