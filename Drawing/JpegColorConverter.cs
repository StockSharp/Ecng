namespace Ecng.Drawing;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

internal sealed class JpegColorConverter
{
	private static readonly byte[] _red = ColorTable(1.402);
	private static readonly byte[] _blue = ColorTable(1.772);
	private static readonly short[] _green = GreenOffsets();
	private readonly JpegComponent[] _components;
	private readonly JpegColorModels _model;
	private readonly int[][] _columns;
	private readonly int _maxV;
	private readonly int _width;

	public JpegColorConverter(int width, JpegComponent[] components, JpegColorModels model)
	{
		_components = components;
		_model = model;
		_width = width;
		var maxH = 1;
		_maxV = 1;
		foreach (var c in components) { maxH = Math.Max(maxH, c.H); _maxV = Math.Max(_maxV, c.V); }
		_columns = new int[components.Length][];
		for (var c = 0; c < components.Length; c++)
		{
			_columns[c] = new int[width];
			for (var x = 0; x < width; x++) _columns[c][x] = x * components[c].H / maxH;
		}
	}

	public void WriteRow(int y, Span<byte> output)
	{
		if (_model == JpegColorModels.YCbCr)
		{
			WriteYcbcrRow(y, output);
			return;
		}

		Span<int> rows = stackalloc int[_components.Length];
		Span<int> values = stackalloc int[_components.Length];
		for (var c = 0; c < _components.Length; c++) rows[c] = y * _components[c].V / _maxV * _components[c].Stride;
		for (var x = 0; x < _width; x++)
		{
			for (var c = 0; c < _components.Length; c++) values[c] = _components[c].Samples[rows[c] + _columns[c][x]];
			var at = x * 4;
			switch (_model)
			{
				case JpegColorModels.Gray:
					output[at] = output[at + 1] = output[at + 2] = (byte)values[0];
					break;
				case JpegColorModels.Rgb:
					output[at] = (byte)values[0];
					output[at + 1] = (byte)values[1];
					output[at + 2] = (byte)values[2];
					break;
				case JpegColorModels.Cmyk:
					output[at] = RasterImage.ToByte(values[0] * values[3] / 255.0);
					output[at + 1] = RasterImage.ToByte(values[1] * values[3] / 255.0);
					output[at + 2] = RasterImage.ToByte(values[2] * values[3] / 255.0);
					break;
				default:
					output[at] = RasterImage.ToByte((255 - Red(values[0], values[2])) * values[3] / 255.0);
					output[at + 1] = RasterImage.ToByte((255 - Green(values[0], values[1], values[2])) * values[3] / 255.0);
					output[at + 2] = RasterImage.ToByte((255 - Blue(values[0], values[1])) * values[3] / 255.0);
					break;
			}
			output[at + 3] = 255;
		}
	}

	public bool RowsEqual(int first, int second)
	{
		for (var c = 0; c < _components.Length; c++)
		{
			var component = _components[c];
			var firstOffset = first * component.V / _maxV * component.Stride;
			var secondOffset = second * component.V / _maxV * component.Stride;
			var length = _columns[c][_width - 1] + 1;
			if (!component.Samples.AsSpan(firstOffset, length).SequenceEqual(component.Samples.AsSpan(secondOffset, length)))
				return false;
		}
		return true;
	}

	private void WriteYcbcrRow(int row, Span<byte> output)
	{
		var yPlane = _components[0];
		var cbPlane = _components[1];
		var crPlane = _components[2];
		var yAt = row * yPlane.V / _maxV * yPlane.Stride;
		var cbAt = row * cbPlane.V / _maxV * cbPlane.Stride;
		var crAt = row * crPlane.V / _maxV * crPlane.Stride;
		if (yPlane.H == cbPlane.H && yPlane.H == crPlane.H)
		{
			WriteDenseYcbcrRow(yPlane.Samples.AsSpan(yAt, _width), cbPlane.Samples.AsSpan(cbAt, _width),
				crPlane.Samples.AsSpan(crAt, _width), output);
			return;
		}
		var yColumns = _columns[0];
		var cbColumns = _columns[1];
		var crColumns = _columns[2];
		for (var x = 0; x < _width; x++)
		{
			var y = yPlane.Samples[yAt + yColumns[x]];
			var cb = cbPlane.Samples[cbAt + cbColumns[x]];
			var cr = crPlane.Samples[crAt + crColumns[x]];
			var at = x * 4;
			output[at] = Red(y, cr);
			output[at + 1] = Green(y, cb, cr);
			output[at + 2] = Blue(y, cb);
			output[at + 3] = 255;
		}
	}

