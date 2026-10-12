namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class WebpHuffmanEncoder
{
	private static readonly int[] _lengthOrder = [17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
	private readonly byte[] _lengths;
	private readonly uint[] _codes;
	private readonly int _single;

	public WebpHuffmanEncoder(int[] frequencies, int maxBits)
	{
		var weights = frequencies.Select(value => (long)value).ToArray();
		if (!weights.Any(value => value != 0)) weights[0] = 1;
		_lengths = BuildLengths(weights, maxBits);
		_single = Array.FindIndex(_lengths, length => length != 0);
		if (_lengths.Count(length => length != 0) != 1) _single = -1;
		_codes = new uint[_lengths.Length];
		var counts = new int[maxBits + 1];
		foreach (var length in _lengths) counts[length]++;
		counts[0] = 0;
		var next = new int[maxBits + 1];
		for (var bits = 1; bits <= maxBits; bits++) next[bits] = (next[bits - 1] + counts[bits - 1]) << 1;
		for (var symbol = 0; symbol < _lengths.Length; symbol++)
		{
			var length = _lengths[symbol];
			if (length == 0) continue;
			var code = next[length]++;
			for (var bit = 0; bit < length; bit++) _codes[symbol] = (_codes[symbol] << 1) | (uint)((code >> bit) & 1);
		}
	}

	public void Write(WebpBitWriter writer, int symbol)
	{
		if (_single < 0) writer.Write(_codes[symbol], _lengths[symbol]);
	}

	public void WriteHeader(WebpBitWriter writer)
	{
		if (_single is >= 0 and < 256)
		{
			writer.Write(1, 1);
			writer.Write(0, 1);
			writer.Write(_single > 1 ? 1u : 0u, 1);
			writer.Write((uint)_single, _single > 1 ? 8 : 1);
			return;
		}
		var tokens = new List<(int symbol, int extra, int bits)>();
		var frequencies = new int[19];
		for (var at = 0; at < _lengths.Length;)
		{
			var length = _lengths[at];
			var end = at + 1;
			while (end < _lengths.Length && _lengths[end] == length) end++;
			var remaining = end - at;
			if (length != 0)
			{
				tokens.Add((length, 0, 0));
				frequencies[length]++;
				remaining--;
			}
			while (remaining > 0)
			{
				var symbol = (int)length;
				var count = 1;
				var bits = 0;
				var offset = 0;
				if (length == 0 && remaining >= 11) { symbol = 18; count = Math.Min(138, remaining); bits = 7; offset = 11; }
				else if (length == 0 && remaining >= 3) { symbol = 17; count = Math.Min(10, remaining); bits = 3; offset = 3; }
				else if (length != 0 && remaining >= 3) { symbol = 16; count = Math.Min(6, remaining); bits = 2; offset = 3; }
				tokens.Add((symbol, bits == 0 ? 0 : count - offset, bits));
				frequencies[symbol]++;
				remaining -= count;
			}
			at = end;
		}
		var lengths = new WebpHuffmanEncoder(frequencies, 7);
		var codes = _lengthOrder.Length;
		while (codes > 4 && lengths._lengths[_lengthOrder[codes - 1]] == 0) codes--;
		writer.Write(0, 1);
		writer.Write((uint)(codes - 4), 4);
		for (var at = 0; at < codes; at++) writer.Write(lengths._lengths[_lengthOrder[at]], 3);
		writer.Write(0, 1);
		foreach (var token in tokens)
		{
			lengths.Write(writer, token.symbol);
			writer.Write((uint)token.extra, token.bits);
		}
	}

	private static byte[] BuildLengths(long[] weights, int maxBits)
	{
		while (true)
		{
			var queue = new PriorityQueue<Node, (long weight, int order)>();
			var order = 0;
			for (var symbol = 0; symbol < weights.Length; symbol++)
				if (weights[symbol] > 0) queue.Enqueue(new(symbol, null, null), (weights[symbol], order++));
			while (queue.Count > 1)
			{
				queue.TryDequeue(out var left, out var a);
				queue.TryDequeue(out var right, out var b);
				queue.Enqueue(new(-1, left, right), (a.weight + b.weight, order++));
			}
			var lengths = new byte[weights.Length];
			Visit(queue.Dequeue(), 0, lengths);
			if (lengths.Max() <= maxBits) return lengths;
			for (var symbol = 0; symbol < weights.Length; symbol++)
				if (weights[symbol] > 0) weights[symbol] = Math.Max(1, weights[symbol] >> 1);
		}
	}

	private static void Visit(Node node, int depth, byte[] lengths)
	{
		if (node.Symbol >= 0) lengths[node.Symbol] = (byte)Math.Max(1, depth);
		else
		{
			Visit(node.Left, depth + 1, lengths);
			Visit(node.Right, depth + 1, lengths);
		}
	}

	private sealed record Node(int Symbol, Node Left, Node Right);
}
