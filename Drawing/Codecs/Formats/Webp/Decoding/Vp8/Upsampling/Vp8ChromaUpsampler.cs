namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Upsampling;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8ChromaUpsampler
{
	public static byte Sample(ReadOnlySpan<byte> chroma, int stride, int chromaWidth, int chromaHeight, int outX, int outY)
	{
		int nearCol = Clamp(outX / 2, chromaWidth - 1);
		int farCol = Clamp((outX / 2) + (((outX & 1) == 1) ? 1 : -1), chromaWidth - 1);
		int nearRow = Clamp(outY / 2, chromaHeight - 1);
		int farRow = Clamp((outY / 2) + (((outY & 1) == 1) ? 1 : -1), chromaHeight - 1);

		int nearNear = chroma[(nearRow * stride) + nearCol];
		int nearFar = chroma[(nearRow * stride) + farCol];
		int farNear = chroma[(farRow * stride) + nearCol];
		int farFar = chroma[(farRow * stride) + farCol];

		int sum = (9 * nearNear) + (3 * nearFar) + (3 * farNear) + farFar;
		return (byte)((sum + 8) >> 4);
	}
	public static void UpsampleRow(
		ReadOnlySpan<byte> nearRow,
		ReadOnlySpan<byte> farRow,
		Span<byte> destination,
		int width,
		int chromaWidth)
	{
		int lastColumn = chromaWidth - 1;
		int pairCount = (width + 1) / 2;

		for (int c = 0; c < pairCount; c++)
		{
			int previous = c > 0 ? c - 1 : 0;
			int next = c < lastColumn ? c + 1 : lastColumn;

			int near = nearRow[c];
			int far = farRow[c];
			int nearFar9 = (9 * near) + (3 * far) + 8;

			destination[2 * c] = (byte)((nearFar9 + (3 * nearRow[previous]) + farRow[previous]) >> 4);

			int odd = (2 * c) + 1;
			if (odd < width)
			{
				destination[odd] = (byte)((nearFar9 + (3 * nearRow[next]) + farRow[next]) >> 4);
			}
		}
	}

	private static int Clamp(int v, int max) => v < 0 ? 0 : v > max ? max : v;
}
