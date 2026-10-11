namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Buffers.Binary;

internal readonly struct TiffReader(byte[] data, TiffByteOrders byteOrder)
{
	public byte[] Data { get; } = data;
	public TiffByteOrders ByteOrder { get; } = byteOrder;
	public int Length => Data.Length;
	public bool HasBytes(int offset, int count) => offset >= 0 && count >= 0 && (long)offset + count <= Data.Length;

	public byte ReadByte(int offset)
	{
		EnsureInBounds(offset, 1);
		return Data[offset];
	}

	public ushort ReadUInt16(int offset)
	{
		EnsureInBounds(offset, 2);
		var span = Data.AsSpan(offset, 2);
		return ByteOrder == TiffByteOrders.LittleEndian
			? BinaryPrimitives.ReadUInt16LittleEndian(span)
			: BinaryPrimitives.ReadUInt16BigEndian(span);
	}

	public uint ReadUInt32(int offset)
	{
		EnsureInBounds(offset, 4);
		var span = Data.AsSpan(offset, 4);
		return ByteOrder == TiffByteOrders.LittleEndian
			? BinaryPrimitives.ReadUInt32LittleEndian(span)
			: BinaryPrimitives.ReadUInt32BigEndian(span);
	}
	public ReadOnlySpan<byte> ReadSpan(int offset, int count)
	{
		EnsureInBounds(offset, count);
		return Data.AsSpan(offset, count);
	}

	private void EnsureInBounds(int offset, int count)
	{
		if (!HasBytes(offset, count))
		{
			throw new TiffDecodingException($"Attempted to read {count} byte(s) at offset {offset}, past the end of a {Data.Length}-byte TIFF file.");
		}
	}
}
