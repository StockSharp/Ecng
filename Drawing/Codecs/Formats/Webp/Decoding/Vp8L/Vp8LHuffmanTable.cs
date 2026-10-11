namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Diagnostics;

internal sealed class Vp8LHuffmanTable
{
	public Vp8LHuffmanCode[] Entries { get; init; }

	public int RootBits { get; init; }
	private const int MaxCodeLength = Internal.WebpDecodingLimits.MaxHuffmanCodeLength;
	public int Decode(Vp8LBitReader reader)
	{
		int rootBits = RootBits;
		uint window = reader.PeekBits(MaxCodeLength);
		uint low = window & ((1u << rootBits) - 1);
		var entries = Entries;
		var root = entries[low];

		if (root.Bits <= rootBits)
		{
			reader.SkipBits(root.Bits);
			return root.Value;
		}

		int extraBits = root.Bits - rootBits;
		uint secondaryOffset = (window >> rootBits) & ((1u << extraBits) - 1);
		var second = entries[(int)low + root.Value + (int)secondaryOffset];

		reader.SkipBits(rootBits + second.Bits);
		return second.Value;
	}
	public int DecodeMain(Vp8LBitReader reader)
	{
		const int rootBits = Vp8LHuffmanTableBuilder.MainRootBits;
		Debug.Assert(RootBits == rootBits, "DecodeMain assumes every table it is called on was built with MainRootBits.");

		uint window = reader.PeekBits(MaxCodeLength);
		uint low = window & ((1u << rootBits) - 1);
		var entries = Entries;
		var root = entries[low];

		if (root.Bits <= rootBits)
		{
			reader.SkipBits(root.Bits);
			return root.Value;
		}

		int extraBits = root.Bits - rootBits;
		uint secondaryOffset = (window >> rootBits) & ((1u << extraBits) - 1);
		var second = entries[(int)low + root.Value + (int)secondaryOffset];

		reader.SkipBits(rootBits + second.Bits);
		return second.Value;
	}
}
