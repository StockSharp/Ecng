namespace Ecng.Collections
{
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Common;

	public class BitArrayWriter
	{
		private readonly List<bool> _bits;

		public BitArrayWriter(int capacity = 0)
		{
			_bits = new List<bool>(capacity);
		}

		public void Write(bool bit)
		{
			_bits.Add(bit);
		}

		public void WriteInt(int value)
		{
			if (value == 0)
				_bits.Add(false);
			else
			{
				_bits.Add(true);

				if (value < 0)
				{
					value = -value;
					_bits.Add(false);
				}
				else
					_bits.Add(true);

				if (value == 1)
					_bits.Add(false);
				else
				{
					_bits.Add(true);
					if (value < 16)
					{
						_bits.Add(false);
						_bits.AddRange(value.ToBits(4));
					}
					else
					{
						_bits.Add(true);
						if (value <= byte.MaxValue)
						{
							_bits.Add(false);
							_bits.AddRange(value.ToBits(8));
						}
						else
						{
							_bits.Add(true);
							if (value <= ushort.MaxValue)
							{
								_bits.Add(false);
								_bits.AddRange(value.ToBits(16));
							}
							else
							{
								_bits.Add(true);
								if (value <= 16777216) // 24 бита
								{
									_bits.Add(false);
									_bits.AddRange(value.ToBits(24));
								}
								else
								{
									_bits.Add(true);
									_bits.AddRange(value.ToBits(32));
								}
							}
						}
					}
				}
			}
		}

		public void WriteLong(long value)
		{
			if (value.Abs() > int.MaxValue)
			{
				_bits.Add(true);
				_bits.Add(value >= 0);
				WriteBits(value.Abs(), 63);
			}
			else
			{
				_bits.Add(false);
				WriteInt((int)value);
			}
		}

		public void WriteBits(int value, int bitCount)
		{
			for (var i = 0; i < bitCount; i++)
				_bits.Add((value & (1 << i)) != 0);
		}

		public void WriteBits(long value, int bitCount)
		{
			for (var i = 0; i < bitCount; i++)
				_bits.Add((value & (1L << i)) != 0);
		}

		public byte[] GetBytes()
		{
			return new BitArray(GetBits()).To<byte[]>();
		}

		public bool[] GetBits()
		{
			return _bits.ToArray();
		}
	}
}