namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>Windows icon directory with PNG or uncompressed/bitfield DIB frames.</summary>
internal static class IconCodec
{
	public static bool IsIcon(ReadOnlySpan<byte> data)
		=> data.Length >= 4 && data[0] == 0 && data[1] == 0 && data[2] == 1 && data[3] == 0;

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> data)
	{
		var frame = ReadFrame(data, out var width, out var height);
		var size = PngCodec.IsPng(frame) ? PngCodec.ReadSize(frame) : ReadDibSize(frame);
		if (size != (width, height))
			throw new InvalidDataException("Icon frame dimensions differ from its directory entry.");
		return size;
	}

	public static RasterImage Decode(byte[] data)
		=> Decode(data, ImageProcessingLimits.Default);

	public static RasterImage Decode(byte[] data, ImageProcessingLimits limits)
	{
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("Icon input exceeds the supported size.");
		var frame = ReadFrame(data, out var width, out var height);
		if (ReadSize(data) != (width, height))
			throw new InvalidDataException("Invalid icon dimensions.");
		if (PngCodec.IsPng(frame))
			return PngCodec.Decode(frame.ToArray(), limits);

		var header = BinaryPrimitives.ReadInt32LittleEndian(frame);
		var bits = BinaryPrimitives.ReadUInt16LittleEndian(frame[14..]);
		var compression = BinaryPrimitives.ReadUInt32LittleEndian(frame[16..]);
		if (bits is not (1 or 4 or 8 or 16 or 24 or 32) || compression is not (0 or 3 or 6))
			throw new NotSupportedException("Unsupported icon DIB encoding.");
		var colors = bits <= 8 ? 1 << bits : 0;
		var used = BinaryPrimitives.ReadUInt32LittleEndian(frame[32..]);
		if (used > colors)
			throw new InvalidDataException("Invalid icon palette length.");
		if (used != 0)
			colors = (int)used;
		var masks = header == 40 && compression is 3 or 6 ? compression == 6 ? 16 : 12 : 0;
		var pixelOffset = header + masks + colors * 4;
		var stride = (width * bits + 31) / 32 * 4;
		var maskStride = (width + 31) / 32 * 4;
		var maskOffset = pixelOffset + stride * height;
		if (maskOffset > frame.Length || (bits != 32 && frame.Length - maskOffset < maskStride * height))
			throw new InvalidDataException("Truncated icon bitmap or mask.");

		var bmp = new byte[14 + maskOffset];
		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 14 + pixelOffset);
		frame[..maskOffset].CopyTo(bmp.AsSpan(14));
		BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), height);
		var image = BmpCodec.Decode(bmp, limits);
		var hasAlpha = false;
		if (bits == 32 && compression == 0)
			for (var y = 0; y < height; y++)
				for (var x = 0; x < width; x++)
					hasAlpha |= frame[pixelOffset + y * stride + x * 4 + 3] != 0;

		for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var at = (y * width + x) * 4 + 3;
				var sourceY = height - y - 1;
				if (hasAlpha)
					image.Pixels[at] = frame[pixelOffset + sourceY * stride + x * 4 + 3];
				else if (frame.Length - maskOffset >= maskStride * height &&
					(frame[maskOffset + sourceY * maskStride + x / 8] & (0x80 >> (x & 7))) != 0)
					image.Pixels[at] = 0;
			}
		return image;
	}

	public static byte[] Encode(RasterImage image)
	{
		if (image.Width > 256 || image.Height > 256)
			throw new InvalidDataException("Icon dimensions exceed 256 pixels.");
		var png = PngCodec.Encode(image, []);
		var output = new byte[22 + png.Length];
		BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(2), 1);
		BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(4), 1);
		output[6] = unchecked((byte)image.Width);
		output[7] = unchecked((byte)image.Height);
		BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(10), 1);
		BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(12), 32);
		BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(14), png.Length);
		BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(18), 22);
		png.CopyTo(output, 22);
		return output;
	}

	private static (int width, int height) ReadDibSize(ReadOnlySpan<byte> frame)
	{
		if (frame.Length < 40)
			throw new InvalidDataException("Truncated icon DIB header.");
		var header = BinaryPrimitives.ReadUInt32LittleEndian(frame);
		if (header is < 40 or > 124 || header > frame.Length)
			throw new NotSupportedException("Unsupported icon DIB header.");
		var width = BinaryPrimitives.ReadInt32LittleEndian(frame[4..]);
		var height = BinaryPrimitives.ReadInt32LittleEndian(frame[8..]);
		if (width < 1 || height < 2 || height % 2 != 0 || BinaryPrimitives.ReadUInt16LittleEndian(frame[12..]) != 1)
			throw new InvalidDataException("Invalid icon DIB dimensions or planes.");
		return (width, height / 2);
	}

	private static ReadOnlySpan<byte> ReadFrame(ReadOnlySpan<byte> data, out int width, out int height)
	{
		if (!IsIcon(data) || data.Length < 6)
			throw new InvalidDataException("Invalid icon directory.");
		var count = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
		var directoryEnd = 6 + count * 16;
		if (count == 0 || directoryEnd > data.Length)
			throw new InvalidDataException("Truncated or empty icon directory.");
		var best = -1;
		var area = 0;
		var depth = -1;
		width = height = 0;
		for (var i = 0; i < count; i++)
		{
			var entry = data.Slice(6 + i * 16, 16);
			var size = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
			var start = BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
			if (start < directoryEnd || start > data.Length || size == 0 || size > data.Length - start)
				throw new InvalidDataException("Icon frame exceeds its file.");
			var w = entry[0] == 0 ? 256 : entry[0];
			var h = entry[1] == 0 ? 256 : entry[1];
			var bits = BinaryPrimitives.ReadUInt16LittleEndian(entry[6..]);
			if (w * h > area || (w * h == area && bits > depth))
			{
				best = i;
				area = w * h;
				depth = bits;
				width = w;
				height = h;
			}
		}
		var selected = data.Slice(6 + best * 16, 16);
		return data.Slice((int)BinaryPrimitives.ReadUInt32LittleEndian(selected[12..]),
			(int)BinaryPrimitives.ReadUInt32LittleEndian(selected[8..]));
	}
}
