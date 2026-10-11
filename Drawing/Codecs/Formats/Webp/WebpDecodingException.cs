namespace Ecng.Drawing.Codecs.Formats.Webp;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class WebpDecodingException : WebpFormatException
{
	public WebpDecodingException(string message)
		: base(message)
	{
	}
	public WebpDecodingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
