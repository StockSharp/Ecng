namespace Ecng.Drawing;

using System;
using System.IO;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

/// <summary>
/// Reads buffered entropy-coded bits, undoing byte stuffing without reading past scan or restart markers.
/// </summary>
internal sealed class JpegBitReader
{
	private readonly byte[] _data;
	private int _position;
	private ulong _current;
	private int _remaining;

	public JpegBitReader(byte[] data, int position)
	{
		_data = data;
		_position = position;
	}

	/// <summary>
	/// Offset of the first byte that has not been consumed.
	/// </summary>
	public int Position => _remaining < 8 ? _position : throw new InvalidDataException("JPEG scan has unconsumed entropy bytes.");

	public int ReadBit()
		=> Read(1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Read(int count)
	{
		if (!TryPeek(count, out var value))
			throw new InvalidDataException("JPEG entropy-coded data is truncated or interrupted by a marker.");
		_remaining -= count;
		return value;
	}

	/// <summary>Looks ahead without consuming bits or crossing an entropy segment's closing marker.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryPeek(int count, out int value)
	{
		if (count is < 0 or > 16)
			throw new InvalidDataException("Invalid JPEG bit count.");
		if (_remaining < count && !Refill(count))
		{
			value = 0;
			return false;
		}
		value = (int)(_current >> (_remaining - count) & ((1u << count) - 1));
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool Refill(int count)
	{
		while (_remaining < count)
		{
			if (_position + 4 <= _data.Length)
			{
				var word = BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan(_position, 4));
				var inverted = ~word;
				if ((unchecked(inverted - 0x01010101u) & word & 0x80808080u) == 0)
				{
					_current = _current << 32 | word;
					_position += 4;
					_remaining += 32;
					continue;
				}
			}
			if (_position >= _data.Length || _data[_position] == 0xff &&
				(_position + 1 >= _data.Length || _data[_position + 1] != 0))
			{
				return false;
			}
			var next = _data[_position++];
			if (next == 0xff) _position++;
			_current = _current << 8 | next;
			_remaining += 8;
		}
		return true;
	}

	public void Skip(int count)
		=> _remaining -= count;

	/// <summary>
	/// Reads a value of the given magnitude category: the upper half of the range is positive, the lower half negative.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadSigned(int size)
	{
		if (size == 0)
			return 0;

		var value = Read(size);
		var mask = (1 << size) - 1;
		return value - (((value - ((mask + 1) >> 1)) >> 31) & mask);
	}

	/// <summary>
	/// Steps over the restart marker that ends an interval, dropping the bits that pad the last byte before it.
	/// </summary>
	/// <param name="index">Number of the interval that has ended, counted from zero.</param>
	public void Restart(int index)
	{
		if (_remaining >= 8)
			throw new InvalidDataException("JPEG restart marker is missing.");
		_remaining = 0;

		if (_position >= _data.Length || _data[_position++] != 0xFF)
			throw new InvalidDataException("JPEG restart marker is missing.");

		while (_position < _data.Length && _data[_position] == 0xFF)
			_position++;

		if (_position >= _data.Length || _data[_position++] != 0xD0 + (index & 7))
			throw new InvalidDataException("JPEG restart markers are out of order.");
	}
}
