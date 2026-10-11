namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Vp8LTransform
{
	public Vp8LTransformTypes Type { get; init; }

	public int Xsize { get; init; }

	public int Ysize { get; init; }
	public int Bits { get; init; }
	public uint[] Data { get; init; }
}
