namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using System.Numerics;
using Ecng.Drawing.Codecs.Formats.Webp.Internal;

internal sealed class Vp8BoolEncoder
{
	private byte[] _buffer = WebpBufferPool.Shared.Rent(256);
	private int _length;

	private int _range;

	private int _value;

	private int _run;

	private int _bits;

	public Vp8BoolEncoder()
	{
		_range = 255 - 1;
		_value = 0;
		_run = 0;
		_bits = -8;
	}

	public void PutBit(int bit, int probability)
	{
		int split = (_range * probability) >> 8;
		if (bit != 0)
		{
			_value += split + 1;
			_range -= split + 1;
		}
		else
		{
			_range = split;
		}

		if (_range < 127)
		{
			int trueRange = _range + 1;
			int shift = 7 - BitOperations.Log2((uint)trueRange);
			trueRange <<= shift;
			_range = trueRange - 1;
			_value <<= shift;
			_bits += shift;
			if (_bits > 0)
			{
				Flush();
			}
		}
	}

	public void PutFlag(bool bit) => PutBit(bit ? 1 : 0, 128);

	public void PutValue(uint value, int numBits)
	{
		for (int i = numBits - 1; i >= 0; i--)
		{
			PutBit((int)((value >> i) & 1), 128);
		}
	}

	public void PutSignedValue(int value, int numBits)
	{
		PutValue((uint)Math.Abs(value), numBits);
		PutFlag(value < 0);
	}

	public void PutTreeIndex(ReadOnlySpan<sbyte> tree, ReadOnlySpan<byte> probabilities, int value, int start = 0)
	{
		Span<int> nodePath = stackalloc int[16];
		Span<int> bitPath = stackalloc int[16];
		int depth = 0;
		bool found = FindPath(tree, start, (sbyte)(-value), nodePath, bitPath, ref depth);
		if (!found)
		{
			throw new ArgumentOutOfRangeException(nameof(value), value, "Value is not a leaf of the given tree.");
		}

		for (int i = 0; i < depth; i++)
		{
			PutBit(bitPath[i], probabilities[nodePath[i] >> 1]);
		}
	}

	private static bool FindPath(ReadOnlySpan<sbyte> tree, int nodeIndex, sbyte targetLeaf, Span<int> nodePath, Span<int> bitPath, ref int depth)
	{
		for (int branch = 0; branch < 2; branch++)
		{
			sbyte entry = tree[nodeIndex + branch];
			if (entry == targetLeaf)
			{
				nodePath[depth] = nodeIndex;
				bitPath[depth] = branch;
				depth++;
				return true;
			}

			if (entry > 0)
			{
				nodePath[depth] = nodeIndex;
				bitPath[depth] = branch;
				depth++;
				if (FindPath(tree, entry, targetLeaf, nodePath, bitPath, ref depth))
				{
					return true;
				}

				depth--;
			}
		}

		return false;
	}

	private void Flush()
	{
		int s = 8 + _bits;
		int bits = _value >> s;
		_value -= bits << s;
		_bits -= 8;

		if ((bits & 0xff) != 0xff)
		{
			if ((bits & 0x100) != 0 && _length > 0)
			{
				_buffer[_length - 1]++;
			}

			if (_run > 0)
			{
				byte fill = (bits & 0x100) != 0 ? (byte)0x00 : (byte)0xff;
				EnsureCapacity(_length + _run);
				for (; _run > 0; _run--)
				{
					_buffer[_length++] = fill;
				}
			}

			EnsureCapacity(_length + 1);
			_buffer[_length++] = (byte)(bits & 0xff);
		}
		else
		{
			_run++;
		}
	}

	public byte[] Finish()
	{
		int padBits = 9 - _bits;
		for (int i = 0; i < padBits; i++)
		{
			PutBit(0, 128);
		}

		_bits = 0;
		Flush();

		// Zero padding aligns entropy partitions on a two-byte boundary.
		byte[] result = new byte[(_length + 1) & ~1];
		_buffer.AsSpan(0, _length).CopyTo(result);
		WebpBufferPool.Shared.Return(_buffer);
		return result;
	}

	private void EnsureCapacity(int required)
	{
		if (required <= _buffer.Length)
		{
			return;
		}

		int newSize = _buffer.Length * 2;
		while (newSize < required)
		{
			newSize *= 2;
		}

		byte[] newBuffer = WebpBufferPool.Shared.Rent(newSize);
		_buffer.AsSpan(0, _length).CopyTo(newBuffer);
		WebpBufferPool.Shared.Return(_buffer);
		_buffer = newBuffer;
	}
}
