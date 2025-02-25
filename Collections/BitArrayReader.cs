namespace Ecng.Collections
{
	using System;
	using System.IO;

	using Ecng.Common;

	/// <summary>
	/// Provides a reader for bit-level data from a stream.
	/// </summary>
	public class BitArrayReader
	{
		private int _bitOffset;
		private long _dataOffset;
		private readonly ulong[] _data;
		private readonly Stream _underlyingStream;

		/// <summary>
		/// Initializes a new instance of the <see cref="BitArrayReader"/> class with the specified underlying stream.
		/// </summary>
		/// <param name="underlyingStream">The stream to read bit-level data from.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="underlyingStream"/> is null.</exception>
		public BitArrayReader(Stream underlyingStream)
		{
			_underlyingStream = underlyingStream ?? throw new ArgumentNullException(nameof(underlyingStream));

			// TODO
			var bytes = underlyingStream.To<byte[]>();

			_data = new ulong[bytes.Length / 8 + 2];
			Buffer.BlockCopy(bytes, 0, _data, 0, bytes.Length);
		}

		/// <summary>
		/// Gets or sets the current bit offset within the stream.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the value is negative.</exception>
		public long Offset
		{
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
			get => (_dataOffset << 6) | _bitOffset;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
			set
			{
				if (value < 0)// || value >= _bits.Length)
					throw new ArgumentOutOfRangeException(nameof(value));

				_dataOffset = value >> 6;
				_bitOffset = (int)(value & 63);
			}
		}

		/// <summary>
		/// Retrieves the 64-bit value at the specified offset.
		/// </summary>
		/// <param name="offset">The offset to read from.</param>
		/// <returns>The 64-bit value at the specified offset.</returns>
		private ulong Get(long offset)
		{
			return _data[offset];
		}

		/// <summary>
		/// Reads a single bit from the stream.
		/// </summary>
		/// <returns>The value of the bit read.</returns>
		public bool Read()
		{
			var b = Get(_dataOffset);

			var value = ((b >> _bitOffset) & 1) != 0;

			if (_bitOffset == 63)
			{
				_bitOffset = 0;
				_dataOffset++;
			}
			else
				_bitOffset++;

			return value;
		}

		/// <summary>
		/// Reads an array of bits from the stream.
		/// </summary>
		/// <param name="count">The number of bits to read.</param>
		/// <returns>An array of boolean values representing the bits read.</returns>
		public bool[] ReadArray(int count)
		{
			var retVal = new bool[count];

			for (var i = 0; i < count; i++)
				retVal[i] = Read();

			return retVal;
		}

		/// <summary>
		/// Reads a specified number of bits from the stream as an integer.
		/// </summary>
		/// <param name="count">The number of bits to read.</param>
		/// <returns>The integer value represented by the bits read.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is invalid.</exception>
		public int Read(int count)
		{
			return (int)ReadLong(count);
		}

		/// <summary>
		/// Moves the current bit offset by the specified number of bits.
		/// </summary>
		/// <param name="offset">The number of bits to move the offset by.</param>
		public void Seek(int offset)
		{
			var newOffset = _bitOffset + offset;
			_dataOffset += newOffset >> 6;
			_bitOffset = newOffset & 63;
		}

		/// <summary>
		/// Reads a specified number of bits from the stream as a long integer.
		/// </summary>
		/// <param name="count">The number of bits to read.</param>
		/// <returns>The long integer value represented by the bits read.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is invalid.</exception>
		public long ReadLong(int count)
		{
			if (count <= 0 || count > 64)
				throw new ArgumentOutOfRangeException(nameof(count), count, "Invalid count value.");

			var bitOffset = _bitOffset;

			var value = Get(_dataOffset) >> bitOffset;

			var shift = 64 - bitOffset;
			if (shift < count)
				value |= Get(_dataOffset + 1) << shift;

			bitOffset += count;
			_dataOffset += bitOffset >> 6;
			_bitOffset = bitOffset & 63;

			value &= ulong.MaxValue >> (64 - count);

			return (long)value;
		}

		/// <summary>
		/// Reads an integer value from the stream using a variable-length encoding.
		/// </summary>
		/// <returns>The integer value read from the stream.</returns>
		public int ReadInt()
		{
			var bits = Get(_dataOffset);
			var bitOffset = _bitOffset;

			bits >>= bitOffset;

			if (bitOffset > 0)
				bits |= Get(_dataOffset + 1) << (64 - bitOffset); // честные 64 бита в битс

			var value = 0;

			int seek;

			if ((bits & 1) == 0)
			{
				seek = 1;
			}
			else if ((bits & 4) == 0)
			{
				seek = 3;
				value = 1;
			}
			else if ((bits & 8) == 0)
			{
				seek = 4 + 4;
				value = ((int)(uint.MaxValue >> (32 - 4))) & (int)(bits >> 4);
			}
			else if ((bits & 16) == 0)
			{
				seek = 5 + 8;
				value = ((int)(uint.MaxValue >> (32 - 8))) & (int)(bits >> 5);
			}
			else if ((bits & 32) == 0)
			{
				seek = 6 + 16;
				value = ((int)(uint.MaxValue >> (32 - 16))) & (int)(bits >> 6);
			}
			else if ((bits & 64) == 0)
			{
				seek = 7 + 24;
				value = ((int)(uint.MaxValue >> (32 - 24))) & (int)(bits >> 7);
			}
			else
			{
				seek = 7 + 32;
				value = (int)(bits >> 7);
			}

			value = (bits & 2) != 0 ? value : -value;

			bitOffset += seek;
			_dataOffset += (bitOffset >> 6);
			_bitOffset = bitOffset & 63;

			return value;
		}

		/// <summary>
		/// Reads a long integer value from the stream using a variable-length encoding.
		/// </summary>
		/// <returns>The long integer value read from the stream.</returns>
		public long ReadLong()
		{
			var offset = _dataOffset;
			var bitOffset = _bitOffset;
			var bits = Get(offset) >> bitOffset;

			if (bitOffset > 0)
				bits |= Get(offset + 1) << (64 - bitOffset);

			long value;
			if ((bits & 1) != 0)
			{
				var isPositive = (bits & 2) != 0;

				bitOffset += 2;

				offset += bitOffset >> 6;
				bitOffset &= 63;
				bits = Get(offset) >> bitOffset;

				if (bitOffset > 0)
					bits |= Get(offset + 1) << (64 - bitOffset);

				bitOffset += 63;

				value = (long)(bits & (ulong.MaxValue >> (64 - 63)));

				if (!isPositive)
					value = -value;
			}
			else
			{
				bitOffset += 1;
				bits >>= 1;

				int seek;

				value = 0;

				if ((bits & 1) == 0)
				{
					seek = 1;
				}
				else if ((bits & 4) == 0)
				{
					seek = 3;
					value = 1;
				}
				else if ((bits & 8) == 0)
				{
					seek = 4 + 4;
					value = ((int)(uint.MaxValue >> (32 - 4))) & (int)(bits >> 4);
				}
				else if ((bits & 16) == 0)
				{
					seek = 5 + 8;
					value = ((int)(uint.MaxValue >> (32 - 8))) & (int)(bits >> 5);
				}
				else if ((bits & 32) == 0)
				{
					seek = 6 + 16;
					value = ((int)(uint.MaxValue >> (32 - 16))) & (int)(bits >> 6);
				}
				else if ((bits & 64) == 0)
				{
					seek = 7 + 24;
					value = ((int)(uint.MaxValue >> (32 - 24))) & (int)(bits >> 7);
				}
				else
				{
					seek = 7 + 32;
					value = (int)(bits >> 7);
				}

				value = (bits & 2) != 0 ? value : -value;
				bitOffset += seek;
			}

			offset += bitOffset >> 6;
			bitOffset &= 63;
			_dataOffset = offset;
			_bitOffset = bitOffset;

			return value;
		}

		/// <summary>
		/// Reads a decimal value from the stream.
		/// </summary>
		/// <returns>The decimal value read from the stream.</returns>
		public decimal ReadDecimal()
		{
			var isPos = Read();

			var i1 = ReadInt();
			var i2 = ReadInt();
			var i3 = ReadInt();
			var i4 = ReadInt() << 16;
			var dec = new[] { i1, i2, i3, i4 }.To<decimal>();

			if (!isPos)
				dec = -dec;

			return dec;
		}
	}
}