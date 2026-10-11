namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Vp8LPixelMath
{
	public static uint AddWrapping(uint a, uint b)
	{
		uint alphaAndGreen = (a & 0xFF00FF00u) + (b & 0xFF00FF00u);
		uint redAndBlue = (a & 0x00FF00FFu) + (b & 0x00FF00FFu);
		return (alphaAndGreen & 0xFF00FF00u) | (redAndBlue & 0x00FF00FFu);
	}
	public static uint SubtractWrapping(uint a, uint b)
	{
		byte r0 = (byte)((byte)a - (byte)b);
		byte r1 = (byte)((byte)(a >> 8) - (byte)(b >> 8));
		byte r2 = (byte)((byte)(a >> 16) - (byte)(b >> 16));
		byte r3 = (byte)((byte)(a >> 24) - (byte)(b >> 24));
		return r0 | ((uint)r1 << 8) | ((uint)r2 << 16) | ((uint)r3 << 24);
	}
}
