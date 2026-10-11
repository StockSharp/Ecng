namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>
/// Pure managed decoder for 8-bit JPEG. Baseline grayscale/YCbCr/RGB/CMYK and restart markers
/// are decoded here; SOF2 progressive scans delegate to JpegProgressiveCodec.
/// Unsupported arithmetic-coded, lossless and 12-bit JPEG fail explicitly.
/// </summary>
internal static class JpegCodec
{
	private const int _maxInputLength = 128 * 1024 * 1024;

	public static bool IsJpeg(ReadOnlySpan<byte> data)
		=> data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff;

	/// <summary>
	/// Reads the size the picture is shown with: the one in the frame header, turned if the Exif orientation says so.
	/// </summary>
	public static (int width, int height) ReadSize(ReadOnlySpan<byte> data)
	{
		var cursor = 2;
		if (!IsJpeg(data)) throw new InvalidDataException("Invalid JPEG signature.");

		while (NextSegment(data, ref cursor, out var marker, out var segment))
		{
			if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
			{
				if (segment.Length < 6) throw new InvalidDataException("Truncated JPEG frame.");
				var h = BinaryPrimitives.ReadUInt16BigEndian(segment[1..]);
				var w = BinaryPrimitives.ReadUInt16BigEndian(segment[3..]);

				if (w == 0 || h == 0)
					throw new InvalidDataException("JPEG frame has no size.");

				return JpegSegments.ReadOrientation(data) >= 5 ? (h, w) : (w, h);
			}
			if (marker == 0xDA) break;
		}

		throw new InvalidDataException("JPEG has no frame dimensions.");
	}

	/// <summary>
	/// Decodes the picture and turns it the way its Exif orientation asks for it to be shown.
	/// </summary>
	public static RasterImage Decode(byte[] data)
	{
		var image = JpegProgressiveCodec.IsProgressive(data) ? JpegProgressiveCodec.Decode(data) : DecodeBaseline(data);
		return image.Orient(JpegSegments.ReadOrientation(data));
	}

	private static RasterImage DecodeBaseline(byte[] data)
	{
		if (data.Length > _maxInputLength)
			throw new InvalidDataException("JPEG input exceeds the supported size.");

		if (!IsJpeg(data))
			throw new InvalidDataException("Invalid JPEG signature.");

		var cursor = 2;
		var quant = new int[4][];
		var dc = new JpegHuffmanTable[4];
		var ac = new JpegHuffmanTable[4];
		var components = Array.Empty<JpegComponent>();
		var width = 0;
		var height = 0;
		var restartInterval = 0;
		var adobeTransform = -1;
		var hasJfif = false;
		var foundScan = false;

		while (NextSegment(data, ref cursor, out var marker, out var segment))
		{
			switch (marker)
			{
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
					if (segment.Length != 2) throw new InvalidDataException("Invalid restart interval.");
					restartInterval = BinaryPrimitives.ReadUInt16BigEndian(segment);
					break;

				case 0xC0:
					if (components.Length != 0)
						throw new InvalidDataException("JPEG has more than one frame.");

					components = JpegSegments.ReadFrame(segment, out width, out height);
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
						var part = components[i];

						if (part.Id != id)
							throw new InvalidDataException("JPEG scan component order differs from the frame.");

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

		JpegSegments.EnsureEnoughData(width, height, data.Length - cursor);

		foreach (var c in components)
		{
			if (quant[c.QuantTable] == null || dc[c.DcTable] == null || ac[c.AcTable] == null)
				throw new InvalidDataException("Missing JPEG quantization or Huffman table.");
			c.Samples = new byte[checked(c.Stride * c.BlocksHigh * 8)];
		}

		var mcuWidth = components[0].BlocksWide / components[0].H;
		var mcuHeight = components[0].BlocksHigh / components[0].V;
		var reader = new JpegBitReader(data, cursor);
		var mcuNumber = 0;
		Span<int> block = stackalloc int[64];

		for (var my = 0; my < mcuHeight; my++)
			for (var mx = 0; mx < mcuWidth; mx++)
			{
				if (restartInterval > 0 && mcuNumber != 0 && mcuNumber % restartInterval == 0)
				{
					reader.Restart(mcuNumber / restartInterval - 1);
					foreach (var c in components) c.Prediction = 0;
				}

				foreach (var c in components)
				{
					var table = quant[c.QuantTable];

					for (var cy = 0; cy < c.V; cy++)
						for (var cx = 0; cx < c.H; cx++)
						{
							block.Clear();
							var dcLength = dc[c.DcTable].Decode(reader);
							if (dcLength > 11) throw new InvalidDataException("Invalid JPEG DC magnitude.");

							c.Prediction += reader.ReadSigned(dcLength);
							block[0] = c.Prediction * table[0];

							for (var at = 1; at < 64;)
							{
								var symbol = ac[c.AcTable].Decode(reader);
								if (symbol == 0) break;
								if (symbol == 0xF0) { at += 16; continue; }

								var run = symbol >> 4;
								var size = symbol & 15;
								if (size == 0 || size > 10 || (at += run) >= 64)
									throw new InvalidDataException("Invalid JPEG AC symbol.");

								var position = JpegTransforms.ZigZag[at++];
								block[position] = reader.ReadSigned(size) * table[position];
							}

							JpegTransforms.InverseDct(block, c.Samples, c.Stride, (mx * c.H + cx) * 8, (my * c.V + cy) * 8);
						}
				}

				mcuNumber++;
			}

		// We consumed the declared MCU count. The next byte-aligned marker must
		// close the image; accepting a missing EOI would silently treat truncated
		// JPEG files as successfully decoded pictures.
		var trailing = reader.Position;
		if (trailing >= data.Length || data[trailing++] != 0xFF)
			throw new InvalidDataException("JPEG end-of-image marker is missing.");
		while (trailing < data.Length && data[trailing] == 0xFF)
			trailing++;
		if (trailing >= data.Length || data[trailing] != 0xD9)
			throw new InvalidDataException("JPEG scan is incomplete or the EOI marker is missing.");

		return JpegTransforms.ToImage(width, height, components, JpegTransforms.ResolveColorModel(components, hasJfif, adobeTransform));
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

		var size = BinaryPrimitives.ReadUInt16BigEndian(data[cursor..]);
		if (size < 2 || size > data.Length - cursor)
			throw new InvalidDataException("Invalid JPEG segment length.");

		segment = data.Slice(cursor + 2, size - 2);
		cursor += size;
		return true;
	}
}
