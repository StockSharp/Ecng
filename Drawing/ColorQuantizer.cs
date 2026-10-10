namespace Ecng.Drawing;

using System;
using System.Collections.Generic;

/// <summary>
/// Chooses the colors of an indexed picture by median cut: the colors in use are split, box by box,
/// along the channel they spread widest on, at the point that halves the pixels.
/// </summary>
internal static class ColorQuantizer
{
	/// <summary>
	/// An alpha below this is transparent in a format that knows only on and off.
	/// </summary>
	public const int OpaqueAlpha = 128;

	// Above this many different colors they are counted in cells of 4x4x4 instead of one by one.
	private const int _maxExactColors = 1 << 16;
	private const int _cellShift = 2;
	private const int _cellBits = 8 - _cellShift;

	// A run of colors in the order array, with what is known about it.
	private struct Box
	{
		public int Start;
		public int Length;
		public long Pixels;

		// The channel the colors of the box spread widest on, and how wide.
		public int Channel;
		public int Range;
	}

	/// <summary>
	/// Gives every opaque pixel of a picture one of at most <paramref name="maxColors"/> colors.
	/// A picture with no more colors than that keeps them exactly.
	/// </summary>
	/// <param name="image">The picture.</param>
	/// <param name="maxColors">The most colors to use.</param>
	/// <param name="indices">Receives, for every opaque pixel, the number of its color plus <paramref name="firstIndex"/>. Other pixels are left as they are.</param>
	/// <param name="firstIndex">The number of the first color.</param>
	/// <returns>The colors, three bytes each.</returns>
	public static byte[] Quantize(RasterImage image, int maxColors, byte[] indices, int firstIndex)
	{
		var pixels = image.Pixels;

		// Per entry: the number of its pixels, then the sums of their red, green and blue.
		var sums = new List<long>();

		// Colors one by one while there are few of them; a photograph has more than is worth sorting.
		var entryOfColor = new Dictionary<int, int>();
		int[] entryOfCell = null;

		for (var i = 0; i < pixels.Length; i += 4)
		{
			if (pixels[i + 3] < OpaqueAlpha)
				continue;

			var color = pixels[i] << 16 | pixels[i + 1] << 8 | pixels[i + 2];

			if (!entryOfColor.TryGetValue(color, out var entry))
			{
				if (entryOfColor.Count == _maxExactColors)
				{
					entryOfCell = new int[1 << (3 * _cellBits)];
					break;
				}

				entry = entryOfColor.Count;
				entryOfColor.Add(color, entry);
				AddEntry(sums);
			}

			Add(sums, entry, pixels, i);
		}

		if (entryOfCell is not null)
		{
			sums.Clear();
			Array.Fill(entryOfCell, -1);

			for (var i = 0; i < pixels.Length; i += 4)
			{
				if (pixels[i + 3] < OpaqueAlpha)
					continue;

				var cell = Cell(pixels, i);

				if (entryOfCell[cell] < 0)
				{
					entryOfCell[cell] = sums.Count / 4;
					AddEntry(sums);
				}

				Add(sums, entryOfCell[cell], pixels, i);
			}
		}

		var count = sums.Count / 4;

		if (count == 0)
			return [];

		// What is cut is the mean color of every entry, weighted by its pixels.
		var colors = new byte[count * 3];
		var weights = new int[count];

		for (var entry = 0; entry < count; entry++)
		{
			var weight = sums[entry * 4];
			weights[entry] = (int)weight;

			for (var channel = 0; channel < 3; channel++)
				colors[entry * 3 + channel] = (byte)((sums[entry * 4 + 1 + channel] + weight / 2) / weight);
		}

		var order = new int[count];

		for (var i = 0; i < count; i++)
			order[i] = i;

		var boxes = new List<Box> { Measure(order, 0, count, colors, weights) };

		while (boxes.Count < maxColors)
		{
			// The box that matters most: many pixels spread over a wide range.
			var widest = -1;
			var best = 0L;

			for (var i = 0; i < boxes.Count; i++)
			{
				var score = boxes[i].Pixels * boxes[i].Range;

				if (boxes[i].Length > 1 && score > best)
				{
					best = score;
					widest = i;
				}
			}

			if (widest < 0)
				break;

			var box = boxes[widest];
			var channel = box.Channel;

			Array.Sort(order, box.Start, box.Length, Comparer<int>.Create((a, b) => colors[a * 3 + channel].CompareTo(colors[b * 3 + channel])));

			// Cut where half of the pixels are behind, leaving something on both sides.
			var half = box.Pixels / 2;
			var behind = 0L;
			var cut = 1;

			for (; cut < box.Length - 1; cut++)
			{
				behind += weights[order[box.Start + cut - 1]];

				if (behind >= half)
					break;
			}

			boxes[widest] = Measure(order, box.Start, cut, colors, weights);
			boxes.Add(Measure(order, box.Start + cut, box.Length - cut, colors, weights));
		}

		var palette = new byte[boxes.Count * 3];
		var boxOfEntry = new byte[count];

		for (var i = 0; i < boxes.Count; i++)
		{
			var box = boxes[i];
			long red = 0, green = 0, blue = 0;

			for (var j = box.Start; j < box.Start + box.Length; j++)
			{
				var entry = order[j];
				boxOfEntry[entry] = (byte)i;
				red += sums[entry * 4 + 1];
				green += sums[entry * 4 + 2];
				blue += sums[entry * 4 + 3];
			}

			palette[i * 3] = (byte)((red + box.Pixels / 2) / box.Pixels);
			palette[i * 3 + 1] = (byte)((green + box.Pixels / 2) / box.Pixels);
			palette[i * 3 + 2] = (byte)((blue + box.Pixels / 2) / box.Pixels);
		}

		// Neighboring pixels are often the same color: the last answer is kept at hand.
		var lastColor = -1;
		var lastIndex = 0;

		for (int i = 0, pixel = 0; i < pixels.Length; i += 4, pixel++)
		{
			if (pixels[i + 3] < OpaqueAlpha)
				continue;

			var color = pixels[i] << 16 | pixels[i + 1] << 8 | pixels[i + 2];

			if (color != lastColor)
			{
				lastColor = color;
				lastIndex = firstIndex + boxOfEntry[entryOfCell is null ? entryOfColor[color] : entryOfCell[Cell(pixels, i)]];
			}

			indices[pixel] = (byte)lastIndex;
		}

		return palette;
	}

