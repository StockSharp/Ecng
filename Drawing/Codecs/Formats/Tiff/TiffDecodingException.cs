namespace Ecng.Drawing.Codecs.Formats.Tiff;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class TiffDecodingException : TiffFormatException
{
	public TiffDecodingException(string message)
		: base(message)
	{
	}
	public TiffDecodingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
