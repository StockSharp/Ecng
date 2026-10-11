namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Alpha;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal readonly record struct WebpAlphaHeader(WebpAlphaCompressionMethods CompressionMethod, WebpAlphaFilterMethods FilterMethod)
{
	public static WebpAlphaHeader Parse(byte headerByte)
	{
		int compression = headerByte & 0x03;
		int filter = (headerByte >> 2) & 0x03;

		// Preprocessing bits (headerByte >> 4 & 0x03) record an encoder-side level-reduction dithering hint —
		// informational only, nothing for the decoder to undo. Bits 6-7 are reserved and ignored.
		if (compression > 1)
		{
			throw new WebpDecodingException($"Unsupported WebP alpha compression method {compression}.");
		}

		return new WebpAlphaHeader((WebpAlphaCompressionMethods)compression, (WebpAlphaFilterMethods)filter);
	}
}
