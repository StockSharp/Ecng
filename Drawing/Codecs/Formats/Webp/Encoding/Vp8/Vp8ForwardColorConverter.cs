namespace Ecng.Drawing.Codecs.Formats.Webp.Encoding.Vp8;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

internal static class Vp8ForwardColorConverter
{
	private const int Fix = 16;
	private const int YRounding = 1 << (Fix - 1);
	private const int UvRounding = 1 << (Fix + 2 - 1);

	public static byte ConvertY(int r, int g, int b)
	{
		int luma = (16839 * r) + (33059 * g) + (6420 * b);
		int y = (luma + YRounding + (16 << Fix)) >> Fix;
		return ClipByte(y);
	}

	public static byte ConvertU(int r, int g, int b)
	{
		int u = (-9719 * r) - (19081 * g) + (28800 * b);
		return ClipUv(u);
	}

	public static byte ConvertV(int r, int g, int b)
	{
		int v = (28800 * r) - (24116 * g) - (4684 * b);
		return ClipUv(v);
	}

	private static byte ClipUv(int uv)
	{
		int result = (uv + UvRounding + (128 << (Fix + 2))) >> (Fix + 2);
		return ClipByte(result);
	}

	private static byte ClipByte(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

	public static void ConvertPlanes(ReadOnlySpan<byte> rgb, int width, int height, Span<byte> yPlane, int yStride, Span<byte> uPlane, Span<byte> vPlane, int chromaStride)
	{
#if NET7_0_OR_GREATER
		int vectorWidth = Vector128.IsHardwareAccelerated ? (width / Block) * Block : 0;
#else
		int vectorWidth = 0;
#endif

		for (int y = 0; y < height; y++)
		{
			int rowBase = y * width * 3;
			int yRowBase = y * yStride;
			int xStart = 0;
			if (vectorWidth > 0)
			{
#if NET7_0_OR_GREATER
				ConvertLumaRow(rgb, rowBase, yPlane, yRowBase, vectorWidth);
#endif
				xStart = vectorWidth;
			}

			for (int x = xStart; x < width; x++)
			{
				int o = rowBase + (x * 3);
				yPlane[yRowBase + x] = ConvertY(rgb[o], rgb[o + 1], rgb[o + 2]);
			}
		}

		int chromaWidth = (width + 1) / 2;
		int chromaHeight = (height + 1) / 2;
		for (int cy = 0; cy < chromaHeight; cy++)
		{
			int y0 = cy * 2;
			int y1 = Math.Min(y0 + 1, height - 1);
			int uvRowBase = cy * chromaStride;

			int cxStart = 0;
			if (vectorWidth > 0 && y0 + 1 < height)
			{
#if NET7_0_OR_GREATER
				ConvertChromaRowPair(rgb, y0 * width * 3, (y0 + 1) * width * 3, uPlane, vPlane, uvRowBase, vectorWidth);
#endif
				cxStart = vectorWidth / 2;
			}

			for (int cx = cxStart; cx < chromaWidth; cx++)
			{
				int x0 = cx * 2;
				int x1 = Math.Min(x0 + 1, width - 1);

				int o00 = (y0 * width * 3) + (x0 * 3);
				int o01 = (y0 * width * 3) + (x1 * 3);
				int o10 = (y1 * width * 3) + (x0 * 3);
				int o11 = (y1 * width * 3) + (x1 * 3);

				int r = rgb[o00 + 0] + rgb[o01 + 0] + rgb[o10 + 0] + rgb[o11 + 0];
				int g = rgb[o00 + 1] + rgb[o01 + 1] + rgb[o10 + 1] + rgb[o11 + 1];
				int b = rgb[o00 + 2] + rgb[o01 + 2] + rgb[o10 + 2] + rgb[o11 + 2];

				uPlane[uvRowBase + cx] = ConvertU(r, g, b);
				vPlane[uvRowBase + cx] = ConvertV(r, g, b);
			}
		}
	}

	private const int Block = 16;

#if NET7_0_OR_GREATER
	private static readonly Vector128<byte>[][] Deinterleave3 = BuildDeinterleaveTable();

	private static Vector128<byte>[][] BuildDeinterleaveTable()
	{
		var table = new Vector128<byte>[3][];
		for (int channel = 0; channel < 3; channel++)
		{
			table[channel] = new Vector128<byte>[3];
			for (int vector = 0; vector < 3; vector++)
			{
				var indices = new byte[16];
				for (int pixel = 0; pixel < 16; pixel++)
				{
					int n = (pixel * 3) + channel - (16 * vector);
					indices[pixel] = n is >= 0 and < 16 ? (byte)n : (byte)255;
				}

				table[channel][vector] = Vector128.Create(indices);
			}
		}

		return table;
	}

	private static void LoadPlanar(ReadOnlySpan<byte> rgb, int offset, out Vector128<byte> r, out Vector128<byte> g, out Vector128<byte> b)
	{
		_ = rgb[offset + (3 * Block) - 1];
		ref byte start = ref Unsafe.Add(ref MemoryMarshal.GetReference(rgb), offset);
		var v0 = Vector128.LoadUnsafe(ref start, 0);
		var v1 = Vector128.LoadUnsafe(ref start, 16);
		var v2 = Vector128.LoadUnsafe(ref start, 32);

		r = Vector128.Shuffle(v0, Deinterleave3[0][0]) | Vector128.Shuffle(v1, Deinterleave3[0][1]) | Vector128.Shuffle(v2, Deinterleave3[0][2]);
		g = Vector128.Shuffle(v0, Deinterleave3[1][0]) | Vector128.Shuffle(v1, Deinterleave3[1][1]) | Vector128.Shuffle(v2, Deinterleave3[1][2]);
		b = Vector128.Shuffle(v0, Deinterleave3[2][0]) | Vector128.Shuffle(v1, Deinterleave3[2][1]) | Vector128.Shuffle(v2, Deinterleave3[2][2]);
	}

	private static Vector128<int> Widen4(Vector128<byte> v, int group)
	{
		var half = group < 2 ? Vector128.WidenLower(v) : Vector128.WidenUpper(v);
		return (group % 2 == 0 ? Vector128.WidenLower(half) : Vector128.WidenUpper(half)).AsInt32();
	}

	private static Vector128<int> ClipToByteRange(Vector128<int> v) => Vector128.Min(Vector128.Max(v, Vector128<int>.Zero), Vector128.Create(255));

	private static void ConvertLumaRow(ReadOnlySpan<byte> rgb, int rowBase, Span<byte> yPlane, int yRowBase, int pixels)
	{
		var kr = Vector128.Create(16839);
		var kg = Vector128.Create(33059);
		var kb = Vector128.Create(6420);
		var bias = Vector128.Create(YRounding + (16 << Fix));

		_ = yPlane[yRowBase + pixels - 1];
		ref byte dst = ref Unsafe.Add(ref MemoryMarshal.GetReference(yPlane), yRowBase);
		for (int x = 0; x < pixels; x += Block)
		{
			LoadPlanar(rgb, rowBase + (x * 3), out var r, out var g, out var b);

			var y0 = LumaGroup(r, g, b, 0, kr, kg, kb, bias);
			var y1 = LumaGroup(r, g, b, 1, kr, kg, kb, bias);
			var y2 = LumaGroup(r, g, b, 2, kr, kg, kb, bias);
			var y3 = LumaGroup(r, g, b, 3, kr, kg, kb, bias);

			Vector128.Narrow(Vector128.Narrow(y0.AsUInt32(), y1.AsUInt32()), Vector128.Narrow(y2.AsUInt32(), y3.AsUInt32()))
				.StoreUnsafe(ref dst, (nuint)x);
		}
	}

	private static Vector128<int> LumaGroup(Vector128<byte> r, Vector128<byte> g, Vector128<byte> b, int group, Vector128<int> kr, Vector128<int> kg, Vector128<int> kb, Vector128<int> bias) =>
		ClipToByteRange(Vector128.ShiftRightArithmetic((Widen4(r, group) * kr) + (Widen4(g, group) * kg) + (Widen4(b, group) * kb) + bias, Fix));

	private static void ConvertChromaRowPair(ReadOnlySpan<byte> rgb, int rowBase0, int rowBase1, Span<byte> uPlane, Span<byte> vPlane, int uvRowBase, int pixels)
	{
		int chromaCount = pixels / 2;
		_ = uPlane[uvRowBase + chromaCount - 1];
		_ = vPlane[uvRowBase + chromaCount - 1];
		ref byte uDst = ref Unsafe.Add(ref MemoryMarshal.GetReference(uPlane), uvRowBase);
		ref byte vDst = ref Unsafe.Add(ref MemoryMarshal.GetReference(vPlane), uvRowBase);

		for (int x = 0; x < pixels; x += Block)
		{
			LoadPlanar(rgb, rowBase0 + (x * 3), out var r0, out var g0, out var b0);
			LoadPlanar(rgb, rowBase1 + (x * 3), out var r1, out var g1, out var b1);

			var rs = HorizontalPairs(r0, r1);
			var gs = HorizontalPairs(g0, g1);
			var bs = HorizontalPairs(b0, b1);

			var u0 = ChromaGroup(rs, gs, bs, upper: false, -9719, -19081, 28800);
			var u1 = ChromaGroup(rs, gs, bs, upper: true, -9719, -19081, 28800);
			var v0 = ChromaGroup(rs, gs, bs, upper: false, 28800, -24116, -4684);
			var v1 = ChromaGroup(rs, gs, bs, upper: true, 28800, -24116, -4684);

			var uBytes = Vector128.Narrow(Vector128.Narrow(u0.AsUInt32(), u1.AsUInt32()), Vector128<ushort>.Zero);
			var vBytes = Vector128.Narrow(Vector128.Narrow(v0.AsUInt32(), v1.AsUInt32()), Vector128<ushort>.Zero);
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref uDst, x / 2), uBytes.AsUInt64().ToScalar());
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref vDst, x / 2), vBytes.AsUInt64().ToScalar());
		}
	}

	private static Vector128<ushort> HorizontalPairs(Vector128<byte> row0, Vector128<byte> row1)
	{
		var low = Vector128.WidenLower(row0) + Vector128.WidenLower(row1);
		var high = Vector128.WidenUpper(row0) + Vector128.WidenUpper(row1);

		var even = Vector128.Create(Vector128.Shuffle(low, EvenLanes).GetLower(), Vector128.Shuffle(high, EvenLanes).GetLower());
		var odd = Vector128.Create(Vector128.Shuffle(low, OddLanes).GetLower(), Vector128.Shuffle(high, OddLanes).GetLower());
		return even + odd;
	}

	private static readonly Vector128<ushort> EvenLanes = Vector128.Create((ushort)0, 2, 4, 6, 255, 255, 255, 255);
	private static readonly Vector128<ushort> OddLanes = Vector128.Create((ushort)1, 3, 5, 7, 255, 255, 255, 255);

	private static Vector128<int> ChromaGroup(Vector128<ushort> rs, Vector128<ushort> gs, Vector128<ushort> bs, bool upper, int kr, int kg, int kb)
	{
		static Vector128<int> Quad(Vector128<ushort> v, bool upper) => (upper ? Vector128.WidenUpper(v) : Vector128.WidenLower(v)).AsInt32();

		var sum = (Quad(rs, upper) * Vector128.Create(kr)) + (Quad(gs, upper) * Vector128.Create(kg)) + (Quad(bs, upper) * Vector128.Create(kb));
		return ClipToByteRange(Vector128.ShiftRightArithmetic(sum + Vector128.Create(UvRounding + (128 << (Fix + 2))), Fix + 2));
	}
#endif
}
