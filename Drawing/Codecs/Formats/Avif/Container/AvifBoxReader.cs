namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Buffers.Binary;
using Ecng.Drawing.Codecs.Formats.Avif.Internal;

internal static class AvifBoxReader
{
	public static List<AvifBox> ReadBoxes(byte[] data, int start, int end, int depth)
	{
		if (depth > AvifDecodingLimits.MaxBoxNestingDepth)
		{
			throw new AvifDecodingException("AVIF container is nested more deeply than the supported limit.");
		}

		var boxes = new List<AvifBox>();
		int offset = start;

		while (offset < end)
		{
			if (end - offset < 8)
			{
				throw new AvifDecodingException("Truncated ISOBMFF box header.");
			}

			long size = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
			string fourCc = AvifBinaryReader.ReadFourCc(data, offset + 4);
			int headerLength = 8;

			if (size == 1)
			{
				if (end - offset < 16)
				{
					throw new AvifDecodingException("Truncated ISOBMFF box header (64-bit largesize).");
				}

				size = checked((long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 8, 8)));
				headerLength = 16;
			}
			else if (size == 0)
			{
				// size == 0 means "extends to the end of the containing region" (typically only legal for
				// the last box in a file/container) — never seen in practice for AVIF's own boxes, but
				// handled per spec rather than rejected outright.
				size = end - offset;
			}

			if (size < headerLength || size > end - offset)
			{
				throw new AvifDecodingException($"Box '{fourCc}' has an invalid size or extends past its container.");
			}

			int payloadOffset = checked(offset + headerLength);
			int payloadLength = checked((int)size - headerLength);

			boxes.Add(new AvifBox(fourCc, payloadOffset, payloadLength));
			offset = checked(offset + (int)size);
		}

		return boxes;
	}
}
