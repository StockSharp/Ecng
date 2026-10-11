namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Avif.Internal;

internal static class AvifItemInfoBox
{
	public static Dictionary<uint, AvifItemInfo> Parse(byte[] data, AvifBox box)
	{
		int offset = box.PayloadOffset;
		int end = box.PayloadOffset + box.PayloadLength;

		byte version = data[offset];
		offset += 4;

		int entryCount = version == 0
			? AvifBinaryReader.ReadUInt16(data, ref offset)
			: checked((int)AvifBinaryReader.ReadUInt32(data, ref offset));

		if (entryCount > AvifDecodingLimits.MaxItemCount)
		{
			throw new AvifDecodingException($"'iinf' box declares {entryCount} items, exceeding the supported maximum of {AvifDecodingLimits.MaxItemCount}.");
		}

		var result = new Dictionary<uint, AvifItemInfo>();

		for (int i = 0; i < entryCount; i++)
		{
			if (offset + 8 > end)
			{
				throw new AvifDecodingException("Truncated 'infe' entry.");
			}

			int entryStart = offset;
			uint entrySize = AvifBinaryReader.ReadUInt32(data, ref offset);
			string entryFourCc = AvifBinaryReader.ReadFourCc(data, offset);
			offset += 4;

			if (entryFourCc != "infe" || entrySize < 8)
			{
				throw new AvifDecodingException($"'iinf' entry {i} is not a valid 'infe' box.");
			}

			int entryEnd = checked(entryStart + (int)entrySize);
			if (entryEnd > end)
			{
				throw new AvifDecodingException("'infe' entry extends past its containing 'iinf' box.");
			}

			byte entryVersion = data[offset];
			offset += 4;

			if (entryVersion is 0 or 1)
			{
				uint legacyItemId = AvifBinaryReader.ReadUInt16(data, ref offset);
				offset += 2; // item_protection_index
				string itemName = AvifBinaryReader.ReadCString(data, ref offset, entryEnd);
				// Pre-v2 'infe' has no explicit item_type field (it's implied by content_type); this legacy
				// form isn't used by modern AV1-in-ISOBMFF encoders for av01/grid items, so it's captured
				// with an empty type rather than fully parsed.
				result[legacyItemId] = new AvifItemInfo(legacyItemId, string.Empty, itemName);
			}
			else
			{
				uint itemId = entryVersion == 2
					? AvifBinaryReader.ReadUInt16(data, ref offset)
					: AvifBinaryReader.ReadUInt32(data, ref offset);
				offset += 2; // item_protection_index
				string itemType = AvifBinaryReader.ReadFourCc(data, offset);
				offset += 4;
				string itemName = AvifBinaryReader.ReadCString(data, ref offset, entryEnd);

				string contentType = itemType == "mime"
					? AvifBinaryReader.ReadCString(data, ref offset, entryEnd)
					: null;

				result[itemId] = new AvifItemInfo(itemId, itemType, itemName, contentType);
			}

			offset = entryEnd;
		}

		return result;
	}
}
