namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Av1BitReader
{
	private readonly byte[] _data;
	private readonly int _start;
	private readonly int _end;
	private int _bitPosition;

	public Av1BitReader(byte[] data, int start, int length)
	{
		_data = data;
		_start = start;
		_end = start + length;
		_bitPosition = 0;
	}
	public int BytePosition => _start + (_bitPosition >> 3);
	public int BitsRead => _bitPosition;
	public uint ReadBits(int n)
	{
		uint value = 0;
		for (int i = 0; i < n; i++)
		{
			value = (value << 1) | ReadBit();
		}

		return value;
	}
	public bool ReadFlag() => ReadBits(1) != 0;
	public uint ReadUvlc()
	{
		int leadingZeroBits = 0;
		while (leadingZeroBits < 32)
		{
			if (ReadBits(1) != 0)
			{
				break;
			}

			leadingZeroBits++;
		}

		if (leadingZeroBits >= 32)
		{
			return uint.MaxValue;
		}

		uint value = ReadBits(leadingZeroBits);
		return value + (1u << leadingZeroBits) - 1;
	}
	public int ReadSu(int n)
	{
		int value = (int)ReadBits(n);
		int signMask = 1 << (n - 1);
		if ((value & signMask) != 0)
		{
			value -= 2 * signMask;
		}

		return value;
	}
	public uint ReadNs(uint n)
	{
		if (n <= 1)
		{
			return 0;
		}

		int w = FloorLog2(n) + 1;
		uint m = (uint)((1 << w) - n);
		uint v = ReadBits(w - 1);
		if (v < m)
		{
			return v;
		}

		uint extraBit = ReadBits(1);
		return (v << 1) - m + extraBit;
	}
	public void ByteAlign()
	{
		_bitPosition = (_bitPosition + 7) & ~7;
	}

	private uint ReadBit()
	{
		int byteIndex = _start + (_bitPosition >> 3);
		if (byteIndex >= _end)
		{
			throw new AvifDecodingException("AV1 bit reader ran past the end of its data.");
		}

		int bitIndexFromMsb = 7 - (_bitPosition & 7);
		uint bit = (uint)((_data[byteIndex] >> bitIndexFromMsb) & 1);
		_bitPosition++;
		return bit;
	}

	private static int FloorLog2(uint x) => x == 0 ? -1 : System.Numerics.BitOperations.Log2(x);
}
