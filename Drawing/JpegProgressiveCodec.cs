namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Pure C# progressive (SOF2) JPEG decoder: coefficient buffers, successive
/// approximation, EOB runs, per-scan Huffman tables and restart markers.
/// Supports 8-bit grayscale, YCbCr and Adobe CMYK/YCCK.
/// </summary>
internal static class JpegProgressiveCodec
{
	private static readonly byte[] _zigzag =
	[
		0,1,8,16,9,2,3,10,17,24,32,25,18,11,4,5,
		12,19,26,33,40,48,41,34,27,20,13,6,7,14,21,
		28,35,42,49,56,57,50,43,36,29,22,15,23,30,37,
		44,51,58,59,52,45,38,31,39,46,53,60,61,54,47,55,
		62,63
	];

	private static readonly double[,] _cos = CreateCosines();
	private static readonly double[] _normal = [1 / Math.Sqrt(2),1,1,1,1,1,1,1];

	private sealed class Component
	{
		public int Id, H, V, QuantIndex, DcIndex, AcIndex;
		public int BlocksWide, BlocksHigh, Prediction;
		public int[] Coefficients;
		public byte[] Samples;
	}

	private sealed class Huffman
	{
		private readonly Dictionary<int, int> _codes = new();

		public Huffman(ReadOnlySpan<byte> counts, ReadOnlySpan<byte> values)
		{
			var next = 0;
			var code = 0;
			for (var length = 1; length <= 16; length++)
			{
				for (var i = 0; i < counts[length - 1]; i++)
					_codes[(length << 16) | code++] = values[next++];

				if (code > (1 << length))
					throw new InvalidDataException("JPEG Huffman table is over-subscribed.");

				code <<= 1;
			}
		}

		public int Decode(Bits bits)
		{
			var code = 0;
			for (var length = 1; length <= 16; length++)
			{
				code = code << 1 | bits.ReadBit();
				if (_codes.TryGetValue((length << 16) | code, out var value))
					return value;
			}
			throw new InvalidDataException("JPEG Huffman symbol not found.");
		}
	}

	private sealed class Bits
	{
		private readonly byte[] _input;
		private int _position, _bitsRemaining, _current;

		public int Position => _position;

		public Bits(byte[] input, int start)
		{
			_input = input;
			_position = start;
		}

		public int ReadBit()
		{
			if (_bitsRemaining == 0)
			{
				if (_position >= _input.Length)
					throw new InvalidDataException("Truncated progressive JPEG scan.");
				_current = _input[_position++];
				if (_current == 0xFF)
				{
					if (_position >= _input.Length || _input[_position++] != 0)
						throw new InvalidDataException("Unexpected marker within JPEG scan.");
				}
				_bitsRemaining = 8;
			}

			return _current >> --_bitsRemaining & 1;
		}

		public int Read(int bits)
		{
			if (bits < 0 || bits > 16)
				throw new InvalidDataException("Invalid JPEG bit width.");
			var output = 0;
			for (var i = 0; i < bits; i++)
				output = output << 1 | ReadBit();
			return output;
		}

		public int Signed(int size)
		{
			if (size == 0) return 0;
			var value = Read(size);
			return value < (1 << (size - 1)) ? value - ((1 << size) - 1) : value;
		}

		public void Restart(int expected)
		{
			_bitsRemaining = 0;
			if (_position >= _input.Length || _input[_position++] != 0xFF)
				throw new InvalidDataException("Missing JPEG restart marker.");

			while (_position < _input.Length && _input[_position] == 0xFF)
				_position++;

			if (_position >= _input.Length || _input[_position++] != 0xD0 + (expected & 7))
				throw new InvalidDataException("Incorrect JPEG restart marker.");
		}
	}

	public static bool IsProgressive(ReadOnlySpan<byte> data)
	{
		if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
			return false;
		var position = 2;
		while (position + 4 <= data.Length)
		{
			var marker = NextMarker(data, ref position);
			if (marker == 0xC2) return true;
			if (marker is 0xC0 or 0xC1 or 0xDA or 0xD9) return false;
			if (marker is >= 0xD0 and <= 0xD7 or 0x01 or 0xD8) continue;
			if (position + 2 > data.Length) return false;
			var size = U16(data, position);
			if (size < 2 || size > data.Length - position) return false;
			position += size;
		}
		return false;
	}

