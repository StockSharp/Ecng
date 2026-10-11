namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.LoopFilter;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8NormalLoopFilter
{
	public static void FilterTopEdge16(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterMacroblockEdge(plane, origin, stride, 1, 16, thresh, interiorLimit, hevThresh);

	public static void FilterLeftEdge16(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterMacroblockEdge(plane, origin, 1, stride, 16, thresh, interiorLimit, hevThresh);

	public static void FilterTopEdgesInner16(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh)
	{
		for (int k = 1; k <= 3; k++)
		{
			FilterTopEdgeInner16(plane, origin + (4 * k * stride), stride, thresh, interiorLimit, hevThresh);
		}
	}
	public static void FilterTopEdgeInner16(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterInnerEdge(plane, origin, stride, 1, 16, thresh, interiorLimit, hevThresh);

	public static void FilterLeftEdgesInner16(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh)
	{
		for (int k = 1; k <= 3; k++)
		{
			FilterLeftEdgeInner16(plane, origin + (4 * k), stride, thresh, interiorLimit, hevThresh);
		}
	}
	public static void FilterLeftEdgeInner16(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterInnerEdge(plane, origin, 1, stride, 16, thresh, interiorLimit, hevThresh);

	public static void FilterTopEdge8(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterMacroblockEdge(plane, origin, stride, 1, 8, thresh, interiorLimit, hevThresh);

	public static void FilterLeftEdge8(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterMacroblockEdge(plane, origin, 1, stride, 8, thresh, interiorLimit, hevThresh);

	public static void FilterTopEdgeInner8(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterInnerEdge(plane, origin + (4 * stride), stride, 1, 8, thresh, interiorLimit, hevThresh);

	public static void FilterLeftEdgeInner8(Span<byte> plane, int origin, int stride, int thresh, int interiorLimit, int hevThresh) =>
		FilterInnerEdge(plane, origin + 4, 1, stride, 8, thresh, interiorLimit, hevThresh);

	private static void FilterMacroblockEdge(Span<byte> plane, int origin, int acrossStep, int alongStep, int size, int thresh, int interiorLimit, int hevThresh)
	{
		// alongStep == 1 means the lanes are adjacent bytes (a horizontal edge); otherwise they are a stride
		// apart (a vertical edge) and need a transpose. Vp8VectorLoopFilter handles both.
#if NET7_0_OR_GREATER
		if (alongStep == 1)
		{
			if (Vp8VectorLoopFilter.CanFilter(size, origin, acrossStep, plane.Length))
			{
				Vp8VectorLoopFilter.FilterContiguousEdge(plane, origin, acrossStep, size, thresh, interiorLimit, hevThresh, macroblockEdge: true);
				return;
			}
		}
		else if (acrossStep == 1 && Vp8VectorLoopFilter.CanFilterStrided(size, origin, alongStep, plane.Length))
		{
			Vp8VectorLoopFilter.FilterStridedEdge(plane, origin, alongStep, size, thresh, interiorLimit, hevThresh, macroblockEdge: true);
			return;
		}
#endif

		int thresh2 = (2 * thresh) + 1;
		for (int i = 0; i < size; i++)
		{
			int pos = origin + (i * alongStep);
			if (Vp8LoopFilterThresholds.NeedsFilter2(plane, pos, acrossStep, thresh2, interiorLimit))
			{
				if (Vp8LoopFilterThresholds.Hev(plane, pos, acrossStep, hevThresh))
				{
					Vp8LoopFilterThresholds.DoFilter2(plane, pos, acrossStep);
				}
				else
				{
					DoFilter6(plane, pos, acrossStep);
				}
			}
		}
	}

	private static void FilterInnerEdge(Span<byte> plane, int origin, int acrossStep, int alongStep, int size, int thresh, int interiorLimit, int hevThresh)
	{
#if NET7_0_OR_GREATER
		if (alongStep == 1)
		{
			if (Vp8VectorLoopFilter.CanFilter(size, origin, acrossStep, plane.Length))
			{
				Vp8VectorLoopFilter.FilterContiguousEdge(plane, origin, acrossStep, size, thresh, interiorLimit, hevThresh, macroblockEdge: false);
				return;
			}
		}
		else if (acrossStep == 1 && Vp8VectorLoopFilter.CanFilterStrided(size, origin, alongStep, plane.Length))
		{
			Vp8VectorLoopFilter.FilterStridedEdge(plane, origin, alongStep, size, thresh, interiorLimit, hevThresh, macroblockEdge: false);
			return;
		}
#endif

		int thresh2 = (2 * thresh) + 1;
		for (int i = 0; i < size; i++)
		{
			int pos = origin + (i * alongStep);
			if (Vp8LoopFilterThresholds.NeedsFilter2(plane, pos, acrossStep, thresh2, interiorLimit))
			{
				if (Vp8LoopFilterThresholds.Hev(plane, pos, acrossStep, hevThresh))
				{
					Vp8LoopFilterThresholds.DoFilter2(plane, pos, acrossStep);
				}
				else
				{
					DoFilter4(plane, pos, acrossStep);
				}
			}
		}
	}
	private static void DoFilter4(Span<byte> p, int pos, int step)
	{
		int p1 = p[pos - (2 * step)];
		int p0 = p[pos - step];
		int q0 = p[pos];
		int q1 = p[pos + step];

		int a = 3 * (q0 - p0);
		int a1 = Vp8LoopFilterThresholds.SClip2((a + 4) >> 3);
		int a2 = Vp8LoopFilterThresholds.SClip2((a + 3) >> 3);
		int a3 = (a1 + 1) >> 1;

		p[pos - (2 * step)] = Vp8LoopFilterThresholds.Clip1(p1 + a3);
		p[pos - step] = Vp8LoopFilterThresholds.Clip1(p0 + a2);
		p[pos] = Vp8LoopFilterThresholds.Clip1(q0 - a1);
		p[pos + step] = Vp8LoopFilterThresholds.Clip1(q1 - a3);
	}
	private static void DoFilter6(Span<byte> p, int pos, int step)
	{
		int p2 = p[pos - (3 * step)];
		int p1 = p[pos - (2 * step)];
		int p0 = p[pos - step];
		int q0 = p[pos];
		int q1 = p[pos + step];
		int q2 = p[pos + (2 * step)];

		int a = Vp8LoopFilterThresholds.SClip1((3 * (q0 - p0)) + Vp8LoopFilterThresholds.SClip1(p1 - q1));
		int a1 = ((27 * a) + 63) >> 7;
		int a2 = ((18 * a) + 63) >> 7;
		int a3 = ((9 * a) + 63) >> 7;

		p[pos - (3 * step)] = Vp8LoopFilterThresholds.Clip1(p2 + a3);
		p[pos - (2 * step)] = Vp8LoopFilterThresholds.Clip1(p1 + a2);
		p[pos - step] = Vp8LoopFilterThresholds.Clip1(p0 + a1);
		p[pos] = Vp8LoopFilterThresholds.Clip1(q0 - a1);
		p[pos + step] = Vp8LoopFilterThresholds.Clip1(q1 - a2);
		p[pos + (2 * step)] = Vp8LoopFilterThresholds.Clip1(q2 - a3);
	}
}