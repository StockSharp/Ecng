namespace Ecng.Collections
{
	using System;
	using System.IO;

	using Ecng.Common;

	public class BitArrayWriter : Disposable
	{
		private readonly Stream _underlyingStream;
		private int _temp;
		private int _bitOffset;

		public BitArrayWriter(Stream underlyingStream)
		{
			_underlyingStream = underlyingStream ?? throw new ArgumentNullException(nameof(underlyingStream));
		}

		private void Flush()
		{
			_underlyingStream.WriteByte((byte)_temp);
			_temp = 0;
			_bitOffset = 0;
		}

		protected override void DisposeManaged()
		{
			if (_bitOffset > 0)
				Flush();

			base.DisposeManaged();
		}

		public void Write(bool bit)
		{
			_temp |= ((bit ? 1 : 0) << _bitOffset);

			_bitOffset++;

			if (_bitOffset < 8)
				return;

			Flush();
		}

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

								if (value <= 16777216) // 24 бита
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

		public void WriteBits(int value, int bitCount)
		{
			for (var i = 0; i < bitCount; i++)
				Write((value & (1 << i)) != 0);
		}

		public void WriteBits(long value, int bitCount)
		{
			for (var i = 0; i < bitCount; i++)
				Write((value & (1L << i)) != 0);
		}

		public void WriteDecimal(decimal value)
		{
			if (value < 0)
			{
				value = -value;
				Write(false);
			}
			else
				Write(true);
			
			var bits = value.To<int[]>();

			WriteInt(bits[0]);
			WriteInt(bits[1]);
			WriteInt(bits[2]);
			WriteInt((bits[3] >> 16) & 0xff);
		}
	}
}