namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal static class JpegRasterSamples
{
	public static void Write(ReadOnlySpan<float> samples, byte[] pixels, int stride, int left, int top)
	{
		if (Avx.IsSupported)
		{
			ref var source = ref MemoryMarshal.GetReference(samples);
			for (var y = 0; y < 8; y++)
			{
				var input = Unsafe.ReadUnaligned<Vector256<float>>(ref Unsafe.As<float, byte>(ref Unsafe.Add(ref source, y * 8)));
				var values = Avx.Add(input, Vector256.Create(128f));
				values = Avx.Min(Avx.Max(values, Vector256<float>.Zero), Vector256.Create(255f));
				var integers = Avx.ConvertToVector256Int32WithTruncation(Avx.RoundToNearestInteger(values));
				var shorts = Sse2.PackSignedSaturate(integers.GetLower(), integers.GetUpper());
				var bytes = Sse2.PackUnsignedSaturate(shorts, Vector128<short>.Zero).AsUInt64().ToScalar();
				BinaryPrimitives.WriteUInt64LittleEndian(pixels.AsSpan((top + y) * stride + left, 8), bytes);
			}
			return;
		}
		for (var y = 0; y < 8; y++)
			for (var x = 0; x < 8; x++) pixels[(top + y) * stride + left + x] = RasterImage.ToByte(samples[y * 8 + x] + 128);
	}
}
