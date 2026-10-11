namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Alpha;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal enum WebpAlphaFilterMethods
{
	None = 0,
	Horizontal = 1,
	Vertical = 2,
	Gradient = 3,
}
