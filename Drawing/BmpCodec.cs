namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Numerics;

/// <summary>
/// BMP/DIB in pure managed code. Palette 1/4/8bpp, RGB555/RGB565 and BGRA32,
/// RGB24, BI_BITFIELDS and RLE4/RLE8 are supported. The encoder emits V4 BGRA32.
/// </summary>
internal static class BmpCodec
{
	public static bool IsBmp(ReadOnlySpan<byte> data) =>
		data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M';

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> data)
	{
		if (!IsBmp(data) || data.Length < 26)
			throw new InvalidDataException("BMP file header is incomplete.");
		var dib = U32(data, 14);
		int width, height;
		if (dib == 12)
		{
			width = U16(data, 18);
			height = U16(data, 20);
		}
		else if (dib >= 40 && dib <= 124 && data.Length >= 14 + dib)
		{
			width = I32(data, 18);
			var h = I32(data, 22);
			if (h == int.MinValue) throw new InvalidDataException("Invalid BMP height.");
			height = Math.Abs(h);
		}
		else throw new NotSupportedException("Unsupported BMP DIB header.");

		if (width <= 0 || height <= 0 || (long)width * height > 25_000_000)
			throw new InvalidDataException("BMP dimensions exceed the 25-megapixel limit.");
		return (width, height);
	}

	public static RasterImage Decode(byte[] bytes)
	{
		var data = bytes.AsSpan();
		var (width, height) = ReadSize(data);
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("BMP is too large.");

		var dib = U32(data, 14);
		var core = dib == 12;
		var bits = U16(data, core ? 24 : 28);
		var compression = core ? 0u : U32(data, 30);
		var topDown = !core && I32(data, 22) < 0;
		if (bits is not (1 or 4 or 8 or 16 or 24 or 32))
			throw new NotSupportedException("BMP bit depth is not supported.");
		if (!(compression == 0 || compression == 3 || compression == 6 ||
			(compression == 1 && bits == 8) || (compression == 2 && bits == 4)))
			throw new NotSupportedException("BMP compression is not supported.");
		if (topDown && compression is 1 or 2)
			throw new InvalidDataException("Compressed BMP cannot be top-down.");

		var pixelOffset = checked((int)U32(data, 10));
		if (pixelOffset > data.Length || pixelOffset < 14 + (int)dib)
			throw new InvalidDataException("Invalid BMP pixel offset.");

		var rMask = bits == 16 ? 0x7C00u : 0x00FF0000u;
		var gMask = bits == 16 ? 0x03E0u : 0x0000FF00u;
		var bMask = bits == 16 ? 0x001Fu : 0x000000FFu;
		var aMask = 0u;

		var cursor = checked(14 + (int)dib);
		if (compression is 3 or 6)
		{
			if (bits is not (16 or 32))
				throw new InvalidDataException("BMP bitfields require 16 or 32 bits.");
			if (dib >= 52)
			{
				rMask = U32(data, 54);
				gMask = U32(data, 58);
				bMask = U32(data, 62);
				if (dib >= 56) aMask = U32(data, 66);
			}
			else
			{
				if (cursor + (compression == 6 ? 16 : 12) > pixelOffset)
					throw new InvalidDataException("BMP masks exceed pixel offset.");
				rMask = U32(data, cursor);
				gMask = U32(data, cursor + 4);
				bMask = U32(data, cursor + 8);
				cursor += 12;
				if (compression == 6) { aMask = U32(data, cursor); cursor += 4; }
				else if (cursor + 4 <= pixelOffset) // Optional alpha mask
				{
					var candidate = U32(data, cursor);
					if ((candidate & (rMask | gMask | bMask)) == 0)
					{
						aMask = candidate;
						cursor += 4;
					}
				}
			}

			if (rMask == 0 || gMask == 0 || bMask == 0 ||
				((rMask & gMask) | (rMask & bMask) | (gMask & bMask)) != 0 ||
				(aMask & (rMask | gMask | bMask)) != 0)
				throw new InvalidDataException("BMP channel masks are overlapping or empty.");
		}
		else if (bits == 32)
			aMask = 0xFF000000;

		var count = bits <= 8 ? 1 << bits : 0;
		if (count != 0 && !core)
		{
			var used = U32(data, 46);
			if (used > count) throw new InvalidDataException("BMP palette is too large.");
			if (used != 0) count = (int)used;
		}

		var palette = new byte[count * 4];
		for (var index = 0; index < count; index++)
		{
			var stride = core ? 3 : 4;
			if (cursor + stride > pixelOffset)
				throw new InvalidDataException("Truncated BMP palette.");
			palette[index * 4] = data[cursor + 2];
			palette[index * 4 + 1] = data[cursor + 1];
			palette[index * 4 + 2] = data[cursor];
			palette[index * 4 + 3] = 255;
			cursor += stride;
		}

		var result = new RasterImage(width, height);
		if (compression is 1 or 2)
		{
			DecodeRle(data[pixelOffset..], result, bits, palette);
			return result;
		}

		var strideBytes = checked((int)(((long)width * bits + 31) / 32 * 4));
		if ((long)pixelOffset + (long)strideBytes * height > data.Length)
			throw new InvalidDataException("Truncated BMP pixel array.");

		var nonZeroAlpha = false;
		for (var y = 0; y < height; y++)
		{
			var sourceRow = pixelOffset + (topDown ? y : height - 1 - y) * strideBytes;
			for (var x = 0; x < width; x++)
			{
				var dest = (y * width + x) * 4;
				if (bits <= 8)
				{
					var packed = data[sourceRow + x * bits / 8];
					var index = (packed >> (8 - bits - (x * bits & 7))) & ((1 << bits) - 1);
					CopyPalette(index, result.Pixels, dest, palette);
				}
				else if (bits == 24)
				{
					var src = sourceRow + x * 3;
					result.Pixels[dest] = data[src + 2];
					result.Pixels[dest + 1] = data[src + 1];
					result.Pixels[dest + 2] = data[src];
					result.Pixels[dest + 3] = 255;
				}
				else
				{
					var src = sourceRow + x * (bits / 8);
					uint value = bits == 16 ? U16(data, src) : U32(data, src);
					result.Pixels[dest] = Channel(value, rMask);
					result.Pixels[dest + 1] = Channel(value, gMask);
					result.Pixels[dest + 2] = Channel(value, bMask);
					var alpha = aMask == 0 ? (byte)255 : Channel(value, aMask);
					result.Pixels[dest + 3] = alpha;
					nonZeroAlpha |= alpha != 0;
				}
			}
		}

		// Windows BI_RGB 32bpp traditionally stores a reserved (not alpha) byte.
		if (bits == 32 && compression == 0 && !nonZeroAlpha)
			for (var i = 3; i < result.Pixels.Length; i += 4)
				result.Pixels[i] = 255;

		return result;
	}

	private static void DecodeRle(ReadOnlySpan<byte> input, RasterImage image, int bits, byte[] palette)
	{
		var x = 0;
		var y = image.Height - 1;
		var position = 0;
		var ended = false;
		// Skipped pixels and unwritten rows use palette index zero.
		for (var i = 0; i < image.Width * image.Height; i++)
			CopyPalette(0, image.Pixels, i * 4, palette);

		void Write(int idx)
		{
			if (y < 0 || x >= image.Width)
				throw new InvalidDataException("BMP RLE writes beyond image bounds.");
			CopyPalette(idx, image.Pixels, (y * image.Width + x) * 4, palette);
			x++;
		}

		while (position < input.Length)
		{
			if (input.Length - position < 2)
				throw new InvalidDataException("Truncated BMP RLE command.");
			var run = input[position++];
			var code = input[position++];
			if (run != 0)
			{
				for (var k = 0; k < run; k++)
					Write(bits == 8 ? code : (k & 1) == 0 ? code >> 4 : code & 15);
				continue;
			}

			if (code == 0) { x = 0; y--; continue; }
			if (code == 1) { ended = true; break; }
			if (code == 2)
			{
				if (input.Length - position < 2)
					throw new InvalidDataException("Truncated BMP RLE delta.");
				x += input[position++];
				y -= input[position++];
				if (x > image.Width || y < 0)
					throw new InvalidDataException("BMP RLE delta outside image.");
				continue;
			}

			var byteCount = bits == 8 ? code : (code + 1) / 2;
			if (input.Length - position < byteCount + (byteCount & 1))
				throw new InvalidDataException("Truncated BMP RLE absolute run.");
			for (var k = 0; k < code; k++)
			{
				var value = input[position + (bits == 8 ? k : k / 2)];
				Write(bits == 8 ? value : (k & 1) == 0 ? value >> 4 : value & 15);
			}
			position += byteCount + (byteCount & 1);
		}
		if (!ended) throw new InvalidDataException("BMP RLE end marker is missing.");
	}

	public static byte[] Encode(RasterImage image)
	{
		ArgumentNullException.ThrowIfNull(image);
		var size = checked(14 + 108 + image.Pixels.Length);
		var output = new byte[size];
		output[0] = (byte)'B';
		output[1] = (byte)'M';
		Put(output, 2, (uint)size);
		Put(output, 10, 122);
		Put(output, 14, 108); // BITMAPV4HEADER
		Put(output, 18, (uint)image.Width);
		Put(output, 22, (uint)-image.Height); // top-down
		BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(26), 1);
		BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(28), 32);
		Put(output, 30, 3); // BI_BITFIELDS
		Put(output, 34, (uint)image.Pixels.Length);
		Put(output, 54, 0x00FF0000);
		Put(output, 58, 0x0000FF00);
		Put(output, 62, 0x000000FF);
		Put(output, 66, 0xFF000000);
		Put(output, 70, 0x73524742); // LCS_sRGB
		for (var i = 0; i < image.Width * image.Height; i++)
		{
			var src = i * 4;
			var dest = 122 + src;
			output[dest] = image.Pixels[src + 2];
			output[dest + 1] = image.Pixels[src + 1];
			output[dest + 2] = image.Pixels[src];
			output[dest + 3] = image.Pixels[src + 3];
		}
		return output;
	}

	private static byte Channel(uint value, uint mask)
	{
		if (mask == 0) return 255;
		var shifted = value & mask;
		var firstBit = BitOperations.TrailingZeroCount(mask);
		var scale = mask >> firstBit;
		var component = shifted >> firstBit;
		return (byte)((component * 255UL + scale / 2UL) / scale);
	}

	private static void CopyPalette(int index, byte[] output, int at, byte[] palette)
	{
		if ((uint)index >= (uint)(palette.Length / 4))
			throw new InvalidDataException("BMP palette index outside table.");
		Array.Copy(palette, index * 4, output, at, 4);
	}

	private static ushort U16(ReadOnlySpan<byte> data, int offset)
	{
		if (offset < 0 || data.Length - offset < 2)
			throw new InvalidDataException("Truncated BMP.");
		return BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);
	}

	private static uint U32(ReadOnlySpan<byte> data, int offset)
	{
		if (offset < 0 || data.Length - offset < 4)
			throw new InvalidDataException("Truncated BMP.");
		return BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
	}

	private static int I32(ReadOnlySpan<byte> data, int offset) =>
		unchecked((int)U32(data, offset));

	private static void Put(byte[] data, int offset, uint value) =>
		BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
}
