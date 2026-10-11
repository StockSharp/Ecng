namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.LoopFilter;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8LoopFilterThresholds
{
	public static byte Clip1(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
	public static int SClip1(int v) => v < -128 ? -128 : v > 127 ? 127 : v;
	public static int SClip2(int v) => v < -16 ? -16 : v > 15 ? 15 : v;

	public static int Abs(int v) => v < 0 ? -v : v;
	public static void DoFilter2(Span<byte> p, int pos, int step)
	{
		int p1 = p[pos - (2 * step)];
		int p0 = p[pos - step];
		int q0 = p[pos];
		int q1 = p[pos + step];

		int a = (3 * (q0 - p0)) + SClip1(p1 - q1);
		int a1 = SClip2((a + 4) >> 3);
		int a2 = SClip2((a + 3) >> 3);

		p[pos - step] = Clip1(p0 + a2);
		p[pos] = Clip1(q0 - a1);
	}
	public static bool Hev(Span<byte> p, int pos, int step, int thresh)
	{
		int p1 = p[pos - (2 * step)];
		int p0 = p[pos - step];
		int q0 = p[pos];
		int q1 = p[pos + step];
		return Abs(p1 - p0) > thresh || Abs(q1 - q0) > thresh;
	}
	public static bool NeedsFilter(Span<byte> p, int pos, int step, int t)
	{
		int p1 = p[pos - (2 * step)];
		int p0 = p[pos - step];
		int q0 = p[pos];
		int q1 = p[pos + step];
		return (4 * Abs(p0 - q0)) + Abs(p1 - q1) <= t;
	}
	public static bool NeedsFilter2(Span<byte> p, int pos, int step, int t, int it)
	{
		int p3 = p[pos - (4 * step)];
		int p2 = p[pos - (3 * step)];
		int p1 = p[pos - (2 * step)];
		int p0 = p[pos - step];
		int q0 = p[pos];
		int q1 = p[pos + step];
		int q2 = p[pos + (2 * step)];
		int q3 = p[pos + (3 * step)];

		if ((4 * Abs(p0 - q0)) + Abs(p1 - q1) > t)
		{
			return false;
		}

		return Abs(p3 - p2) <= it && Abs(p2 - p1) <= it && Abs(p1 - p0) <= it &&
			   Abs(q3 - q2) <= it && Abs(q2 - q1) <= it && Abs(q1 - q0) <= it;
	}
}