	public static RasterImage Decode(byte[] data)
	{
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("JPEG input is too large.");

		var qtables = new int[4][];
		var dc = new Huffman[4];
		var ac = new Huffman[4];
		var components = Array.Empty<Component>();
		var width = 0;
		var height = 0;
		var maxH = 0;
		var maxV = 0;
		var restartInterval = 0;
		var adobeTransform = -1;
		var position = 2;
		var gotSof = false;
		var scans = 0;
		var reachedEnd = false;

		while (position < data.Length)
		{
			var marker = NextMarker(data, ref position);
			if (marker == 0xD9) { reachedEnd = true; break; }

			if (marker is >= 0xD0 and <= 0xD7 or 0xD8 or 0x01)
				throw new InvalidDataException("Unexpected stand-alone JPEG marker.");

			if (position + 2 > data.Length)
				throw new InvalidDataException("Truncated JPEG segment.");
			var length = U16(data, position);
			if (length < 2 || length > data.Length - position)
				throw new InvalidDataException("Malformed JPEG segment.");

			var payload = data.AsSpan(position + 2, length - 2);
			position += length;

			switch (marker)
			{
				case 0xEE:
					if (payload.Length >= 12 && payload[..5].SequenceEqual("Adobe"u8))
						adobeTransform = payload[11];
					break;

				case 0xDB:
					for (var i = 0; i < payload.Length;)
					{
						var tag = payload[i++];
						var precision = tag >> 4;
						var id = tag & 15;
						if (id >= 4 || precision > 1 || payload.Length - i < 64 * (precision + 1))
							throw new InvalidDataException("Malformed JPEG quantization table.");

						var table = new int[64];
						for (var k = 0; k < 64; k++)
						{
							var value = precision == 0 ? payload[i++] : U16(payload, i);
							if (precision != 0) i += 2;
							if (value == 0)
								throw new InvalidDataException("Invalid zero JPEG quantizer.");
							table[_zigzag[k]] = value;
						}
						qtables[id] = table;
					}
					break;

				case 0xC4:
					for (var i = 0; i < payload.Length;)
					{
						if (payload.Length - i < 17)
							throw new InvalidDataException("Truncated JPEG Huffman table.");
						var tag = payload[i++];
						var kind = tag >> 4;
						var index = tag & 15;
						if (kind > 1 || index >= 4)
							throw new InvalidDataException("Malformed JPEG Huffman selector.");

						var counts = payload.Slice(i, 16);
						i += 16;
						var symbolCount = 0;
						foreach (var c in counts) symbolCount += c;
						if (symbolCount > 256 || symbolCount > payload.Length - i)
							throw new InvalidDataException("Truncated JPEG Huffman codes.");

						var table = new Huffman(counts, payload.Slice(i, symbolCount));
						i += symbolCount;
						if (kind == 0) dc[index] = table;
						else ac[index] = table;
					}
					break;

				case 0xDD:
					if (payload.Length != 2)
						throw new InvalidDataException("Malformed JPEG restart interval.");
					restartInterval = U16(payload, 0);
					break;

				case 0xC2:
					if (gotSof || payload.Length < 6 || payload[0] != 8)
						throw new NotSupportedException("Only progressive 8-bit JPEG is supported.");

					gotSof = true;
					height = U16(payload, 1);
					width = U16(payload, 3);
					if (width < 1 || height < 1 || (long)width * height > 25_000_000)
						throw new InvalidDataException("Progressive JPEG size exceeds 25 megapixels.");

					var count = payload[5];
					if (count is not (1 or 3 or 4) || payload.Length != 6 + count * 3)
						throw new NotSupportedException("Only grayscale, YCbCr and CMYK JPEG are supported.");

					components = new Component[count];
					for (var index = 0; index < count; index++)
					{
						var offset = 6 + index * 3;
						var h = payload[offset + 1] >> 4;
						var v = payload[offset + 1] & 15;
						var q = payload[offset + 2];
						if (h is < 1 or > 4 || v is < 1 or > 4 || q >= 4)
							throw new InvalidDataException("Invalid JPEG sampling factors.");
						components[index] = new Component { Id = payload[offset], H = h, V = v, QuantIndex = q };
						maxH = Math.Max(h, maxH);
						maxV = Math.Max(v, maxV);
					}

					var mcuW = (width + maxH * 8 - 1) / (maxH * 8);
					var mcuH = (height + maxV * 8 - 1) / (maxV * 8);
					long totalCoefficients = 0;
					foreach (var component in components)
					{
						component.BlocksWide = checked(mcuW * component.H);
						component.BlocksHigh = checked(mcuH * component.V);
						totalCoefficients += (long)component.BlocksWide * component.BlocksHigh * 64;
					}
					if (totalCoefficients > 40_000_000)
						throw new InvalidDataException("JPEG progressive coefficient storage limit exceeded.");
					foreach (var component in components)
						component.Coefficients = new int[checked(component.BlocksWide * component.BlocksHigh * 64)];
					break;

				case 0xDA:
					if (!gotSof || payload.Length < 6)
						throw new InvalidDataException("JPEG scan precedes progressive frame.");

					var scanCount = payload[0];
					if (scanCount < 1 || scanCount > components.Length || payload.Length != 1 + scanCount * 2 + 3)
						throw new InvalidDataException("Invalid progressive JPEG scan.");

					var scan = new Component[scanCount];
					for (var i = 0; i < scanCount; i++)
					{
						var id = payload[1 + 2 * i];
						Component matched = null;
						foreach (var c in components)
							if (c.Id == id) { matched = c; break; }
						if (matched == null || Array.IndexOf(scan, matched) >= 0)
							throw new InvalidDataException("Repeated or unknown JPEG scan component.");
						var selectors = payload[2 + 2 * i];
						matched.DcIndex = selectors >> 4;
						matched.AcIndex = selectors & 15;
						if (matched.DcIndex >= 4 || matched.AcIndex >= 4)
							throw new InvalidDataException("JPEG Huffman selector exceeds table count.");
						scan[i] = matched;
					}

					var ss = payload[1 + scanCount * 2];
					var se = payload[2 + scanCount * 2];
					var ah = payload[3 + scanCount * 2] >> 4;
					var al = payload[3 + scanCount * 2] & 15;

					if (ss > se || se >= 64 || ah > 13 || al > 13 ||
						(ss == 0 && se != 0) || (ss != 0 && scanCount != 1) ||
						(ah != 0 && ah != al + 1))
						throw new InvalidDataException("Invalid JPEG progressive scan parameters.");

					foreach (var c in scan)
						if ((ss == 0 && ah == 0 && dc[c.DcIndex] == null) ||
							(ss != 0 && ac[c.AcIndex] == null) ||
							qtables[c.QuantIndex] == null)
							throw new InvalidDataException("Progressive JPEG requires missing tables.");

					var bits = new Bits(data, position);
					ReadScan(bits, components, scan, dc, ac, ss, se, ah, al,
						restartInterval, width, height, maxH, maxV);
					position = bits.Position;
					scans++;
					if (scans > 256)
						throw new InvalidDataException("Too many progressive JPEG scans.");
					break;

				default:
					if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
						throw new NotSupportedException("Unexpected second JPEG frame.");
					break;
			}
		}

		if (!reachedEnd || scans == 0)
			throw new InvalidDataException("JPEG scan data or end marker is missing.");

		foreach (var c in components)
		{
			var q = qtables[c.QuantIndex] ?? throw new InvalidDataException("Missing JPEG quantization table.");
			c.Samples = new byte[checked(c.BlocksWide * c.BlocksHigh * 64)];
			var block = new int[64];
			for (var by = 0; by < c.BlocksHigh; by++)
				for (var bx = 0; bx < c.BlocksWide; bx++)
				{
					var offset = (by * c.BlocksWide + bx) * 64;
					for (var i = 0; i < 64; i++)
						block[i] = checked(c.Coefficients[offset + i] * q[i]);
					InverseDct(block, c.Samples, c.BlocksWide * 8, bx * 8, by * 8);
				}
		}

		var result = new RasterImage(width, height);
		for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var offset = (y * width + x) * 4;
				var a = Sample(components[0], x, y, maxH, maxV);
				if (components.Length == 1)
				{
					result.Pixels[offset] = result.Pixels[offset + 1] = result.Pixels[offset + 2] = a;
				}
				else if (components.Length == 3)
				{
					var cb = Sample(components[1], x, y, maxH, maxV) - 128;
					var cr = Sample(components[2], x, y, maxH, maxV) - 128;
					result.Pixels[offset] = RasterImage.ToByte(a + 1.402 * cr);
					result.Pixels[offset + 1] = RasterImage.ToByte(a - 0.344136 * cb - 0.714136 * cr);
					result.Pixels[offset + 2] = RasterImage.ToByte(a + 1.772 * cb);
				}
				else
				{
					var b = Sample(components[1], x, y, maxH, maxV);
					var c = Sample(components[2], x, y, maxH, maxV);
					var k = Sample(components[3], x, y, maxH, maxV);
					if (adobeTransform == 2)
					{
						var cb = b - 128;
						var cr = c - 128;
						result.Pixels[offset] = RasterImage.ToByte((255 - RasterImage.ToByte(a + 1.402 * cr)) * k / 255.0);
						result.Pixels[offset + 1] = RasterImage.ToByte((255 - RasterImage.ToByte(a - 0.344136 * cb - 0.714136 * cr)) * k / 255.0);
						result.Pixels[offset + 2] = RasterImage.ToByte((255 - RasterImage.ToByte(a + 1.772 * cb)) * k / 255.0);
					}
					else if (adobeTransform == 0)
					{
						result.Pixels[offset] = RasterImage.ToByte(a * k / 255.0);
						result.Pixels[offset + 1] = RasterImage.ToByte(b * k / 255.0);
						result.Pixels[offset + 2] = RasterImage.ToByte(c * k / 255.0);
					}
					else if (adobeTransform == -1)
					{
						result.Pixels[offset] = RasterImage.ToByte((255 - a) * (255 - k) / 255.0);
						result.Pixels[offset + 1] = RasterImage.ToByte((255 - b) * (255 - k) / 255.0);
						result.Pixels[offset + 2] = RasterImage.ToByte((255 - c) * (255 - k) / 255.0);
					}
					else
						throw new NotSupportedException("Unsupported Adobe JPEG color transform.");
				}
				result.Pixels[offset + 3] = 255;
			}
		return result;
	}

	private static void ReadScan(
		Bits bits, Component[] components, Component[] scan,
		Huffman[] dc, Huffman[] ac,
		int ss, int se, int ah, int al, int restartInterval,
		int width, int height, int maxH, int maxV)
	{
		var interleaved = scan.Length > 1;
		var unitsX = interleaved
			? (width + 8 * maxH - 1) / (8 * maxH)
			: (width * scan[0].H + 8 * maxH - 1) / (8 * maxH);
		var unitsY = interleaved
			? (height + 8 * maxV - 1) / (8 * maxV)
			: (height * scan[0].V + 8 * maxV - 1) / (8 * maxV);

		var eobRun = 0;
		var unitIndex = 0;
		foreach (var c in scan) c.Prediction = 0;

		for (var by = 0; by < unitsY; by++)
			for (var bx = 0; bx < unitsX; bx++)
			{
				if (restartInterval > 0 && unitIndex > 0 && unitIndex % restartInterval == 0)
				{
					bits.Restart(unitIndex / restartInterval - 1);
					foreach (var c in scan) c.Prediction = 0;
					eobRun = 0;
				}

				foreach (var c in scan)
				{
					var blocksH = interleaved ? c.H : 1;
					var blocksV = interleaved ? c.V : 1;
					for (var subY = 0; subY < blocksV; subY++)
						for (var subX = 0; subX < blocksH; subX++)
						{
							var x = interleaved ? bx * c.H + subX : bx;
							var y = interleaved ? by * c.V + subY : by;
							var offset = (y * c.BlocksWide + x) * 64;
							var coeff = c.Coefficients;

							if (ss == 0)
							{
								if (ah == 0)
								{
									var size = dc[c.DcIndex].Decode(bits);
									if (size > 11)
										throw new InvalidDataException("Invalid progressive JPEG DC magnitude.");
									c.Prediction += bits.Signed(size);
									coeff[offset] = c.Prediction << al;
								}
								else if (bits.ReadBit() == 1)
									coeff[offset] |= 1 << al;
							}
							else if (ah == 0)
								AcFirst(bits, ac[c.AcIndex], coeff, offset, ss, se, al, ref eobRun);
							else
								AcRefine(bits, ac[c.AcIndex], coeff, offset, ss, se, al, ref eobRun);
						}
				}
				unitIndex++;
			}
	}

	private static void AcFirst(Bits bits, Huffman table, int[] coeff,
		int offset, int ss, int se, int al, ref int eobRun)
	{
		if (eobRun != 0) { eobRun--; return; }

		for (var k = ss; k <= se;)
		{
			var symbol = table.Decode(bits);
			var run = symbol >> 4;
			var size = symbol & 15;
			if (size == 0)
			{
				if (run == 15) { k += 16; continue; }
				eobRun = (1 << run) + bits.Read(run) - 1;
				return;
			}

			if (size > 10 || (k += run) > se)
				throw new InvalidDataException("Malformed progressive JPEG AC coefficient.");

			coeff[offset + _zigzag[k++]] = bits.Signed(size) << al;
		}
	}

	private static void AcRefine(Bits bits, Huffman table, int[] coeff,
		int offset, int ss, int se, int al, ref int eobRun)
	{
		var bitValue = 1 << al;
		var k = ss;

		if (eobRun == 0)
		{
			for (; k <= se;)
			{
				var symbol = table.Decode(bits);
				var zeros = symbol >> 4;
				var size = symbol & 15;
				int newlyNonzero = 0;
				if (size == 1)
					newlyNonzero = bits.ReadBit() == 1 ? bitValue : -bitValue;
				else if (size == 0)
				{
					if (zeros != 15)
					{
						eobRun = (1 << zeros) + bits.Read(zeros);
						break;
					}
					zeros = 16;
				}
				else
					throw new InvalidDataException("Progressive JPEG AC refinement size must be 0 or 1.");

				for (; k <= se; k++)
				{
					var index = offset + _zigzag[k];
					if (coeff[index] != 0)
					{
						var correction = bits.ReadBit();
						if (correction == 1 && (Math.Abs(coeff[index]) & bitValue) == 0)
							coeff[index] += coeff[index] > 0 ? bitValue : -bitValue;
					}
					else
					{
						if (zeros == 0) break;
						zeros--;
					}
				}

				if (newlyNonzero != 0)
				{
					if (k > se)
						throw new InvalidDataException("Progressive JPEG AC refinement overflow.");
					coeff[offset + _zigzag[k++]] = newlyNonzero;
				}
			}
		}

		if (eobRun > 0)
		{
			for (; k <= se; k++)
			{
				var index = offset + _zigzag[k];
				if (coeff[index] != 0 && bits.ReadBit() == 1 &&
					(Math.Abs(coeff[index]) & bitValue) == 0)
					coeff[index] += coeff[index] > 0 ? bitValue : -bitValue;
			}
			eobRun--;
		}
	}

	private static byte Sample(Component c, int x, int y, int maxH, int maxV)
		=> c.Samples[(y * c.V / maxV) * c.BlocksWide * 8 + (x * c.H / maxH)];

	private static void InverseDct(int[] coeff, byte[] samples, int stride, int x, int y)
	{
		for (var iy = 0; iy < 8; iy++)
			for (var ix = 0; ix < 8; ix++)
			{
				double total = 0;
				for (var v = 0; v < 8; v++)
					for (var u = 0; u < 8; u++)
						total += _normal[u] * _normal[v] * coeff[v * 8 + u] *
							_cos[ix, u] * _cos[iy, v];

				samples[(y + iy) * stride + x + ix] = RasterImage.ToByte(total / 4 + 128);
			}
	}

	private static double[,] CreateCosines()
	{
		var result = new double[8, 8];
		for (var x = 0; x < 8; x++)
			for (var u = 0; u < 8; u++)
				result[x, u] = Math.Cos((2 * x + 1) * u * Math.PI / 16);
		return result;
	}

	private static int NextMarker(ReadOnlySpan<byte> data, ref int position)
	{
		if (position >= data.Length || data[position++] != 0xFF)
			throw new InvalidDataException("JPEG marker not found at expected offset.");

		while (position < data.Length && data[position] == 0xFF)
			position++;

		if (position >= data.Length)
			throw new InvalidDataException("Truncated JPEG marker.");

		var marker = data[position++];
		if (marker == 0)
			throw new InvalidDataException("Unexpected stuffed JPEG byte outside scan.");
		return marker;
	}

	private static int U16(ReadOnlySpan<byte> data, int position)
		=> (data[position] << 8) | data[position + 1];
}
