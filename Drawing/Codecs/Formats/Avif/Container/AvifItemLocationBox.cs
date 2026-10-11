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

internal static class AvifItemLocationBox
{
	public static Dictionary<uint, AvifItemLocation> Parse(byte[] data, AvifBox box)
	{
		int offset = box.PayloadOffset;
		int end = box.PayloadOffset + box.PayloadLength;

		byte version = data[offset];
		offset += 4; // version(1) + flags(3)

		byte sizesByte1 = AvifBinaryReader.ReadUInt8(data, ref offset);
		int offsetSize = sizesByte1 >> 4;
		int lengthSize = sizesByte1 & 0xF;

		byte sizesByte2 = AvifBinaryReader.ReadUInt8(data, ref offset);
		int baseOffsetSize = sizesByte2 >> 4;
		int indexSize = sizesByte2 & 0xF; // only meaningful for version 1/2; reserved (and ignored) otherwise

		int itemCount = version < 2
			? AvifBinaryReader.ReadUInt16(data, ref offset)
			: checked((int)AvifBinaryReader.ReadUInt32(data, ref offset));

		if (itemCount > AvifDecodingLimits.MaxItemCount)
		{
			throw new AvifDecodingException($"'iloc' box declares {itemCount} items, exceeding the supported maximum of {AvifDecodingLimits.MaxItemCount}.");
		}

		var result = new Dictionary<uint, AvifItemLocation>();

		for (int i = 0; i < itemCount; i++)
		{
			uint itemId = version < 2
				? AvifBinaryReader.ReadUInt16(data, ref offset)
				: AvifBinaryReader.ReadUInt32(data, ref offset);

			int constructionMethod = 0;
			if (version is 1 or 2)
			{
				int reservedAndMethod = AvifBinaryReader.ReadUInt16(data, ref offset);
				constructionMethod = reservedAndMethod & 0xF;
			}

			AvifBinaryReader.ReadUInt16(data, ref offset); // data_reference_index -- always "this file" for our scope

			long baseOffset = checked((long)AvifBinaryReader.ReadUIntN(data, ref offset, baseOffsetSize));

			int extentCount = AvifBinaryReader.ReadUInt16(data, ref offset);
			var extents = new List<AvifItemExtent>(extentCount);
			for (int j = 0; j < extentCount; j++)
			{
				if (version is 1 or 2 && indexSize > 0)
				{
					AvifBinaryReader.ReadUIntN(data, ref offset, indexSize); // extent_index -- unused for our scope
				}

				long extentOffset = checked((long)AvifBinaryReader.ReadUIntN(data, ref offset, offsetSize));
				long extentLength = checked((long)AvifBinaryReader.ReadUIntN(data, ref offset, lengthSize));
				extents.Add(new AvifItemExtent(baseOffset + extentOffset, extentLength));
			}

			if (constructionMethod == 2)
			{
				throw new AvifDecodingException($"Item {itemId} uses iloc construction_method=2 (item offset), which is not supported.");
			}

			result[itemId] = new AvifItemLocation
			{
				ItemId = itemId,
				ConstructionMethod = constructionMethod,
				Extents = extents,
			};
		}

		if (offset > end)
		{
			throw new AvifDecodingException("Truncated 'iloc' box.");
		}

		return result;
	}
}
