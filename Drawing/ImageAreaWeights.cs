namespace Ecng.Drawing;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Numerics;

internal static class ImageAreaWeights
{
	public static void Add(ReadOnlySpan<double> source, Span<double> destination, long weight, bool isFirstContribution)
	{
		if (destination.Length < source.Length)
			throw new ArgumentException("Area sums do not fit the destination.", nameof(destination));
		var index = 0;
		if (Avx.IsSupported)
		{
			var factor = Vector256.Create((double)weight);
			ref double sourceStart = ref MemoryMarshal.GetReference(source);
			ref double destinationStart = ref MemoryMarshal.GetReference(destination);
			for (; index + 4 <= source.Length; index += 4)
			{
				var input = Unsafe.ReadUnaligned<Vector256<double>>(ref Unsafe.As<double, byte>(ref Unsafe.Add(ref sourceStart, index)));
				ref byte target = ref Unsafe.As<double, byte>(ref Unsafe.Add(ref destinationStart, index));
				var value = Avx.Multiply(input, factor);
				if (!isFirstContribution)
					value = Avx.Add(Unsafe.ReadUnaligned<Vector256<double>>(ref target), value);
				Unsafe.WriteUnaligned(ref target, value);
			}
		}
		for (; index < source.Length; index++)
		{
			var value = source[index] * weight;
			destination[index] = isFirstContribution ? value : destination[index] + value;
		}
	}

	public static void WriteRow(ReadOnlySpan<double> sums, Span<byte> pixels, double area)
	{
		if (sums.Length != pixels.Length || sums.Length % 4 != 0)
			throw new ArgumentException("Area sums must describe complete RGBA pixels.", nameof(sums));
		for (var at = 0; at < sums.Length; at += 4)
		{
			var alpha = sums[at];
			if (Avx.IsSupported)
			{
				ref double start = ref MemoryMarshal.GetReference(sums);
				var values = Unsafe.ReadUnaligned<Vector256<double>>(ref Unsafe.As<double, byte>(ref Unsafe.Add(ref start, at)));
				var denominator = alpha > 0 ? alpha : 1;
				var result = Avx.RoundToNearestInteger(Avx.Divide(values, Vector256.Create(area, denominator, denominator, denominator)));
				var integers = Avx.ConvertToVector128Int32WithTruncation(result);
				var words = Sse2.PackSignedSaturate(integers, Vector128<int>.Zero);
				var bytes = Sse2.PackUnsignedSaturate(words, Vector128<short>.Zero).AsUInt32().ToScalar();
				Unsafe.WriteUnaligned(ref pixels[at], BitOperations.RotateRight(bytes, 8));
			}
			else
			{
				if (alpha > 0)
				{
					pixels[at] = RasterImage.ToByte(sums[at + 1] / alpha);
					pixels[at + 1] = RasterImage.ToByte(sums[at + 2] / alpha);
					pixels[at + 2] = RasterImage.ToByte(sums[at + 3] / alpha);
				}
				pixels[at + 3] = RasterImage.ToByte(alpha / area);
			}
		}
	}
}
