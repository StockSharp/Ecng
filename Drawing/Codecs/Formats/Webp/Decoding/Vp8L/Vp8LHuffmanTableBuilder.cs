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

internal static class Vp8LHuffmanTableBuilder
{
	public const int MainRootBits = 8;
	public const int LengthsRootBits = 7;

	private const int MaxAllowedCodeLength = WebpDecodingLimits.MaxHuffmanCodeLength;
	public static Vp8LHuffmanTable Build(ReadOnlySpan<int> codeLengths, int rootBits)
	{
		int totalSize = BuildInternal(codeLengths, rootBits, null);
		if (totalSize <= 0)
		{
			throw new WebpDecodingException("Invalid VP8L Huffman code: code lengths do not form a valid canonical Huffman tree.");
		}

		var entries = new Vp8LHuffmanCode[totalSize];
		BuildInternal(codeLengths, rootBits, entries);

		return new Vp8LHuffmanTable { Entries = entries, RootBits = rootBits };
	}
	private static int BuildInternal(ReadOnlySpan<int> codeLengths, int rootBits, Vp8LHuffmanCode[] entries)
	{
		int alphabetSize = codeLengths.Length;
		Span<int> count = stackalloc int[MaxAllowedCodeLength + 1];
		Span<int> offset = stackalloc int[MaxAllowedCodeLength + 1];

		for (int symbol = 0; symbol < alphabetSize; symbol++)
		{
			int length = codeLengths[symbol];
			if (length > MaxAllowedCodeLength)
			{
				return 0;
			}

			count[length]++;
		}

		if (count[0] == alphabetSize)
		{
			// Every code length is zero: no symbols are actually encodable.
			return 0;
		}

		offset[1] = 0;
		for (int length = 1; length < MaxAllowedCodeLength; length++)
		{
			if (count[length] > (1 << length))
			{
				return 0;
			}

			offset[length + 1] = offset[length] + count[length];
		}

		var sorted = new int[alphabetSize];
		for (int symbol = 0; symbol < alphabetSize; symbol++)
		{
			int length = codeLengths[symbol];
			if (length > 0)
			{
				sorted[offset[length]++] = symbol;
			}
		}

		int totalSize = 1 << rootBits;

		// Special case: exactly one symbol is used at all. Its "code" is zero bits long — decoding always
		// returns that symbol without consuming anything from the stream.
		if (offset[MaxAllowedCodeLength] == 1)
		{
			entries?.AsSpan(0, totalSize).Fill(new Vp8LHuffmanCode { Bits = 0, Value = (ushort)sorted[0] });
			return totalSize;
		}

		int tableBits = rootBits;
		int tableSize = 1 << tableBits;
		uint low = 0xFFFFFFFFu;
		uint mask = (uint)(totalSize - 1);
		uint key = 0;
		int numNodes = 1;
		int numOpen = 1;
		int symbolIndex = 0;
		int tableStart = 0;

		// Fill the root table (lengths 1..rootBits).
		for (int length = 1, step = 2; length <= rootBits; length++, step <<= 1)
		{
			numOpen <<= 1;
			numNodes += numOpen;
			numOpen -= count[length];
			if (numOpen < 0)
			{
				return 0;
			}

			if (entries == null)
			{
				continue;
			}

			for (; count[length] > 0; count[length]--)
			{
				var code = new Vp8LHuffmanCode { Bits = (byte)length, Value = (ushort)sorted[symbolIndex++] };
				ReplicateValue(entries, (int)key, step, tableSize, code);
				key = GetNextKey(key, length);
			}
		}

		// Fill second-level tables and their root pointers (lengths rootBits+1..MaxAllowedCodeLength).
		for (int length = rootBits + 1, step = 2; length <= MaxAllowedCodeLength; length++, step <<= 1)
		{
			numOpen <<= 1;
			numNodes += numOpen;
			numOpen -= count[length];
			if (numOpen < 0)
			{
				return 0;
			}

			for (; count[length] > 0; count[length]--)
			{
				if ((key & mask) != low)
				{
					if (entries != null)
					{
						tableStart += tableSize;
					}

					tableBits = NextTableBitSize(count, length, rootBits);
					tableSize = 1 << tableBits;
					totalSize += tableSize;
					low = key & mask;

					if (entries != null)
					{
						entries[low].Bits = (byte)(tableBits + rootBits);
						entries[low].Value = (ushort)(tableStart - (int)low);
					}
				}

				if (entries != null)
				{
					var code = new Vp8LHuffmanCode { Bits = (byte)(length - rootBits), Value = (ushort)sorted[symbolIndex++] };
					ReplicateValue(entries, tableStart + (int)(key >> rootBits), step, tableSize, code);
				}

				key = GetNextKey(key, length);
			}
		}

		if (numNodes != (2 * offset[MaxAllowedCodeLength]) - 1)
		{
			// The tree isn't "full" (Kraft's equality doesn't hold exactly) — under- or over-subscribed.
			return 0;
		}

		return totalSize;
	}
	private static void ReplicateValue(Vp8LHuffmanCode[] table, int offset, int step, int end, Vp8LHuffmanCode code)
	{
		int current = end;
		do
		{
			current -= step;
			table[offset + current] = code;
		}
		while (current > 0);
	}
	private static uint GetNextKey(uint key, int length)
	{
		uint step = 1u << (length - 1);
		while ((key & step) != 0)
		{
			step >>= 1;
		}

		return step != 0 ? (key & (step - 1)) + step : key;
	}
	private static int NextTableBitSize(ReadOnlySpan<int> count, int length, int rootBits)
	{
		int left = 1 << (length - rootBits);
		while (length < MaxAllowedCodeLength)
		{
			left -= count[length];
			if (left <= 0)
			{
				break;
			}

			length++;
			left <<= 1;
		}

		return length - rootBits;
	}
}
