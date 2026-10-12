namespace Ecng.Drawing;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal static class JpegCoefficientQuantizer
{
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static void Write(ReadOnlySpan<float> source, ReadOnlySpan<double> factors, Span<short> destination)
	{
		var zigzag = JpegTransforms.ZigZag;
		if (Avx.IsSupported)
		{
			ref float sourceStart = ref MemoryMarshal.GetReference(source);
			ref double factorsStart = ref MemoryMarshal.GetReference(factors);
			ref short destinationStart = ref MemoryMarshal.GetReference(destination);
			for (var i = 0; i < 64; i += 4)
			{
				var samples = Vector128.Create(Unsafe.Add(ref sourceStart, zigzag[i]), Unsafe.Add(ref sourceStart, zigzag[i + 1]),
					Unsafe.Add(ref sourceStart, zigzag[i + 2]), Unsafe.Add(ref sourceStart, zigzag[i + 3]));
				var scale = Unsafe.ReadUnaligned<Vector256<double>>(ref Unsafe.As<double, byte>(ref Unsafe.Add(ref factorsStart, i)));
				var values = Avx.Multiply(Avx.ConvertToVector256Double(samples), scale);
				var rounding = Avx.Or(Avx.And(values, Vector256.Create(-0.0)), Vector256.Create(0.5));
				var integers = Avx.ConvertToVector128Int32WithTruncation(Avx.Add(values, rounding));
				var outside = Sse2.Or(Sse2.CompareGreaterThan(integers, Vector128.Create((int)short.MaxValue)),
					Sse2.CompareGreaterThan(Vector128.Create((int)short.MinValue), integers));
				if (Sse2.MoveMask(outside.AsByte()) != 0)
					throw new OverflowException("JPEG coefficient exceeds the supported range.");
				var packed = Sse2.PackSignedSaturate(integers, Vector128<int>.Zero).AsUInt64().ToScalar();
				Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref Unsafe.Add(ref destinationStart, i)), packed);
			}
			return;
		}
		for (var i = 0; i < 64; i++)
		{
			var value = source[zigzag[i]] * factors[i];
			destination[i] = checked((short)(int)(value + (value < 0 ? -0.5 : 0.5)));
		}
	}
}
