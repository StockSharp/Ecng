namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal readonly struct Vp8DecodedFrame
{
	public int Width { get; init; }

	public int Height { get; init; }
	public PixelFormat PixelFormat { get; init; }
	public byte[] Pixels { get; init; }
}
