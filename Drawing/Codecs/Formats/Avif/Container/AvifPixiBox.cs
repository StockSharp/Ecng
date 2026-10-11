namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class AvifPixiBox
{
	public static AvifPixi Parse(byte[] data, AvifBox box)
	{
		if (box.PayloadLength < 5)
		{
			throw new AvifDecodingException("Truncated 'pixi' box.");
		}

		int offset = box.PayloadOffset + 4; // version(1) + flags(3)
		int numChannels = AvifBinaryReader.ReadUInt8(data, ref offset);

		if (box.PayloadOffset + box.PayloadLength < offset + numChannels)
		{
			throw new AvifDecodingException("Truncated 'pixi' box.");
		}

		var bits = new List<int>(numChannels);
		for (int i = 0; i < numChannels; i++)
		{
			bits.Add(AvifBinaryReader.ReadUInt8(data, ref offset));
		}

		return new AvifPixi(bits);
	}
}
