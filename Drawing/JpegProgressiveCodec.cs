namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>
/// Pure C# progressive (SOF2) JPEG decoder: coefficient buffers, successive
/// approximation, EOB runs, per-scan Huffman tables and restart markers.
/// Supports 8-bit grayscale, YCbCr, RGB and Adobe CMYK/YCCK.
/// </summary>
internal static class JpegProgressiveCodec
{
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
			var size = BinaryPrimitives.ReadUInt16BigEndian(data[position..]);
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
		var dc = new JpegHuffmanTable[4];
		var ac = new JpegHuffmanTable[4];
		JpegComponent[] components = null;
		var width = 0;
		var height = 0;
		var restartInterval = 0;
		var adobeTransform = -1;
		var hasJfif = false;
		var position = 2;
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
			var length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position));
			if (length < 2 || length > data.Length - position)
				throw new InvalidDataException("Malformed JPEG segment.");

			var payload = data.AsSpan(position + 2, length - 2);
			position += length;

			switch (marker)
			{
				case JpegSegments.Jfif:
					hasJfif |= JpegSegments.IsJfif(payload);
					break;

				case JpegSegments.Adobe:
					if (JpegSegments.TryReadAdobeTransform(payload, out var transform))
						adobeTransform = transform;
					break;

				case 0xDB:
					JpegSegments.ReadQuantizationTables(payload, qtables);
					break;

				case 0xC4:
					JpegSegments.ReadHuffmanTables(payload, dc, ac);
					break;

				case 0xDD:
					if (payload.Length != 2)
						throw new InvalidDataException("Malformed JPEG restart interval.");
					restartInterval = BinaryPrimitives.ReadUInt16BigEndian(payload);
					break;

				case 0xC2:
					if (components != null)
						throw new InvalidDataException("JPEG has more than one frame.");

					components = JpegSegments.ReadFrame(payload, out width, out height);
					JpegSegments.EnsureEnoughData(width, height, data.Length - position);

					foreach (var component in components)
						component.Coefficients = new short[checked(component.BlocksWide * component.BlocksHigh * 64)];
					break;

				case 0xDA:
					if (components == null || payload.Length < 6)
						throw new InvalidDataException("JPEG scan precedes progressive frame.");

					var scanCount = payload[0];
					if (scanCount < 1 || scanCount > components.Length || payload.Length != 1 + scanCount * 2 + 3)
						throw new InvalidDataException("Invalid progressive JPEG scan.");

					var scan = new JpegComponent[scanCount];
					var previousComponent = -1;
					for (var i = 0; i < scanCount; i++)
					{
						var id = payload[1 + 2 * i];
						JpegComponent matched = null;
						foreach (var c in components)
							if (c.Id == id) { matched = c; break; }
						var componentIndex = matched == null ? -1 : Array.IndexOf(components, matched);
						if (componentIndex <= previousComponent)
							throw new InvalidDataException("Repeated, unknown or out-of-order JPEG scan component.");
						previousComponent = componentIndex;
						var selectors = payload[2 + 2 * i];
						matched.DcTable = selectors >> 4;
						matched.AcTable = selectors & 15;
						if (matched.DcTable >= 4 || matched.AcTable >= 4)
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
						if ((ss == 0 && ah == 0 && dc[c.DcTable] == null) ||
							(ss != 0 && ac[c.AcTable] == null) ||
							qtables[c.QuantTable] == null)
							throw new InvalidDataException("Progressive JPEG requires missing tables.");

					var bits = new JpegBitReader(data, position);
					ReadScan(bits, components, scan, dc, ac, ss, se, ah, al, restartInterval, width, height);
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

		Span<int> block = stackalloc int[64];

		foreach (var c in components)
		{
			var q = qtables[c.QuantTable] ?? throw new InvalidDataException("Missing JPEG quantization table.");
			c.Samples = new byte[checked(c.Stride * c.BlocksHigh * 8)];

			for (var by = 0; by < c.BlocksHigh; by++)
				for (var bx = 0; bx < c.BlocksWide; bx++)
				{
					var offset = (by * c.BlocksWide + bx) * 64;
					for (var i = 0; i < 64; i++)
						block[i] = c.Coefficients[offset + i] * q[i];
					JpegTransforms.InverseDct(block, c.Samples, c.Stride, bx * 8, by * 8);
				}
		}

		return JpegTransforms.ToImage(width, height, components, JpegTransforms.ResolveColorModel(components, hasJfif, adobeTransform));
	}

	private static void ReadScan(
		JpegBitReader bits, JpegComponent[] components, JpegComponent[] scan,
		JpegHuffmanTable[] dc, JpegHuffmanTable[] ac,
		int ss, int se, int ah, int al, int restartInterval,
		int width, int height)
	{
		var maxH = 1;
		var maxV = 1;
		foreach (var c in components)
		{
			maxH = Math.Max(maxH, c.H);
			maxV = Math.Max(maxV, c.V);
		}

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
									var size = dc[c.DcTable].Decode(bits);
									if (size > 11)
										throw new InvalidDataException("Invalid progressive JPEG DC magnitude.");
									c.Prediction += bits.ReadSigned(size);
									coeff[offset] = Coefficient((long)c.Prediction << al);
								}
								else if (bits.ReadBit() == 1)
									coeff[offset] |= (short)(1 << al);
							}
							else if (ah == 0)
								AcFirst(bits, ac[c.AcTable], coeff, offset, ss, se, al, ref eobRun);
							else
								AcRefine(bits, ac[c.AcTable], coeff, offset, ss, se, al, ref eobRun);
						}
				}
				unitIndex++;
			}
	}

	private static void AcFirst(JpegBitReader bits, JpegHuffmanTable table, short[] coeff,
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

			coeff[offset + JpegTransforms.ZigZag[k++]] = Coefficient((long)bits.ReadSigned(size) << al);
		}
	}

	private static void AcRefine(JpegBitReader bits, JpegHuffmanTable table, short[] coeff,
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
				}
				else
					throw new InvalidDataException("Progressive JPEG AC refinement size must be 0 or 1.");

				// Coefficients that are already non-zero each take a correction bit; the run counts the zero ones
				// only, and ends at the zero coefficient that follows it.
				for (; k <= se; k++)
				{
					var index = offset + JpegTransforms.ZigZag[k];
					if (coeff[index] != 0)
						Correct(bits, coeff, index, bitValue);
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
					coeff[offset + JpegTransforms.ZigZag[k]] = (short)newlyNonzero;
				}

				// That coefficient is the new one, or - for a run of sixteen zeros, which brings no
				// coefficient - the sixteenth zero: either way the next symbol starts after it.
				k++;
			}
		}

		if (eobRun > 0)
		{
			for (; k <= se; k++)
			{
				var index = offset + JpegTransforms.ZigZag[k];
				if (coeff[index] != 0)
					Correct(bits, coeff, index, bitValue);
			}
			eobRun--;
		}
	}

	// A correction bit of one moves a coefficient away from zero by the bit being refined.
	private static void Correct(JpegBitReader bits, short[] coeff, int index, int bitValue)
	{
		if (bits.ReadBit() == 1 && (Math.Abs(coeff[index]) & bitValue) == 0)
			coeff[index] = Coefficient(coeff[index] + (coeff[index] > 0 ? bitValue : -bitValue));
	}

	// Coefficients of an 8-bit picture fit 16 bits; a stream that makes them larger is not a picture.
	private static short Coefficient(long value)
	{
		if (value is < -short.MaxValue or > short.MaxValue)
			throw new InvalidDataException("JPEG coefficient is out of range.");

		return (short)value;
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
}
