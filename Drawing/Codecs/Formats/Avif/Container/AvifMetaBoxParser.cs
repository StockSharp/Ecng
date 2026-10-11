namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class AvifMetaBoxParser
{
	public static AvifMetaBoxInfo Parse(byte[] data, AvifBox metaBox, int depth)
	{
		// 'meta' is itself a FullBox (4-byte version+flags header) before its children begin -- unlike
		// 'iprp'/'ipco', which are plain container Boxes.
		int childrenStart = metaBox.PayloadOffset + 4;
		int childrenEnd = metaBox.PayloadOffset + metaBox.PayloadLength;
		var children = AvifBoxReader.ReadBoxes(data, childrenStart, childrenEnd, depth);

		uint? primaryItemId = null;
		IReadOnlyDictionary<uint, AvifItemInfo> items = new Dictionary<uint, AvifItemInfo>();
		IReadOnlyDictionary<uint, AvifItemLocation> locations = new Dictionary<uint, AvifItemLocation>();
		IReadOnlyList<AvifItemReference> references = [];
		AvifItemPropertiesBox properties = AvifItemPropertiesBox.Empty;
		int? idatOffset = null;

		foreach (var child in children)
		{
			switch (child.FourCc)
			{
				case "pitm":
					primaryItemId = ParsePitm(data, child);
					break;

				case "iloc":
					locations = AvifItemLocationBox.Parse(data, child);
					break;

				case "iinf":
					items = AvifItemInfoBox.Parse(data, child);
					break;

				case "iref":
					references = AvifItemReferenceBox.Parse(data, child);
					break;

				case "iprp":
					properties = AvifItemPropertiesBox.Parse(data, child, depth + 1);
					break;

				case "idat":
					idatOffset = child.PayloadOffset;
					break;

				default:
					// 'hdlr', 'dinf', thumbnail-related boxes, etc. -- not needed for still-image decode.
					break;
			}
		}

		return new AvifMetaBoxInfo
		{
			PrimaryItemId = primaryItemId,
			Items = items,
			Locations = locations,
			References = references,
			Properties = properties,
			IdatPayloadOffset = idatOffset,
		};
	}

	private static uint ParsePitm(byte[] data, AvifBox box)
	{
		int offset = box.PayloadOffset;
		byte version = data[offset];
		offset += 4;
		return version == 0 ? AvifBinaryReader.ReadUInt16(data, ref offset) : AvifBinaryReader.ReadUInt32(data, ref offset);
	}
}
