namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class AvifItemPropertiesBox
{
	public static AvifItemPropertiesBox Empty { get; } = new()
	{
		Properties = [],
		ItemPropertyIndices = new Dictionary<uint, IReadOnlyList<int>>(),
	};
	public IReadOnlyList<AvifBox> Properties { get; init; }
	public IReadOnlyDictionary<uint, IReadOnlyList<int>> ItemPropertyIndices { get; init; }

	public static AvifItemPropertiesBox Parse(byte[] data, AvifBox iprpBox, int depth)
	{
		var children = AvifBoxReader.ReadBoxes(data, iprpBox.PayloadOffset, iprpBox.PayloadOffset + iprpBox.PayloadLength, depth);

		AvifBox? ipcoBox = null;
		foreach (var child in children)
		{
			if (child.FourCc == "ipco")
			{
				ipcoBox = child;
				break;
			}
		}

		if (ipcoBox is not { } ipco)
		{
			throw new AvifDecodingException("'iprp' box is missing its 'ipco' child.");
		}

		var properties = AvifBoxReader.ReadBoxes(data, ipco.PayloadOffset, ipco.PayloadOffset + ipco.PayloadLength, depth + 1);

		var associations = new Dictionary<uint, List<int>>();
		foreach (var child in children)
		{
			if (child.FourCc == "ipma")
			{
				ParseIpma(data, child, associations);
			}
		}

		return new AvifItemPropertiesBox
		{
			Properties = properties,
			ItemPropertyIndices = associations.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<int>)kv.Value),
		};
	}

	private static void ParseIpma(byte[] data, AvifBox ipmaBox, Dictionary<uint, List<int>> associations)
	{
		int offset = ipmaBox.PayloadOffset;
		int end = ipmaBox.PayloadOffset + ipmaBox.PayloadLength;

		byte version = data[offset];
		byte flagsLowByte = data[offset + 3];
		offset += 4;

		uint entryCount = AvifBinaryReader.ReadUInt32(data, ref offset);
		bool largePropertyIndex = (flagsLowByte & 0x1) != 0;

		for (uint i = 0; i < entryCount; i++)
		{
			uint itemId = version < 1
				? AvifBinaryReader.ReadUInt16(data, ref offset)
				: AvifBinaryReader.ReadUInt32(data, ref offset);
			int associationCount = AvifBinaryReader.ReadUInt8(data, ref offset);

			if (!associations.TryGetValue(itemId, out var list))
			{
				list = [];
				associations[itemId] = list;
			}

			for (int j = 0; j < associationCount; j++)
			{
				// Top bit of the property-index field is the 'essential' flag; the remaining bits (15 or 7,
				// depending on the flags-driven index width) are the 1-based index into ipco. 'essential'
				// itself isn't used by this decoder — a property we don't recognize is simply ignored
				// whether or not the encoder marked it essential, matching this codebase's general leniency
				// toward ancillary/unrecognized data.
				int propertyIndex = largePropertyIndex
					? AvifBinaryReader.ReadUInt16(data, ref offset) & 0x7FFF
					: AvifBinaryReader.ReadUInt8(data, ref offset) & 0x7F;

				if (propertyIndex != 0)
				{
					list.Add(propertyIndex);
				}
			}
		}

		if (offset > end)
		{
			throw new AvifDecodingException("Truncated 'ipma' box.");
		}
	}
}
