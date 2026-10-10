namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Pure managed decoder for baseline sequential 8-bit JPEG (grayscale and YCbCr, including
/// common 4:2:0 / 4:2:2 subsampling and DRI restart markers). Progressive, arithmetic-coded,
/// CMYK and multi-scan JPEGs are deliberately rejected rather than misdecoded.
/// </summary>
internal static class JpegCodec
{
	private static readonly byte[] _zigzag =
	[
		0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5,
		12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21,
		28, 35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37,
		44, 51, 58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55,
		62, 63
	];

	private static readonly double[,] _cos = CreateCosines();
	private static readonly double[] _normal = [1 / Math.Sqrt(2), 1, 1, 1, 1, 1, 1, 1];

	private sealed class Component
	{
		public int Id, H, V, Q, DcTable, AcTable, DcPredictor, Stride;
		public byte[] Samples;
	}

	private sealed class Huffman
	{
		private readonly Dictionary<int, byte> _lookup = new();

		public Huffman(ReadOnlySpan<byte> counts, ReadOnlySpan<byte> symbols)
		{
			var code = 0;
			var index = 0;
			for (var length = 1; length <= 16; length++)
			{
				for (var n = 0; n < counts[length - 1]; n++)
					_lookup[(length << 16) | code++] = symbols[index++];

				if (code > (1 << length))
					throw new InvalidDataException("Invalid JPEG Huffman code lengths.");

				code <<= 1;
			}
		}

		public int ReadSymbol(Bits bits)
		{
			var code = 0;
			for (var length = 1; length <= 16; length++)
			{
				code = code << 1 | bits.Bit();
				if (_lookup.TryGetValue((length << 16) | code, out var symbol))
					return symbol;
			}
			throw new InvalidDataException("Malformed JPEG Huffman bitstream.");
		}
	}

	private sealed class Bits
	{
		private readonly byte[] _data;
		private int _offset;
		private int _value, _remaining;
		public Bits(byte[] data, int offset) { _data = data; _offset = offset; }

		public int Bit()
		{
			if (_remaining == 0)
			{
				if (_offset >= _data.Length)
					throw new InvalidDataException("Truncated JPEG entropy data.");

				_value = _data[_offset++];
				if (_value == 0xFF)
				{
					if (_offset >= _data.Length || _data[_offset++] != 0x00)
						throw new InvalidDataException("Unexpected marker in JPEG entropy data.");
				}
				_remaining = 8;
			}
			return _value >> --_remaining & 1;
		}

		public int Read(int count)
		{
			var value = 0;
			for (var i = 0; i < count; i++)
				value = value << 1 | Bit();
			return value;
		}

		public int Signed(int count)
		{
			if (count == 0) return 0;
			var v = Read(count);
			return v < (1 << (count - 1)) ? v - ((1 << count) - 1) : v;
		}

		public void Restart(int index)
		{
			_remaining = 0;
			if (_offset >= _data.Length || _data[_offset++] != 0xFF)
				throw new InvalidDataException("Missing JPEG restart marker.");
			while (_offset < _data.Length && _data[_offset] == 0xFF)
				_offset++;
			if (_offset >= _data.Length || _data[_offset++] != 0xD0 + index % 8)
				throw new InvalidDataException("Unexpected JPEG restart marker.");
		}
	}

	public static bool IsJpeg(ReadOnlySpan<byte> data)
		=> data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff;

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> data)
	{
		var cursor = 2;
		if (!IsJpeg(data)) throw new InvalidDataException("Invalid JPEG signature.");

		while (NextSegment(data, ref cursor, out var marker, out var segment))
		{
			if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
			{
				if (segment.Length < 6) throw new InvalidDataException("Truncated JPEG frame.");
				var h = U16(segment, 1);
				var w = U16(segment, 3);
				CheckSize(w, h);
				return (w, h);
			}
			if (marker == 0xDA) break;
		}

		throw new InvalidDataException("JPEG has no frame dimensions.");
	}

	public static RasterImage Decode(byte[] data)
	{
		if (JpegProgressiveCodec.IsProgressive(data))
			return JpegProgressiveCodec.Decode(data);

		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("JPEG input exceeds the supported size.");

		if (!IsJpeg(data))
			throw new InvalidDataException("Invalid JPEG signature.");

		var cursor = 2;
		var quant = new int[4][];
		var dc = new Huffman[4];
		var ac = new Huffman[4];
		var components = Array.Empty<Component>();
		var width = 0;
		var height = 0;
		var restartInterval = 0;
		var adobeTransform = -1;
		var foundScan = false;

		while (NextSegment(data, ref cursor, out var marker, out var segment))
		{
			switch (marker)
			{
				case 0xEE:
					if (segment.Length >= 12 && segment.Slice(0, 5).SequenceEqual("Adobe"u8))
						adobeTransform = segment[11];
					break;

				case 0xDB:
					for (var p = 0; p < segment.Length;)
					{
						var t = segment[p++];
						var prec = t >> 4;
						var idx = t & 15;
						if (idx > 3 || prec > 1 || segment.Length - p < 64 * (prec + 1))
							throw new InvalidDataException("Invalid JPEG quantization table.");

						var values = new int[64];
						for (var i = 0; i < 64; i++)
						{
							values[_zigzag[i]] = prec == 0 ? segment[p++] : U16(segment, p);
							if (prec != 0) p += 2;
						}
						quant[idx] = values;
					}
					break;

				case 0xC4:
					for (var p = 0; p < segment.Length;)
					{
						if (segment.Length - p < 17)
							throw new InvalidDataException("Truncated JPEG Huffman table.");

						var id = segment[p++];
						var cls = id >> 4;
						var index = id & 15;
						if (cls > 1 || index > 3)
							throw new InvalidDataException("Invalid JPEG Huffman table index.");

						var counts = segment.Slice(p, 16);
						p += 16;
						var symbolCount = 0;
						foreach (var n in counts) symbolCount += n;
						if (symbolCount > 256 || symbolCount > segment.Length - p)
							throw new InvalidDataException("Invalid JPEG Huffman symbols.");

						var table = new Huffman(counts, segment.Slice(p, symbolCount));
						p += symbolCount;
						if (cls == 0) dc[index] = table;
						else ac[index] = table;
					}
					break;

				case 0xDD:
					if (segment.Length != 2) throw new InvalidDataException("Invalid restart interval.");
					restartInterval = U16(segment, 0);
					break;

				case 0xC0:
					if (components.Length != 0 || segment.Length < 6 || segment[0] != 8)
						throw new NotSupportedException("Only 8-bit baseline JPEG is supported.");

					height = U16(segment, 1);
					width = U16(segment, 3);
					CheckSize(width, height);
					var count = segment[5];

					if (count != 1 && count != 3 && count != 4)
						throw new NotSupportedException("JPEG must have 1, 3 or 4 color components.");

					if (segment.Length != 6 + count * 3)
						throw new InvalidDataException("Malformed JPEG frame components.");

					components = new Component[count];
					for (var i = 0; i < count; i++)
					{
						var at = 6 + i * 3;
						var h = segment[at + 1] >> 4;
						var v = segment[at + 1] & 15;
						var q = segment[at + 2];
						if (h < 1 || h > 4 || v < 1 || v > 4 || q > 3)
							throw new NotSupportedException("Unsupported JPEG sampling factors.");

						components[i] = new Component { Id = segment[at], H = h, V = v, Q = q };
					}
					break;

				case 0xDA:
					if (components.Length == 0 || segment.Length < 4)
						throw new InvalidDataException("JPEG scan precedes frame.");

					if (segment[0] != components.Length || segment.Length != 1 + 2 * components.Length + 3 ||
						segment[^3] != 0 || segment[^2] != 63 || segment[^1] != 0)
						throw new NotSupportedException("Multi-scan or non-baseline JPEG is unsupported.");

					for (var i = 0; i < components.Length; i++)
					{
						var id = segment[1 + 2 * i];
						Component part = null;
						foreach (var c in components)
							if (c.Id == id) part = c;

						if (part == null)
							throw new InvalidDataException("Invalid JPEG scan component.");

						var selector = segment[2 + 2 * i];
						part.DcTable = selector >> 4;
						part.AcTable = selector & 15;
						if (part.DcTable > 3 || part.AcTable > 3)
							throw new InvalidDataException("Invalid JPEG scan table.");
					}

					// Stop parsing at first SOS; the bitstream contains byte-stuffed markers.
					foundScan = true;
					break;

				default:
					if (marker is >= 0xC1 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
						throw new NotSupportedException("Progressive, lossless and arithmetic-coded JPEG formats are unsupported.");
					break;
			}
			if (foundScan) break;
		}

		if (!foundScan)
			throw new InvalidDataException("Missing JPEG image scan.");

		var maxH = 1;
		var maxV = 1;
		foreach (var c in components)
		{
			maxH = Math.Max(maxH, c.H);
			maxV = Math.Max(maxV, c.V);
		}

		var mcuWidth = (width + maxH * 8 - 1) / (maxH * 8);
		var mcuHeight = (height + maxV * 8 - 1) / (maxV * 8);

		foreach (var c in components)
		{
			if (quant[c.Q] == null || dc[c.DcTable] == null || ac[c.AcTable] == null)
				throw new InvalidDataException("Missing JPEG quantization or Huffman table.");
			c.Stride = mcuWidth * c.H * 8;
			c.Samples = new byte[checked(c.Stride * mcuHeight * c.V * 8)];
		}

		var reader = new Bits(data, cursor);
		var mcuNumber = 0;
		var block = new int[64];

		for (var my = 0; my < mcuHeight; my++)
			for (var mx = 0; mx < mcuWidth; mx++)
			{
				if (restartInterval > 0 && mcuNumber != 0 && mcuNumber % restartInterval == 0)
				{
					reader.Restart(mcuNumber / restartInterval - 1);
					foreach (var c in components) c.DcPredictor = 0;
				}

				foreach (var c in components)
					for (var cy = 0; cy < c.V; cy++)
						for (var cx = 0; cx < c.H; cx++)
						{
							Array.Clear(block);
							var dcLength = dc[c.DcTable].ReadSymbol(reader);
							if (dcLength > 11) throw new InvalidDataException("Invalid JPEG DC magnitude.");

							c.DcPredictor += reader.Signed(dcLength);
							block[0] = c.DcPredictor * quant[c.Q][0];

							for (var at = 1; at < 64;)
							{
								var symbol = ac[c.AcTable].ReadSymbol(reader);
								if (symbol == 0) break;
								if (symbol == 0xF0) { at += 16; continue; }

								var run = symbol >> 4;
								var size = symbol & 15;
								if (size == 0 || size > 10 || (at += run) >= 64)
									throw new InvalidDataException("Invalid JPEG AC symbol.");

								block[_zigzag[at++]] = reader.Signed(size) * quant[c.Q][_zigzag[at - 1]];
							}

							InverseDct(block, c.Samples, c.Stride, (mx * c.H + cx) * 8, (my * c.V + cy) * 8);
						}

				mcuNumber++;
			}

		var result = new RasterImage(width, height);
		for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var off = (y * width + x) * 4;
				var yy = Value(components[0], x, y, maxH, maxV);

				if (components.Length == 1)
				{
					result.Pixels[off] = result.Pixels[off + 1] = result.Pixels[off + 2] = yy;
				}
				else if (components.Length == 3)
				{
					var cb = Value(components[1], x, y, maxH, maxV) - 128;
					var cr = Value(components[2], x, y, maxH, maxV) - 128;
					result.Pixels[off] = RasterImage.ToByte(yy + 1.402 * cr);
					result.Pixels[off + 1] = RasterImage.ToByte(yy - 0.344136 * cb - 0.714136 * cr);
					result.Pixels[off + 2] = RasterImage.ToByte(yy + 1.772 * cb);
				}
				else
				{
					var k = Value(components[3], x, y, maxH, maxV);
					if (adobeTransform == 2) // YCCK: YCbCr encodes inverted CMY
					{
						var cb = Value(components[1], x, y, maxH, maxV) - 128;
						var cr = Value(components[2], x, y, maxH, maxV) - 128;
						result.Pixels[off] = RasterImage.ToByte((yy + 1.402 * cr) * k / 255.0);
						result.Pixels[off + 1] = RasterImage.ToByte((yy - 0.344136 * cb - 0.714136 * cr) * k / 255.0);
						result.Pixels[off + 2] = RasterImage.ToByte((yy + 1.772 * cb) * k / 255.0);
					}
					else if (adobeTransform == 0) // Adobe inverted CMYK
					{
						result.Pixels[off] = RasterImage.ToByte(yy * k / 255.0);
						result.Pixels[off + 1] = RasterImage.ToByte(Value(components[1], x, y, maxH, maxV) * k / 255.0);
						result.Pixels[off + 2] = RasterImage.ToByte(Value(components[2], x, y, maxH, maxV) * k / 255.0);
					}
					else if (adobeTransform == -1) // non-Adobe conventional CMYK
					{
						result.Pixels[off] = RasterImage.ToByte((255 - yy) * (255 - k) / 255.0);
						result.Pixels[off + 1] = RasterImage.ToByte((255 - Value(components[1], x, y, maxH, maxV)) * (255 - k) / 255.0);
						result.Pixels[off + 2] = RasterImage.ToByte((255 - Value(components[2], x, y, maxH, maxV)) * (255 - k) / 255.0);
					}
					else throw new NotSupportedException("Unsupported Adobe JPEG color transform.");
				}
				result.Pixels[off + 3] = 255;
			}

		return result;
	}

	private static byte Value(Component c, int x, int y, int maxH, int maxV)
		=> c.Samples[(y * c.V / maxV) * c.Stride + (x * c.H / maxH)];

	private static void InverseDct(int[] coeff, byte[] samples, int stride, int left, int top)
	{
		for (var y = 0; y < 8; y++)
			for (var x = 0; x < 8; x++)
			{
				double sum = 0;
				for (var v = 0; v < 8; v++)
					for (var u = 0; u < 8; u++)
						sum += _normal[u] * _normal[v] * coeff[v * 8 + u] * _cos[x, u] * _cos[y, v];

				samples[(top + y) * stride + left + x] = RasterImage.ToByte(sum / 4 + 128);
			}
	}

	private static double[,] CreateCosines()
	{
		var table = new double[8, 8];
		for (var x = 0; x < 8; x++)
			for (var u = 0; u < 8; u++)
				table[x, u] = Math.Cos((2 * x + 1) * u * Math.PI / 16);
		return table;
	}

	private static bool NextSegment(ReadOnlySpan<byte> data, ref int cursor, out int marker, out ReadOnlySpan<byte> segment)
	{
		segment = default;
		while (cursor < data.Length && data[cursor] != 0xFF)
			cursor++;
		if (cursor == data.Length) { marker = 0; return false; }
		while (cursor < data.Length && data[cursor] == 0xFF)
			cursor++;
		if (cursor == data.Length) { marker = 0; return false; }

		marker = data[cursor++];
		if (marker == 0xD9) return false;
		if (marker is >= 0xD0 and <= 0xD7 or 0x01 or 0xD8)
			return true;

		if (cursor + 2 > data.Length)
			throw new InvalidDataException("Truncated JPEG segment length.");

		var size = U16(data, cursor);
		if (size < 2 || size > data.Length - cursor)
			throw new InvalidDataException("Invalid JPEG segment length.");

		segment = data.Slice(cursor + 2, size - 2);
		cursor += size;
		return true;
	}

	private static int U16(ReadOnlySpan<byte> data, int index)
		=> data[index] << 8 | data[index + 1];

	private static void CheckSize(int width, int height)
	{
		if (width <= 0 || height <= 0 || (long)width * height > 25_000_000)
			throw new InvalidDataException("JPEG exceeds the supported 25-megapixel size limit.");
	}
}
