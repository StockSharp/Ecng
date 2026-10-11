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
		if (width <= 0 || height <= 0 || method == WebpAlphaFilterMethods.None)
		{
			return;
		}

		switch (method)
		{
			case WebpAlphaFilterMethods.Horizontal:
				for (int y = 0; y < height; y++)
				{
					var row = plane.Slice(y * width, width);
					byte initialPredictor = y == 0 ? (byte)0 : plane[(y - 1) * width];
					ReverseHorizontalRow(row, initialPredictor);
				}

				return;

			case WebpAlphaFilterMethods.Vertical:
				ReverseHorizontalRow(plane[..width], 0);
				for (int y = 1; y < height; y++)
				{
					var row = plane.Slice(y * width, width);
					var previousRow = plane.Slice((y - 1) * width, width);
					for (var x = 0; x < width; x++)
						row[x] = unchecked((byte)(row[x] + previousRow[x]));
				}

				return;

			case WebpAlphaFilterMethods.Gradient:
				ReverseHorizontalRow(plane[..width], 0);
				for (int y = 1; y < height; y++)
				{
					ReverseGradientRow(plane, width, y);
				}

				return;
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
	private static void ReverseGradientRow(Span<byte> plane, int width, int y)
	{
		var row = plane.Slice(y * width, width);
		var previousRow = plane.Slice((y - 1) * width, width);

		byte left = previousRow[0];
		byte topLeft = previousRow[0];

		for (int x = 0; x < width; x++)
		{
			byte top = previousRow[x];
			int predicted = Clip255(left + top - topLeft);
			row[x] = (byte)(row[x] + predicted);
			left = row[x];
			topLeft = top;
		}
	}

	private static byte Clip255(int value) => (byte)Math.Clamp(value, 0, 255);
}
