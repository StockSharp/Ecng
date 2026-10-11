namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>Area averaging over successive RGBA rows, keeping only one destination row of sums.</summary>
internal sealed class ImageDownscaler
{
	private readonly int _sourceWidth;
	private readonly int _sourceHeight;
	private readonly int[] _starts;
	private readonly int[] _ends;
	private readonly int[] _firstWeights;
	private readonly int[] _lastWeights;
	private readonly double[] _horizontal;
	private readonly double[] _sums;
	private int _nextRow;

	public ImageDownscaler(int sourceWidth, int sourceHeight, int width, int height)
	{
		if (sourceWidth < width || sourceHeight < height || width < 1 || height < 1)
			throw new ArgumentOutOfRangeException(nameof(width));

		_sourceWidth = sourceWidth;
		_sourceHeight = sourceHeight;
		Image = new RasterImage(width, height);
		_starts = new int[width];
		_ends = new int[width];
		_firstWeights = new int[width];
		_lastWeights = new int[width];
		_horizontal = new double[width * 4];
		_sums = new double[width * 4];

		for (var x = 0; x < width; x++)
		{
			var left = (long)x * sourceWidth;
			var right = left + sourceWidth;
			var first = _starts[x] = (int)(left / width);
			var end = _ends[x] = (int)((right + width - 1) / width);
			_firstWeights[x] = (int)(Math.Min(right, (long)(first + 1) * width) - left);
			_lastWeights[x] = (int)(right - (long)(end - 1) * width);
		}
	}

	public RasterImage Image { get; }

	public void AddRow(ReadOnlySpan<byte> pixels)
	{
		if (pixels.Length != (long)_sourceWidth * 4 || _nextRow >= _sourceHeight)
			throw new InvalidDataException("Invalid image row supplied for downscaling.");

		var width = Image.Width;
		for (var x = 0; x < width; x++)
		{
			double alpha = 0, red = 0, green = 0, blue = 0;
			var first = _starts[x];
			var end = _ends[x];
			for (var sourceX = first; sourceX < end; sourceX++)
			{
				var at = sourceX * 4;
				var weight = sourceX == first ? _firstWeights[x] : sourceX == end - 1 ? _lastWeights[x] : width;
				var a = (double)pixels[at + 3] * weight;
				alpha += a;
				red += pixels[at] * a;
				green += pixels[at + 1] * a;
				blue += pixels[at + 2] * a;
			}
			var to = x * 4;
			_horizontal[to] = alpha;
			_horizontal[to + 1] = red;
			_horizontal[to + 2] = green;
			_horizontal[to + 3] = blue;
		}

		var top = (long)_nextRow++ * Image.Height;
		var bottom = top + Image.Height;
		while (top < bottom)
		{
			var destinationY = (int)(top / _sourceHeight);
			var end = Math.Min(bottom, (long)(destinationY + 1) * _sourceHeight);
			var weight = end - top;
			for (var i = 0; i < _sums.Length; i++)
				_sums[i] += _horizontal[i] * weight;

			if (end == (long)(destinationY + 1) * _sourceHeight)
				WriteRow(destinationY);
			top = end;
		}
	}

	private void WriteRow(int y)
	{
		var area = (double)_sourceWidth * _sourceHeight;
		var pixels = Image.Pixels.AsSpan(y * Image.Width * 4, Image.Width * 4);
		for (var x = 0; x < Image.Width; x++)
		{
			var at = x * 4;
			var alpha = _sums[at];
			if (alpha > 0)
			{
				pixels[at] = RasterImage.ToByte(_sums[at + 1] / alpha);
				pixels[at + 1] = RasterImage.ToByte(_sums[at + 2] / alpha);
				pixels[at + 2] = RasterImage.ToByte(_sums[at + 3] / alpha);
			}
			pixels[at + 3] = RasterImage.ToByte(alpha / area);
		}
		Array.Clear(_sums);
	}
}
