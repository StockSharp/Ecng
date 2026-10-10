namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>
/// A JPEG Huffman table: codes are assigned in order of length, so a code is found by comparing
/// it with the largest code of every length in turn.
/// </summary>
internal sealed class JpegHuffmanTable
{
	private const int _maxLength = 16;
	private const int _symbolCount = 256;

	private readonly int[] _minCode = new int[_maxLength + 1];
	private readonly int[] _maxCode = new int[_maxLength + 1];
	private readonly int[] _firstSymbol = new int[_maxLength + 1];
	private readonly byte[] _symbols;

	/// <param name="counts">How many codes there are of each length from 1 to 16.</param>
	/// <param name="symbols">The symbols, in order of their codes.</param>
	public JpegHuffmanTable(ReadOnlySpan<byte> counts, ReadOnlySpan<byte> symbols)
	{
		_symbols = symbols.ToArray();

		var code = 0;
		var index = 0;

		for (var length = 1; length <= _maxLength; length++)
		{
			var count = counts[length - 1];

			_firstSymbol[length] = index;
			_minCode[length] = code;
			_maxCode[length] = count == 0 ? -1 : code + count - 1;

			code += count;
			index += count;

			if (code > 1 << length)
				throw new InvalidDataException("JPEG Huffman table has more codes than its lengths allow.");

			code <<= 1;
		}
	}

	/// <summary>
	/// Works out the table that makes a scan shortest: the more often a symbol occurs, the shorter its
	/// code (section K.2 of the standard). No code is longer than sixteen bits, and none is all ones.
	/// </summary>
	/// <param name="frequencies">How many times each of the 256 symbols occurs; at least one of them does.</param>
	/// <param name="counts">Receives how many codes there are of each length from 1 to 16.</param>
	/// <returns>The symbols that occur, in order of their codes.</returns>
	public static byte[] Optimize(ReadOnlySpan<int> frequencies, Span<byte> counts)
	{
		// One symbol more than there are: it ends up with the longest code, the one of all ones.
		Span<int> frequency = stackalloc int[_symbolCount + 1];
		frequencies.CopyTo(frequency);
		frequency[_symbolCount] = 1;

		// The length of the code of every symbol, and the next symbol of the branch it hangs on.
		Span<int> length = stackalloc int[_symbolCount + 1];
		Span<int> next = stackalloc int[_symbolCount + 1];
		next.Fill(-1);

		while (true)
		{
			// The two rarest; of equals the one with the greater value.
			var first = -1;
			var second = -1;
			var least = int.MaxValue;

			for (var i = 0; i <= _symbolCount; i++)
			{
				if (frequency[i] != 0 && frequency[i] <= least)
				{
					least = frequency[i];
					first = i;
				}
			}

			least = int.MaxValue;

			for (var i = 0; i <= _symbolCount; i++)
			{
				if (frequency[i] != 0 && frequency[i] <= least && i != first)
				{
					least = frequency[i];
					second = i;
				}
			}

			if (second < 0)
				break;

			frequency[first] += frequency[second];
			frequency[second] = 0;

			// Both become branches of one node: every symbol under them gets a bit longer.
			for (length[first]++; next[first] >= 0; length[first]++)
				first = next[first];

			next[first] = second;

			for (length[second]++; next[second] >= 0; length[second]++)
				second = next[second];
		}

		// A code is as long at most as there are symbols.
		Span<int> codes = stackalloc int[_symbolCount + 2];

		for (var i = 0; i <= _symbolCount; i++)
			codes[length[i]]++;

		codes[0] = 0;

		// A pair of codes that are too long moves up a bit, and a shorter code moves down to make room for it.
		for (var size = _symbolCount + 1; size > _maxLength; size--)
			while (codes[size] > 0)
			{
				var shorter = size - 2;

				while (codes[shorter] == 0)
					shorter--;

				codes[size] -= 2;
				codes[size - 1]++;
				codes[shorter + 1] += 2;
				codes[shorter]--;
			}

		// The extra symbol gives its code back.
		var longest = _maxLength;

		while (codes[longest] == 0)
			longest--;

		codes[longest]--;

		var total = 0;

		for (var size = 1; size <= _maxLength; size++)
		{
			counts[size - 1] = (byte)codes[size];
			total += codes[size];
		}

		// The symbols by the length their codes had before they were shortened: the order is the same.
		var symbols = new byte[total];
		var at = 0;

		for (var size = 1; size <= _symbolCount; size++)
			for (var i = 0; i < _symbolCount; i++)
			{
				if (length[i] == size)
					symbols[at++] = (byte)i;
			}

		return symbols;
	}

	public int Decode(JpegBitReader bits)
	{
		var code = 0;

		for (var length = 1; length <= _maxLength; length++)
		{
			code = code << 1 | bits.ReadBit();

			if (code <= _maxCode[length])
				return _symbols[_firstSymbol[length] + code - _minCode[length]];
		}

		throw new InvalidDataException("JPEG Huffman code is not in the table.");
	}
}
