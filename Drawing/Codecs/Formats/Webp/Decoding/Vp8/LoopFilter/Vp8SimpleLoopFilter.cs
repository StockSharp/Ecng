namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8.LoopFilter;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal static class Vp8SimpleLoopFilter
{
	public static void FilterTopEdge(Span<byte> plane, int origin, int stride, int thresh) => FilterEdge(plane, origin, stride, 1, thresh);
	public static void FilterLeftEdge(Span<byte> plane, int origin, int stride, int thresh) => FilterEdge(plane, origin, 1, stride, thresh);

	public static void FilterTopEdgesInner(Span<byte> plane, int origin, int stride, int thresh)
	{
		for (int k = 1; k <= 3; k++)
		{
			FilterTopEdge(plane, origin + (4 * k * stride), stride, thresh);
		}
	}

	public static void FilterLeftEdgesInner(Span<byte> plane, int origin, int stride, int thresh)
	{
		for (int k = 1; k <= 3; k++)
		{
			FilterLeftEdge(plane, origin + (4 * k), stride, thresh);
		}
	}

	private static void FilterEdge(Span<byte> plane, int origin, int acrossStep, int alongStep, int thresh)
	{
		int thresh2 = (2 * thresh) + 1;
		for (int i = 0; i < 16; i++)
		{
			int pos = origin + (i * alongStep);
			if (Vp8LoopFilterThresholds.NeedsFilter(plane, pos, acrossStep, thresh2))
			{
				Vp8LoopFilterThresholds.DoFilter2(plane, pos, acrossStep);
			}
		}
	}
}