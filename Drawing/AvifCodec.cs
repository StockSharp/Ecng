namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;

using Ecng.Drawing.Codecs;
using Ecng.Drawing.Codecs.Formats.Avif;
using Ecng.Drawing.Codecs.Formats.Avif.Container;
using Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

internal static class AvifCodec
{
	public static bool IsAvif(ReadOnlySpan<byte> data)
	{
		if (data.Length < 16 || !data.Slice(4, 4).SequenceEqual("ftyp"u8))
			return false;
		var length = BinaryPrimitives.ReadUInt32BigEndian(data);
		if (length < 16 || length > data.Length)
			return false;
		for (var offset = 8; offset <= length - 4; offset += 4)
		{
			if (offset != 12 && (data.Slice(offset, 4).SequenceEqual("avif"u8) || data.Slice(offset, 4).SequenceEqual("avis"u8)))
				return true;
		}
		return false;
	}

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> data)
	{
		var container = ReadContainer(data.ToArray());
		return (int)container.Orientation >= 5 ? (container.Height, container.Width) : (container.Width, container.Height);
	}

	public static RasterImage Decode(byte[] data)
	{
		Av1TileComposer.CompositedFrame color = null, alpha = null;
		try
		{
			var container = ReadContainer(data);
			RasterImage.EnsureDecodable(container.Width, container.Height);
			color = Av1TileComposer.Composite(container.ColorTiles, container.GridRows, container.GridColumns, container.Width, container.Height);
			if (container.HasAlpha)
				alpha = Av1TileComposer.Composite(container.AlphaTiles, container.GridRows, container.GridColumns, container.Width, container.Height);

			var sequence = color.Sequence;
			var pixels = Av1YuvToRgbConverter.Convert(color.Planes, color.Widths, sequence.MonoChrome,
				sequence.SubsamplingX, sequence.SubsamplingY, sequence.MatrixCoefficients, sequence.ColorRange,
				sequence.BitDepth, alpha?.Planes[0], alpha?.Widths[0] ?? 0, container.Width, container.Height);
			var format = container.HasAlpha
				? sequence.BitDepth > 8 ? CodecPixelFormats.Rgba64 : CodecPixelFormats.Rgba32
				: sequence.MonoChrome
					? sequence.BitDepth > 8 ? CodecPixelFormats.Gray16 : CodecPixelFormats.Gray8
					: sequence.BitDepth > 8 ? CodecPixelFormats.Rgb48 : CodecPixelFormats.Rgb24;
			using var image = CodecImage.FromBuffer(container.Width, container.Height, format, pixels, false);
			return image.ToRaster().Orient((int)container.Orientation);
		}
		catch (AvifUnsupportedFeatureException error)
		{
			throw new NotSupportedException(error.Message, error);
		}
		catch (Exception error) when (error is ImageFormatException or ArgumentException or IndexOutOfRangeException or OverflowException)
		{
			throw new InvalidDataException("Invalid AVIF image.", error);
		}
		finally
		{
			if (alpha is not null) Av1TileComposer.ReturnPlanes(alpha);
			if (color is not null) Av1TileComposer.ReturnPlanes(color);
		}
	}

	private static AvifContainerInfo ReadContainer(byte[] data)
	{
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("AVIF exceeds the supported file size.");
		try
		{
			using var stream = new MemoryStream(data, false);
			return AvifContainerReader.Read(stream, new ImageMetadata());
		}
		catch (AvifUnsupportedFeatureException error)
		{
			throw new NotSupportedException(error.Message, error);
		}
		catch (ImageFormatException error)
		{
			throw new InvalidDataException("Invalid AVIF container.", error);
		}
	}
}
