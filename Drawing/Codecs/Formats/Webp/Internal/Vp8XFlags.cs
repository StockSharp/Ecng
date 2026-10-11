namespace Ecng.Drawing.Codecs.Formats.Webp.Internal;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8XFlags
{
	public const byte IccBit = 0x20;
	public const byte AlphaBit = 0x10;
	public const byte ExifBit = 0x08;
	public const byte XmpBit = 0x04;
	public const byte AnimationBit = 0x02;
	public const byte AnmfDisposeToBackgroundBit = 0x01;
	public const byte AnmfDoNotBlendBit = 0x02;
}
