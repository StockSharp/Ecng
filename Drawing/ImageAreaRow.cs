namespace Ecng.Drawing;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/// <summary>Horizontal area contributions shared by dense scanlines and Adam7 passes.</summary>
internal sealed class ImageAreaRow
{
	private readonly int _width;
	private readonly int _sourceWidth;
	private readonly int[] _starts;
	private readonly int[] _ends;
	private readonly int[] _firstWeights;
	private readonly int[] _lastWeights;

	public ImageAreaRow(int sourceWidth, int width)
	{
		_width = width;
		_sourceWidth = sourceWidth;
		Sums = new double[width * 4];
		_starts = new int[width];
		_ends = new int[width];
		_firstWeights = new int[width];
		_lastWeights = new int[width];
		for (var x = 0; x < width; x++)
		{
			var left = (long)x * sourceWidth;
			var right = left + sourceWidth;
			var first = _starts[x] = (int)(left / width);
			var end = _ends[x] = (int)((right + width - 1) / width);
			_firstWeights[x] = (int)(Math.Min(right, (long)(first + 1) * width) - left);
			_lastWeights[x] = (int)(right - (long)(end - 1) * width);
		}
	}

	public double[] Sums { get; }

	public void Sum(ReadOnlySpan<byte> pixels, int startX, int stepX)
	{
		var dense = startX == 0 && stepX == 1 && pixels.Length == (long)_sourceWidth * 4;
		if (dense && Avx.IsSupported && Sse41.IsSupported)
		{
			SumDense(pixels);
			return;
		}
		for (var x = 0; x < _width; x++)
		{
			long alpha = 0, red = 0, green = 0, blue = 0;
			var first = _starts[x];
			var end = _ends[x];
			var start = dense ? first : Math.Max(0, (first - startX + stepX - 1) / stepX);
			var stop = dense ? end : Math.Min(pixels.Length / 4, (end - startX + stepX - 1) / stepX);
			for (var index = start; index < stop; index++)
			{
				var at = index * 4;
				var a = pixels[at + 3];
				alpha += a;
				red += pixels[at] * a;
				green += pixels[at + 1] * a;
				blue += pixels[at + 2] * a;
			}
			alpha *= _width;
			red *= _width;
			green *= _width;
			blue *= _width;
			if (start < stop)
			{
				if (_width != _firstWeights[x] && (dense || startX + start * stepX == first))
					RemoveExcess(pixels, start * 4, _width - _firstWeights[x], ref alpha, ref red, ref green, ref blue);
				if (end - 1 != first && _width != _lastWeights[x] && (dense || startX + (stop - 1) * stepX == end - 1))
					RemoveExcess(pixels, (stop - 1) * 4, _width - _lastWeights[x], ref alpha, ref red, ref green, ref blue);
			}
			var to = x * 4;
			Sums[to] = alpha;
			Sums[to + 1] = red;
			Sums[to + 2] = green;
			Sums[to + 3] = blue;
		}
	}

	private void SumDense(ReadOnlySpan<byte> pixels)
	{
		ref byte source = ref MemoryMarshal.GetReference(pixels);
		ref double destination = ref MemoryMarshal.GetArrayDataReference(Sums);
		var weight = Vector256.Create((double)_width);
		for (var x = 0; x < _width; x++)
		{
			var first = _starts[x];
			var end = _ends[x];
			var sum = Vector256<double>.Zero;
			for (var index = first + 1; index < end - 1; index++)
				sum = Avx.Add(sum, Pixel(ref source, index));
			sum = Avx.Multiply(sum, weight);
			sum = Avx.Add(sum, Avx.Multiply(Pixel(ref source, first), Vector256.Create((double)_firstWeights[x])));
			if (end - 1 != first)
				sum = Avx.Add(sum, Avx.Multiply(Pixel(ref source, end - 1), Vector256.Create((double)_lastWeights[x])));
			Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref Unsafe.Add(ref destination, x * 4)), sum);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<double> Pixel(ref byte source, int index)
	{
		var rgba = Vector128.CreateScalar(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref source, index * 4))).AsByte();
		var channels = Sse41.ConvertToVector128Int32(rgba);
		var alpha = Sse41.Insert(Sse2.Shuffle(channels, 255), 1, 3);
		var values = Sse41.MultiplyLow(channels, alpha);
		return Avx.ConvertToVector256Double(Sse2.Shuffle(values, 147));
	}

	private static void RemoveExcess(ReadOnlySpan<byte> pixels, int at, int weight, ref long alpha, ref long red, ref long green, ref long blue)
	{
		var a = (long)pixels[at + 3] * weight;
		alpha -= a;
		red -= pixels[at] * a;
		green -= pixels[at + 1] * a;
		blue -= pixels[at + 2] * a;
	}
}
