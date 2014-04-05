namespace Ecng.Collections
{
	using System;
	using System.Collections;

	public class BitArrayReader
	{
		public BitArrayReader(BitArray bitArray)
		{
			if (bitArray == null)
				throw new ArgumentNullException("bitArray");

			Data = new ulong[(bitArray.Count / 64) + 2];
			bitArray.CopyTo(Data, 0);
		}

		protected int BitOffset;
		protected int DataOffset;
		[CLSCompliant(false)]
		protected readonly ulong[] Data;

		public BitArrayReader(byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException("bytes");

			Data = new ulong[bytes.Length / 8 + 2];
			Buffer.BlockCopy(bytes, 0, Data, 0, bytes.Length);
		}

		public int Offset
		{
			get { return (DataOffset << 6) | BitOffset; }
			set
			{
				if (value < 0)// || value >= _bits.Length)
					throw new ArgumentOutOfRangeException();

				DataOffset = value >> 6;
				BitOffset = value & 63;
			}
		}

		public bool Read()
		{
			var b = Data[DataOffset];

			var value = ((b >> BitOffset) & 1) != 0;

			if (BitOffset == 63)
			{
				BitOffset = 0;
				DataOffset++;
			}
			else
				BitOffset++;

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
			int newOffset = BitOffset + offset;
			DataOffset += newOffset >> 6;
			BitOffset = newOffset & 63;
		}

		/// <summary>
		/// Просмотреть ближайшие 16 бит. 
		/// </summary>
		/// <returns></returns>
		[CLSCompliant(false)]
		public ulong Lookahead()
		{
			int offset = DataOffset;
			ulong bits = Data[offset];
			int bo = BitOffset;
			bits >>= bo;
			if (bo > 0)
				bits |= Data[offset + 1] << (64 - bo);
			return bits;
		}

		public long ReadLong(int count)
		{
			if (count <= 0 || count > 64)
				throw new ArgumentOutOfRangeException("count", count, "Invalid count value.");

			int offset = DataOffset;
			int bitOffset = BitOffset;

			ulong value = Data[offset] >> bitOffset;

			int shift = 64 - bitOffset;
			if (shift < count)
				value |= Data[offset + 1] << shift;

			bitOffset = bitOffset + count;
			DataOffset += bitOffset >> 6;
			BitOffset = bitOffset & 63;

			value &= ulong.MaxValue >> (64 - count);

			return (long)value;
		}

		public int ReadInt()
		{
			var offset = DataOffset;
			var bits = Data[offset];
			var bitOffset = BitOffset;

			bits >>= bitOffset;

			if (bitOffset > 0)
				bits |= Data[offset + 1] << (64 - bitOffset); // честные 64 бита в битс

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
			DataOffset = offset + (bitOffset >> 6);
			BitOffset = bitOffset & 63;

			return value;
		}

		public long ReadLong()
		{
			var offset = DataOffset;
			var bitOffset = BitOffset;
			var bits = Data[offset] >> bitOffset;

			if (bitOffset > 0)
				bits |= Data[offset + 1] << (64 - bitOffset);

			long value;
			if ((bits & 1) != 0)
			{
				var isPositive = (bits & 2) != 0;

				bitOffset += 2;

				offset += bitOffset >> 6;
				bitOffset &= 63;
				bits = Data[offset] >> bitOffset;

				if (bitOffset > 0)
					bits |= Data[offset + 1] << (64 - bitOffset);

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
			DataOffset = offset;
			BitOffset = bitOffset;

			return value;
		}

		public DateTime ReadTime(DateTime date, DateTime prevTime)
		{
			var offset = DataOffset;
			var bits = Data[offset];
			var bitOffset = BitOffset;

			bits >>= bitOffset;

			if (bitOffset > 0)
				bits |= Data[offset + 1] << (64 - bitOffset); // честные 64 бита в битс

			int seek, value;

			long addTicks = 0;

			if ((bits & 1) != 0)
			{
				var h = (int)((bits >> 1) & (ulong.MaxValue >> 64 - 5));
				var m = (int)((bits >> 6) & (ulong.MaxValue >> 64 - 6));
				var s = (int)((bits >> 12) & (ulong.MaxValue >> 64 - 6));

				bitOffset += 1 + 5 + 6 + 6;
				prevTime = date + new TimeSpan(h, m, s);
			}
			else
			{
				bits >>= 1;
				bitOffset += 1;

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

				//prevTime = prevTime.AddSeconds(value);
				addTicks = value * TimeSpan.TicksPerSecond;
			}

			offset += bitOffset >> 6;

			bitOffset &= 63;

			bits = Data[offset] >> bitOffset;

			if (bitOffset > 0)
				bits |= Data[offset + 1] << (64 - bitOffset);

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

			addTicks += value * TimeSpan.TicksPerMillisecond;
			//prevTime = prevTime.AddMilliseconds(value);
			prevTime = prevTime.AddTicks(addTicks);

			offset += bitOffset >> 6;
			bitOffset &= 63;
			DataOffset = offset;
			BitOffset = bitOffset;

			return prevTime;
		}
	}
}