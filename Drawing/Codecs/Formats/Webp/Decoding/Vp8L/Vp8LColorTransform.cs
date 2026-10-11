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
		int bits = transform.Bits;
		int tileWidth = 1 << bits;
		var tileData = transform.Data!;
		int tilesPerRow = Vp8LMetaHuffmanImage.SubSampleSize(width, bits);
		var kernel = Vp8LTransformKernelSelector.Instance;

		for (int y = 0; y < height; y++)
		{
			var row = pixels.Slice(y * width, width);
			var tileRow = tileData.AsSpan((y >> bits) * tilesPerRow, tilesPerRow);

			int x = 0;
			int tileIndex = 0;
			while (x < width)
			{
				int runLength = Math.Min(tileWidth, width - x);
				var (greenToRed, greenToBlue, redToBlue) = ExtractMultipliers(tileRow[tileIndex]);
				kernel.ColorTransformInverse(row.Slice(x, runLength), greenToRed, greenToBlue, redToBlue);
				x += runLength;
				tileIndex++;
			}
		}
	}

	private static (sbyte GreenToRed, sbyte GreenToBlue, sbyte RedToBlue) ExtractMultipliers(uint colorCode) =>
		((sbyte)(colorCode & 0xFF), (sbyte)((colorCode >> 8) & 0xFF), (sbyte)((colorCode >> 16) & 0xFF));
}
