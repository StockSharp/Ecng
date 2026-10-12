namespace Ecng.Drawing.Codecs.Jpeg;


using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal struct JpegBlock8x8F
{
	public const int Size = 64;

	public Vector4 V0L;
	public Vector4 V0R;

	public Vector4 V1L;
	public Vector4 V1R;

	public Vector4 V2L;
	public Vector4 V2R;

	public Vector4 V3L;
	public Vector4 V3R;

	public Vector4 V4L;
	public Vector4 V4R;

	public Vector4 V5L;
	public Vector4 V5R;

	public Vector4 V6L;
	public Vector4 V6R;

	public Vector4 V7L;
	public Vector4 V7R;

	public float this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if ((uint)index >= Size)
			{
				ThrowArgumentOutOfRangeException(nameof(index));
			}
			ref float selfRef = ref Unsafe.As<JpegBlock8x8F, float>(ref this);
			return Unsafe.Add(ref selfRef, index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			if ((uint)index >= Size)
			{
				ThrowArgumentOutOfRangeException(nameof(index));
			}
			ref float selfRef = ref Unsafe.As<JpegBlock8x8F, float>(ref this);
			Unsafe.Add(ref selfRef, index) = value;
		}
	}

	public float this[int x, int y]
	{
		get => this[(y * 8) + x];
		set => this[(y * 8) + x] = value;
	}

	public static JpegBlock8x8F operator *(in JpegBlock8x8F block, float value)
	{
		ref JpegBlock8x8F blockRef = ref Unsafe.AsRef(in block);
		JpegBlock8x8F result = block;
		result.V0L = Vector4.Multiply(blockRef.V0L, value);
		result.V0R = Vector4.Multiply(blockRef.V0R, value);
		result.V1L = Vector4.Multiply(blockRef.V1L, value);
		result.V1R = Vector4.Multiply(blockRef.V1R, value);
		result.V2L = Vector4.Multiply(blockRef.V2L, value);
		result.V2R = Vector4.Multiply(blockRef.V2R, value);
		result.V3L = Vector4.Multiply(blockRef.V3L, value);
		result.V3R = Vector4.Multiply(blockRef.V3R, value);
		result.V4L = Vector4.Multiply(blockRef.V4L, value);
		result.V4R = Vector4.Multiply(blockRef.V4R, value);
		result.V5L = Vector4.Multiply(blockRef.V5L, value);
		result.V5R = Vector4.Multiply(blockRef.V5R, value);
		result.V6L = Vector4.Multiply(blockRef.V6L, value);
		result.V6R = Vector4.Multiply(blockRef.V6R, value);
		result.V7L = Vector4.Multiply(blockRef.V7L, value);
		result.V7R = Vector4.Multiply(blockRef.V7R, value);
		return result;
	}

	public static JpegBlock8x8F operator /(in JpegBlock8x8F block, float value)
	{
		ref JpegBlock8x8F blockRef = ref Unsafe.AsRef(in block);
		JpegBlock8x8F result = block;
		result.V0L = Vector4.Divide(blockRef.V0L, value);
		result.V0R = Vector4.Divide(blockRef.V0R, value);
		result.V1L = Vector4.Divide(blockRef.V1L, value);
		result.V1R = Vector4.Divide(blockRef.V1R, value);
		result.V2L = Vector4.Divide(blockRef.V2L, value);
		result.V2R = Vector4.Divide(blockRef.V2R, value);
		result.V3L = Vector4.Divide(blockRef.V3L, value);
		result.V3R = Vector4.Divide(blockRef.V3R, value);
		result.V4L = Vector4.Divide(blockRef.V4L, value);
		result.V4R = Vector4.Divide(blockRef.V4R, value);
		result.V5L = Vector4.Divide(blockRef.V5L, value);
		result.V5R = Vector4.Divide(blockRef.V5R, value);
		result.V6L = Vector4.Divide(blockRef.V6L, value);
		result.V6R = Vector4.Divide(blockRef.V6R, value);
		result.V7L = Vector4.Divide(blockRef.V7L, value);
		result.V7R = Vector4.Divide(blockRef.V7R, value);
		return result;
	}

	public static JpegBlock8x8F operator +(in JpegBlock8x8F block, float value)
	{
		ref JpegBlock8x8F blockRef = ref Unsafe.AsRef(in block);
		JpegBlock8x8F result = block;
		Vector4 valueVector = new Vector4(value);
		result.V0L = Vector4.Add(blockRef.V0L, valueVector);
		result.V0R = Vector4.Add(blockRef.V0R, valueVector);
		result.V1L = Vector4.Add(blockRef.V1L, valueVector);
		result.V1R = Vector4.Add(blockRef.V1R, valueVector);
		result.V2L = Vector4.Add(blockRef.V2L, valueVector);
		result.V2R = Vector4.Add(blockRef.V2R, valueVector);
		result.V3L = Vector4.Add(blockRef.V3L, valueVector);
		result.V3R = Vector4.Add(blockRef.V3R, valueVector);
		result.V4L = Vector4.Add(blockRef.V4L, valueVector);
		result.V4R = Vector4.Add(blockRef.V4R, valueVector);
		result.V5L = Vector4.Add(blockRef.V5L, valueVector);
		result.V5R = Vector4.Add(blockRef.V5R, valueVector);
		result.V6L = Vector4.Add(blockRef.V6L, valueVector);
		result.V6R = Vector4.Add(blockRef.V6R, valueVector);
		result.V7L = Vector4.Add(blockRef.V7L, valueVector);
		result.V7R = Vector4.Add(blockRef.V7R, valueVector);
		return result;
	}

	public static JpegBlock8x8F operator -(in JpegBlock8x8F block, float value)
	{
		ref JpegBlock8x8F blockRef = ref Unsafe.AsRef(in block);
		JpegBlock8x8F result = block;
		Vector4 valueVector = new Vector4(value);
		result.V0L = Vector4.Subtract(blockRef.V0L, valueVector);
		result.V0R = Vector4.Subtract(blockRef.V0R, valueVector);
		result.V1L = Vector4.Subtract(blockRef.V1L, valueVector);
		result.V1R = Vector4.Subtract(blockRef.V1R, valueVector);
		result.V2L = Vector4.Subtract(blockRef.V2L, valueVector);
		result.V2R = Vector4.Subtract(blockRef.V2R, valueVector);
		result.V3L = Vector4.Subtract(blockRef.V3L, valueVector);
		result.V3R = Vector4.Subtract(blockRef.V3R, valueVector);
		result.V4L = Vector4.Subtract(blockRef.V4L, valueVector);
		result.V4R = Vector4.Subtract(blockRef.V4R, valueVector);
		result.V5L = Vector4.Subtract(blockRef.V5L, valueVector);
		result.V5R = Vector4.Subtract(blockRef.V5R, valueVector);
		result.V6L = Vector4.Subtract(blockRef.V6L, valueVector);
		result.V6R = Vector4.Subtract(blockRef.V6R, valueVector);
		result.V7L = Vector4.Subtract(blockRef.V7L, valueVector);
		result.V7R = Vector4.Subtract(blockRef.V7R, valueVector);
		return result;
	}

	public void MultiplyInplace(float value)
	{
		V0L *= value;
		V0R *= value;
		V1L *= value;
		V1R *= value;
		V2L *= value;
		V2R *= value;
		V3L *= value;
		V3R *= value;
		V4L *= value;
		V4R *= value;
		V5L *= value;
		V5R *= value;
		V6L *= value;
		V6R *= value;
		V7L *= value;
		V7R *= value;
	}

	public void MultiplyInplace(ref JpegBlock8x8F other)
	{
		V0L *= other.V0L;
		V0R *= other.V0R;
		V1L *= other.V1L;
		V1R *= other.V1R;
		V2L *= other.V2L;
		V2R *= other.V2R;
		V3L *= other.V3L;
		V3R *= other.V3R;
		V4L *= other.V4L;
		V4R *= other.V4R;
		V5L *= other.V5L;
		V5R *= other.V5R;
		V6L *= other.V6L;
		V6R *= other.V6R;
		V7L *= other.V7L;
		V7R *= other.V7R;
	}

	public void AddToAllInplace(Vector4 diff)
	{
		V0L += diff;
		V0R += diff;
		V1L += diff;
		V1R += diff;
		V2L += diff;
		V2R += diff;
		V3L += diff;
		V3R += diff;
		V4L += diff;
		V4R += diff;
		V5L += diff;
		V5R += diff;
		V6L += diff;
		V6R += diff;
		V7L += diff;
		V7R += diff;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void TransposeInto(ref JpegBlock8x8F d)
	{
		if (Avx.IsSupported)
		{
			JpegVectorDct.Transpose(ref this, ref d);
			return;
		}
		if (Sse.IsSupported)
		{
			TransposeQuadrant(ref V0L, ref V1L, ref V2L, ref V3L, ref d.V0L, ref d.V1L, ref d.V2L, ref d.V3L);
			TransposeQuadrant(ref V0R, ref V1R, ref V2R, ref V3R, ref d.V4L, ref d.V5L, ref d.V6L, ref d.V7L);
			TransposeQuadrant(ref V4L, ref V5L, ref V6L, ref V7L, ref d.V0R, ref d.V1R, ref d.V2R, ref d.V3R);
			TransposeQuadrant(ref V4R, ref V5R, ref V6R, ref V7R, ref d.V4R, ref d.V5R, ref d.V6R, ref d.V7R);
			return;
		}
		d.V0L.X = V0L.X;
		d.V1L.X = V0L.Y;
		d.V2L.X = V0L.Z;
		d.V3L.X = V0L.W;
		d.V4L.X = V0R.X;
		d.V5L.X = V0R.Y;
		d.V6L.X = V0R.Z;
		d.V7L.X = V0R.W;

		d.V0L.Y = V1L.X;
		d.V1L.Y = V1L.Y;
		d.V2L.Y = V1L.Z;
		d.V3L.Y = V1L.W;
		d.V4L.Y = V1R.X;
		d.V5L.Y = V1R.Y;
		d.V6L.Y = V1R.Z;
		d.V7L.Y = V1R.W;

		d.V0L.Z = V2L.X;
		d.V1L.Z = V2L.Y;
		d.V2L.Z = V2L.Z;
		d.V3L.Z = V2L.W;
		d.V4L.Z = V2R.X;
		d.V5L.Z = V2R.Y;
		d.V6L.Z = V2R.Z;
		d.V7L.Z = V2R.W;

		d.V0L.W = V3L.X;
		d.V1L.W = V3L.Y;
		d.V2L.W = V3L.Z;
		d.V3L.W = V3L.W;
		d.V4L.W = V3R.X;
		d.V5L.W = V3R.Y;
		d.V6L.W = V3R.Z;
		d.V7L.W = V3R.W;

		d.V0R.X = V4L.X;
		d.V1R.X = V4L.Y;
		d.V2R.X = V4L.Z;
		d.V3R.X = V4L.W;
		d.V4R.X = V4R.X;
		d.V5R.X = V4R.Y;
		d.V6R.X = V4R.Z;
		d.V7R.X = V4R.W;

		d.V0R.Y = V5L.X;
		d.V1R.Y = V5L.Y;
		d.V2R.Y = V5L.Z;
		d.V3R.Y = V5L.W;
		d.V4R.Y = V5R.X;
		d.V5R.Y = V5R.Y;
		d.V6R.Y = V5R.Z;
		d.V7R.Y = V5R.W;

		d.V0R.Z = V6L.X;
		d.V1R.Z = V6L.Y;
		d.V2R.Z = V6L.Z;
		d.V3R.Z = V6L.W;
		d.V4R.Z = V6R.X;
		d.V5R.Z = V6R.Y;
		d.V6R.Z = V6R.Z;
		d.V7R.Z = V6R.W;

		d.V0R.W = V7L.X;
		d.V1R.W = V7L.Y;
		d.V2R.W = V7L.Z;
		d.V3R.W = V7L.W;
		d.V4R.W = V7R.X;
		d.V5R.W = V7R.Y;
		d.V6R.W = V7R.Z;
		d.V7R.W = V7R.W;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void TransposeQuadrant(ref Vector4 a, ref Vector4 b, ref Vector4 c, ref Vector4 e,
		ref Vector4 o0, ref Vector4 o1, ref Vector4 o2, ref Vector4 o3)
	{
		var av = Unsafe.As<Vector4, Vector128<float>>(ref a);
		var bv = Unsafe.As<Vector4, Vector128<float>>(ref b);
		var cv = Unsafe.As<Vector4, Vector128<float>>(ref c);
		var ev = Unsafe.As<Vector4, Vector128<float>>(ref e);
		var low0 = Sse.UnpackLow(av, bv);
		var high0 = Sse.UnpackHigh(av, bv);
		var low1 = Sse.UnpackLow(cv, ev);
		var high1 = Sse.UnpackHigh(cv, ev);
		Unsafe.As<Vector4, Vector128<float>>(ref o0) = Sse.MoveLowToHigh(low0, low1);
		Unsafe.As<Vector4, Vector128<float>>(ref o1) = Sse.MoveHighToLow(low1, low0);
		Unsafe.As<Vector4, Vector128<float>>(ref o2) = Sse.MoveLowToHigh(high0, high1);
		Unsafe.As<Vector4, Vector128<float>>(ref o3) = Sse.MoveHighToLow(high1, high0);
	}

	private static void ThrowArgumentOutOfRangeException(string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName);
	}

	private static void ThrowArgumentException(string message)
	{
		throw new ArgumentException(message);
	}
}
