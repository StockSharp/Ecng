namespace Ecng.Drawing;

using System;
using System.Buffers;
using System.IO;

using Ecng.Drawing.Codecs;
using Ecng.Drawing.Codecs.Formats.Avif.Container;
using Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

internal static class AvifThumbnailDecoder
{
	public static RasterImage Decode(AvifContainerInfo container, int width, int height, ImageProcessingLimits limits)
	{
		if (container.ColorTiles.Count != (long)container.GridRows * container.GridColumns ||
			container.HasAlpha && container.AlphaTiles.Count != container.ColorTiles.Count)
			throw new InvalidDataException("AVIF tile counts do not match the image grid.");
		var first = Av1FrameDecoder.ParseFrameHeader(container.ColorTiles[0]);
		var tileWidth = first.Frame.UpscaledWidth;
		var tileHeight = first.Frame.FrameHeight;
		var rotated = (int)container.Orientation >= 5;
		var outputWidth = rotated ? height : width;
		var outputHeight = rotated ? width : height;
		var retainedRows = Math.Min(outputHeight, (int)(((long)tileHeight * outputHeight + container.Height - 1) / container.Height) + 1);
		var destinationBytes = (long)outputWidth * outputHeight * 4 + (long)outputWidth * retainedRows * 32 + (long)outputWidth * 48;
		var accumulator = new ImageAreaAccumulator(container.Width, container.Height, outputWidth, outputHeight, retainedRows, limits);
		for (var index = 0; index < container.ColorTiles.Count; index++)
		{
			var colorEstimate = Av1FrameDecoder.EstimateBuffers(container.ColorTiles[index]);
			var alphaEstimate = container.HasAlpha ? Av1FrameDecoder.EstimateBuffers(container.AlphaTiles[index]) : default;
			var peak = Math.Max(alphaEstimate.working, colorEstimate.working + alphaEstimate.retained);
			limits.EnsureWorkingMemory(peak + destinationBytes + (long)tileWidth * 12);
			Av1DecodedFrame color = null, alpha = null;
			try
			{
				if (container.HasAlpha)
					alpha = Av1FrameDecoder.Decode(container.AlphaTiles[index], limits, true);
				color = Av1FrameDecoder.Decode(container.ColorTiles[index], limits, true);
				var left = index % container.GridColumns * tileWidth;
				var top = index / container.GridColumns * tileHeight;
				var copyWidth = Math.Min(tileWidth, container.Width - left);
				var copyHeight = Math.Min(tileHeight, container.Height - top);
				if (copyWidth <= 0 || copyHeight <= 0)
					continue;
				if (color.Frame.UpscaledWidth < copyWidth || color.Frame.FrameHeight < copyHeight ||
					alpha is not null && (alpha.Frame.UpscaledWidth < copyWidth || alpha.Frame.FrameHeight < copyHeight))
					throw new InvalidDataException("AVIF tile does not cover its grid cell.");
				var sequence = color.Sequence;
				if ((sequence.SubsamplingX && (left & 1) != 0) || (sequence.SubsamplingY && (top & 1) != 0))
					throw new InvalidDataException("AVIF grid offsets are not aligned to chroma samples.");
				var format = container.HasAlpha
					? sequence.BitDepth > 8 ? CodecPixelFormats.Rgba64 : CodecPixelFormats.Rgba32
					: sequence.MonoChrome
						? sequence.BitDepth > 8 ? CodecPixelFormats.Gray16 : CodecPixelFormats.Gray8
						: sequence.BitDepth > 8 ? CodecPixelFormats.Rgb48 : CodecPixelFormats.Rgb24;
				var rgba = new byte[checked(copyWidth * 4)];
				var rowIndex = top;
				Av1YuvToRgbConverter.ConvertRows(color.Planes, color.PlaneWidths, sequence.MonoChrome,
					sequence.SubsamplingX, sequence.SubsamplingY, sequence.MatrixCoefficients, sequence.ColorRange,
					sequence.BitDepth, alpha?.Planes[0], alpha?.PlaneWidths[0] ?? 0, copyWidth, copyHeight, row =>
					{
						CodecImage.WriteRgba(row, format, rgba);
						accumulator.AddRow(rgba, left, rowIndex++, 1);
					});
				if ((index + 1) % container.GridColumns == 0)
					accumulator.CompleteThrough(top + copyHeight);
			}
			finally
			{
				ReturnPlanes(color);
				ReturnPlanes(alpha);
			}
		}
		accumulator.Complete();
		return accumulator.Image.Orient((int)container.Orientation);
	}

	private static void ReturnPlanes(Av1DecodedFrame frame)
	{
		if (frame is null)
			return;
		foreach (var plane in frame.Planes)
			if (plane is not null && plane.Length != 0)
				ArrayPool<int>.Shared.Return(plane);
	}
}
