namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>Huffman sequential JPEG, including separate component scans and reduced inverse DCT.</summary>
internal static class JpegSequentialCodec
{
	public static RasterImage Decode(byte[] data, int scale)
	{
		if (data.Length > 128 * 1024 * 1024 || !JpegCodec.IsJpeg(data))
			throw new InvalidDataException("Invalid or oversized JPEG input.");

		var cursor = 2;
		var quant = new int[4][];
		var dc = new JpegHuffmanTable[4];
		var ac = new JpegHuffmanTable[4];
		JpegComponent[] components = null;
		var width = 0;
		var height = 0;
		var restartInterval = 0;
		var adobeTransform = -1;
		var hasJfif = false;
		var scans = 0;

		while (JpegCodec.NextSegment(data, ref cursor, out var marker, out var segment))
		{
			switch (marker)
			{
				case 0xD9:
					if (components is null || scans == 0)
						throw new InvalidDataException("JPEG has no scan.");
					foreach (var c in components)
						if (!c.IsDecoded)
							throw new InvalidDataException("JPEG component has no scan.");
					return JpegTransforms.ToImage((width + scale - 1) / scale, (height + scale - 1) / scale,
						components, JpegTransforms.ResolveColorModel(components, hasJfif, adobeTransform));

				case JpegSegments.Jfif:
					hasJfif |= JpegSegments.IsJfif(segment);
					break;
				case JpegSegments.Adobe:
					if (JpegSegments.TryReadAdobeTransform(segment, out var transform))
						adobeTransform = transform;
					break;
				case 0xDB:
					JpegSegments.ReadQuantizationTables(segment, quant);
					break;
				case 0xC4:
					JpegSegments.ReadHuffmanTables(segment, dc, ac);
					break;
				case 0xDD:
					if (segment.Length != 2)
						throw new InvalidDataException("Invalid restart interval.");
					restartInterval = BinaryPrimitives.ReadUInt16BigEndian(segment);
					break;
				case 0xC0:
				case 0xC1:
					if (components is not null)
						throw new InvalidDataException("JPEG has more than one frame.");
					components = JpegSegments.ReadFrame(segment, out width, out height, scale);
					JpegSegments.EnsureEnoughData(width, height, data.Length - cursor);
					foreach (var c in components)
						c.Samples = new byte[checked(c.Stride * c.BlocksHigh * c.BlockSize)];
					break;
				case 0xDA:
					if (components is null || segment.Length < 6)
						throw new InvalidDataException("JPEG scan precedes frame.");
					var count = segment[0];
					if (count < 1 || count > components.Length || segment.Length != 1 + count * 2 + 3 ||
						segment[^3] != 0 || segment[^2] != 63 || segment[^1] != 0)
						throw new InvalidDataException("Invalid sequential JPEG scan.");

					var scan = new JpegComponent[count];
					var previous = -1;
					for (var i = 0; i < count; i++)
					{
						var id = segment[1 + i * 2];
						var index = Array.FindIndex(components, c => c.Id == id);
						if (index <= previous || components[index].IsDecoded)
							throw new InvalidDataException("Repeated, unknown or out-of-order JPEG scan component.");
						previous = index;
						var c = scan[i] = components[index];
						c.DcTable = segment[2 + i * 2] >> 4;
						c.AcTable = segment[2 + i * 2] & 15;
						if (c.DcTable > 3 || c.AcTable > 3 ||
							quant[c.QuantTable] is null || dc[c.DcTable] is null || ac[c.AcTable] is null)
							throw new InvalidDataException("Missing or invalid JPEG scan table.");
					}

					var reader = new JpegBitReader(data, cursor);
					ReadScan(reader, width, height, components, scan, quant, dc, ac, restartInterval);
					cursor = reader.Position;
					if (cursor >= data.Length || data[cursor] != 0xFF)
						throw new InvalidDataException("JPEG scan is incomplete or its closing marker is missing.");
					foreach (var c in scan)
						c.IsDecoded = true;
					scans++;
					break;
				default:
					if (marker is >= 0xC2 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
						throw new NotSupportedException("Unsupported JPEG frame coding.");
					break;
			}
		}
		throw new InvalidDataException("JPEG end-of-image marker is missing.");
	}

	private static void ReadScan(JpegBitReader reader, int width, int height,
		JpegComponent[] components, JpegComponent[] scan, int[][] quant,
		JpegHuffmanTable[] dc, JpegHuffmanTable[] ac, int restartInterval)
	{
		var maxH = 1;
		var maxV = 1;
		foreach (var c in components)
		{
			maxH = Math.Max(maxH, c.H);
			maxV = Math.Max(maxV, c.V);
		}
		var interleaved = scan.Length > 1;
		var unitsX = interleaved ? (width + maxH * 8 - 1) / (maxH * 8) : (width * scan[0].H + maxH * 8 - 1) / (maxH * 8);
		var unitsY = interleaved ? (height + maxV * 8 - 1) / (maxV * 8) : (height * scan[0].V + maxV * 8 - 1) / (maxV * 8);
		var unit = 0;
		Span<int> block = stackalloc int[64];
		foreach (var c in scan)
			c.Prediction = 0;

		for (var my = 0; my < unitsY; my++)
			for (var mx = 0; mx < unitsX; mx++, unit++)
			{
				if (restartInterval > 0 && unit != 0 && unit % restartInterval == 0)
				{
					reader.Restart(unit / restartInterval - 1);
					foreach (var c in scan)
						c.Prediction = 0;
				}
				foreach (var c in scan)
				{
					var table = quant[c.QuantTable];
					for (var cy = 0; cy < (interleaved ? c.V : 1); cy++)
						for (var cx = 0; cx < (interleaved ? c.H : 1); cx++)
						{
							block.Clear();
							var dcLength = dc[c.DcTable].Decode(reader);
							if (dcLength > c.Precision + 3)
								throw new InvalidDataException("Invalid JPEG DC magnitude.");
							c.Prediction += reader.ReadSigned(dcLength);
							var magnitude = 1 << (c.Precision + 2);
							if (c.Prediction < -magnitude || c.Prediction >= magnitude)
								throw new InvalidDataException("JPEG DC coefficient is out of range.");
							block[0] = c.Prediction * table[0];
							for (var at = 1; at < 64;)
							{
								var symbol = ac[c.AcTable].Decode(reader);
								if (symbol == 0)
									break;
								if (symbol == 0xF0)
								{
									at += 16;
									if (at > 64)
										throw new InvalidDataException("JPEG zero run exceeds block.");
									continue;
								}
								var size = symbol & 15;
								if (size == 0 || size > c.Precision + 2 || (at += symbol >> 4) >= 64)
									throw new InvalidDataException("Invalid JPEG AC symbol.");
								var position = JpegTransforms.ZigZag[at++];
								block[position] = reader.ReadSigned(size) * table[position];
							}
							var x = interleaved ? mx * c.H + cx : mx;
							var y = interleaved ? my * c.V + cy : my;
							JpegTransforms.InverseDct(block, c.Samples, c.Stride, x * c.BlockSize, y * c.BlockSize, c.Scale, c.Precision);
						}
				}
			}
	}
}
