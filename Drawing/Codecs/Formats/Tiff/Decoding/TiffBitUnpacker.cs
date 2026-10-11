namespace Ecng.Drawing.Codecs.Formats.Tiff.Decoding;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

internal static class TiffBitUnpacker
{
	public static void Unpack(ReadOnlySpan<byte> packedRow, int bitDepth, int sampleCount, TiffByteOrders byteOrder, Span<ushort> samplesOut)
	{
		switch (bitDepth)
		{
			case 16:
				UnpackWord(packedRow, sampleCount, byteOrder, samplesOut);
				return;

			case 8:
				UnpackByte(packedRow, sampleCount, samplesOut);
				return;

			case 4:
			case 2:
			case 1:
				UnpackSubByte(packedRow, bitDepth, sampleCount, samplesOut);
				return;

			default:
				throw new TiffDecodingException($"Unsupported TIFF bit depth {bitDepth}.");
		}
	}
	private static void UnpackWord(ReadOnlySpan<byte> packedRow, int sampleCount, TiffByteOrders byteOrder, Span<ushort> samplesOut)
	{
		var wordRow = packedRow[..(sampleCount * 2)];

		if (byteOrder == TiffByteOrders.LittleEndian)
		{
			MemoryMarshal.Cast<byte, ushort>(wordRow).CopyTo(samplesOut);
			return;
		}

		SwapBigEndian16(wordRow, samplesOut);
	}

	// Reverses each adjacent byte pair within a 16-byte vector (8 ushorts): byte i <-> byte i^1. Exactly the
	// same shuffle Png16BitByteSwap uses for its (always-big-endian) PNG rows. Deliberately Vector128 only,
	// not also Vector256: measured a Vector256 tier using this same mask replicated across both 128-bit
	// lanes and it produced wrong output for the *second* lane -- .NET's portable Vector256.Shuffle does not
	// guarantee AVX2 vpshufb's per-128-bit-lane semantics the way raw platform intrinsics would, so indices
	// valid for one lane aren't safe to reuse verbatim for the other. Matches this codebase's existing
	// documented call not to reach for Vector256 on shuffle-based kernels (see
	// PixelFormatConversionKernels's remarks on AVX2's vpshufb not crossing the 128-bit lane boundary).
	private static readonly Vector128<byte> SwapPairsShuffle128 =
		Vector128.Create((byte)1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14);

	private static void SwapBigEndian16(ReadOnlySpan<byte> bigEndianBytes, Span<ushort> samplesOut)
	{
		var destBytes = MemoryMarshal.Cast<ushort, byte>(samplesOut);
		int i = 0;

#if NET7_0_OR_GREATER
		if (Vector128.IsHardwareAccelerated)
		{
			for (; i + 16 <= bigEndianBytes.Length; i += 16)
			{
				var v = Vector128.LoadUnsafe(in bigEndianBytes[i]);
				Vector128.Shuffle(v, SwapPairsShuffle128).StoreUnsafe(ref destBytes[i]);
			}
		}
#endif

		for (; i < bigEndianBytes.Length; i += 2)
		{
			destBytes[i] = bigEndianBytes[i + 1];
			destBytes[i + 1] = bigEndianBytes[i];
		}
	}
	private static void UnpackByte(ReadOnlySpan<byte> packedRow, int sampleCount, Span<ushort> samplesOut)
	{
		int i = 0;

#if NET7_0_OR_GREATER
		if (Vector256.IsHardwareAccelerated)
		{
			for (; i + 32 <= sampleCount; i += 32)
			{
				var bytes = Vector256.LoadUnsafe(in packedRow[i]);
				var (lower, upper) = Vector256.Widen(bytes);
				lower.StoreUnsafe(ref samplesOut[i]);
				upper.StoreUnsafe(ref samplesOut[i + 16]);
			}
		}
		else if (Vector128.IsHardwareAccelerated)
		{
			for (; i + 16 <= sampleCount; i += 16)
			{
				var bytes = Vector128.LoadUnsafe(in packedRow[i]);
				Vector128.WidenLower(bytes).StoreUnsafe(ref samplesOut[i]);
				Vector128.WidenUpper(bytes).StoreUnsafe(ref samplesOut[i + 8]);
			}
		}
#endif

		for (; i < sampleCount; i++)
		{
			samplesOut[i] = packedRow[i];
		}
	}

	private static void UnpackSubByte(ReadOnlySpan<byte> packedRow, int bitDepth, int sampleCount, Span<ushort> samplesOut)
	{
		int samplesPerByte = 8 / bitDepth;
		byte mask = (byte)((1 << bitDepth) - 1);

		for (int i = 0; i < sampleCount; i++)
		{
			int byteIndex = i / samplesPerByte;
			int sampleInByte = i % samplesPerByte;
			int shift = 8 - bitDepth - (sampleInByte * bitDepth);
			samplesOut[i] = (ushort)((packedRow[byteIndex] >> shift) & mask);
		}
	}
}
