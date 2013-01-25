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
	}
}