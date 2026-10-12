namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

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
using System.Runtime.CompilerServices;

internal sealed class Vp8LBitReader
{
	private readonly byte[] _data;
	private readonly int _length;
	private int _bytePos;
	private ulong _bitBuffer;
	private int _bitCount;
	private long _bitPosition;

	private readonly long _totalBits;
	public Vp8LBitReader(byte[] data, int startOffset, int endOffset)
	{
		_data = data;
		_bytePos = startOffset;
		_length = endOffset;
		_totalBits = (long)(endOffset - startOffset) * 8;
	}
	public bool IsOverBudget => _bitPosition - _bitCount > _totalBits;
	public Vp8LBitReader Clone() => (Vp8LBitReader)MemberwiseClone();
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint PeekBits(int bitCount)
	{
		if (bitCount == 0)
		{
			return 0;
		}

		EnsureBits(bitCount);
		return (uint)(_bitBuffer & ((1UL << bitCount) - 1));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SkipBits(int bitCount)
	{
		_bitBuffer >>= bitCount;
		_bitCount -= bitCount;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint ReadBits(int bitCount)
	{
		uint value = PeekBits(bitCount);
		SkipBits(bitCount);
		return value;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnsureBits(int bitCount)
	{
		if (_bitCount < bitCount)
		{
			Refill(bitCount);
		}
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Refill(int bitCount)
	{
		Debug.Assert(bitCount is > 0 and <= 32, "VP8L never requests more than 32 bits at once.");
		Debug.Assert(_bitCount < 32, "A refill below 32 buffered bits is what guarantees the 32-bit word fits.");
		Debug.Assert(_bitCount == 64 || (_bitBuffer >> _bitCount) == 0, "Bits above _bitCount must be zero for the OR below to be lossless.");

		if (_bytePos + 4 <= _length)
		{
			_bitBuffer |= (ulong)BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_bytePos, 4)) << _bitCount;
			_bytePos += 4;
			_bitCount += 32;
			_bitPosition += 32;
			return;
		}

		// Tail-byte fallback: top up one byte at a time (real bytes while available, then implicit zero
		// padding once the buffer is exhausted) until there are enough bits to satisfy the request.
		while (_bitCount < bitCount)
		{
			byte next = _bytePos < _length ? _data[_bytePos++] : (byte)0;
			_bitBuffer |= (ulong)next << _bitCount;
			_bitCount += 8;
			_bitPosition += 8;
		}
	}
}
