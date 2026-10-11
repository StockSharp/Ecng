namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8IntraPrediction4x4
{
	private static byte Avg2(int a, int b) => (byte)((a + b + 1) >> 1);

	private static byte Avg3(int a, int b, int c) => (byte)((a + (2 * b) + c + 2) >> 2);

	public static void Predict(int mode, Span<byte> plane, int origin, int stride, ReadOnlySpan<byte> aboveRight)
	{
		switch (mode)
		{
			case Vp8PredictionModes.BDcPred:
				Vp8IntraPredictionWholeBlock.PredictDc(plane, origin, stride, 4, hasAbove: true, hasLeft: true);
				break;
			case Vp8PredictionModes.BTmPred:
				Vp8IntraPredictionWholeBlock.PredictTrueMotion(plane, origin, stride, 4);
				break;
			case Vp8PredictionModes.BVePred:
				PredictVe(plane, origin, stride, aboveRight);
				break;
			case Vp8PredictionModes.BHePred:
				PredictHe(plane, origin, stride);
				break;
			case Vp8PredictionModes.BRdPred:
				PredictRd(plane, origin, stride);
				break;
			case Vp8PredictionModes.BVrPred:
				PredictVr(plane, origin, stride);
				break;
			case Vp8PredictionModes.BLdPred:
				PredictLd(plane, origin, stride, aboveRight);
				break;
			case Vp8PredictionModes.BVlPred:
				PredictVl(plane, origin, stride, aboveRight);
				break;
			case Vp8PredictionModes.BHdPred:
				PredictHd(plane, origin, stride);
				break;
			case Vp8PredictionModes.BHuPred:
				PredictHu(plane, origin, stride);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown VP8 4x4 subblock prediction mode.");
		}
	}

	private static void Set(Span<byte> plane, int origin, int stride, int x, int y, byte v) => plane[origin + (y * stride) + x] = v;

	private static void PredictVe(Span<byte> plane, int origin, int stride, ReadOnlySpan<byte> aboveRight)
	{
		int corner = plane[origin - stride - 1];
		int a0 = plane[origin - stride + 0];
		int a1 = plane[origin - stride + 1];
		int a2 = plane[origin - stride + 2];
		int a3 = plane[origin - stride + 3];
		int a4 = aboveRight[0];

		byte v0 = Avg3(corner, a0, a1);
		byte v1 = Avg3(a0, a1, a2);
		byte v2 = Avg3(a1, a2, a3);
		byte v3 = Avg3(a2, a3, a4);

		for (int y = 0; y < 4; y++)
		{
			Set(plane, origin, stride, 0, y, v0);
			Set(plane, origin, stride, 1, y, v1);
			Set(plane, origin, stride, 2, y, v2);
			Set(plane, origin, stride, 3, y, v3);
		}
	}

	private static void PredictHe(Span<byte> plane, int origin, int stride)
	{
		int a = plane[origin - stride - 1];
		int b = plane[origin + (0 * stride) - 1];
		int c = plane[origin + (1 * stride) - 1];
		int d = plane[origin + (2 * stride) - 1];
		int e = plane[origin + (3 * stride) - 1];

		FillRow(plane, origin, stride, 0, Avg3(a, b, c));
		FillRow(plane, origin, stride, 1, Avg3(b, c, d));
		FillRow(plane, origin, stride, 2, Avg3(c, d, e));
		FillRow(plane, origin, stride, 3, Avg3(d, e, e));
	}

	private static void FillRow(Span<byte> plane, int origin, int stride, int y, byte v)
	{
		int rowOrigin = origin + (y * stride);
		plane[rowOrigin] = v;
		plane[rowOrigin + 1] = v;
		plane[rowOrigin + 2] = v;
		plane[rowOrigin + 3] = v;
	}

	private static void PredictRd(Span<byte> plane, int origin, int stride)
	{
		int i = plane[origin + (0 * stride) - 1];
		int j = plane[origin + (1 * stride) - 1];
		int k = plane[origin + (2 * stride) - 1];
		int l = plane[origin + (3 * stride) - 1];
		int x = plane[origin - stride - 1];
		int a = plane[origin - stride + 0];
		int b = plane[origin - stride + 1];
		int c = plane[origin - stride + 2];
		int d = plane[origin - stride + 3];

		Set(plane, origin, stride, 0, 3, Avg3(j, k, l));
		byte v102 = Avg3(i, j, k);
		Set(plane, origin, stride, 1, 3, v102);
		Set(plane, origin, stride, 0, 2, v102);
		byte v210201 = Avg3(x, i, j);
		Set(plane, origin, stride, 2, 3, v210201);
		Set(plane, origin, stride, 1, 2, v210201);
		Set(plane, origin, stride, 0, 1, v210201);
		byte v333222111000 = Avg3(a, x, i);
		Set(plane, origin, stride, 3, 3, v333222111000);
		Set(plane, origin, stride, 2, 2, v333222111000);
		Set(plane, origin, stride, 1, 1, v333222111000);
		Set(plane, origin, stride, 0, 0, v333222111000);
		byte v320210100 = Avg3(b, a, x);
		Set(plane, origin, stride, 3, 2, v320210100);
		Set(plane, origin, stride, 2, 1, v320210100);
		Set(plane, origin, stride, 1, 0, v320210100);
		byte v3120 = Avg3(c, b, a);
		Set(plane, origin, stride, 3, 1, v3120);
		Set(plane, origin, stride, 2, 0, v3120);
		Set(plane, origin, stride, 3, 0, Avg3(d, c, b));
	}

	private static void PredictLd(Span<byte> plane, int origin, int stride, ReadOnlySpan<byte> aboveRight)
	{
		int a = plane[origin - stride + 0];
		int b = plane[origin - stride + 1];
		int c = plane[origin - stride + 2];
		int d = plane[origin - stride + 3];
		int e = aboveRight[0];
		int f = aboveRight[1];
		int g = aboveRight[2];
		int h = aboveRight[3];

		Set(plane, origin, stride, 0, 0, Avg3(a, b, c));
		byte v1001 = Avg3(b, c, d);
		Set(plane, origin, stride, 1, 0, v1001);
		Set(plane, origin, stride, 0, 1, v1001);
		byte v201102 = Avg3(c, d, e);
		Set(plane, origin, stride, 2, 0, v201102);
		Set(plane, origin, stride, 1, 1, v201102);
		Set(plane, origin, stride, 0, 2, v201102);
		byte v30211203 = Avg3(d, e, f);
		Set(plane, origin, stride, 3, 0, v30211203);
		Set(plane, origin, stride, 2, 1, v30211203);
		Set(plane, origin, stride, 1, 2, v30211203);
		Set(plane, origin, stride, 0, 3, v30211203);
		byte v3122313 = Avg3(e, f, g);
		Set(plane, origin, stride, 3, 1, v3122313);
		Set(plane, origin, stride, 2, 2, v3122313);
		Set(plane, origin, stride, 1, 3, v3122313);
		byte v3223 = Avg3(f, g, h);
		Set(plane, origin, stride, 3, 2, v3223);
		Set(plane, origin, stride, 2, 3, v3223);
		Set(plane, origin, stride, 3, 3, Avg3(g, h, h));
	}

	private static void PredictVr(Span<byte> plane, int origin, int stride)
	{
		int i = plane[origin + (0 * stride) - 1];
		int j = plane[origin + (1 * stride) - 1];
		int k = plane[origin + (2 * stride) - 1];
		int x = plane[origin - stride - 1];
		int a = plane[origin - stride + 0];
		int b = plane[origin - stride + 1];
		int c = plane[origin - stride + 2];
		int d = plane[origin - stride + 3];

		byte v0012 = Avg2(x, a);
		Set(plane, origin, stride, 0, 0, v0012);
		Set(plane, origin, stride, 1, 2, v0012);
		byte v1022 = Avg2(a, b);
		Set(plane, origin, stride, 1, 0, v1022);
		Set(plane, origin, stride, 2, 2, v1022);
		byte v2032 = Avg2(b, c);
		Set(plane, origin, stride, 2, 0, v2032);
		Set(plane, origin, stride, 3, 2, v2032);
		Set(plane, origin, stride, 3, 0, Avg2(c, d));

		Set(plane, origin, stride, 0, 3, Avg3(k, j, i));
		Set(plane, origin, stride, 0, 2, Avg3(j, i, x));
		byte v0113 = Avg3(i, x, a);
		Set(plane, origin, stride, 0, 1, v0113);
		Set(plane, origin, stride, 1, 3, v0113);
		byte v1123 = Avg3(x, a, b);
		Set(plane, origin, stride, 1, 1, v1123);
		Set(plane, origin, stride, 2, 3, v1123);
		byte v2133 = Avg3(a, b, c);
		Set(plane, origin, stride, 2, 1, v2133);
		Set(plane, origin, stride, 3, 3, v2133);
		Set(plane, origin, stride, 3, 1, Avg3(b, c, d));
	}

	private static void PredictVl(Span<byte> plane, int origin, int stride, ReadOnlySpan<byte> aboveRight)
	{
		int a = plane[origin - stride + 0];
		int b = plane[origin - stride + 1];
		int c = plane[origin - stride + 2];
		int d = plane[origin - stride + 3];
		int e = aboveRight[0];
		int f = aboveRight[1];
		int g = aboveRight[2];
		int h = aboveRight[3];

		Set(plane, origin, stride, 0, 0, Avg2(a, b));
		byte v1002 = Avg2(b, c);
		Set(plane, origin, stride, 1, 0, v1002);
		Set(plane, origin, stride, 0, 2, v1002);
		byte v2012 = Avg2(c, d);
		Set(plane, origin, stride, 2, 0, v2012);
		Set(plane, origin, stride, 1, 2, v2012);
		byte v3022 = Avg2(d, e);
		Set(plane, origin, stride, 3, 0, v3022);
		Set(plane, origin, stride, 2, 2, v3022);

		Set(plane, origin, stride, 0, 1, Avg3(a, b, c));
		byte v1103 = Avg3(b, c, d);
		Set(plane, origin, stride, 1, 1, v1103);
		Set(plane, origin, stride, 0, 3, v1103);
		byte v2113 = Avg3(c, d, e);
		Set(plane, origin, stride, 2, 1, v2113);
		Set(plane, origin, stride, 1, 3, v2113);
		byte v3123 = Avg3(d, e, f);
		Set(plane, origin, stride, 3, 1, v3123);
		Set(plane, origin, stride, 2, 3, v3123);
		Set(plane, origin, stride, 3, 2, Avg3(e, f, g));
		Set(plane, origin, stride, 3, 3, Avg3(f, g, h));
	}

	private static void PredictHu(Span<byte> plane, int origin, int stride)
	{
		int i = plane[origin + (0 * stride) - 1];
		int j = plane[origin + (1 * stride) - 1];
		int k = plane[origin + (2 * stride) - 1];
		int l = plane[origin + (3 * stride) - 1];

		Set(plane, origin, stride, 0, 0, Avg2(i, j));
		byte v2001 = Avg2(j, k);
		Set(plane, origin, stride, 2, 0, v2001);
		Set(plane, origin, stride, 0, 1, v2001);
		byte v2112 = Avg2(k, l);
		Set(plane, origin, stride, 2, 1, v2112);
		Set(plane, origin, stride, 0, 2, v2112);
		Set(plane, origin, stride, 1, 0, Avg3(i, j, k));
		byte v3011 = Avg3(j, k, l);
		Set(plane, origin, stride, 3, 0, v3011);
		Set(plane, origin, stride, 1, 1, v3011);
		byte v3112 = Avg3(k, l, l);
		Set(plane, origin, stride, 3, 1, v3112);
		Set(plane, origin, stride, 1, 2, v3112);
		Set(plane, origin, stride, 3, 2, (byte)l);
		Set(plane, origin, stride, 2, 2, (byte)l);
		Set(plane, origin, stride, 0, 3, (byte)l);
		Set(plane, origin, stride, 1, 3, (byte)l);
		Set(plane, origin, stride, 2, 3, (byte)l);
		Set(plane, origin, stride, 3, 3, (byte)l);
	}

	private static void PredictHd(Span<byte> plane, int origin, int stride)
	{
		int i = plane[origin + (0 * stride) - 1];
		int j = plane[origin + (1 * stride) - 1];
		int k = plane[origin + (2 * stride) - 1];
		int l = plane[origin + (3 * stride) - 1];
		int x = plane[origin - stride - 1];
		int a = plane[origin - stride + 0];
		int b = plane[origin - stride + 1];
		int c = plane[origin - stride + 2];

		byte v0021 = Avg2(i, x);
		Set(plane, origin, stride, 0, 0, v0021);
		Set(plane, origin, stride, 2, 1, v0021);
		byte v0122 = Avg2(j, i);
		Set(plane, origin, stride, 0, 1, v0122);
		Set(plane, origin, stride, 2, 2, v0122);
		byte v0223 = Avg2(k, j);
		Set(plane, origin, stride, 0, 2, v0223);
		Set(plane, origin, stride, 2, 3, v0223);
		Set(plane, origin, stride, 0, 3, Avg2(l, k));

		Set(plane, origin, stride, 3, 0, Avg3(a, b, c));
		Set(plane, origin, stride, 2, 0, Avg3(x, a, b));
		byte v1031 = Avg3(i, x, a);
		Set(plane, origin, stride, 1, 0, v1031);
		Set(plane, origin, stride, 3, 1, v1031);
		byte v1132 = Avg3(j, i, x);
		Set(plane, origin, stride, 1, 1, v1132);
		Set(plane, origin, stride, 3, 2, v1132);
		byte v1233 = Avg3(k, j, i);
		Set(plane, origin, stride, 1, 2, v1233);
		Set(plane, origin, stride, 3, 3, v1233);
		Set(plane, origin, stride, 1, 3, Avg3(l, k, j));
	}
}