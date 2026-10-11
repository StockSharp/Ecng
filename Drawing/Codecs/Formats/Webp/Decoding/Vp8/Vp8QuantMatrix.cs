namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal readonly struct Vp8QuantMatrix
{
	public int Y1Dc { get; init; }

	public int Y1Ac { get; init; }

	public int Y2Dc { get; init; }

	public int Y2Ac { get; init; }

	public int UvDc { get; init; }

	public int UvAc { get; init; }
}
