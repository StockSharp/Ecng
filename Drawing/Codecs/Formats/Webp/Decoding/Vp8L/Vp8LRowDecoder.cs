namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;
using System.Collections.Generic;

internal sealed class Vp8LRowDecoder
{
	private readonly List<Vp8LTransform> _transforms;
	private readonly Vp8LPixelRows _pixels;
	private readonly uint[] _row;
	private readonly uint[] _expanded;
	private readonly uint[] _predictor;
	private readonly int _packedWidth;
	private int _nextRow;

	public Vp8LRowDecoder(Vp8LBitReader reader, int width, int height, ImageProcessingLimits limits, long reservedBytes)
	{
		Width = width;
		Height = height;
		var packedWidth = width;
		_transforms = Vp8LTransformReader.ReadTransforms(reader, ref packedWidth, height, limits);
		_packedWidth = packedWidth;
		var scratchBytes = (long)width * 16;
		foreach (var transform in _transforms)
			scratchBytes += (long)(transform.Data?.Length ?? 0) * 4;
		_pixels = new(reader, _packedWidth, height, limits, scratchBytes + reservedBytes);
		WorkingBytes = _pixels.WorkingBytes + scratchBytes;
		_row = new uint[width];
		_expanded = new uint[width];
		_predictor = new uint[checked(width * 2)];
	}

	public int Width { get; }
	public int Height { get; }
	public long WorkingBytes { get; }

	public ReadOnlySpan<uint> ReadRow()
	{
		if (_nextRow >= Height)
			throw new WebpDecodingException("VP8L rows exceed the declared image height.");
		_pixels.Read(_row.AsSpan(0, _packedWidth));
		var current = _row;
		var rowWidth = _packedWidth;
		for (var i = _transforms.Count - 1; i >= 0; i--)
		{
			var transform = _transforms[i];
			switch (transform.Type)
			{
				case Vp8LTransformTypes.SubtractGreen:
					Vp8LSubtractGreenTransform.ApplyInverse(current.AsSpan(0, rowWidth));
					break;
				case Vp8LTransformTypes.CrossColor:
					Vp8LColorTransform.ApplyInverseRow(current.AsSpan(0, rowWidth), _nextRow, transform);
					break;
				case Vp8LTransformTypes.Predictor:
					current.AsSpan(0, rowWidth).CopyTo(_predictor.AsSpan(rowWidth, rowWidth));
					Vp8LPredictorTransform.ApplyInverseRow(_predictor.AsSpan(0, rowWidth * 2), rowWidth, _nextRow, transform);
					_predictor.AsSpan(rowWidth, rowWidth).CopyTo(current);
					_predictor.AsSpan(rowWidth, rowWidth).CopyTo(_predictor);
					break;
				case Vp8LTransformTypes.ColorIndexing:
					Vp8LColorIndexingTransform.ApplyInverseRow(current.AsSpan(0, rowWidth), _expanded.AsSpan(0, transform.Xsize), transform);
					current = _expanded;
					rowWidth = transform.Xsize;
					break;
			}
		}
		_nextRow++;
		return current.AsSpan(0, Width);
	}
}
