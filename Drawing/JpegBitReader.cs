namespace Ecng.Drawing;

using System.IO;

/// <summary>
/// Reads the entropy-coded data of a JPEG scan bit by bit, undoing the byte stuffing.
/// </summary>
internal sealed class JpegBitReader
{
	private readonly byte[] _data;
	private int _position;
	private int _current;
	private int _remaining;

	public JpegBitReader(byte[] data, int position)
	{
		_data = data;
		_position = position;
	}

	/// <summary>
	/// Offset of the first byte that has not been consumed.
	/// </summary>
	public int Position => _position;

	public int ReadBit()
	{
		if (_remaining == 0)
		{
			if (_position >= _data.Length)
				throw new InvalidDataException("JPEG entropy-coded data is truncated.");

			_current = _data[_position++];

			// A 0xFF that is data is followed by a stuffed zero; anything else after it is a marker.
			if (_current == 0xFF && (_position >= _data.Length || _data[_position++] != 0))
				throw new InvalidDataException("A marker interrupts JPEG entropy-coded data.");

			_remaining = 8;
		}

		return _current >> --_remaining & 1;
	}

	public int Read(int count)
	{
		if (count is < 0 or > 16)
			throw new InvalidDataException("Invalid JPEG bit count.");

		var value = 0;

		for (var i = 0; i < count; i++)
			value = value << 1 | ReadBit();

		return value;
	}

	/// <summary>
	/// Reads a value of the given magnitude category: the upper half of the range is positive, the lower half negative.
	/// </summary>
	public int ReadSigned(int size)
	{
		if (size == 0)
			return 0;

		var value = Read(size);
		return value < 1 << (size - 1) ? value - ((1 << size) - 1) : value;
	}

	/// <summary>
	/// Steps over the restart marker that ends an interval, dropping the bits that pad the last byte before it.
	/// </summary>
	/// <param name="index">Number of the interval that has ended, counted from zero.</param>
	public void Restart(int index)
	{
		_remaining = 0;

		if (_position >= _data.Length || _data[_position++] != 0xFF)
			throw new InvalidDataException("JPEG restart marker is missing.");

		while (_position < _data.Length && _data[_position] == 0xFF)
			_position++;

		if (_position >= _data.Length || _data[_position++] != 0xD0 + (index & 7))
			throw new InvalidDataException("JPEG restart markers are out of order.");
	}
}
