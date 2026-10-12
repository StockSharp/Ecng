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
		=> Decode(data, ImageProcessingLimits.Default);

	public static RasterImage Decode(byte[] data, ImageProcessingLimits limits)
		=> DecodeCore(data, 0, 0, limits);

	public static RasterImage DecodeDownscaled(byte[] data, int width, int height, ImageProcessingLimits limits)
		=> DecodeCore(data, width, height, limits);

	private static RasterImage DecodeCore(byte[] data, int outputWidth, int outputHeight, ImageProcessingLimits limits)
	{
		Av1TileComposer.CompositedFrame color = null, alpha = null;
		try
		{
			var container = ReadContainer(data);
			if (outputWidth > 0) return AvifThumbnailDecoder.Decode(container, outputWidth, outputHeight, limits);
			RasterImage.EnsureDecodable(container.Width, container.Height, limits);
			color = Av1TileComposer.Composite(container.ColorTiles, container.GridRows, container.GridColumns, container.Width, container.Height, limits);
			if (container.HasAlpha)
			{
				long colorBytes = 0, alphaBytes = 0, alphaTemporary = 0;
				foreach (var plane in color.Planes) if (plane is not null) colorBytes += (long)plane.Length * 4;
				foreach (var tile in container.AlphaTiles)
				{
					var estimate = Av1FrameDecoder.EstimateBuffers(tile);
					alphaBytes += estimate.planes;
					alphaTemporary = Math.Max(alphaTemporary, estimate.working - estimate.planes);
				}
				limits.EnsureWorkingMemory(colorBytes + alphaBytes + alphaTemporary);
				alpha = Av1TileComposer.Composite(container.AlphaTiles, container.GridRows, container.GridColumns, container.Width, container.Height, limits);
			}

			var sequence = color.Sequence;
			var format = container.HasAlpha
				? sequence.BitDepth > 8 ? CodecPixelFormats.Rgba64 : CodecPixelFormats.Rgba32
				: sequence.MonoChrome
					? sequence.BitDepth > 8 ? CodecPixelFormats.Gray16 : CodecPixelFormats.Gray8
					: sequence.BitDepth > 8 ? CodecPixelFormats.Rgb48 : CodecPixelFormats.Rgb24;
			var pixels = Av1YuvToRgbConverter.Convert(color.Planes, color.Widths, sequence.MonoChrome,
				sequence.SubsamplingX, sequence.SubsamplingY, sequence.MatrixCoefficients, sequence.ColorRange,
				sequence.BitDepth, alpha?.Planes[0], alpha?.Widths[0] ?? 0, container.Width, container.Height);
			using var image = CodecImage.FromBuffer(container.Width, container.Height, format, pixels, false, limits);
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
