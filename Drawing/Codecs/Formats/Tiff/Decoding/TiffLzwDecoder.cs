namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class TiffLzwDecoder
{
	private const int ClearCode = 256;
	private const int EndCode = 257;
	private const int FirstFreeCode = 258;
	private const int MinCodeWidth = 9;
	private const int MaxCodeWidth = 12;
	private const int MaxCodeTableSize = 4096;

	public static void Decode(ReadOnlySpan<byte> compressed, Span<byte> output)
	{
		int outputLength = output.Length;
		if (outputLength == 0)
		{
			return;
		}

		var reader = new TiffLzwBitReader(compressed);

		int codeWidth = MinCodeWidth;
		int nextCode = FirstFreeCode;
		int bumpThreshold = (1 << codeWidth) - 1;

		var prefix = new ushort[MaxCodeTableSize];
		var suffix = new byte[MaxCodeTableSize];
		var stack = new byte[MaxCodeTableSize];

		int prevCode = -1;
		int writePos = 0;

		while (writePos < outputLength)
		{
			if (!reader.TryReadCode(codeWidth, out int code))
			{
				break;
			}

			if (code == ClearCode)
			{
				nextCode = FirstFreeCode;
				codeWidth = MinCodeWidth;
				bumpThreshold = (1 << codeWidth) - 1;
				prevCode = -1;
				continue;
			}

			if (code == EndCode)
			{
				break;
			}

			bool isNewCode = code == nextCode && prevCode != -1;
			if (!(code < nextCode || isNewCode))
			{
				// Invalid/corrupt code (out-of-range, and not the one legal "not yet in the table" case).
				break;
			}

			int stackTop = 0;
			int c = isNewCode ? prevCode : code;
			while (c >= FirstFreeCode)
			{
				if (stackTop >= stack.Length)
				{
					return;
				}

				stack[stackTop++] = suffix[c];
				c = prefix[c];
			}

			stack[stackTop++] = (byte)c;
			byte firstChar = (byte)c;

			int runLength = stackTop + (isNewCode ? 1 : 0);
			if (runLength <= outputLength - writePos)
			{
				for (int i = stackTop - 1; i >= 0; i--)
				{
					output[writePos++] = stack[i];
				}

				if (isNewCode)
				{
					output[writePos++] = firstChar;
				}
			}
			else
			{
				for (int i = stackTop - 1; i >= 0 && writePos < outputLength; i--)
				{
					output[writePos++] = stack[i];
				}

				if (isNewCode && writePos < outputLength)
				{
					output[writePos++] = firstChar;
				}
			}

			if (prevCode != -1 && nextCode < MaxCodeTableSize)
			{
				prefix[nextCode] = (ushort)prevCode;
				suffix[nextCode] = firstChar;
				nextCode++;
				if (nextCode >= bumpThreshold && codeWidth < MaxCodeWidth)
				{
					codeWidth++;
					bumpThreshold = (1 << codeWidth) - 1;
				}
			}

			prevCode = code;
		}
		if (writePos != outputLength)
			throw new TiffDecodingException("Truncated or invalid TIFF LZW strip.");
	}
}
