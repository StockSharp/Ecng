namespace Ecng.Drawing;

using System;
using System.IO;

internal sealed class WebpBitWriter : IDisposable
{
	private readonly MemoryStream _stream = new();
	private ulong _bits;
	private int _count;

	public void Write(uint value, int count)
	{
		_bits |= (ulong)value << _count;
		_count += count;
		while (_count >= 8)
		{
			_stream.WriteByte((byte)_bits);
			_bits >>= 8;
			_count -= 8;
		}
	}

	public byte[] Finish()
	{
		if (_count != 0)
			_stream.WriteByte((byte)_bits);
		return _stream.ToArray();
	}

	public void Dispose() => _stream.Dispose();
}
