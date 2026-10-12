namespace Ecng.Drawing.Codecs.Formats.Tiff.Internal;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class TiffDecodingLimits
{
	public const long MaxDeclaredStripByteCount = 128L * 1024 * 1024;
	public const int MaxIfdEntryCount = 65_535;
	public const long MaxFileSize = 128L * 1024 * 1024;
	public const int MaxArrayEntryCount = 4_000_000;
}
