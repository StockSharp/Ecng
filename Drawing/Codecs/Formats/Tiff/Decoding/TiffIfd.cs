namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using Ecng.Drawing.Codecs.Formats.Tiff.Internal;

internal sealed class TiffIfd(TiffReader reader, IReadOnlyDictionary<ushort, TiffIfdEntry> entries)
{
	public bool HasTag(ushort tag) => entries.ContainsKey(tag);
	public uint GetUInt32(ushort tag, uint defaultValue) =>
		entries.TryGetValue(tag, out var entry) ? ReadValues(entry)[0] : defaultValue;
	public ushort GetUInt16(ushort tag, ushort defaultValue) => (ushort)GetUInt32(tag, defaultValue);
	public uint RequireUInt32(ushort tag) =>
		entries.TryGetValue(tag, out var entry)
			? ReadValues(entry)[0]
			: throw new TiffDecodingException($"Missing TIFF tag {tag}.");
	public uint[] GetUInt32Array(ushort tag) => entries.TryGetValue(tag, out var entry) ? ReadValues(entry) : [];
	public uint[] RequireUInt32Array(ushort tag) =>
		entries.TryGetValue(tag, out var entry)
			? ReadValues(entry)
			: throw new TiffDecodingException($"Missing TIFF tag {tag}.");
	public byte[] TryGetBytes(ushort tag)
	{
		if (!entries.TryGetValue(tag, out var entry))
		{
			return null;
		}

		int typeSize = entry.Type.GetByteSize();
		if (typeSize == 0 || entry.Count == 0 || entry.Count > TiffDecodingLimits.MaxArrayEntryCount)
		{
			return null;
		}

		long totalBytes = (long)typeSize * entry.Count;
		if (totalBytes > 4 && !reader.HasBytes(entry.ValueFieldOffset, 4))
		{
			return null;
		}

		long valuesOffsetLong = totalBytes <= 4 ? entry.ValueFieldOffset : reader.ReadUInt32(entry.ValueFieldOffset);
		if (valuesOffsetLong is < 0 or > int.MaxValue)
		{
			return null;
		}

		int valuesOffset = (int)valuesOffsetLong;
		return reader.HasBytes(valuesOffset, (int)totalBytes) ? reader.ReadSpan(valuesOffset, (int)totalBytes).ToArray() : null;
	}

	private uint[] ReadValues(TiffIfdEntry entry)
	{
		int typeSize = entry.Type.GetByteSize();
		if (typeSize == 0)
		{
			throw new TiffDecodingException($"Tag {entry.Tag} has an unrecognized value type ({(ushort)entry.Type}).");
		}

		if (entry.Count == 0)
		{
			throw new TiffDecodingException($"Tag {entry.Tag} declares zero values.");
		}

		if (entry.Count > TiffDecodingLimits.MaxArrayEntryCount)
		{
			throw new TiffDecodingException($"Tag {entry.Tag} declares {entry.Count} values, exceeding the supported maximum of {TiffDecodingLimits.MaxArrayEntryCount}.");
		}

		long totalBytes = (long)typeSize * entry.Count;
		long valuesOffsetLong = totalBytes <= 4 ? entry.ValueFieldOffset : reader.ReadUInt32(entry.ValueFieldOffset);
		if (valuesOffsetLong is < 0 or > int.MaxValue)
		{
			throw new TiffDecodingException($"Tag {entry.Tag} has an out-of-range value offset.");
		}

		int valuesOffset = (int)valuesOffsetLong;
		var result = new uint[entry.Count];

		for (int i = 0; i < entry.Count; i++)
		{
			int offset = valuesOffset + (i * typeSize);
			result[i] = entry.Type switch
			{
				TiffTagTypes.Byte or TiffTagTypes.SByte or TiffTagTypes.Undefined or TiffTagTypes.Ascii => reader.ReadByte(offset),
				TiffTagTypes.Short or TiffTagTypes.SShort => reader.ReadUInt16(offset),
				TiffTagTypes.Long or TiffTagTypes.SLong => reader.ReadUInt32(offset),
				_ => throw new TiffDecodingException($"Tag {entry.Tag} has a value type ({entry.Type}) this decoder cannot read numerically."),
			};
		}

		return result;
	}
}
