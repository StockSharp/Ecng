namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>Area averaging over successive RGBA rows, keeping only one destination row of sums.</summary>
internal sealed class ImageDownscaler
{
	private readonly int _sourceWidth;
	private readonly int _sourceHeight;
	private readonly ImageAreaRow _row;
	private readonly double[] _sums;
	private int _nextRow;
	private bool _isRowStarted;

	public ImageDownscaler(int sourceWidth, int sourceHeight, int width, int height)
		: this(sourceWidth, sourceHeight, width, height, ImageProcessingLimits.Default)
	{
	}

	public ImageDownscaler(int sourceWidth, int sourceHeight, int width, int height, ImageProcessingLimits limits)
	{
		if (sourceWidth < width || sourceHeight < height || width < 1 || height < 1)
			throw new ArgumentOutOfRangeException(nameof(width));

		_sourceWidth = sourceWidth;
		_sourceHeight = sourceHeight;
		limits.EnsureWorkingMemory((long)width * height * 4 + (long)sourceWidth * 4 + (long)width * 80);
		Image = new RasterImage(width, height, limits);
		_row = new(sourceWidth, width);
		_sums = new double[width * 4];
	}

	public RasterImage Image { get; }

	public void AddRow(ReadOnlySpan<byte> pixels)
		=> AddRow(pixels, false);

	public void AddRow(ReadOnlySpan<byte> pixels, bool isRepeated)
	{
		if (pixels.Length != (long)_sourceWidth * 4 || _nextRow >= _sourceHeight)
			throw new InvalidDataException("Invalid image row supplied for downscaling.");

		if (!isRepeated || _nextRow == 0)
			_row.Sum(pixels, 0, 1);
		var horizontal = _row.Sums;

		var top = (long)_nextRow++ * Image.Height;
		var bottom = top + Image.Height;
		while (top < bottom)
		{
			var destinationY = (int)(top / _sourceHeight);
			var end = Math.Min(bottom, (long)(destinationY + 1) * _sourceHeight);
			var weight = end - top;
			ImageAreaWeights.Add(horizontal, _sums, weight, !_isRowStarted);
			_isRowStarted = true;

			if (end == (long)(destinationY + 1) * _sourceHeight)
				WriteRow(destinationY);
			top = end;
		}
	}

	private void WriteRow(int y)
	{
		var area = (double)_sourceWidth * _sourceHeight;
		var pixels = Image.Pixels.AsSpan(y * Image.Width * 4, Image.Width * 4);
		ImageAreaWeights.WriteRow(_sums, pixels, area);
		_isRowStarted = false;
	}
}
