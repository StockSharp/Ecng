namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class AvifIspeBox
{
	public static AvifIspe Parse(byte[] data, AvifBox box)
	{
		if (box.PayloadLength < 12)
		{
			throw new AvifDecodingException("Truncated 'ispe' box.");
		}

		int offset = box.PayloadOffset + 4; // version(1) + flags(3)
		int width = checked((int)AvifBinaryReader.ReadUInt32(data, ref offset));
		int height = checked((int)AvifBinaryReader.ReadUInt32(data, ref offset));
		return new AvifIspe(width, height);
	}
}
