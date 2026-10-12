namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

using Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

/// <summary>Lossless VP8L and explicit-quality VP8 compression with alpha and RIFF animation frames.</summary>
internal static class WebpEncoder
{
	public static byte[] Encode(RasterImage image, WebpEncodingOptions options)
	{
		ArgumentNullException.ThrowIfNull(image);
		ArgumentNullException.ThrowIfNull(options);
		using var stream = new MemoryStream();
		using var writer = Header(stream);
		if (options.Mode == WebpCompressionModes.Lossy && HasAlpha(image))
			Chunk(writer, 0x58385056, ExtendedHeader(image.Width, image.Height, 0x10));
		WriteImage(writer, image, options);
		return Finish(writer);
	}

	public static byte[] Encode(Animation animation, WebpEncodingOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (animation.Plays is < 0 or > ushort.MaxValue)
			throw new NotSupportedException("WebP loop count must fit in 16 bits.");
		Animation.EnsureWithinBudget(animation.FrameCount, (long)animation.Width * animation.Height * animation.FrameCount, animation.Limits);
		using var stream = new MemoryStream();
		using var writer = Header(stream);
		Chunk(writer, 0x58385056, ExtendedHeader(animation.Width, animation.Height, 0x12));
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
			WriteImage(frameWriter, frame.Image, options);
			Chunk(writer, 0x464d4e41, payload.ToArray());
			count++;
		}
		if (count != animation.FrameCount) throw new InvalidDataException("WebP animation frame count differs.");
		return Finish(writer);
	}

	public static byte[] Encode(RasterImage image) => Encode(image, WebpEncodingOptions.Lossless);
	public static byte[] Encode(Animation animation) => Encode(animation, WebpEncodingOptions.Lossless);

	private static void WriteImage(BinaryWriter writer, RasterImage image, WebpEncodingOptions options)
	{
		if (options.Mode == WebpCompressionModes.Lossless)
		{
			Chunk(writer, 0x4c385056, Bitstream(image));
			return;
		}
		if (image.Width > 16383 || image.Height > 16383) throw new NotSupportedException("VP8 dimensions cannot exceed 16383.");
		var rgb = new byte[checked(image.Width * image.Height * 3)];
		for (var i = 0; i < image.Pixels.Length / 4; i++)
		{
			rgb[i * 3] = image.Pixels[i * 4];
			rgb[i * 3 + 1] = image.Pixels[i * 4 + 1];
			rgb[i * 3 + 2] = image.Pixels[i * 4 + 2];
		}
		if (HasAlpha(image))
		{
			var channel = new RasterImage(image.Width, image.Height, image.Limits);
			for (var i = 0; i < channel.Pixels.Length; i += 4)
			{
				channel.Pixels[i + 1] = image.Pixels[i + 3];
				channel.Pixels[i + 3] = 255;
			}
			var compressed = WebpLosslessEncoder.Encode(channel);
			var alpha = new byte[compressed.Length - 4];
			alpha[0] = 1;
			compressed.AsSpan(5).CopyTo(alpha.AsSpan(1));
			Chunk(writer, 0x48504c41, alpha);
		}
		Chunk(writer, 0x20385056, Vp8ImageEncoder.Encode(rgb, image.Width, image.Height, options.Quality));
	}

	private static bool HasAlpha(RasterImage image)
	{
		for (var i = 3; i < image.Pixels.Length; i += 4) if (image.Pixels[i] != 255) return true;
		return false;
	}

	private static byte[] ExtendedHeader(int width, int height, byte flags)
	{
		var header = new byte[10];
		header[0] = flags;
		Write24(header.AsSpan(4), width - 1);
		Write24(header.AsSpan(7), height - 1);
		return header;
	}

	private static byte[] Bitstream(RasterImage image)
	{
		if (image.Width > 16384 || image.Height > 16384)
			throw new NotSupportedException("VP8L dimensions cannot exceed 16384.");
		return WebpLosslessEncoder.Encode(image);
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

}
