namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

/// <summary>
/// Minimal standards-compliant APNG writer. A GIF frame is emitted as an entire
/// RGBA8 source-composited frame (blend SOURCE, disposal NONE), retaining every
/// frame and its timing instead of silently dropping animation to static PNG.
/// </summary>
internal static class ApngEncoder
{
	private static ReadOnlySpan<byte> PngSignature => [137,80,78,71,13,10,26,10];

	public static byte[] Encode(GifAnimation source)
	{
		if (source.Frames.Count == 0)
			throw new InvalidDataException("Cannot export zero GIF frames as APNG.");

		using var png = new MemoryStream();
		png.Write(PngSignature);
		var ihdr = new byte[13];
		Put(ihdr, 0, (uint)source.Width);
		Put(ihdr, 4, (uint)source.Height);
		ihdr[8] = 8; // bit depth
		ihdr[9] = 6; // RGBA
		Chunk(png, "IHDR"u8, ihdr);

		var animation = new byte[8];
		Put(animation, 0, (uint)source.Frames.Count);
		// GIF NETSCAPE=0 means infinite; no extension means play once.
		Put(animation, 4, (uint)(source.LoopCount < 0 ? 1 : source.LoopCount));
		Chunk(png, "acTL"u8, animation);

		uint sequence = 0;
		for (var index = 0; index < source.Frames.Count; index++)
		{
			var frame = source.Frames[index];
			if (frame.Image.Width != source.Width || frame.Image.Height != source.Height)
				throw new InvalidDataException("APNG frame dimensions must equal canvas.");

			var control = new byte[26];
			Put(control, 0, sequence++);
			Put(control, 4, (uint)source.Width);
			Put(control, 8, (uint)source.Height);
			// Full-canvas frame offset (0,0).
			BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(20), (ushort)frame.DelayCentiseconds);
			BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(22), 100);
			control[24] = 0; // APNG_DISPOSE_OP_NONE
			control[25] = 0; // APNG_BLEND_OP_SOURCE
			Chunk(png, "fcTL"u8, control);

			using var compressed = new MemoryStream();
			using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
			{
				for (var y = 0; y < source.Height; y++)
				{
					zlib.WriteByte(0);
					zlib.Write(frame.Image.Pixels, y * source.Width * 4, source.Width * 4);
				}
			}

			if (index == 0)
				Chunk(png, "IDAT"u8, compressed.ToArray());
			else
			{
				var payload = new byte[checked(compressed.Length + 4)];
				Put(payload, 0, sequence++);
				compressed.Position = 0;
				compressed.ReadExactly(payload.AsSpan(4));
				Chunk(png, "fdAT"u8, payload);
			}
		}

		Chunk(png, "IEND"u8, []);
		return png.ToArray();
	}

	private static void Chunk(Stream output, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> payload)
	{
		Span<byte> buffer = stackalloc byte[4];
		Put(buffer, 0, (uint)payload.Length);
		output.Write(buffer);
		output.Write(tag);
		output.Write(payload);
		uint crc = 0xFFFFFFFF;
		foreach (var b in tag) crc = CrcStep(crc, b);
		foreach (var b in payload) crc = CrcStep(crc, b);
		Put(buffer, 0, ~crc);
		output.Write(buffer);
	}

	private static uint CrcStep(uint crc, byte value)
	{
		crc ^= value;
		for (var i = 0; i < 8; i++)
			crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
		return crc;
	}

	private static void Put(Span<byte> data, int at, uint value) =>
		BinaryPrimitives.WriteUInt32BigEndian(data[at..], value);
}
