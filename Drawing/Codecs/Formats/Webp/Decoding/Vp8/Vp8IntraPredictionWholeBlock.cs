namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

internal static class Vp8IntraPredictionWholeBlock
{
	public static void PredictDc(Span<byte> plane, int origin, int stride, int size, bool hasAbove, bool hasLeft)
	{
		int dc;
		if (hasAbove && hasLeft)
		{
			int sum = 0;
			for (int i = 0; i < size; i++)
			{
				sum += plane[origin - stride + i] + plane[origin + (i * stride) - 1];
			}

			int shift = BitOperations.Log2((uint)size) + 1;
			dc = (sum + size) >> shift;
		}
		else if (hasAbove)
		{
			int sum = 0;
			for (int i = 0; i < size; i++)
			{
				sum += plane[origin - stride + i];
			}

			int shift = BitOperations.Log2((uint)size);
			dc = (sum + (size >> 1)) >> shift;
		}
		else if (hasLeft)
		{
			int sum = 0;
			for (int i = 0; i < size; i++)
			{
				sum += plane[origin + (i * stride) - 1];
			}

			int shift = BitOperations.Log2((uint)size);
			dc = (sum + (size >> 1)) >> shift;
		}
		else
		{
			dc = 128;
		}

		byte value = (byte)dc;
		for (int y = 0; y < size; y++)
		{
			plane.Slice(origin + (y * stride), size).Fill(value);
		}
	}

	public static void PredictVertical(Span<byte> plane, int origin, int stride, int size)
	{
		ReadOnlySpan<byte> above = plane.Slice(origin - stride, size);
		for (int y = 0; y < size; y++)
		{
			above.CopyTo(plane.Slice(origin + (y * stride), size));
		}
	}

	public static void PredictHorizontal(Span<byte> plane, int origin, int stride, int size)
	{
		for (int y = 0; y < size; y++)
		{
			byte value = plane[origin + (y * stride) - 1];
			plane.Slice(origin + (y * stride), size).Fill(value);
		}
	}

	public static void PredictTrueMotion(Span<byte> plane, int origin, int stride, int size)
	{
		int corner = plane[origin - stride - 1];

		// Each pixel is clip(above + left - corner); 16-bit lanes hold the widest intermediate (-255..510)
		// exactly, so clamping then narrowing matches ClipByte lane for lane.
#if NET7_0_OR_GREATER
		if (Vector128.IsHardwareAccelerated && size is 16 or 8)
		{
			var aboveBytes = size == 16
				? Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(plane.Slice(origin - stride, 16)))
				: Vector128.CreateScalar(MemoryMarshal.Read<ulong>(plane.Slice(origin - stride, 8))).AsByte();
			var aboveLow = Vector128.WidenLower(aboveBytes).AsInt16();
			var aboveHigh = Vector128.WidenUpper(aboveBytes).AsInt16();
			var zero = Vector128<short>.Zero;
			var max = Vector128.Create((short)255);

			for (int y = 0; y < size; y++)
			{
				int rowOrigin = origin + (y * stride);
				var delta = Vector128.Create((short)(plane[rowOrigin - 1] - corner));
				var low = Vector128.Min(Vector128.Max(aboveLow + delta, zero), max).AsUInt16();
				if (size == 16)
				{
					var high = Vector128.Min(Vector128.Max(aboveHigh + delta, zero), max).AsUInt16();
					Vector128.Narrow(low, high).StoreUnsafe(ref MemoryMarshal.GetReference(plane.Slice(rowOrigin, 16)));
				}
				else
				{
					MemoryMarshal.Write(plane.Slice(rowOrigin, 8), Vector128.Narrow(low, low).AsUInt64().ToScalar());
				}
			}

			return;
		}
#endif

		for (int y = 0; y < size; y++)
		{
			int left = plane[origin + (y * stride) - 1];
			int rowOrigin = origin + (y * stride);
			for (int x = 0; x < size; x++)
			{
				int above = plane[origin - stride + x];
				plane[rowOrigin + x] = ClipByte(above + left - corner);
			}
		}
	}
	public static void PredictModeWholeBlock(int mode, Span<byte> plane, int origin, int stride, int size, bool hasAbove, bool hasLeft)
	{
		switch (mode)
		{
			case Vp8PredictionModes.DcPred:
				PredictDc(plane, origin, stride, size, hasAbove, hasLeft);
				break;
			case Vp8PredictionModes.VPred:
				PredictVertical(plane, origin, stride, size);
				break;
			case Vp8PredictionModes.HPred:
				PredictHorizontal(plane, origin, stride, size);
				break;
			case Vp8PredictionModes.TmPred:
				PredictTrueMotion(plane, origin, stride, size);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown VP8 whole-block prediction mode.");
		}
	}

	internal static byte ClipByte(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
}
