namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>Managed PNG decoder/encoder (all standard color types, 1/2/4/8/16-bit samples, Adam7).</summary>
internal static class PngCodec
{
	private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];
	private static readonly uint[] _crcTable = CreateCrcTable();
	private static readonly (int x, int y, int dx, int dy)[] _passes =
	[
		(0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4),
		(0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2),
	];

	public static bool IsPng(ReadOnlySpan<byte> data) => data.StartsWith(Signature);

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length < 24 || !IsPng(bytes) || !bytes.Slice(12, 4).SequenceEqual("IHDR"u8) ||
			BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8, 4)) != 13)
			throw new InvalidDataException("Invalid PNG IHDR header.");

		var width = BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(16, 4));
		var height = BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(20, 4));
		ValidateSize(width, height);
		return (width, height);
	}

	public static RasterImage Decode(byte[] data)
	{
		var (width, height) = ReadSize(data);
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("PNG input exceeds the supported size.");

		var depth = 0;
		var color = 0;
		var interlace = 0;
		var palette = Array.Empty<byte>();
		var transparency = Array.Empty<byte>();
		var gotHeader = false;
		var gotData = false;
		var ended = false;
		using var compressed = new MemoryStream();
		var offset = 8;

		while (offset <= data.Length - 12)
		{
			var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
			if (length > int.MaxValue || length > data.Length - offset - 12)
				throw new InvalidDataException("Invalid PNG chunk length.");

			var tag = data.AsSpan(offset + 4, 4);
			var content = data.AsSpan(offset + 8, (int)length);
			var actualCrc = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8 + (int)length, 4));
			if (actualCrc != Crc(data.AsSpan(offset + 4, (int)length + 4)))
				throw new InvalidDataException("PNG CRC mismatch.");

			if (tag.SequenceEqual("IHDR"u8))
			{
				if (gotHeader || offset != 8 || length != 13)
					throw new InvalidDataException("Invalid PNG IHDR placement.");

				gotHeader = true;
				depth = content[8];
				color = content[9];
				interlace = content[12];
				if (content[10] != 0 || content[11] != 0 || interlace > 1 || !ValidFormat(depth, color))
					throw new NotSupportedException("Unsupported PNG bit depth, color type, or interlace method.");
			}
			else if (tag.SequenceEqual("PLTE"u8))
			{
				if (!gotHeader || gotData || length == 0 || length > 768 || length % 3 != 0)
					throw new InvalidDataException("Invalid PNG palette.");

				palette = content.ToArray();
			}
			else if (tag.SequenceEqual("tRNS"u8))
			{
				if (!gotHeader || gotData)
					throw new InvalidDataException("Invalid PNG transparency chunk.");

				transparency = content.ToArray();
			}
			else if (tag.SequenceEqual("IDAT"u8))
			{
				if (!gotHeader)
					throw new InvalidDataException("PNG data precedes header.");
				gotData = true;
				compressed.Write(content);
			}
			else if (tag.SequenceEqual("IEND"u8))
			{
				if (length != 0) throw new InvalidDataException("Invalid IEND.");
				ended = true;
				break;
			}
			else if ((tag[0] & 0x20) == 0)
				throw new NotSupportedException("Unknown critical PNG chunk.");

			offset = checked(offset + 12 + (int)length);
		}

		if (!gotHeader || !gotData || !ended)
			throw new InvalidDataException("PNG is incomplete.");
		if (color == 3 && (palette.Length == 0 || transparency.Length > palette.Length / 3))
			throw new InvalidDataException("Invalid indexed PNG palette.");
		if ((color == 0 && transparency.Length != 0 && transparency.Length != 2) ||
			(color == 2 && transparency.Length != 0 && transparency.Length != 6) ||
			(color != 0 && color != 2 && color != 3 && transparency.Length != 0))
			throw new InvalidDataException("Invalid PNG transparency.");

		var channels = color switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new InvalidDataException() };
		var rowBytes = (int)(((long)width * channels * depth + 7) / 8);
		var needed = interlace == 0 ? checked((long)(rowBytes + 1) * height) : 0L;

		if (interlace == 1)
			foreach (var (px, py, dx, dy) in _passes)
			{
				var w = PassSize(width, px, dx);
				var h = PassSize(height, py, dy);
				if (w != 0 && h != 0)
					needed += (((long)w * channels * depth + 7) / 8 + 1) * h;
			}

		if (needed > 200_000_000)
			throw new InvalidDataException("Decompressed PNG exceeds the supported size.");

		byte[] raw;
		compressed.Position = 0;
		using (var zlib = new ZLibStream(compressed, CompressionMode.Decompress, leaveOpen: true))
		using (var inflated = new MemoryStream())
		{
			var block = new byte[8192];
			while (true)
			{
				var read = zlib.Read(block, 0, block.Length);
				if (read == 0) break;
				if (inflated.Length + read > needed)
					throw new InvalidDataException("PNG decompression exceeded expected row data.");
				inflated.Write(block, 0, read);
			}
			raw = inflated.ToArray();
		}

		if (raw.LongLength != needed)
			throw new InvalidDataException("PNG scanlines have an invalid length.");

		var result = new RasterImage(width, height);
		var cursor = 0;

		if (interlace == 0)
			DecodePass(result, raw, ref cursor, width, height, 0, 0, 1, 1,
				channels, depth, color, palette, transparency);
		else
			foreach (var (px, py, dx, dy) in _passes)
			{
				var w = PassSize(width, px, dx);
				var h = PassSize(height, py, dy);
				if (w != 0 && h != 0)
					DecodePass(result, raw, ref cursor, w, h, px, py, dx, dy,
						channels, depth, color, palette, transparency);
			}

		return result;
	}

	public static byte[] Encode(RasterImage image)
	{
		using var output = new MemoryStream();
		output.Write(Signature);
		var header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), image.Width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), image.Height);
		header[8] = 8;
		header[9] = 6; // RGBA8
		WriteChunk(output, "IHDR"u8, header);

		using var compressed = new MemoryStream();
		using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
		{
			var rowBytes = image.Width * 4;
			for (var y = 0; y < image.Height; y++)
			{
				zlib.WriteByte(0); // Filter None: deterministic encoding
				zlib.Write(image.Pixels, y * rowBytes, rowBytes);
			}
		}

		WriteChunk(output, "IDAT"u8, compressed.ToArray());
		WriteChunk(output, "IEND"u8, []);
		return output.ToArray();
	}

	private static void DecodePass(RasterImage image, byte[] raw, ref int cursor,
		int width, int height, int px, int py, int dx, int dy,
		int channels, int depth, int color, byte[] palette, byte[] transparency)
	{
		var rowLen = checked((int)(((long)width * channels * depth + 7) / 8));
		var stride = Math.Max(1, (channels * depth + 7) / 8);
		var previous = new byte[rowLen];
		var row = new byte[rowLen];

		for (var y = 0; y < height; y++)
		{
			var filter = raw[cursor++];
			Array.Copy(raw, cursor, row, 0, rowLen);
			cursor += rowLen;

			for (var i = 0; i < rowLen; i++)
			{
				var a = i >= stride ? row[i - stride] : 0;
				var b = previous[i];
				var c = i >= stride ? previous[i - stride] : 0;
				var prediction = filter switch
				{
					0 => 0,
					1 => a,
					2 => b,
					3 => (a + b) / 2,
					4 => Paeth(a, b, c),
					_ => throw new InvalidDataException("Unsupported PNG scanline filter."),
				};
				row[i] = unchecked((byte)(row[i] + prediction));
			}

			for (var x = 0; x < width; x++)
			{
				var dest = ((py + y * dy) * image.Width + px + x * dx) * 4;
				var sample = x * channels;
				switch (color)
				{
					case 0:
						var grayRaw = Sample(row, sample, depth);
						var gray = Normalize(grayRaw, depth);
						var ga = transparency.Length == 2 &&
							grayRaw == BinaryPrimitives.ReadUInt16BigEndian(transparency) ? (byte)0 : (byte)255;
						Assign(image.Pixels, dest, gray, gray, gray, ga);
						break;

					case 2:
						var rr = Sample(row, sample, depth);
						var gg = Sample(row, sample + 1, depth);
						var bb = Sample(row, sample + 2, depth);
						var ra = transparency.Length == 6 &&
							rr == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0, 2)) &&
							gg == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2, 2)) &&
							bb == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4, 2)) ? (byte)0 : (byte)255;
						Assign(image.Pixels, dest, Normalize(rr, depth), Normalize(gg, depth), Normalize(bb, depth), ra);
						break;

					case 3:
						var index = Sample(row, x, depth);
						if (index >= palette.Length / 3)
							throw new InvalidDataException("PNG palette index outside palette.");
						Assign(image.Pixels, dest, palette[index * 3], palette[index * 3 + 1],
							palette[index * 3 + 2], index < transparency.Length ? transparency[index] : (byte)255);
						break;

					case 4:
						var l = Normalize(Sample(row, sample, depth), depth);
						Assign(image.Pixels, dest, l, l, l, Normalize(Sample(row, sample + 1, depth), depth));
						break;

					case 6:
						Assign(image.Pixels, dest,
							Normalize(Sample(row, sample, depth), depth),
							Normalize(Sample(row, sample + 1, depth), depth),
							Normalize(Sample(row, sample + 2, depth), depth),
							Normalize(Sample(row, sample + 3, depth), depth));
						break;
				}
			}

			(row, previous) = (previous, row);
		}
	}

	private static void Assign(byte[] p, int offset, byte r, byte g, byte b, byte a)
	{
		p[offset] = r;
		p[offset + 1] = g;
		p[offset + 2] = b;
		p[offset + 3] = a;
	}

	private static int Sample(byte[] row, int index, int depth)
	{
		if (depth == 8) return row[index];
		if (depth == 16) return BinaryPrimitives.ReadUInt16BigEndian(row.AsSpan(index * 2, 2));

		var offset = index * depth;
		return row[offset / 8] >> (8 - depth - offset % 8) & ((1 << depth) - 1);
	}

	private static byte Normalize(int sample, int depth)
		=> depth switch
		{
			8 => (byte)sample,
			16 => (byte)(sample >> 8),
			_ => (byte)(sample * 255 / ((1 << depth) - 1)),
		};

	private static bool ValidFormat(int depth, int color) => color switch
	{
		0 => depth is 1 or 2 or 4 or 8 or 16,
		2 => depth is 8 or 16,
		3 => depth is 1 or 2 or 4 or 8,
		4 => depth is 8 or 16,
		6 => depth is 8 or 16,
		_ => false,
	};

	private static int PassSize(int total, int start, int step)
		=> total <= start ? 0 : (total - start + step - 1) / step;

	private static void ValidateSize(int width, int height)
	{
		if (width <= 0 || height <= 0 || (long)width * height > 25_000_000)
			throw new InvalidDataException("PNG size exceeds the 25-megapixel limit.");
	}

	private static int Paeth(int a, int b, int c)
	{
		var p = a + b - c;
		var da = Math.Abs(p - a);
		var db = Math.Abs(p - b);
		var dc = Math.Abs(p - c);
		return da <= db && da <= dc ? a : db <= dc ? b : c;
	}

	private static void WriteChunk(Stream output, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> content)
	{
		Span<byte> length = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(length, content.Length);
		output.Write(length);
		output.Write(tag);
		output.Write(content);
		var buffer = new byte[checked(4 + content.Length)];
		tag.CopyTo(buffer);
		content.CopyTo(buffer.AsSpan(4));
		BinaryPrimitives.WriteUInt32BigEndian(length, Crc(buffer));
		output.Write(length);
	}

	private static uint Crc(ReadOnlySpan<byte> content)
	{
		uint crc = 0xFFFFFFFF;
		foreach (var b in content)
			crc = _crcTable[(byte)(crc ^ b)] ^ (crc >> 8);
		return crc ^ 0xFFFFFFFF;
	}

	private static uint[] CreateCrcTable()
	{
		var table = new uint[256];
		for (uint i = 0; i < table.Length; i++)
		{
			var c = i;
			for (var j = 0; j < 8; j++)
				c = (c & 1) == 1 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
			table[i] = c;
		}
		return table;
	}
}
