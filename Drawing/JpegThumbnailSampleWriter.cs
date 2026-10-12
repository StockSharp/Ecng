namespace Ecng.Drawing;

using System;
using System.IO;

using Ecng.Drawing.Codecs.Jpeg;

internal sealed class JpegThumbnailSampleWriter : JpegBlockOutputWriter
{
	private readonly JpegSampleWriter _writer;
	private readonly ImageDownscaler _downscaler;
	private readonly JpegColorConverter _converter;
	private readonly byte[] _row;
	private readonly int[] _available;
	private readonly int _width;
	private readonly int _height;
	private readonly int _rowCount;
	private int _nextRow;

	public JpegThumbnailSampleWriter(JpegComponent[] components, int width, int height, int outputWidth, int outputHeight,
		int precision, int pointTransform, bool isLossless, int rowCount, JpegColorModels model, ImageProcessingLimits limits)
	{
		_width = width;
		_height = height;
		_rowCount = rowCount;
		_available = new int[components.Length];
		_row = new byte[checked(width * 4)];
		_downscaler = new(width, height, outputWidth, outputHeight, limits);
		_converter = new(width, components, model);
		_writer = new(components, width, height, precision, pointTransform, isLossless)
		{
			RowCount = rowCount,
			BlockCompleted = CompleteBlock,
		};
	}

	public override void WriteBlock(ref short blockRef, int componentIndex, int x, int y)
	{
		if (x >= _width || y >= _height) return;
		if (y < _nextRow || y >= _nextRow + _rowCount) throw new InvalidDataException("JPEG sample blocks exceed the bounded row window.");
		_writer.WriteBlock(ref blockRef, componentIndex, x, y);
	}

	private void CompleteBlock(int componentIndex, int x, int y)
	{
		if (x + 8 < _width) return;
		_available[componentIndex] = Math.Max(_available[componentIndex], Math.Min(_height, y + 8));
		var end = _height;
		foreach (var available in _available) end = Math.Min(end, available);
		while (_nextRow < end)
		{
			_converter.WriteRow(_nextRow++ % _rowCount, _row);
			_downscaler.AddRow(_row);
		}
	}

	public RasterImage Complete()
	{
		if (_nextRow != _height) throw new InvalidDataException("JPEG samples do not cover every output row.");
		return _downscaler.Image;
	}
}
