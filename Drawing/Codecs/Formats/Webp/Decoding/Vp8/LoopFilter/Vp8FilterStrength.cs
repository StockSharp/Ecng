namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.LoopFilter;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal readonly struct Vp8FilterStrength
{
	public int Limit { get; init; }

	public int InteriorLimit { get; init; }

	public int HevThreshold { get; init; }

	public bool IsFilteringEnabled => Limit != 0;
}
