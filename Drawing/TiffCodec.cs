namespace Ecng.Drawing;

using System;
using System.IO;

using Ecng.Drawing.Codecs;
using Ecng.Drawing.Codecs.Formats.Tiff;
using Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

internal static class TiffCodec
{
	public static bool IsTiff(ReadOnlySpan<byte> data)
		=> data.Length >= 4 && (data[0] == 'I' && data[1] == 'I' && data[2] == 42 && data[3] == 0
			|| data[0] == 'M' && data[1] == 'M' && data[2] == 0 && data[3] == 42);

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> data)
	{
		try
		{
			var (ifd, orientation) = ReadDirectory(data.ToArray());
			var width = ifd.RequireUInt32(TiffTags.ImageWidth);
			var height = ifd.RequireUInt32(TiffTags.ImageLength);
			if (width is 0 or > int.MaxValue || height is 0 or > int.MaxValue)
				throw new InvalidDataException("Invalid TIFF dimensions.");
			return orientation >= 5 ? ((int)height, (int)width) : ((int)width, (int)height);
		}
		catch (Exception error) when (error is ImageFormatException or ArgumentException or IndexOutOfRangeException or OverflowException)
		{
			throw new InvalidDataException("Invalid TIFF header.", error);
		}
	}

	public static RasterImage Decode(byte[] data)
	{
		try
		{
			var (_, orientation) = ReadDirectory(data);
			using var stream = new MemoryStream(data, false);
			using var image = TiffImageDecoder.Decode(stream);
			return image.ToRaster().Orient(orientation);
		}
		catch (TiffUnsupportedFeatureException error)
		{
			throw new NotSupportedException(error.Message, error);
		}
		catch (Exception error) when (error is ImageFormatException or ArgumentException or IndexOutOfRangeException or OverflowException)
		{
			throw new InvalidDataException("Invalid TIFF image.", error);
		}
	}

	private static (TiffIfd ifd, int orientation) ReadDirectory(byte[] data)
	{
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("TIFF exceeds the supported file size.");
		var header = TiffHeaderReader.Read(data);
		var directory = TiffIfdReader.Read(new TiffReader(data, header.ByteOrder), header.FirstIfdOffset);
		var orientation = directory.GetUInt32(TiffTags.Orientation, 1);
		if (orientation is < 1 or > 8)
			throw new InvalidDataException("Invalid TIFF orientation.");
		return (directory, (int)orientation);
	}
}
