namespace Ecng.Collections
{
	using System;
	using System.IO;

	using Ecng.Common;

	public class BitArrayReader
	{
		private int _bitOffset;
		private long _dataOffset;
		private readonly ulong[] _data;
		private readonly Stream _underlyingStream;

		public BitArrayReader(Stream underlyingStream)
		{
			_underlyingStream = underlyingStream ?? throw new ArgumentNullException(nameof(underlyingStream));

			// TODO
			var bytes = underlyingStream.To<byte[]>();

			_data = new ulong[bytes.Length / 8 + 2];
			Buffer.BlockCopy(bytes, 0, _data, 0, bytes.Length);
		}

		public long Offset
		{
			get => (_dataOffset << 6) | _bitOffset;
			set
			{
				if (value < 0)// || value >= _bits.Length)
					throw new ArgumentOutOfRangeException();

				_dataOffset = value >> 6;
				_bitOffset = (int)(value & 63);
			}
		}

		private ulong Get(long offset)
		{
			return _data[offset];
		}

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

		public bool[] ReadArray(int count)
		{
			var retVal = new bool[count];

			for (var i = 0; i < count; i++)
				retVal[i] = Read();

			return retVal;
		}

		public int Read(int count)
		{
			return (int)ReadLong(count);
		}

		public void Seek(int offset)
		{
			var newOffset = _bitOffset + offset;
			_dataOffset += newOffset >> 6;
			_bitOffset = newOffset & 63;
		}

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