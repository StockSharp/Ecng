namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.Numerics;

using Ecng.Drawing.Codecs.Formats.Webp.Decoding.Vp8L;

internal static class WebpLosslessEncoder
{
	private const int _blockBits = 4;
	private const int _cacheBits = 8;
	private static readonly WebpPredictorModes[] _modes = [WebpPredictorModes.Left, WebpPredictorModes.Top, WebpPredictorModes.Average, WebpPredictorModes.Select, WebpPredictorModes.Gradient];

	public static byte[] Encode(RasterImage image)
	{
		var colors = new uint[image.Width * image.Height];
		var alpha = false;
		for (var pixel = 0; pixel < colors.Length; pixel++)
		{
			var at = pixel * 4;
			colors[pixel] = (uint)image.Pixels[at + 3] << 24 | (uint)image.Pixels[at] << 16 | (uint)image.Pixels[at + 1] << 8 | image.Pixels[at + 2];
			alpha |= image.Pixels[at + 3] != 255;
		}
		using var writer = new WebpBitWriter();
		writer.Write(0x2f, 8);
		writer.Write((uint)(image.Width - 1), 14);
		writer.Write((uint)(image.Height - 1), 14);
		writer.Write(alpha ? 1u : 0u, 1);
		writer.Write(0, 3);
		var palette = new Dictionary<uint, int>();
		foreach (var color in colors)
		{
			if (!palette.ContainsKey(color)) palette.Add(color, palette.Count);
			if (palette.Count > 256) break;
		}
		var width = image.Width;
		if (palette.Count <= 256)
		{
			writer.Write(1, 1);
			writer.Write((uint)Vp8LTransformTypes.ColorIndexing, 2);
			writer.Write((uint)(palette.Count - 1), 8);
			var table = new uint[palette.Count];
			uint previous = 0;
			foreach (var pair in palette)
			{
				table[pair.Value] = Subtract(pair.Key, previous);
				previous = pair.Key;
			}
			WritePixels(writer, table, palette.Count, false);
			var bits = palette.Count <= 2 ? 1 : palette.Count <= 4 ? 2 : palette.Count <= 16 ? 4 : 8;
			var packed = 8 / bits;
			width = (width + packed - 1) / packed;
			var indices = new uint[width * image.Height];
			for (var y = 0; y < image.Height; y++)
				for (var x = 0; x < image.Width; x++) indices[y * width + x / packed] |= (uint)palette[colors[y * image.Width + x]] << (8 + x % packed * bits);
			colors = indices;
		}
		else
		{
			writer.Write(1, 1);
			writer.Write((uint)Vp8LTransformTypes.SubtractGreen, 2);
			for (var pixel = 0; pixel < colors.Length; pixel++)
			{
				var color = colors[pixel];
				var green = (color >> 8) & 255;
				colors[pixel] = (color & 0xff00ff00u) | (((color >> 16) - green) & 255) << 16 | ((color - green) & 255);
			}
			var mapWidth = (width + (1 << _blockBits) - 1) >> _blockBits;
			var mapHeight = (image.Height + (1 << _blockBits) - 1) >> _blockBits;
			var map = new uint[mapWidth * mapHeight];
			for (var by = 0; by < mapHeight; by++)
				for (var bx = 0; bx < mapWidth; bx++)
				{
					var best = long.MaxValue;
					foreach (var mode in _modes)
					{
						long score = 0;
						for (var y = by << _blockBits; y < Math.Min(image.Height, (by + 1) << _blockBits); y++)
							for (var x = bx << _blockBits; x < Math.Min(width, (bx + 1) << _blockBits); x++)
							{
								var residual = Subtract(colors[y * width + x], Predict(colors, width, x, y, mode));
								for (var shift = 0; shift < 32; shift += 8)
								{
									var value = (int)((residual >> shift) & 255);
									score += Math.Min(value, 256 - value);
								}
							}
						if (score < best) { best = score; map[by * mapWidth + bx] = (uint)mode << 8; }
					}
				}
			writer.Write(1, 1);
			writer.Write((uint)Vp8LTransformTypes.Predictor, 2);
			writer.Write(_blockBits - 2, 3);
			WritePixels(writer, map, mapWidth, false);
			for (var y = image.Height - 1; y >= 0; y--)
				for (var x = width - 1; x >= 0; x--)
				{
					var mode = (WebpPredictorModes)(map[(y >> _blockBits) * mapWidth + (x >> _blockBits)] >> 8);
					colors[y * width + x] = Subtract(colors[y * width + x], Predict(colors, width, x, y, mode));
				}
		}
		writer.Write(0, 1);
		WritePixels(writer, colors, width, true);
		return writer.Finish();
	}

	private static void WritePixels(WebpBitWriter writer, uint[] colors, int width, bool isMain)
	{
		var cacheBits = isMain ? _cacheBits : 0;
		writer.Write(cacheBits == 0 ? 0u : 1u, 1);
		if (cacheBits != 0) writer.Write((uint)cacheBits, 4);
		if (isMain) writer.Write(0, 1);
		int[][] frequencies = [new int[280 + (cacheBits == 0 ? 0 : 1 << cacheBits)], new int[256], new int[256], new int[256], new int[40]];
		var previous = new int[colors.Length];
		Parse(colors, width, cacheBits, previous, frequencies, null, null);
		var trees = new WebpHuffmanEncoder[5];
		for (var tree = 0; tree < trees.Length; tree++)
		{
			trees[tree] = new(frequencies[tree], 15);
			trees[tree].WriteHeader(writer);
		}
		Parse(colors, width, cacheBits, previous, frequencies, trees, writer);
	}

