namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class WebpBitstreamHeaderPeek
{
	public static bool TryPeekVp8L(ReadOnlySpan<byte> chunkData, out int width, out int height, out bool alphaIsUsed)
	{
		if (chunkData.Length < 5 || chunkData[0] != 0x2F)
		{
			width = 0;
			height = 0;
			alphaIsUsed = false;
			return false;
		}

		uint bits = (uint)(chunkData[1] | (chunkData[2] << 8) | (chunkData[3] << 16) | (chunkData[4] << 24));
		width = (int)(bits & 0x3FFF) + 1;
		height = (int)((bits >> 14) & 0x3FFF) + 1;
		alphaIsUsed = ((bits >> 28) & 0x1) != 0;
		return true;
	}
	public static bool TryPeekVp8(ReadOnlySpan<byte> chunkData, out int width, out int height)
	{
		if (chunkData.Length < 10
			|| chunkData[3] != 0x9d || chunkData[4] != 0x01 || chunkData[5] != 0x2a)
		{
			width = 0;
			height = 0;
			return false;
		}

		width = (chunkData[6] | (chunkData[7] << 8)) & 0x3FFF;
		height = (chunkData[8] | (chunkData[9] << 8)) & 0x3FFF;
		return true;
	}
}
