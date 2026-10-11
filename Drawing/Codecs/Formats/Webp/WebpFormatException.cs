namespace Ecng.Drawing.Codecs.Formats.Webp;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal class WebpFormatException : ImageFormatException
{
	public WebpFormatException(string message)
		: base(message, "webp")
	{
	}
	public WebpFormatException(string message, Exception innerException)
		: base(message, "webp", innerException)
	{
	}
}
