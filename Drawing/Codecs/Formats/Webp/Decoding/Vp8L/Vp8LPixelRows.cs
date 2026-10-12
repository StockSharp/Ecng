namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

using System;

internal sealed class Vp8LPixelRows
{
	private readonly Vp8LPixelStream _stream;
	private readonly uint[] _history;
	private readonly Vp8LColorCache _cache;
	private Vp8LPixelCommand _command;
	private int _remaining;
	private int _position;
	private int _nextHistory;
	private bool _isStopped;

	public Vp8LPixelRows(Vp8LBitReader reader, int width, int height, ImageProcessingLimits limits, long reservedBytes)
	{
		_stream = new(reader, width, height, true, limits);
		// Forty distance-prefix symbols encode at most 2^20 before the 120 plane codes are removed.
		var historyLength = Math.Min(_stream.PixelCount, (1 << 20) - 120);
		if ((long)historyLength * 4 + _stream.WorkingBytes + reservedBytes > limits.MaxWorkingMemoryBytes)
		{
			var scan = _stream.Clone();
			historyLength = 1;
			var position = 0;
			while (scan.TryRead(position, out var command))
			{
				historyLength = Math.Max(historyLength, command.Distance);
				position += command.Length;
			}
		}
		WorkingBytes = _stream.WorkingBytes + (long)historyLength * 4;
		limits.EnsureWorkingMemory(WorkingBytes + reservedBytes);
		_history = new uint[historyLength];
		_cache = _stream.CacheBits == 0 ? null : new Vp8LColorCache(_stream.CacheBits);
	}

	public long WorkingBytes { get; }

	public void Read(Span<uint> output)
	{
		if (output.Length > _stream.PixelCount - _position)
			throw new WebpDecodingException("VP8L rows exceed the declared image dimensions.");
		for (var x = 0; x < output.Length; x++)
		{
			if (_remaining == 0 && !_isStopped)
			{
				_isStopped = !_stream.TryRead(_position, out _command);
				_remaining = _isStopped ? 0 : _command.Length;
			}
			uint pixel = 0;
			if (!_isStopped)
			{
				if (_command.Distance != 0)
				{
					var source = _nextHistory - _command.Distance;
					if (source < 0)
						source += _history.Length;
					pixel = _history[source];
				}
				else
					pixel = _command.CacheIndex >= 0 ? _cache.Lookup(_command.CacheIndex) : _command.Color;
				_cache?.Insert(pixel);
				_remaining--;
			}
			_history[_nextHistory] = pixel;
			if (++_nextHistory == _history.Length)
				_nextHistory = 0;
			output[x] = pixel;
			_position++;
		}
	}
}
