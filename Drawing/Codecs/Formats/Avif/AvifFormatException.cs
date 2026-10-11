namespace Ecng.Drawing.Codecs.Formats.Avif;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal class AvifFormatException : ImageFormatException
{
	public AvifFormatException(string message)
		: base(message, "avif")
	{
	}
	public AvifFormatException(string message, Exception innerException)
		: base(message, "avif", innerException)
	{
	}
}
