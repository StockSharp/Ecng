namespace Ecng.Drawing.Codecs.Formats.Avif;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class AvifUnsupportedFeatureException : AvifFormatException
{
	public AvifUnsupportedFeatureException(string message)
		: base(message)
	{
	}
	public AvifUnsupportedFeatureException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
