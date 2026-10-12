namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Webp.Kernels;

internal static class Vp8LColorTransform
{
	public static void ApplyInverse(Span<uint> pixels, Vp8LTransform transform)
	{
		int width = transform.Xsize;
		int height = transform.Ysize;
		for (int y = 0; y < height; y++)
		{
			ApplyInverseRow(pixels.Slice(y * width, width), y, transform);
		}
	}

	public static void ApplyInverseRow(Span<uint> row, int y, Vp8LTransform transform)
	{
		var tileWidth = 1 << transform.Bits;
		var tilesPerRow = Vp8LMetaHuffmanImage.SubSampleSize(transform.Xsize, transform.Bits);
		var tileRow = transform.Data.AsSpan((y >> transform.Bits) * tilesPerRow, tilesPerRow);
		var kernel = Vp8LTransformKernelSelector.Instance;
		for (var x = 0; x < row.Length; x += tileWidth)
		{
			var (greenToRed, greenToBlue, redToBlue) = ExtractMultipliers(tileRow[x >> transform.Bits]);
			kernel.ColorTransformInverse(row.Slice(x, Math.Min(tileWidth, row.Length - x)), greenToRed, greenToBlue, redToBlue);
		}
	}

	private static (sbyte GreenToRed, sbyte GreenToBlue, sbyte RedToBlue) ExtractMultipliers(uint colorCode) =>
		((sbyte)(colorCode & 0xFF), (sbyte)((colorCode >> 8) & 0xFF), (sbyte)((colorCode >> 16) & 0xFF));
}
