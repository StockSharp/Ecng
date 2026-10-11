namespace Ecng.Drawing.Codecs.Formats.Tiff;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal class TiffFormatException : ImageFormatException
{
	public TiffFormatException(string message)
		: base(message, "tiff")
	{
	}
	public TiffFormatException(string message, Exception innerException)
		: base(message, "tiff", innerException)
	{
	}
}
