namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Webp.Internal;

internal static class Vp8LColorIndexingTransform
{
	public static uint[] ExpandPalette(uint[] rawDeltas, int numColors, int bits)
	{
		int finalCount = 1 << (8 >> bits);
		var palette = new uint[finalCount];

		if (numColors > 0)
		{
			palette[0] = rawDeltas[0];
			for (int i = 1; i < numColors; i++)
			{
				palette[i] = Vp8LPixelMath.AddWrapping(palette[i - 1], rawDeltas[i]);
			}
		}

		return palette;
	}
	public static uint[] ApplyInverse(ReadOnlySpan<uint> src, Vp8LTransform transform, bool rentFromPool = false)
	{
		int fullWidth = transform.Xsize;
		int height = transform.Ysize;
		int bits = transform.Bits;
		var palette = transform.Data!;
		int bitsPerPixel = 8 >> bits;

		long pixelCount = (long)fullWidth * height;
		var dst = rentFromPool ? WebpBufferPool.SharedUInt32.Rent((int)pixelCount) : new uint[pixelCount];

		if (bitsPerPixel == 8)
		{
			// No packing: exactly one index per pixel, same width as the destination. Bounded by pixelCount,
			// not dst.Length -- a pool-rented dst may come back larger than pixelCount (an ArrayPool
			// contract), and src (always exactly pixelCount long) would throw past that bound anyway.
			for (int i = 0; i < pixelCount; i++)
			{
				int index = (int)((src[i] >> 8) & 0xFF);
				dst[i] = palette[index];
			}

			return dst;
		}

		int pixelsPerByte = 1 << bits;
		uint bitMask = (uint)((1 << bitsPerPixel) - 1);
		int srcWidth = Vp8LMetaHuffmanImage.SubSampleSize(fullWidth, bits);

		for (int y = 0; y < height; y++)
		{
			int srcRowBase = y * srcWidth;
			int dstRowBase = y * fullWidth;
			uint packed = 0;
			int srcX = 0;

			for (int x = 0; x < fullWidth; x++)
			{
				if (x % pixelsPerByte == 0)
				{
					packed = (src[srcRowBase + srcX] >> 8) & 0xFF;
					srcX++;
				}

				int index = (int)(packed & bitMask);
				dst[dstRowBase + x] = palette[index];
				packed >>= bitsPerPixel;
			}
		}

		return dst;
	}
}
