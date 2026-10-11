namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class AvifAv1ConfigBox
{
	public static AvifAv1Config Parse(byte[] data, AvifBox box)
	{
		if (box.PayloadLength < 4)
		{
			throw new AvifDecodingException("Truncated 'av1C' box.");
		}

		int offset = box.PayloadOffset;
		byte b0 = data[offset];
		byte b1 = data[offset + 1];
		byte b2 = data[offset + 2];

		bool marker = (b0 & 0x80) != 0;
		int version = b0 & 0x7F;
		if (!marker || version != 1)
		{
			throw new AvifDecodingException($"Unsupported AV1CodecConfigurationRecord marker/version (marker={marker}, version={version}).");
		}

		int seqProfile = (b1 >> 5) & 0x7;
		int seqLevelIdx0 = b1 & 0x1F;
		bool seqTier0 = (b2 & 0x80) != 0;
		bool highBitdepth = (b2 & 0x40) != 0;
		bool twelveBit = (b2 & 0x20) != 0;
		bool monochrome = (b2 & 0x10) != 0;
		bool subsamplingX = (b2 & 0x08) != 0;
		bool subsamplingY = (b2 & 0x04) != 0;
		int chromaSamplePosition = b2 & 0x03;

		// Cheapest, earliest point to reject 12-bit AVIF -- well before any AV1 bitstream decode work.
		if (twelveBit)
		{
			throw new AvifUnsupportedFeatureException("12-bit AVIF is not supported.");
		}

		return new AvifAv1Config(
			seqProfile,
			seqLevelIdx0,
			seqTier0,
			highBitdepth,
			twelveBit,
			monochrome,
			subsamplingX,
			subsamplingY,
			chromaSamplePosition);
	}
}
