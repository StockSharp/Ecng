namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1ObuType
{
	public const int SequenceHeader = 1;
	public const int TemporalDelimiter = 2;
	public const int FrameHeader = 3;
	public const int TileGroup = 4;
	public const int Metadata = 5;
	public const int Frame = 6;
	public const int RedundantFrameHeader = 7;
	public const int TileList = 8;
	public const int Padding = 15;
}
