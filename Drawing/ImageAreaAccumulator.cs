namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>Area averaging of sparse image rows, with storage proportional to the destination.</summary>
internal sealed class ImageAreaAccumulator
{
	private readonly int _sourceWidth;
	private readonly int _sourceHeight;
	private readonly ImageAreaRow _row;
	private readonly double[] _sums;
	private readonly int _retainedRows;
	private int _completedRows;

	public ImageAreaAccumulator(int sourceWidth, int sourceHeight, int width, int height, ImageProcessingLimits limits)
		: this(sourceWidth, sourceHeight, width, height, height, limits)
	{
	}

	public ImageAreaAccumulator(int sourceWidth, int sourceHeight, int width, int height, int retainedRows, ImageProcessingLimits limits)
	{
		if (sourceWidth < width || sourceHeight < height || width < 1 || height < 1)
			throw new ArgumentOutOfRangeException(nameof(width));
		if (retainedRows < 1 || retainedRows > height) throw new ArgumentOutOfRangeException(nameof(retainedRows));

		_sourceWidth = sourceWidth;
		_sourceHeight = sourceHeight;
		_retainedRows = retainedRows;
		limits.EnsureWorkingMemory((long)width * height * 4 + (long)width * retainedRows * 32 + (long)sourceWidth * 4 + (long)width * 48);
		Image = new RasterImage(width, height, limits);
		_row = new(sourceWidth, width);
		_sums = new double[checked(width * retainedRows * 4)];
	}

	public RasterImage Image { get; }

	public void AddRow(ReadOnlySpan<byte> pixels, int startX, int y, int stepX)
	{
		if (pixels.Length == 0 || pixels.Length % 4 != 0 || startX < 0 || stepX < 1 ||
			(long)startX + (pixels.Length / 4 - 1) * (long)stepX >= _sourceWidth || y < 0 || y >= _sourceHeight)
			throw new InvalidDataException("Invalid sparse image row supplied for downscaling.");

		var width = Image.Width;
		_row.Sum(pixels, startX, stepX);
		var horizontal = _row.Sums;

		var top = (long)y * Image.Height;
		var bottom = top + Image.Height;
		while (top < bottom)
		{
			var destinationY = (int)(top / _sourceHeight);
			var end = Math.Min(bottom, (long)(destinationY + 1) * _sourceHeight);
			var weight = end - top;
			if (destinationY < _completedRows || destinationY - _completedRows >= _retainedRows)
				throw new InvalidDataException("Image rows lie outside the active area-averaging window.");
			var offset = destinationY % _retainedRows * width * 4;
			ImageAreaWeights.Add(horizontal, _sums.AsSpan(offset, horizontal.Length), weight, false);
			top = end;
		}
	}

	public void Complete()
		=> CompleteThrough(_sourceHeight);

	public void CompleteThrough(int sourceRowExclusive)
	{
		if (sourceRowExclusive < 0 || sourceRowExclusive > _sourceHeight)
			throw new ArgumentOutOfRangeException(nameof(sourceRowExclusive));
		var end = (int)((long)sourceRowExclusive * Image.Height / _sourceHeight);
		if (end < _completedRows || end - _completedRows > _retainedRows)
			throw new InvalidDataException("Image completion lies outside the active area-averaging window.");
		var area = (double)_sourceWidth * _sourceHeight;
		var pixels = Image.Pixels;
		for (; _completedRows < end; _completedRows++)
		{
			var offset = _completedRows % _retainedRows * Image.Width * 4;
			var destination = _completedRows * Image.Width * 4;
			ImageAreaWeights.WriteRow(_sums.AsSpan(offset, Image.Width * 4), pixels.AsSpan(destination, Image.Width * 4), area);
			Array.Clear(_sums, offset, Image.Width * 4);
		}
	}
}
