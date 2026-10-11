namespace Ecng.Drawing.Codecs.Formats.Avif.Internal;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class AvifDecodingLimits
{
	public const int MaxDimension = 16_384;
	public const long MaxPixelCount = RasterImage.MaxPixels;
	public const long MaxFileSize = 128L * 1024 * 1024;
	public const long MaxItemDataLength = MaxFileSize;
	public const int MaxBoxNestingDepth = 12;
	public const int MaxItemCount = 4_096;
	public const int MaxGridTiles = 256;
}
