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
using System.Text;

internal static class AvifBinaryReader
{
	public static string ReadFourCc(byte[] data, int offset) => Encoding.ASCII.GetString(data, offset, 4);

	public static byte ReadUInt8(byte[] data, ref int offset)
	{
		byte value = data[offset];
		offset += 1;
		return value;
	}

	public static ushort ReadUInt16(byte[] data, ref int offset)
	{
		ushort value = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
		offset += 2;
		return value;
	}

	public static uint ReadUInt32(byte[] data, ref int offset)
	{
		uint value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
		offset += 4;
		return value;
	}

	public static ulong ReadUInt64(byte[] data, ref int offset)
	{
		ulong value = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset, 8));
		offset += 8;
		return value;
	}
	public static ulong ReadUIntN(byte[] data, ref int offset, int byteCount)
	{
		if (byteCount == 0)
		{
			return 0;
		}

		ulong value = 0;
		for (int i = 0; i < byteCount; i++)
		{
			value = (value << 8) | data[offset + i];
		}

		offset += byteCount;
		return value;
	}
	public static string ReadCString(byte[] data, ref int offset, int end)
	{
		int start = offset;
		int i = start;
		while (i < end && data[i] != 0)
		{
			i++;
		}

		string value = Encoding.UTF8.GetString(data, start, i - start);
		offset = i < end ? i + 1 : i;
		return value;
	}
}
