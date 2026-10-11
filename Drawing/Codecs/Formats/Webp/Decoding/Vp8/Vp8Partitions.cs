namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Webp.Internal;

internal static class Vp8Partitions
{
	public static Vp8BoolDecoder[] Build(byte[] data, int partition0End, int partitionCount)
	{
		if (partitionCount < 1 || partitionCount > WebpDecodingLimits.MaxVp8Partitions)
		{
			throw new WebpDecodingException($"VP8 coefficient partition count {partitionCount} is out of range.");
		}

		int sizeTableBytes = 3 * (partitionCount - 1);
		if (partition0End + sizeTableBytes > data.Length)
		{
			throw new WebpDecodingException("VP8 coefficient partition size table exceeds the available chunk data.");
		}

		var partitions = new Vp8BoolDecoder[partitionCount];
		int sizeTablePos = partition0End;
		int dataPos = partition0End + sizeTableBytes;

		for (int p = 0; p < partitionCount - 1; p++)
		{
			int size = data[sizeTablePos] | (data[sizeTablePos + 1] << 8) | (data[sizeTablePos + 2] << 16);
			sizeTablePos += 3;

			if (dataPos + size > data.Length)
			{
				throw new WebpDecodingException("VP8 coefficient partition size exceeds the available chunk data.");
			}

			partitions[p] = new Vp8BoolDecoder(data, dataPos, size);
			dataPos += size;
		}

		// The last partition consumes whatever bytes remain in the chunk.
		int lastSize = data.Length - dataPos;
		partitions[partitionCount - 1] = new Vp8BoolDecoder(data, dataPos, lastSize);

		return partitions;
	}
}