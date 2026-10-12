namespace Ecng.Drawing;

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal static class PngRowFilter
{
	public static int SumVectors(ReadOnlySpan<byte> row, ReadOnlySpan<byte> above, byte[][] candidates, int index,
		ref long none, ref long sub, ref long up, ref long average, ref long paeth)
	{
#if NET7_0_OR_GREATER
		if (!Vector128.IsHardwareAccelerated) return index;
		long n = 0, s = 0, u = 0, a = 0, p = 0;
		ref var source = ref MemoryMarshal.GetReference(row);
		ref var previous = ref MemoryMarshal.GetReference(above);
		for (; index + 16 <= row.Length; index += 16)
		{
			var value = Vector128.LoadUnsafe(ref source, (nuint)index);
			var top = Vector128.LoadUnsafe(ref previous, (nuint)index);
			var left = Vector128.LoadUnsafe(ref source, (nuint)(index - 4));
			var corner = Vector128.LoadUnsafe(ref previous, (nuint)(index - 4));
			var leftLow = Vector128.WidenLower(left);
			var leftHigh = Vector128.WidenUpper(left);
			var topLow = Vector128.WidenLower(top);
			var topHigh = Vector128.WidenUpper(top);
			var mean = Vector128.Narrow(Vector128.ShiftRightLogical(leftLow + topLow, 1), Vector128.ShiftRightLogical(leftHigh + topHigh, 1));
			var prediction = Vector128.Narrow(
				Paeth(leftLow.AsInt16(), topLow.AsInt16(), Vector128.WidenLower(corner).AsInt16()).AsUInt16(),
				Paeth(leftHigh.AsInt16(), topHigh.AsInt16(), Vector128.WidenUpper(corner).AsInt16()).AsUInt16());
			var filteredSub = value - left;
			var filteredUp = value - top;
			var filteredAverage = value - mean;
			var filteredPaeth = value - prediction;
			filteredSub.StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(candidates[1]), (nuint)index);
			filteredUp.StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(candidates[2]), (nuint)index);
			filteredAverage.StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(candidates[3]), (nuint)index);
			filteredPaeth.StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(candidates[4]), (nuint)index);
			n += Magnitude(value);
			s += Magnitude(filteredSub);
			u += Magnitude(filteredUp);
			a += Magnitude(filteredAverage);
			p += Magnitude(filteredPaeth);
		}
		none += n;
		sub += s;
		up += u;
		average += a;
		paeth += p;
#endif
		return index;
	}

#if NET7_0_OR_GREATER
	private static Vector128<short> Paeth(Vector128<short> left, Vector128<short> top, Vector128<short> corner)
	{
		var leftError = Vector128.Abs(top - corner);
		var topError = Vector128.Abs(left - corner);
		var cornerError = Vector128.Abs(left + top - corner - corner);
		var useLeft = Vector128.LessThanOrEqual(leftError, topError) & Vector128.LessThanOrEqual(leftError, cornerError);
		return Vector128.ConditionalSelect(useLeft, left,
			Vector128.ConditionalSelect(Vector128.LessThanOrEqual(topError, cornerError), top, corner));
	}

	private static int Magnitude(Vector128<byte> value)
	{
		var magnitude = Vector128.Abs(value.AsSByte()).AsByte();
		if (Sse2.IsSupported)
		{
			var sums = Sse2.SumAbsoluteDifferences(magnitude, Vector128<byte>.Zero).AsUInt64();
			return (int)(sums.GetElement(0) + sums.GetElement(1));
		}
		return Vector128.Sum(Vector128.WidenLower(magnitude)) + Vector128.Sum(Vector128.WidenUpper(magnitude));
	}
#endif
}
