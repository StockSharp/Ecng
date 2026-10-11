namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal sealed class Vp8MacroblockModes
{
	public int Segment;

	public bool Skip;

	public bool IsI4x4;
	public int YMode;

	public int UvMode;
	public readonly int[] SubModes = new int[16];
}
