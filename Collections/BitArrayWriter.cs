namespace Ecng.Collections;

using System;
using System.IO;

using Ecng.Common;

/// <summary>
/// Provides a writer for bit-level data to a stream.
/// </summary>
public class BitArrayWriter(Stream underlyingStream) : Disposable
{
	private readonly Stream _underlyingStream = underlyingStream ?? throw new ArgumentNullException(nameof(underlyingStream));
	private int _temp;
	private int _bitOffset;
#if NET5_0_OR_GREATER
	private readonly int[] _decimalBits = new int[4];
#endif

	/// <summary>
	/// Flushes the current byte to the underlying stream and resets the buffer.
	/// </summary>
	private void Flush()
	{
		_underlyingStream.WriteByte((byte)_temp);
		_temp = 0;
		_bitOffset = 0;
	}

	/// <summary>
	/// Disposes the writer, ensuring any remaining bits are flushed to the stream.
	/// </summary>
	protected override void DisposeManaged()
	{
		if (_bitOffset > 0)
			Flush();

		base.DisposeManaged();
	}

	/// <summary>
	/// Writes a single bit to the stream.
	/// </summary>
	/// <param name="bit">The bit to write.</param>
	public void Write(bool bit)
	{
		_temp |= ((bit ? 1 : 0) << _bitOffset);

		_bitOffset++;

		if (_bitOffset < 8)
			return;

		Flush();
	}

	/// <summary>
	/// Writes an integer value to the stream using a variable-length encoding.
	/// </summary>
	/// <param name="value">The integer value to write.</param>
	public void WriteInt(int value)
	{
		if (value == 0)
			Write(false);
		else
		{
			Write(true);

			if (value < 0)
			{
				value = -value;
				Write(false);
			}
			else
				Write(true);

			if (value == 1)
				Write(false);
			else
			{
				Write(true);

				if (value < 16)
				{
					Write(false);
					WriteBits(value, 4);
				}
				else
				{
					Write(true);

					if (value <= byte.MaxValue)
					{
						Write(false);
						WriteBits(value, 8);
					}
					else
					{
						Write(true);

						if (value <= ushort.MaxValue)
						{
							Write(false);
							WriteBits(value, 16);
						}
						else
						{
							Write(true);

							if (value <= 16777216) // 24 bits
							{
								Write(false);
								WriteBits(value, 24);
							}
							else
							{
								Write(true);
								WriteBits(value, 32);
							}
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Writes a long integer value to the stream using a variable-length encoding.
	/// </summary>
	/// <param name="value">The long integer value to write.</param>
	public void WriteLong(long value)
	{
		if (value.Abs() > int.MaxValue)
		{
			Write(true);
			Write(value >= 0);
			WriteBits(value.Abs(), 63);
		}
		else
		{
			Write(false);
			WriteInt((int)value);
		}
	}

	/// <summary>
	/// Writes a specified number of bits from an integer value to the stream.
	/// </summary>
	/// <param name="value">The integer value to write.</param>
	/// <param name="bitCount">The number of bits to write.</param>
	public void WriteBits(int value, int bitCount)
	{
		for (var i = 0; i < bitCount; i++)
			Write((value & (1 << i)) != 0);
	}

	/// <summary>
	/// Writes a specified number of bits from a long integer value to the stream.
	/// </summary>
	/// <param name="value">The long integer value to write.</param>
	/// <param name="bitCount">The number of bits to write.</param>
	public void WriteBits(long value, int bitCount)
	{
		for (var i = 0; i < bitCount; i++)
			Write((value & (1L << i)) != 0);
	}

	/// <summary>
	/// Writes a decimal value to the stream.
	/// </summary>
	/// <param name="value">The decimal value to write.</param>
	public void WriteDecimal(decimal value)
	{
		if (value < 0)
		{
			value = -value;
			Write(false);
		}
		else
			Write(true);

		int[] bits;

#if NET5_0_OR_GREATER
		decimal.GetBits(value, _decimalBits);
		bits = _decimalBits;
#else
		bits = value.To<int[]>();
#endif

		WriteInt(bits[0]);
		WriteInt(bits[1]);
		WriteInt(bits[2]);
		WriteInt((bits[3] >> 16) & 0xff);
	}
}