	private static void WriteDenseYcbcrRow(ReadOnlySpan<byte> luminance, ReadOnlySpan<byte> blueChroma,
		ReadOnlySpan<byte> redChroma, Span<byte> output)
	{
		var x = 0;
		if (Avx.IsSupported && Sse41.IsSupported)
		{
			ref byte luma = ref MemoryMarshal.GetReference(luminance);
			ref byte cbSource = ref MemoryMarshal.GetReference(blueChroma);
			ref byte crSource = ref MemoryMarshal.GetReference(redChroma);
			for (; x + 4 <= luminance.Length; x += 4)
			{
				var y = FourBytes(ref luma, x);
				var cb = Avx.Subtract(FourBytes(ref cbSource, x), Vector256.Create(128.0));
				var cr = Avx.Subtract(FourBytes(ref crSource, x), Vector256.Create(128.0));
				var red = Pack(Avx.Add(y, Avx.Multiply(cr, Vector256.Create(1.402))));
				var green = Pack(Avx.Subtract(Avx.Subtract(y, Avx.Multiply(cb, Vector256.Create(0.344136))),
					Avx.Multiply(cr, Vector256.Create(0.714136))));
				var blue = Pack(Avx.Add(y, Avx.Multiply(cb, Vector256.Create(1.772))));
				var rg = Sse2.UnpackLow(red, green);
				var ba = Sse2.UnpackLow(blue, Vector128.Create((byte)255));
				var rgba = Sse2.UnpackLow(rg.AsUInt16(), ba.AsUInt16()).AsByte();
				Unsafe.WriteUnaligned(ref output[x * 4], rgba);
			}
		}
		for (; x < luminance.Length; x++)
		{
			var y = luminance[x];
			var cb = blueChroma[x];
			var cr = redChroma[x];
			var at = x * 4;
			output[at] = Red(y, cr);
			output[at + 1] = Green(y, cb, cr);
			output[at + 2] = Blue(y, cb);
			output[at + 3] = 255;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<double> FourBytes(ref byte source, int offset)
		=> Avx.ConvertToVector256Double(Sse41.ConvertToVector128Int32(
			Vector128.CreateScalar(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref source, offset))).AsByte()));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> Pack(Vector256<double> values)
	{
		var integers = Avx.ConvertToVector128Int32(Avx.RoundToNearestInteger(values));
		return Sse2.PackUnsignedSaturate(Sse2.PackSignedSaturate(integers, Vector128<int>.Zero), Vector128<short>.Zero);
	}

	private static byte[] ColorTable(double factor)
	{
		var table = new byte[256 * 256];
		for (var chroma = 0; chroma < 256; chroma++)
			for (var y = 0; y < 256; y++) table[chroma * 256 + y] = RasterImage.ToByte(y + factor * (chroma - 128));
		return table;
	}

	private static short[] GreenOffsets()
	{
		var table = new short[256 * 256];
		for (var cb = 0; cb < 256; cb++)
			for (var cr = 0; cr < 256; cr++)
			{
				var offset = -0.344136 * (cb - 128) - 0.714136 * (cr - 128);
				// At a rounding boundary, preserve the scalar expression's dependence on luminance and parity.
				table[cb * 256 + cr] = Math.Abs(offset - Math.Floor(offset) - 0.5) < 1e-12 ? short.MinValue : (short)Math.Round(offset);
			}
		return table;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte Red(int y, int cr) => _red[cr * 256 + y];
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte Blue(int y, int cb) => _blue[cb * 256 + y];
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte Green(int y, int cb, int cr)
	{
		var offset = _green[cb * 256 + cr];
		return offset == short.MinValue
			? RasterImage.ToByte(y - 0.344136 * (cb - 128) - 0.714136 * (cr - 128))
			: (byte)Math.Clamp(y + offset, 0, 255);
	}
}
