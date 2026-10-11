namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>Lossless VP8L literal coding with canonical Huffman trees and RIFF animation frames.</summary>
internal static class WebpEncoder
{
	private static readonly byte[] _reversed = ReverseBytes();

	public static byte[] Encode(RasterImage image)
	{
		using var stream = new MemoryStream();
		using var writer = Header(stream);
		Chunk(writer, 0x4c385056, Bitstream(image));
		return Finish(writer);
	}

	public static byte[] Encode(Animation animation)
	{
		if (animation.Plays is < 0 or > ushort.MaxValue)
			throw new NotSupportedException("WebP loop count must fit in 16 bits.");
		Animation.EnsureWithinBudget(animation.FrameCount, (long)animation.Width * animation.Height * animation.FrameCount);
		using var stream = new MemoryStream();
		using var writer = Header(stream);
		var extended = new byte[10];
		extended[0] = 0x12; // Animation with an alpha channel.
		Write24(extended.AsSpan(4), animation.Width - 1);
		Write24(extended.AsSpan(7), animation.Height - 1);
		Chunk(writer, 0x58385056, extended);
		var control = new byte[6];
		BinaryPrimitives.WriteUInt16LittleEndian(control.AsSpan(4), (ushort)animation.Plays);
		Chunk(writer, 0x4d494e41, control);
		var count = 0;
		foreach (var frame in animation.Frames)
		{
			if (frame.Image.Width != animation.Width || frame.Image.Height != animation.Height)
				throw new InvalidDataException("WebP frame differs from its canvas.");
			using var payload = new MemoryStream();
			using var frameWriter = new BinaryWriter(payload);
			var header = new byte[16];
			Write24(header.AsSpan(6), animation.Width - 1);
			Write24(header.AsSpan(9), animation.Height - 1);
			Write24(header.AsSpan(12), frame.DelayMilliseconds);
			header[15] = 2; // A full canvas replaces the preceding one.
			frameWriter.Write(header);
			Chunk(frameWriter, 0x4c385056, Bitstream(frame.Image));
			Chunk(writer, 0x464d4e41, payload.ToArray());
			count++;
		}
		if (count != animation.FrameCount) throw new InvalidDataException("WebP animation frame count differs.");
		return Finish(writer);
	}

	private static byte[] Bitstream(RasterImage image)
	{
		if (image.Width > 16384 || image.Height > 16384)
			throw new NotSupportedException("VP8L dimensions cannot exceed 16384.");
		using var bits = new WebpBitWriter();
		bits.Write(0x2f, 8);
		bits.Write((uint)(image.Width - 1), 14);
		bits.Write((uint)(image.Height - 1), 14);
		bits.Write(1, 1);
		bits.Write(0, 3);
		bits.Write(0, 1); // No transforms.
		bits.Write(0, 1); // No color cache.
		bits.Write(0, 1); // One Huffman group.
		var pixels = image.Pixels;
		bool[] uniform = [true, true, true, true];
		for (var offset = 4; offset < pixels.Length; offset += 4)
			for (var channel = 0; channel < 4; channel++)
				uniform[channel] &= pixels[offset + channel] == pixels[channel];
		int[] order = [1, 0, 2, 3];
		foreach (var channel in order)
			Tree(bits, uniform[channel], pixels[channel], channel == 1 ? 280 : 256);
		Tree(bits, true, 0, 40); // Distance tree; literal coding emits no backward references.
		for (var offset = 0; offset < pixels.Length; offset += 4)
			foreach (var channel in order)
				if (!uniform[channel]) bits.Write(_reversed[pixels[offset + channel]], 8);
		return bits.Finish();
	}

	private static void Tree(WebpBitWriter bits, bool uniform, byte value, int alphabet)
	{
		if (uniform)
		{
			bits.Write(1, 1);
			bits.Write(0, 1); // One symbol.
			bits.Write(value > 1 ? 1u : 0u, 1);
			bits.Write(value, value > 1 ? 8 : 1);
			return;
		}
		bits.Write(0, 1);
		bits.Write(8, 4); // Twelve code-length symbols, through symbol 8 in the VP8L ordering.
		for (var index = 0; index < 12; index++) bits.Write(index is 2 or 11 ? 1u : 0u, 3);
		bits.Write(0, 1); // Lengths for the entire alphabet.
		for (var symbol = 0; symbol < alphabet; symbol++) bits.Write(symbol < 256 ? 1u : 0u, 1);
	}

	private static BinaryWriter Header(MemoryStream stream)
	{
		var writer = new BinaryWriter(stream);
		writer.Write(0x46464952u);
		writer.Write(0u);
		writer.Write(0x50424557u);
		return writer;
	}

	private static void Chunk(BinaryWriter writer, uint type, byte[] data)
	{
		if (writer.BaseStream.Length + 8L + data.Length + (data.Length & 1) > 128 * 1024 * 1024)
			throw new InvalidDataException("Encoded WebP exceeds the supported file size.");
		writer.Write(type);
		writer.Write((uint)data.Length);
		writer.Write(data);
		if ((data.Length & 1) != 0) writer.Write((byte)0);
	}

	private static byte[] Finish(BinaryWriter writer)
	{
		var stream = (MemoryStream)writer.BaseStream;
		writer.Seek(4, SeekOrigin.Begin);
		writer.Write(checked((uint)stream.Length - 8));
		return stream.ToArray();
	}

	private static void Write24(Span<byte> destination, int value)
	{
		if (value is < 0 or > 0xffffff) throw new NotSupportedException("WebP frame value exceeds 24 bits.");
		destination[0] = (byte)value;
		destination[1] = (byte)(value >> 8);
		destination[2] = (byte)(value >> 16);
	}

	private static byte[] ReverseBytes()
	{
		var reversed = new byte[256];
		for (var value = 0; value < reversed.Length; value++)
			for (var bit = 0; bit < 8; bit++) reversed[value] |= (byte)(((value >> bit) & 1) << (7 - bit));
		return reversed;
	}
}
