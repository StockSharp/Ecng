namespace Ecng.Drawing.Codecs.Jpeg;

// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.
// Eight-column adaptation of FastFloatingPointDCT.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal static class JpegVectorDct
{
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static bool TryTransform(ref JpegBlock8x8F source, ref JpegBlock8x8F destination, ref JpegBlock8x8F scratch, bool inverse, bool offset)
	{
		if (!Avx.IsSupported)
			return false;
		source.TransposeInto(ref scratch);
		if (offset)
			for (var i = 0; i < 8; i++)
				Row(ref scratch, i) = Add(Row(ref scratch, i), Vector256.Create(-128f));
		if (inverse)
			InverseColumns(ref scratch, ref destination);
		else
			ForwardColumns(ref scratch, ref destination);
		destination.TransposeInto(ref scratch);
		if (inverse)
			InverseColumns(ref scratch, ref destination);
		else
			ForwardColumns(ref scratch, ref destination);
		for (var i = 0; i < 8; i++)
			Row(ref destination, i) = Mul(Row(ref destination, i), 0.125f);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Transpose(ref JpegBlock8x8F source, ref JpegBlock8x8F destination)
	{
		var a0 = Avx.UnpackLow(Row(ref source, 0), Row(ref source, 1));
		var a1 = Avx.UnpackHigh(Row(ref source, 0), Row(ref source, 1));
		var a2 = Avx.UnpackLow(Row(ref source, 2), Row(ref source, 3));
		var a3 = Avx.UnpackHigh(Row(ref source, 2), Row(ref source, 3));
		var a4 = Avx.UnpackLow(Row(ref source, 4), Row(ref source, 5));
		var a5 = Avx.UnpackHigh(Row(ref source, 4), Row(ref source, 5));
		var a6 = Avx.UnpackLow(Row(ref source, 6), Row(ref source, 7));
		var a7 = Avx.UnpackHigh(Row(ref source, 6), Row(ref source, 7));
		var b0 = Avx.Shuffle(a0, a2, 0x44);
		var b1 = Avx.Shuffle(a0, a2, 0xee);
		var b2 = Avx.Shuffle(a1, a3, 0x44);
		var b3 = Avx.Shuffle(a1, a3, 0xee);
		var b4 = Avx.Shuffle(a4, a6, 0x44);
		var b5 = Avx.Shuffle(a4, a6, 0xee);
		var b6 = Avx.Shuffle(a5, a7, 0x44);
		var b7 = Avx.Shuffle(a5, a7, 0xee);
		Row(ref destination, 0) = Avx.Permute2x128(b0, b4, 0x20);
		Row(ref destination, 1) = Avx.Permute2x128(b1, b5, 0x20);
		Row(ref destination, 2) = Avx.Permute2x128(b2, b6, 0x20);
		Row(ref destination, 3) = Avx.Permute2x128(b3, b7, 0x20);
		Row(ref destination, 4) = Avx.Permute2x128(b0, b4, 0x31);
		Row(ref destination, 5) = Avx.Permute2x128(b1, b5, 0x31);
		Row(ref destination, 6) = Avx.Permute2x128(b2, b6, 0x31);
		Row(ref destination, 7) = Avx.Permute2x128(b3, b7, 0x31);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref Vector256<float> Row(ref JpegBlock8x8F block, int row)
		=> ref Unsafe.Add(ref Unsafe.As<JpegBlock8x8F, Vector256<float>>(ref block), row);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<float> Add(Vector256<float> left, Vector256<float> right) => Avx.Add(left, right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<float> Sub(Vector256<float> left, Vector256<float> right) => Avx.Subtract(left, right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<float> Mul(Vector256<float> value, float factor) => Avx.Multiply(value, Vector256.Create(factor));

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void ForwardColumns(ref JpegBlock8x8F source, ref JpegBlock8x8F destination)
	{
		var t0 = Add(Row(ref source, 0), Row(ref source, 7));
		var t7 = Sub(Row(ref source, 0), Row(ref source, 7));
		var t1 = Add(Row(ref source, 1), Row(ref source, 6));
		var t6 = Sub(Row(ref source, 1), Row(ref source, 6));
		var t2 = Add(Row(ref source, 2), Row(ref source, 5));
		var t5 = Sub(Row(ref source, 2), Row(ref source, 5));
		var t3 = Add(Row(ref source, 3), Row(ref source, 4));
		var t4 = Sub(Row(ref source, 3), Row(ref source, 4));
		var c0 = Add(t0, t3);
		var c3 = Sub(t0, t3);
		var c1 = Add(t1, t2);
		var c2 = Sub(t1, t2);
		Row(ref destination, 0) = Add(c0, c1);
		Row(ref destination, 4) = Sub(c0, c1);
		Row(ref destination, 2) = Add(Mul(c2, 0.541196f), Mul(c3, 1.306563f));
		Row(ref destination, 6) = Sub(Mul(c3, 0.541196f), Mul(c2, 1.306563f));
		c3 = Add(Mul(t4, 1.175876f), Mul(t7, 0.785695f));
		c0 = Sub(Mul(t7, 1.175876f), Mul(t4, 0.785695f));
		c2 = Add(Mul(t5, 1.387040f), Mul(t6, 0.275899f));
		c1 = Sub(Mul(t6, 1.387040f), Mul(t5, 0.275899f));
		Row(ref destination, 3) = Sub(c0, c2);
		Row(ref destination, 5) = Sub(c3, c1);
		c0 = Mul(Add(c0, c2), 0.707107f);
		c3 = Mul(Add(c3, c1), 0.707107f);
		Row(ref destination, 1) = Add(c0, c3);
		Row(ref destination, 7) = Sub(c0, c3);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void InverseColumns(ref JpegBlock8x8F source, ref JpegBlock8x8F destination)
	{
		var my1 = Row(ref source, 1);
		var my7 = Row(ref source, 7);
		var mz0 = Add(my1, my7);
		var my3 = Row(ref source, 3);
		var mz2 = Add(my3, my7);
		var my5 = Row(ref source, 5);
		var mz1 = Add(my3, my5);
		var mz3 = Add(my1, my5);
		var mz4 = Mul(Add(mz0, mz1), 1.175875602f);
		mz2 = Add(Mul(mz2, -1.961570560f), mz4);
		mz3 = Add(Mul(mz3, -0.390180644f), mz4);
		mz0 = Mul(mz0, -0.899976223f);
		mz1 = Mul(mz1, -2.562915447f);
		var mb3 = Add(Add(Mul(my7, 0.298631336f), mz0), mz2);
		var mb2 = Add(Add(Mul(my5, 2.053119869f), mz1), mz3);
		var mb1 = Add(Add(Mul(my3, 3.072711026f), mz1), mz2);
		var mb0 = Add(Add(Mul(my1, 1.501321110f), mz0), mz3);
		var my2 = Row(ref source, 2);
		var my6 = Row(ref source, 6);
		mz4 = Mul(Add(my2, my6), 0.541196100f);
		var my0 = Row(ref source, 0);
		var my4 = Row(ref source, 4);
		mz0 = Add(my0, my4);
		mz1 = Sub(my0, my4);
		mz2 = Add(mz4, Mul(my6, -1.847759065f));
		mz3 = Add(mz4, Mul(my2, 0.765366865f));
		my0 = Add(mz0, mz3);
		my3 = Sub(mz0, mz3);
		my1 = Add(mz1, mz2);
		my2 = Sub(mz1, mz2);
		Row(ref destination, 0) = Add(my0, mb0);
		Row(ref destination, 7) = Sub(my0, mb0);
		Row(ref destination, 1) = Add(my1, mb1);
		Row(ref destination, 6) = Sub(my1, mb1);
		Row(ref destination, 2) = Add(my2, mb2);
		Row(ref destination, 5) = Sub(my2, mb2);
		Row(ref destination, 3) = Add(my3, mb3);
		Row(ref destination, 4) = Sub(my3, mb3);
	}
}
