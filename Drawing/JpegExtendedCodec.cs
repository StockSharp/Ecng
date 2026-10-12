namespace Ecng.Drawing;

using System;
using System.IO;

using Ecng.Drawing.Codecs.Jpeg;

using JpegDecoder = Ecng.Drawing.Codecs.Jpeg.JpegDecoder;

internal static class JpegExtendedCodec
{
	public static bool IsExtended(ReadOnlySpan<byte> data)
	{
		var cursor = 2;
		while (JpegCodec.NextSegment(data, ref cursor, out var marker, out _))
		{
			if (marker is >= 0xc0 and <= 0xcf and not (0xc4 or 0xc8 or 0xcc)) return marker is 0xc3 or 0xc9 or 0xca;
			if (marker == 0xda) break;
		}
		return false;
	}

	public static RasterImage Decode(byte[] data)
		=> Decode(data, ImageProcessingLimits.Default);

	public static RasterImage Decode(byte[] data, ImageProcessingLimits limits)
		=> DecodeCore(data, 0, 0, limits);

	public static RasterImage DecodeRows(byte[] data, int width, int height, ImageProcessingLimits limits)
		=> DecodeCore(data, width, height, limits);

	private static RasterImage DecodeCore(byte[] data, int outputWidth, int outputHeight, ImageProcessingLimits limits)
	{
		if (data.Length > 128 * 1024 * 1024) throw new InvalidDataException("Oversized JPEG input.");
		JpegCodec.ValidateScanHeader(data);
		var cursor = 2;
		JpegComponent[] components = null;
		var hasJfif = false;
		var adobeTransform = -1;
		var pointTransform = 0;
		var isLossless = false;
		while (JpegCodec.NextSegment(data, ref cursor, out var marker, out var segment))
		{
			if (marker == JpegSegments.Jfif) hasJfif |= JpegSegments.IsJfif(segment);
			if (marker == JpegSegments.Adobe && JpegSegments.TryReadAdobeTransform(segment, out var transform)) adobeTransform = transform;
			if (marker is 0xc3 or 0xc9 or 0xca)
			{
				isLossless = marker == 0xc3;
				components = JpegSegments.ReadFrame(segment, out _, out _, 1, isLossless, outputWidth > 0, limits);
			}
			if (marker == 0xda)
			{
				if (isLossless) pointTransform = segment[^1] & 15;
				break;
			}
		}
		if (components is null) throw new InvalidDataException("JPEG has no supported frame.");
		try
		{
			var decoder = new JpegDecoder();
			decoder.BufferRows = outputWidth > 0;
			decoder.SetInput(data);
			decoder.Identify();
			if (outputWidth == 0) RasterImage.EnsureDecodable(decoder.Width, decoder.Height, limits);
			if (pointTransform >= decoder.Precision) throw new InvalidDataException("Invalid lossless JPEG point transform.");
			var maxH = decoder.GetMaximumHorizontalSampling();
			var maxV = decoder.GetMaximumVerticalSampling();
			var rowCount = outputWidth == 0 ? 0 : maxV * 16;
			var rows = outputWidth == 0 ? (decoder.Height + 7) / 8 * 8 : rowCount;
			var alignedWidth = (decoder.Width + 7) / 8 * 8;
			long workingBytes = (long)alignedWidth * rows * components.Length + (long)decoder.Width * 4 +
				(outputWidth == 0 ? (long)decoder.Width * decoder.Height * 4 : (long)outputWidth * outputHeight * 4 + (long)outputWidth * 80);
			if (isLossless) workingBytes += (long)alignedWidth * 32 * components.Length;
			else if (decoder.StartOfFrame == JpegMarkers.StartOfFrame10)
				workingBytes += (long)alignedWidth * (outputWidth == 0 ? rows : maxV * 8) * components.Length * 4;
			limits.EnsureWorkingMemory(workingBytes);
			foreach (var component in components)
			{
				if (maxH % component.H != 0 || maxV % component.V != 0) throw new NotSupportedException("Non-integral subsampling in arithmetic or lossless JPEG.");
				component.H = component.V = 1;
				component.BlocksWide = (decoder.Width + 7) / 8;
				component.BlocksHigh = (decoder.Height + 7) / 8;
				component.Samples = new byte[checked(component.Stride * (outputWidth == 0 ? component.BlocksHigh * 8 : rowCount))];
			}
			if (outputWidth > 0)
			{
				var writer = new JpegThumbnailSampleWriter(components, decoder.Width, decoder.Height, outputWidth, outputHeight,
					decoder.Precision, pointTransform, isLossless, rowCount,
					JpegTransforms.ResolveColorModel(components, hasJfif, adobeTransform), limits);
				decoder.SetOutputWriter(writer);
				decoder.Decode();
				return writer.Complete();
			}
			decoder.SetOutputWriter(new JpegSampleWriter(components, decoder.Width, decoder.Height, decoder.Precision, pointTransform, isLossless));
			decoder.Decode();
			return JpegTransforms.ToImage(decoder.Width, decoder.Height, components, JpegTransforms.ResolveColorModel(components, hasJfif, adobeTransform), limits);
		}
		catch (Exception error) when (error is ArgumentException or IndexOutOfRangeException or OverflowException or InvalidOperationException)
		{
			throw new InvalidDataException("Invalid arithmetic or lossless JPEG.", error);
		}
	}
}
