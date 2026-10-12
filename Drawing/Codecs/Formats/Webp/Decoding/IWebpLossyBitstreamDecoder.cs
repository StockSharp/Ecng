namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal interface IWebpLossyBitstreamDecoder
{
	Vp8DecodedFrame Decode(ReadOnlyMemory<byte> vp8Bytes, byte[] alphaPlane, ImageProcessingLimits limits);
}
