namespace Ecng.Drawing.Codecs.Formats.Webp.Decoding.Alpha;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;

internal static class WebpAlphaFilter
{
	public static void Reverse(WebpAlphaFilterMethods method, Span<byte> plane, int width, int height)
	{
		if (width <= 0 || height <= 0 || method == WebpAlphaFilterMethods.None) return;
		for (var y = 0; y < height; y++)
		{
			var previous = y == 0 ? ReadOnlySpan<byte>.Empty : plane.Slice((y - 1) * width, width);
			ReverseRow(method, plane.Slice(y * width, width), previous, y == 0);
		}
	}
	private static void ReverseHorizontalRow(Span<byte> row, byte initialPredictor)
	{
		byte predictor = initialPredictor;
		for (int x = 0; x < row.Length; x++)
		{
			row[x] = (byte)(row[x] + predictor);
			predictor = row[x];
		}
	}
	public static void ReverseRow(WebpAlphaFilterMethods method, Span<byte> row, ReadOnlySpan<byte> previous, bool first)
	{
		if (method == WebpAlphaFilterMethods.None) return;
		if (first || method == WebpAlphaFilterMethods.Horizontal)
		{
			ReverseHorizontalRow(row, first ? (byte)0 : previous[0]);
			return;
		}
		if (method == WebpAlphaFilterMethods.Vertical)
		{
			for (var x = 0; x < row.Length; x++) row[x] = unchecked((byte)(row[x] + previous[x]));
			return;
		}
		byte left = previous[0], topLeft = previous[0];
		for (var x = 0; x < row.Length; x++)
		{
			var top = previous[x];
			row[x] = unchecked((byte)(row[x] + Clip255(left + top - topLeft)));
			left = row[x];
			topLeft = top;
		}
	}
	private static byte Clip255(int value) => (byte)Math.Clamp(value, 0, 255);
}
