namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.Dct;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#if NET7_0_OR_GREATER
internal static class Vp8VectorInverseDct
{
	public static bool CanTransform => Vp8Interleave.IsSupported;

	private static readonly Vector128<int> C1 = Vector128.Create(Vp8InverseTransformConstants.C1);
	private static readonly Vector128<int> C2 = Vector128.Create(Vp8InverseTransformConstants.C2);
	private static readonly Vector128<int> Four = Vector128.Create(4);
	private static readonly Vector128<int> Zero = Vector128<int>.Zero;
	private static readonly Vector128<int> TwoFiftyFive = Vector128.Create(255);
	public static void TransformFullAndAdd(ReadOnlySpan<short> coefficients, Span<byte> dst, int offset, int stride)
	{
		// Lane j of each vector = original column j's coefficient at that row position.
		var in0 = LoadRowOfColumns(coefficients, 0);
		var in4 = LoadRowOfColumns(coefficients, 4);
		var in8 = LoadRowOfColumns(coefficients, 8);
		var in12 = LoadRowOfColumns(coefficients, 12);

		// Column pass: 4 independent butterflies, one per lane (= per original column). Lane-parallel, no
		// transpose needed yet -- this mirrors the scalar loop's `for (int i = 0; i < 4; i++)` exactly, just
		// computing all 4 iterations of it at once instead of one at a time.
		var a = in0 + in8;
		var b = in0 - in8;
		var c = Mul2(in4) - Mul1(in12);
		var d = Mul1(in4) + Mul2(in12);

		// slotN[j] = column j's N-th butterfly output. This is a genuinely different axis from the columns
		// pass 1 was lane-parallel over: slot0..slot3 need to become pass 2's *inputs*, one full vector per
		// column rather than one lane per column, which is exactly what a transpose does.
		var slot0 = a + d;
		var slot1 = b + c;
		var slot2 = b - c;
		var slot3 = a - d;

		Transpose4x4(slot0, slot1, slot2, slot3, out var col0, out var col1, out var col2, out var col3);

		// col0..col3 are now column 0..3's own 4 butterfly outputs, one full vector per column. That is
		// exactly (t0, t4, t8, t12) for a row pass made lane-parallel by output row instead of by column:
		// col0's lane r is output row r's t0, col1's lane r is row r's t4, and so on.
		var dc = col0 + Four;
		var a2 = dc + col2;
		var b2 = dc - col2;
		var c2 = Mul2(col1) - Mul1(col3);
		var d2 = Mul1(col1) + Mul2(col3);

		// outN[r] = output row r's N-th pixel delta (before the final >>3). Transposing once more turns this
		// back into one vector per row, each holding that row's 4 contiguous pixel deltas -- ready to shift,
		// add to the existing prediction, clamp, and store.
		var out0 = a2 + d2;
		var out1 = b2 + c2;
		var out2 = b2 - c2;
		var out3 = a2 - d2;

		Transpose4x4(out0, out1, out2, out3, out var row0, out var row1, out var row2, out var row3);

		AddClipStoreRow(dst, offset, row0);
		AddClipStoreRow(dst, offset + stride, row1);
		AddClipStoreRow(dst, offset + (2 * stride), row2);
		AddClipStoreRow(dst, offset + (3 * stride), row3);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<int> LoadRowOfColumns(ReadOnlySpan<short> coefficients, int baseIndex) =>
		Vector128.WidenLower(Vector128.CreateScalar(MemoryMarshal.Read<ulong>(MemoryMarshal.AsBytes(coefficients.Slice(baseIndex, 4)))).AsInt16());
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<int> Mul1(Vector128<int> v) => v + Vector128.ShiftRightArithmetic(v * C1, 16);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<int> Mul2(Vector128<int> v) => Vector128.ShiftRightArithmetic(v * C2, 16);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Transpose4x4(
		Vector128<int> row0, Vector128<int> row1, Vector128<int> row2, Vector128<int> row3,
		out Vector128<int> col0, out Vector128<int> col1, out Vector128<int> col2, out Vector128<int> col3)
	{
		var lo01 = Vp8Interleave.UnpackLow(row0, row1);
		var hi01 = Vp8Interleave.UnpackHigh(row0, row1);
		var lo23 = Vp8Interleave.UnpackLow(row2, row3);
		var hi23 = Vp8Interleave.UnpackHigh(row2, row3);

		col0 = Vp8Interleave.UnpackLow(lo01.AsInt64(), lo23.AsInt64()).AsInt32();
		col1 = Vp8Interleave.UnpackHigh(lo01.AsInt64(), lo23.AsInt64()).AsInt32();
		col2 = Vp8Interleave.UnpackLow(hi01.AsInt64(), hi23.AsInt64()).AsInt32();
		col3 = Vp8Interleave.UnpackHigh(hi01.AsInt64(), hi23.AsInt64()).AsInt32();
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AddClipStoreRow(Span<byte> dst, int rowOffset, Vector128<int> deltas)
	{
		var shifted = Vector128.ShiftRightArithmetic(deltas, 3);
		var row = dst.Slice(rowOffset, 4);
		var basePixels = Vector128.WidenLower(Vector128.WidenLower(Vector128.CreateScalar(MemoryMarshal.Read<uint>(row)).AsByte())).AsInt32();
		var clamped = Vector128.Min(Vector128.Max(basePixels + shifted, Zero), TwoFiftyFive).AsUInt32();

		// Every lane is in [0,255], so the truncating narrows keep the values and put the four bytes in lane 0.
		var bytes = Vector128.Narrow(Vector128.Narrow(clamped, clamped), Vector128.Narrow(clamped, clamped));
		MemoryMarshal.Write(row, bytes.AsUInt32().ToScalar());
	}
}

#endif
