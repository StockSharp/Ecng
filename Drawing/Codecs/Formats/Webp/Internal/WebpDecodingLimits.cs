namespace Ecng.Drawing.Codecs.Formats.Webp.Internal;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class WebpDecodingLimits
{
	public const int MaxDimension = 16_384;
	public const int MaxHuffmanCodeLength = 15;
	public const int MaxColorCacheBits = 11;
	public const int MaxGreenAlphabetSize = 256 + 24 + (1 << MaxColorCacheBits);
	public const int DistanceAlphabetSize = 40;
	public const int MaxTransforms = 4;
	public const int MaxColorIndexingPaletteSize = 256;
	public const int MaxMetaHuffmanGroups = 1 << 16;
	public const int MaxVp8Partitions = 16;
	public const int MaxFrameCount = Animation.MaxFrames;
}
