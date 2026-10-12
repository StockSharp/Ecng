namespace Ecng.Drawing;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Ecng.Drawing.Codecs.Jpeg;

internal static class JpegInputColorConverter
{
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static bool TryReadBlock(RasterImage image, int left, int top, Span<double> y, Span<double> cb, Span<double> cr)
	{
#if NET7_0_OR_GREATER
		if (!Avx.IsSupported || left + 8 > image.Width || top + 8 > image.Height) return false;
		ref var pixels = ref MemoryMarshal.GetArrayDataReference(image.Pixels);
		for (var row = 0; row < 8; row++)
		{
			var packed = Vector256.LoadUnsafe(ref pixels, (nuint)(((top + row) * image.Width + left) * 4)).AsUInt32();
			Convert(packed.GetLower(), y, cb, cr, row * 8);
			Convert(packed.GetUpper(), y, cb, cr, row * 8 + 4);
		}
		return true;
#else
		return false;
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static bool TryReadBlock(RasterImage image, int left, int top, ref JpegBlock8x8F y, ref JpegBlock8x8F cb, ref JpegBlock8x8F cr)
	{
#if NET7_0_OR_GREATER
		if (!Avx.IsSupported || left + 8 > image.Width || top + 8 > image.Height) return false;
		ref var pixels = ref MemoryMarshal.GetArrayDataReference(image.Pixels);
		ref float yStart = ref Unsafe.As<JpegBlock8x8F, float>(ref y);
		ref float cbStart = ref Unsafe.As<JpegBlock8x8F, float>(ref cb);
		ref float crStart = ref Unsafe.As<JpegBlock8x8F, float>(ref cr);
		for (var row = 0; row < 8; row++)
			for (var column = 0; column < 8; column += 4)
			{
				var packed = Vector128.LoadUnsafe(ref pixels, (nuint)(((top + row) * image.Width + left + column) * 4)).AsUInt32();
				ConvertPixels(packed, out var luma, out var blue, out var red);
				var at = row * 8 + column;
				Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref Unsafe.Add(ref yStart, at)), Avx.ConvertToVector128Single(luma));
				Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref Unsafe.Add(ref cbStart, at)), Avx.ConvertToVector128Single(blue));
				Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref Unsafe.Add(ref crStart, at)), Avx.ConvertToVector128Single(red));
			}
		return true;
#else
		return false;
#endif
	}

#if NET7_0_OR_GREATER
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Convert(Vector128<uint> pixels, Span<double> y, Span<double> cb, Span<double> cr, int offset)
	{
		ConvertPixels(pixels, out var luma, out var blue, out var red);
		luma.StoreUnsafe(ref MemoryMarshal.GetReference(y), (nuint)offset);
		blue.StoreUnsafe(ref MemoryMarshal.GetReference(cb), (nuint)offset);
		red.StoreUnsafe(ref MemoryMarshal.GetReference(cr), (nuint)offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ConvertPixels(Vector128<uint> pixels, out Vector256<double> luma,
		out Vector256<double> chromaBlue, out Vector256<double> chromaRed)
	{
		var mask = Vector128.Create(255u);
		var red = Widen(pixels & mask);
		var green = Widen(Vector128.ShiftRightLogical(pixels, 8) & mask);
		var blue = Widen(Vector128.ShiftRightLogical(pixels, 16) & mask);
		var alphaBytes = Vector128.ShiftRightLogical(pixels, 24);
		if (!Vector128.EqualsAll(alphaBytes, mask))
		{
			var alpha = Widen(alphaBytes);
			var maximum = Vector256.Create(255.0);
			var white = maximum * (maximum - alpha);
			red = (red * alpha + white) / maximum;
			green = (green * alpha + white) / maximum;
			blue = (blue * alpha + white) / maximum;
		}
		luma = Vector256.Create(0.299) * red + Vector256.Create(0.587) * green + Vector256.Create(0.114) * blue - Vector256.Create(128.0);
		chromaBlue = Vector256.Create(-0.168736) * red - Vector256.Create(0.331264) * green + Vector256.Create(0.5) * blue;
		chromaRed = Vector256.Create(0.5) * red - Vector256.Create(0.418688) * green - Vector256.Create(0.081312) * blue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<double> Widen(Vector128<uint> values)
		=> Avx.ConvertToVector256Double(values.AsInt32());
#endif
}
