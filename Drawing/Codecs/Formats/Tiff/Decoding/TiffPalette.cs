namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class TiffPalette
{
	public static byte[] Resolve(uint[] colorMap, int bitsPerSample)
	{
		int entryCount = 1 << bitsPerSample;
		var rgb = new byte[entryCount * 3];

		for (int i = 0; i < entryCount; i++)
		{
			rgb[(i * 3) + 0] = (byte)(colorMap[i] >> 8);
			rgb[(i * 3) + 1] = (byte)(colorMap[entryCount + i] >> 8);
			rgb[(i * 3) + 2] = (byte)(colorMap[(entryCount * 2) + i] >> 8);
		}

		return rgb;
	}
}
