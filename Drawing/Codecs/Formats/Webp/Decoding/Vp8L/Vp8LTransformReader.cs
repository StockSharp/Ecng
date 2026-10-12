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

internal static class Vp8LTransformReader
{
	public static List<Vp8LTransform> ReadTransforms(Vp8LBitReader reader, ref int xsize, int ysize, ImageProcessingLimits limits)
	{
		var transforms = new List<Vp8LTransform>();
		int seenMask = 0;

		while (reader.ReadBits(1) != 0)
		{
			if (transforms.Count >= WebpDecodingLimits.MaxTransforms)
			{
				throw new WebpDecodingException("VP8L bitstream declares more transforms than the maximum of one per kind.");
			}

			var type = (Vp8LTransformTypes)reader.ReadBits(2);
			int typeBit = 1 << (int)type;
			if ((seenMask & typeBit) != 0)
			{
				throw new WebpDecodingException($"VP8L bitstream declares the {type} transform more than once.");
			}

			seenMask |= typeBit;

			// Captured *before* this transform can mutate the running width (only ColorIndexing does) --
			// this is the width/height its own inverse must operate over when transforms are unwound later.
			int entryXsize = xsize;
			int entryYsize = ysize;

			Vp8LTransform transform = type switch
			{
				Vp8LTransformTypes.Predictor or Vp8LTransformTypes.CrossColor =>
					ReadTileParameterTransform(reader, type, entryXsize, entryYsize, limits),
				Vp8LTransformTypes.ColorIndexing =>
					ReadColorIndexingTransform(reader, entryXsize, entryYsize, ref xsize, limits),
				Vp8LTransformTypes.SubtractGreen =>
					new Vp8LTransform { Type = type, Xsize = entryXsize, Ysize = entryYsize },
				_ => throw new WebpDecodingException($"Unknown VP8L transform type {(int)type}."),
			};

			transforms.Add(transform);
		}

		return transforms;
	}

	private static Vp8LTransform ReadTileParameterTransform(Vp8LBitReader reader, Vp8LTransformTypes type, int xsize, int ysize, ImageProcessingLimits limits)
	{
		int bits = 2 + (int)reader.ReadBits(3);
		int blockXsize = Vp8LMetaHuffmanImage.SubSampleSize(xsize, bits);
		int blockYsize = Vp8LMetaHuffmanImage.SubSampleSize(ysize, bits);
		var data = Vp8LPixelDecoder.DecodeImageStream(reader, blockXsize, blockYsize, allowRecursion: false, limits);

		return new Vp8LTransform { Type = type, Xsize = xsize, Ysize = ysize, Bits = bits, Data = data };
	}

	private static Vp8LTransform ReadColorIndexingTransform(Vp8LBitReader reader, int xsize, int ysize, ref int outerXsize, ImageProcessingLimits limits)
	{
		int numColors = (int)reader.ReadBits(8) + 1;
		int bits = numColors switch
		{
			> 16 => 0,
			> 4 => 1,
			> 2 => 2,
			_ => 3,
		};

		var rawPalette = Vp8LPixelDecoder.DecodeImageStream(reader, numColors, 1, allowRecursion: false, limits);
		var palette = Vp8LColorIndexingTransform.ExpandPalette(rawPalette, numColors, bits);

		// From this point on, the main pixel stream (and any transform declared after this one) is decoded
		// at the packed, reduced width -- each byte of the green channel holds multiple palette indices.
		outerXsize = Vp8LMetaHuffmanImage.SubSampleSize(xsize, bits);

		return new Vp8LTransform { Type = Vp8LTransformTypes.ColorIndexing, Xsize = xsize, Ysize = ysize, Bits = bits, Data = palette };
	}
}
