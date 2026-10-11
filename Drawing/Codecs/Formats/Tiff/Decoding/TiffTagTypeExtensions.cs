namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class TiffTagTypeExtensions
{
	public static int GetByteSize(this TiffTagTypes type) => type switch
	{
		TiffTagTypes.Byte or TiffTagTypes.Ascii or TiffTagTypes.SByte or TiffTagTypes.Undefined => 1,
		TiffTagTypes.Short or TiffTagTypes.SShort => 2,
		TiffTagTypes.Long or TiffTagTypes.SLong or TiffTagTypes.Float => 4,
		TiffTagTypes.Rational or TiffTagTypes.SRational or TiffTagTypes.Double => 8,
		_ => 0,
	};
}
