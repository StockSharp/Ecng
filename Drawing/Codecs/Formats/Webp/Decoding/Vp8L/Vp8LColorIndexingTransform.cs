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
	public static uint[] ApplyInverse(ReadOnlySpan<uint> source, Vp8LTransform transform, bool rentFromPool = false)
	{
		var width = transform.Xsize;
		var packedWidth = Vp8LMetaHuffmanImage.SubSampleSize(width, transform.Bits);
		var count = checked(width * transform.Ysize);
		var destination = rentFromPool ? WebpBufferPool.SharedUInt32.Rent(count) : new uint[count];
		for (var y = 0; y < transform.Ysize; y++)
			ApplyInverseRow(source.Slice(y * packedWidth, packedWidth), destination.AsSpan(y * width, width), transform);
		return destination;
	}
	public static void ApplyInverseRow(ReadOnlySpan<uint> source, Span<uint> destination, Vp8LTransform transform)
	{
		var bitsPerPixel = 8 >> transform.Bits;
		var mask = (1u << bitsPerPixel) - 1;
		for (var x = 0; x < destination.Length; x++)
		{
			var packed = source[x >> transform.Bits] >> 8;
			var index = (packed >> ((x & ((1 << transform.Bits) - 1)) * bitsPerPixel)) & mask;
			destination[x] = transform.Data[index];
		}
	}
}
