namespace Ecng.Drawing.Codecs.Formats.Avif.Container;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class AvifBoxWriter
{
	public static byte[] Box(string fourCc, byte[] payload)
	{
		var result = new byte[8 + payload.Length];
		WriteUInt32(result, 0, (uint)result.Length);
		WriteFourCc(result, 4, fourCc);
		Array.Copy(payload, 0, result, 8, payload.Length);
		return result;
	}
	public static byte[] Box(string fourCc, params byte[][] children) => Box(fourCc, Concat(children));
	public static byte[] FullBox(string fourCc, byte version, uint flags, byte[] payload)
	{
		var full = new byte[4 + payload.Length];
		full[0] = version;
		full[1] = (byte)(flags >> 16);
		full[2] = (byte)(flags >> 8);
		full[3] = (byte)flags;
		Array.Copy(payload, 0, full, 4, payload.Length);
		return Box(fourCc, full);
	}

	public static byte[] Concat(ReadOnlySpan<byte[]> chunks)
	{
		int total = 0;
		foreach (var chunk in chunks)
		{
			total += chunk.Length;
		}

		var result = new byte[total];
		int offset = 0;
		foreach (var chunk in chunks)
		{
			Array.Copy(chunk, 0, result, offset, chunk.Length);
			offset += chunk.Length;
		}

		return result;
	}

	public static void WriteUInt8(byte[] buffer, int offset, byte value) => buffer[offset] = value;

	public static void WriteUInt16(byte[] buffer, int offset, ushort value)
	{
		buffer[offset] = (byte)(value >> 8);
		buffer[offset + 1] = (byte)value;
	}

	public static void WriteUInt32(byte[] buffer, int offset, uint value)
	{
		buffer[offset] = (byte)(value >> 24);
		buffer[offset + 1] = (byte)(value >> 16);
		buffer[offset + 2] = (byte)(value >> 8);
		buffer[offset + 3] = (byte)value;
	}

	public static void WriteFourCc(byte[] buffer, int offset, string fourCc)
	{
		for (int i = 0; i < 4; i++)
		{
			buffer[offset + i] = (byte)fourCc[i];
		}
	}

	public static byte[] FourCcBytes(string fourCc)
	{
		var bytes = new byte[4];
		WriteFourCc(bytes, 0, fourCc);
		return bytes;
	}
}
