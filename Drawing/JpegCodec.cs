namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

/// <summary>
/// Managed JPEG decoder dispatch for sequential, progressive, arithmetic and lossless frames.
/// Sequential Huffman frames can be decoded at a reduced DCT scale for thumbnails.
/// </summary>
internal static class JpegCodec
{
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
		=> Decode(data, ImageProcessingLimits.Default);

	public static RasterImage Decode(byte[] data, ImageProcessingLimits limits)
	{
		var image = JpegExtendedCodec.IsExtended(data) ? JpegExtendedCodec.Decode(data, limits) :
			JpegProgressiveCodec.IsProgressive(data) ? JpegProgressiveCodec.Decode(data, limits) : JpegSequentialCodec.Decode(data, 1, limits);
		return image.Orient(JpegSegments.ReadOrientation(data));
	}

	public static RasterImage DecodeDownscaled(byte[] data, int width, int height)
		=> DecodeDownscaled(data, width, height, ImageProcessingLimits.Default);

	public static RasterImage DecodeDownscaled(byte[] data, int width, int height, ImageProcessingLimits limits)
	{
		var orientation = JpegSegments.ReadOrientation(data);
		if (JpegExtendedCodec.IsExtended(data))
			return JpegExtendedCodec.DecodeRows(data, orientation >= 5 ? height : width, orientation >= 5 ? width : height, limits).Orient(orientation);
		if (JpegProgressiveCodec.IsProgressive(data))
			return JpegProgressiveCodec.DecodeRows(data, orientation >= 5 ? height : width, orientation >= 5 ? width : height, limits).Orient(orientation);
		if (JpegSequentialCodec.CanDecodeRows(data))
			return JpegSequentialCodec.DecodeRows(data, orientation >= 5 ? height : width, orientation >= 5 ? width : height, limits).Orient(orientation);
		var (sourceWidth, sourceHeight) = ReadSize(data);
		var scale = 1;
		while (scale < 8 && (sourceWidth + scale * 2 - 1) / (scale * 2) >= width &&
			(sourceHeight + scale * 2 - 1) / (scale * 2) >= height)
			scale *= 2;
		var image = JpegSequentialCodec.Decode(data, scale, limits).Orient(JpegSegments.ReadOrientation(data));
		return image.Width == width && image.Height == height ? image : image.Downscale(width, height);
	}

	public static void ValidateScanHeader(ReadOnlySpan<byte> data)
	{
		var cursor = 2;
		byte[] frame = null;
		while (NextSegment(data, ref cursor, out var marker, out var segment))
		{
			if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
			{
				if (frame is not null || segment.Length < 6 || segment.Length != 6 + segment[5] * 3)
					throw new InvalidDataException("Invalid JPEG frame.");
				frame = segment.ToArray();
			}
			else if (marker == 0xDA)
			{
				if (frame is null || segment.Length < 6 || segment[0] < 1 || segment[0] > frame[5] ||
					segment.Length != 1 + segment[0] * 2 + 3)
					throw new InvalidDataException("Invalid JPEG scan.");
				var previous = -1;
				for (var i = 0; i < segment[0]; i++)
				{
					var index = -1;
					for (var j = 0; j < frame[5]; j++)
						if (frame[6 + j * 3] == segment[1 + i * 2])
							index = j;
					if (index <= previous)
						throw new InvalidDataException("Repeated, unknown or out-of-order JPEG scan component.");
					previous = index;
				}
				return;
			}
			else if (marker == 0xD9)
				break;
		}
		throw new InvalidDataException("JPEG scan is missing.");
	}

	internal static bool NextSegment(ReadOnlySpan<byte> data, ref int cursor, out int marker, out ReadOnlySpan<byte> segment)
	{
		segment = default;
		while (cursor < data.Length && data[cursor] != 0xFF)
			cursor++;
		if (cursor == data.Length) { marker = 0; return false; }
		while (cursor < data.Length && data[cursor] == 0xFF)
			cursor++;
		if (cursor == data.Length) { marker = 0; return false; }

		marker = data[cursor++];
		if (marker == 0xD9) return true;
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