	private static int Cell(byte[] pixels, int at)
		=> pixels[at] >> _cellShift << (2 * _cellBits) | pixels[at + 1] >> _cellShift << _cellBits | pixels[at + 2] >> _cellShift;

	private static void AddEntry(List<long> sums)
	{
		for (var i = 0; i < 4; i++)
			sums.Add(0);
	}

	private static void Add(List<long> sums, int entry, byte[] pixels, int at)
	{
		sums[entry * 4]++;
		sums[entry * 4 + 1] += pixels[at];
		sums[entry * 4 + 2] += pixels[at + 1];
		sums[entry * 4 + 3] += pixels[at + 2];
	}

	private static Box Measure(int[] order, int start, int length, byte[] colors, int[] weights)
	{
		var box = new Box { Start = start, Length = length };
		Span<int> min = stackalloc int[] { 255, 255, 255 };
		Span<int> max = stackalloc int[] { 0, 0, 0 };

		for (var i = start; i < start + length; i++)
		{
			var entry = order[i];
			box.Pixels += weights[entry];

			for (var channel = 0; channel < 3; channel++)
			{
				var value = colors[entry * 3 + channel];
				min[channel] = Math.Min(min[channel], value);
				max[channel] = Math.Max(max[channel], value);
			}
		}

		for (var channel = 0; channel < 3; channel++)
		{
			if (max[channel] - min[channel] > box.Range)
			{
				box.Range = max[channel] - min[channel];
				box.Channel = channel;
			}
		}

		return box;
	}
}
