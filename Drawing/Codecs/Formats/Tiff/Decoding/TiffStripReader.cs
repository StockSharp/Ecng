namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;

internal sealed class TiffStripReader(ReadOnlyMemory<byte> input, int compression, long expectedLength)
{
	private readonly ushort[] _prefix = compression == 5 ? new ushort[4096] : [];
	private readonly byte[] _suffix = compression == 5 ? new byte[4096] : [];
	private readonly byte[] _stack = compression == 5 ? new byte[4096] : [];
	private int _position;
	private long _written;
	private int _run;
	private int _value;
	private bool _literal;
	private int _bit;
	private int _width = 9;
	private int _nextCode = 258;
	private int _previous = -1;
	private int _stackTop;
	private bool _extra;
	private byte _first;

	public void Read(Span<byte> output)
	{
		if (output.Length > expectedLength - _written) throw new TiffDecodingException("TIFF strip exceeds its row count.");
		if (compression == 1)
		{
			if (output.Length > input.Length - _position) throw new TiffDecodingException("Truncated TIFF strip.");
			input.Span.Slice(_position, output.Length).CopyTo(output);
			_position += output.Length;
			_written += output.Length;
			return;
		}
		for (var i = 0; i < output.Length; i++)
		{
			output[i] = compression == 5 ? ReadLzw() : ReadPackBits();
			_written++;
		}
	}

	private byte ReadPackBits()
	{
		while (_run == 0)
		{
			if (_position >= input.Length) throw new TiffDecodingException("Truncated TIFF PackBits strip.");
			var control = (sbyte)input.Span[_position++];
			if (control == -128) continue;
			_literal = control >= 0;
			_run = _literal ? control + 1 : 1 - control;
			if (_run > expectedLength - _written || (_literal ? _run : 1) > input.Length - _position)
				throw new TiffDecodingException("Invalid TIFF PackBits run.");
			if (!_literal) _value = input.Span[_position++];
		}
		_run--;
		return _literal ? input.Span[_position++] : (byte)_value;
	}

	private byte ReadLzw()
	{
		while (_stackTop == 0 && !_extra)
		{
			if ((long)_bit + _width > (long)input.Length * 8) throw new TiffDecodingException("Truncated TIFF LZW strip.");
			var code = 0;
			for (var i = 0; i < _width; i++, _bit++) code = code << 1 | (input.Span[_bit / 8] >> (7 - (_bit & 7)) & 1);
			if (code == 256) { _nextCode = 258; _width = 9; _previous = -1; continue; }
			if (code == 257) throw new TiffDecodingException("TIFF LZW strip ends before its last row.");
			var isNew = code == _nextCode && _previous >= 0;
			if (code > _nextCode || code == _nextCode && !isNew) throw new TiffDecodingException("Invalid TIFF LZW code.");
			var current = isNew ? _previous : code;
			while (current >= 258)
			{
				if (_stackTop == 4095 || current >= _nextCode) throw new TiffDecodingException("Invalid TIFF LZW prefix chain.");
				_stack[_stackTop++] = _suffix[current];
				current = _prefix[current];
			}
			_first = (byte)current;
			_stack[_stackTop++] = _first;
			_extra = isNew;
			if (_previous >= 0 && _nextCode < 4096)
			{
				_prefix[_nextCode] = (ushort)_previous;
				_suffix[_nextCode++] = _first;
				if (_nextCode >= (1 << _width) - 1 && _width < 12) _width++;
			}
			_previous = code;
		}
		if (_stackTop > 0) return _stack[--_stackTop];
		_extra = false;
		return _first;
	}
}
