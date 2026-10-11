namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

internal sealed class Vp8BoolDecoder
{
	private const int RefillBits = 56;

	private readonly byte[] _buffer;
	private readonly int _start;
	private readonly int _length;

	private int _pos;
	private ulong _value;
	private int _bits;
	private uint _range;

	public Vp8BoolDecoder(byte[] buffer, int start, int length)
	{
		ArgumentNullException.ThrowIfNull(buffer);

		// Bounds are already validated by both callers (Vp8FrameHeader.Parse for partition 0, Vp8Partitions.Build
		// for the coefficient partitions), but they are re-checked here because GetBit's bulk refill reads eight
		// bytes at a time: this one check is what makes that read safe without having to re-audit every caller.
		if (start < 0 || length < 0 || start > buffer.Length - length)
		{
			throw new WebpDecodingException($"VP8 bool decoder range [{start}, {start + length}) lies outside the {buffer.Length}-byte chunk.");
		}

		_buffer = buffer;
		_start = start;
		_length = length;
		_pos = 0;

		// -8 rather than 0: the first GetBit triggers the initial load, replacing the reference decoder's eager
		// two-byte prime. Same bits, just fetched lazily.
		_bits = -8;
		_range = 255 - 1;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetBit(int probability)
	{
		// Read the range into a local *before* the refill call. libwebp flags the same ordering as load-bearing
		// for speed, and for the same reason: it keeps the value in a register across a call that never touches it.
		uint range = _range;
		if (_bits < 0)
		{
			LoadNewBytes();
		}

		int pos = _bits;
		uint split = (range * (uint)probability) >> 8;
		uint value = (uint)(_value >> pos);

		int bit;
		if (value > split)
		{
			bit = 1;
			range -= split;
			_value -= (ulong)(split + 1) << pos;
		}
		else
		{
			bit = 0;
			range = split + 1;
		}

		Debug.Assert(range is >= 1 and <= 255, "The coder's true range stays within [1,255], which is what bounds the renormalization shift to [0,7].");

		int shift = 7 - BitOperations.Log2(range);
		range <<= shift;
		_bits -= shift;
		_range = range - 1;

		return bit;
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void LoadNewBytes()
	{
		if (_pos + (sizeof(ulong) - 1) < _length)
		{
			ulong incoming = BinaryPrimitives.ReadUInt64BigEndian(_buffer.AsSpan(_start + _pos, sizeof(ulong)));
			_pos += RefillBits >> 3;
			_value = (incoming >> (64 - RefillBits)) | (_value << RefillBits);
			_bits += RefillBits;
			return;
		}

		LoadFinalBytes();
	}
	private void LoadFinalBytes()
	{
		// Reading a real byte and synthesizing a zero one are the same operation with a different incoming byte:
		// shift the register up and drop the new byte into the low 8 bits.
		ulong incoming = _pos < _length ? _buffer[_start + _pos++] : 0u;
		_value = incoming | (_value << 8);
		_bits += 8;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool GetFlag() => GetBit(128) != 0;
	public uint GetValue(int numBits)
	{
		uint v = 0;
		for (int i = 0; i < numBits; i++)
		{
			v = (v << 1) | (uint)GetBit(128);
		}

		return v;
	}
	public int GetSignedValue(int numBits)
	{
		int magnitude = (int)GetValue(numBits);
		return GetFlag() ? -magnitude : magnitude;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTreeIndex(ReadOnlySpan<sbyte> tree, ReadOnlySpan<byte> probabilities, int start = 0)
	{
		int i = start;
		int next;
		while ((next = tree[i + GetBit(probabilities[i >> 1])]) > 0)
		{
			i = next;
		}

		return -next;
	}
}