	private static void Parse(uint[] colors, int width, int cacheBits, int[] previous, int[][] frequencies, WebpHuffmanEncoder[] trees, WebpBitWriter writer)
	{
		var heads = new int[65536];
		Array.Fill(heads, -1);
		var cache = new uint[cacheBits == 0 ? 0 : 1 << cacheBits];
		int[] nearby = [width, 1];
		for (var at = 0; at < colors.Length;)
		{
			var length = 0;
			var distance = 0;
			var maximum = Math.Min(4096, colors.Length - at);
			foreach (var offset in nearby)
			{
				if (offset > at) continue;
				var count = 0;
				while (count < maximum && colors[at - offset + count] == colors[at + count]) count++;
				if (count > length) { length = count; distance = offset; }
			}
			if (at + 2 < colors.Length)
			{
				var hash = Hash(colors, at);
				var candidate = heads[hash];
				previous[at] = candidate;
				heads[hash] = at;
				for (var tries = 0; candidate >= 0 && at - candidate <= 262144 && tries < 24; tries++, candidate = previous[candidate])
				{
					var count = 0;
					while (count < maximum && colors[candidate + count] == colors[at + count]) count++;
					if (count > length) { length = count; distance = at - candidate; }
					if (length == maximum) break;
				}
			}
			if (length >= 3)
			{
				Prefix(length, out var code, out var extra, out var bits);
				Symbol(0, 256 + code, frequencies, trees, writer);
				writer?.Write((uint)extra, bits);
				Prefix(distance == width ? 1 : distance == 1 ? 2 : distance + 120, out code, out extra, out bits);
				Symbol(4, code, frequencies, trees, writer);
				writer?.Write((uint)extra, bits);
			}
			else
			{
				length = 1;
				var color = colors[at];
				var slot = cacheBits == 0 ? 0 : (int)(unchecked(color * 0x1e35a7bdu) >> (32 - cacheBits));
				if (cacheBits != 0 && cache[slot] == color) Symbol(0, 280 + slot, frequencies, trees, writer);
				else
				{
					Symbol(0, (int)((color >> 8) & 255), frequencies, trees, writer);
					Symbol(1, (int)((color >> 16) & 255), frequencies, trees, writer);
					Symbol(2, (int)(color & 255), frequencies, trees, writer);
					Symbol(3, (int)(color >> 24), frequencies, trees, writer);
				}
			}
			for (var index = at; index < at + length; index++)
			{
				if (index > at && index + 2 < colors.Length)
				{
					var hash = Hash(colors, index);
					previous[index] = heads[hash];
					heads[hash] = index;
				}
				if (cacheBits != 0) cache[unchecked(colors[index] * 0x1e35a7bdu) >> (32 - cacheBits)] = colors[index];
			}
			at += length;
		}
	}

	private static int Hash(uint[] colors, int at)
		=> (int)(unchecked(colors[at] * 0x9e3779b1u + colors[at + 1] * 0x85ebca6bu + colors[at + 2] * 0xc2b2ae35u) >> 16);

	private static void Symbol(int tree, int symbol, int[][] frequencies, WebpHuffmanEncoder[] trees, WebpBitWriter writer)
	{
		if (writer is null) frequencies[tree][symbol]++;
		else trees[tree].Write(writer, symbol);
	}

	private static void Prefix(int value, out int code, out int extra, out int bits)
	{
		var number = value - 1;
		var high = number < 4 ? 0 : BitOperations.Log2((uint)number);
		code = number < 4 ? number : high * 2 + ((number >> (high - 1)) & 1);
		bits = code < 4 ? 0 : (code - 2) >> 1;
		extra = bits == 0 ? 0 : number - ((2 + (code & 1)) << bits);
	}

	private static uint Subtract(uint color, uint prediction)
		=> (((color & 0x00ff00ffu) | 0x01000100u) - (prediction & 0x00ff00ffu)) & 0x00ff00ffu
			| (((((color >> 8) & 0x00ff00ffu) | 0x01000100u) - ((prediction >> 8) & 0x00ff00ffu)) & 0x00ff00ffu) << 8;

	private static uint Predict(uint[] colors, int width, int x, int y, WebpPredictorModes mode)
	{
		if (y == 0) return x == 0 ? 0xff000000u : colors[x - 1];
		var top = colors[(y - 1) * width + x];
		if (x == 0 || mode == WebpPredictorModes.Top) return top;
		var left = colors[y * width + x - 1];
		if (mode == WebpPredictorModes.Left) return left;
		var corner = colors[(y - 1) * width + x - 1];
		if (mode == WebpPredictorModes.Average) return (left & top) + (((left ^ top) & 0xfefefefeu) >> 1);
		uint result = 0;
		var leftScore = 0;
		var topScore = 0;
		for (var shift = 0; shift < 32; shift += 8)
		{
			var l = (int)((left >> shift) & 255);
			var t = (int)((top >> shift) & 255);
			var c = (int)((corner >> shift) & 255);
			var estimate = l + t - c;
			result |= (uint)Math.Clamp(estimate, 0, 255) << shift;
			leftScore += Math.Abs(estimate - l);
			topScore += Math.Abs(estimate - t);
		}
		return mode == WebpPredictorModes.Select ? leftScore < topScore ? left : top : result;
	}
}
