namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class Av1ObuReader
{
	public static List<Av1Obu> ReadObus(byte[] data, int start, int length)
	{
		var result = new List<Av1Obu>();
		int offset = start;
		int end = start + length;

		while (offset < end)
		{
			byte headerByte = data[offset];

			// bit7 = obu_forbidden_bit (must be 0, but tolerated rather than hard-rejected -- matches this
			// codebase's general leniency toward reserved/forward-compatibility bits).
			int obuType = (headerByte >> 3) & 0xF;
			bool extensionFlag = ((headerByte >> 2) & 1) != 0;
			bool hasSizeField = ((headerByte >> 1) & 1) != 0;

			int cursor = offset + 1;
			int temporalId = 0;
			int spatialId = 0;

			if (extensionFlag)
			{
				if (cursor >= end)
				{
					throw new AvifDecodingException("Truncated AV1 OBU extension header.");
				}

				byte extensionByte = data[cursor];
				temporalId = (extensionByte >> 5) & 0x7;
				spatialId = (extensionByte >> 3) & 0x3;
				cursor++;
			}

			long payloadLength;
			if (hasSizeField)
			{
				payloadLength = ReadLeb128(data, ref cursor, end);
			}
			else
			{
				// Only legal for the very last OBU in the stream; AVIF item data always sets has_size, but
				// handled per spec rather than rejected.
				payloadLength = end - cursor;
			}

			if (payloadLength < 0 || cursor + payloadLength > end)
			{
				throw new AvifDecodingException("AV1 OBU size extends past its containing data.");
			}

			result.Add(new Av1Obu(obuType, temporalId, spatialId, cursor, (int)payloadLength));
			offset = cursor + (int)payloadLength;
		}

		return result;
	}

	private static long ReadLeb128(byte[] data, ref int offset, int end)
	{
		long value = 0;
		for (int i = 0; i < 8; i++)
		{
			if (offset >= end)
			{
				throw new AvifDecodingException("Truncated AV1 leb128 value.");
			}

			byte b = data[offset];
			offset++;
			value |= (long)(b & 0x7F) << (i * 7);
			if ((b & 0x80) == 0)
			{
				return value;
			}
		}

		throw new AvifDecodingException("AV1 leb128 value exceeds the 8-byte limit.");
	}
}
