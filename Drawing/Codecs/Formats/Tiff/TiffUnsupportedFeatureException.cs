namespace Ecng.Drawing.Codecs.Formats.Tiff;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class TiffUnsupportedFeatureException : TiffFormatException
{
	public TiffUnsupportedFeatureException(string message)
		: base(message)
	{
	}
	public TiffUnsupportedFeatureException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
