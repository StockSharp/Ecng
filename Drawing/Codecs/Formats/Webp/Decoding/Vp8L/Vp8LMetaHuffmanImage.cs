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

internal sealed class Vp8LMetaHuffmanImage
{
	private readonly int[] _groupIndexPerTile;
	private readonly int _tileXSize;
	private readonly int _subsampleBits;

	private Vp8LMetaHuffmanImage(int[] groupIndexPerTile, int tileXSize, int subsampleBits)
	{
		_groupIndexPerTile = groupIndexPerTile;
		_tileXSize = tileXSize;
		_subsampleBits = subsampleBits;
	}
	public int GetGroupIndex(int x, int y) => _groupIndexPerTile[(_tileXSize * (y >> _subsampleBits)) + (x >> _subsampleBits)];
	public long WorkingBytes => (long)_groupIndexPerTile.Length * 4;
	public static Vp8LHuffmanGroup[] ReadGroups(Vp8LBitReader reader, int xsize, int ysize, int colorCacheBits, bool allowRecursion, ImageProcessingLimits limits, out Vp8LMetaHuffmanImage metaImage)
	{
		if (allowRecursion && reader.ReadBits(1) != 0)
		{
			// MIN_HUFFMAN_BITS + NUM_HUFFMAN_BITS-wide field, verified against libwebp's format_constants.h
			// (2 + ReadBits(3)) -- the same shape as the predictor/color transforms' own tile-size field.
			int subsampleBits = 2 + (int)reader.ReadBits(3);
			int tileXSize = SubSampleSize(xsize, subsampleBits);
			int tileYSize = SubSampleSize(ysize, subsampleBits);

			uint[] tilePixels = Vp8LPixelDecoder.DecodeImageStream(reader, tileXSize, tileYSize, allowRecursion: false, limits);

			int tileCount = tileXSize * tileYSize;
			var groupIndexPerTile = new int[tileCount];
			int maxGroupIndex = -1;
			for (int i = 0; i < tileCount; i++)
			{
				// The group index is packed into the red+green bytes of the decoded entropy-image pixel.
				int group = (int)((tilePixels[i] >> 8) & 0xFFFF);
				groupIndexPerTile[i] = group;
				if (group > maxGroupIndex)
				{
					maxGroupIndex = group;
				}
			}

			int groupCount = maxGroupIndex + 1;
			if (groupCount > WebpDecodingLimits.MaxMetaHuffmanGroups)
			{
				throw new WebpDecodingException($"VP8L meta-Huffman image declares {groupCount} groups, exceeding the maximum of {WebpDecodingLimits.MaxMetaHuffmanGroups}.");
			}

			var groups = new Vp8LHuffmanGroup[groupCount];
			for (int i = 0; i < groupCount; i++)
			{
				groups[i] = Vp8LHuffmanGroup.Read(reader, colorCacheBits);
			}

			metaImage = new Vp8LMetaHuffmanImage(groupIndexPerTile, tileXSize, subsampleBits);
			return groups;
		}

		metaImage = null;
		return [Vp8LHuffmanGroup.Read(reader, colorCacheBits)];
	}
	public static int SubSampleSize(int size, int sampleBits) => (size + (1 << sampleBits) - 1) >> sampleBits;
}
