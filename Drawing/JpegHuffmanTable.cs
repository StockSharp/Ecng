namespace Ecng.Drawing;

using System;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// Canonical JPEG Huffman codes with an eight-bit lookup and compact tables for longer prefixes.
/// </summary>
internal sealed class JpegHuffmanTable
{
	private const int _maxLength = 16;
	private const int _symbolCount = 256;

	private readonly int[] _minCode = new int[_maxLength + 1];
	private readonly int[] _maxCode = new int[_maxLength + 1];
	private readonly int[] _firstSymbol = new int[_maxLength + 1];
	private readonly byte[] _symbols;
	private readonly ushort[] _shortCodes = new ushort[256];
	private readonly byte[] _longBits = new byte[256];
	private readonly int[] _longOffsets = new int[256];
	private readonly ushort[] _longCodes;

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
			if (index > _symbols.Length)
				throw new InvalidDataException("JPEG Huffman table has fewer symbols than its code lengths declare.");

			if (length <= 8)
				for (var i = 0; i < count; i++)
				{
					var prefix = (_minCode[length] + i) << (8 - length);
					for (var tail = 0; tail < 1 << (8 - length); tail++)
					{
						_shortCodes[prefix + tail] = (ushort)(length << 8 | _symbols[_firstSymbol[length] + i]);
					}
				}

			code <<= 1;
		}
		for (var length = 9; length <= _maxLength; length++)
			for (var i = 0; i < counts[length - 1]; i++)
				_longBits[(_minCode[length] + i) >> (length - 8)] = (byte)(length - 8);
		var longCount = 0;
		for (var prefix = 0; prefix < _longBits.Length; prefix++)
		{
			_longOffsets[prefix] = longCount;
			if (_longBits[prefix] != 0) longCount += 1 << _longBits[prefix];
		}
		_longCodes = new ushort[longCount];
		for (var length = 9; length <= _maxLength; length++)
			for (var i = 0; i < counts[length - 1]; i++)
			{
				var value = _minCode[length] + i;
				var prefix = value >> (length - 8);
				var padding = _longBits[prefix] - (length - 8);
				var offset = _longOffsets[prefix] + ((value & ((1 << (length - 8)) - 1)) << padding);
				for (var tail = 0; tail < 1 << padding; tail++)
					_longCodes[offset + tail] = (ushort)(length << 8 | _symbols[_firstSymbol[length] + i]);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Decode(JpegBitReader bits)
	{
		if (bits.TryPeek(8, out var prefix))
		{
			var code = _shortCodes[prefix];
			var length = code >> 8;
			if (length != 0)
			{
				bits.Skip(length);
				return code & 255;
			}
			var extra = _longBits[prefix];
			if (extra != 0 && bits.TryPeek(8 + extra, out var window))
			{
				code = _longCodes[_longOffsets[prefix] + (window & ((1 << extra) - 1))];
				length = code >> 8;
				if (length != 0)
				{
					bits.Skip(length);
					return code & 255;
				}
			}
		}
		return DecodeLong(bits);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int DecodeValue(JpegBitReader bits, out int value)
	{
		if (bits.TryPeek(16, out var window))
		{
			var code = _shortCodes[window >> 8];
			var length = code >> 8;
			var symbol = code & 255;
			var magnitude = symbol & 15;
			var total = length + magnitude;
			if (length != 0 && total <= 16)
			{
				bits.Skip(total);
				var mask = (1 << magnitude) - 1;
				value = (window >> (16 - total)) & mask;
				value -= ((value - ((mask + 1) >> 1)) >> 31) & mask;
				return symbol;
			}
		}
		var decoded = Decode(bits);
		value = bits.ReadSigned(decoded & 15);
		return decoded;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private int DecodeLong(JpegBitReader bits)
	{
		var code = 0;

		for (var length = 1; length <= _maxLength; length++)
		{
			code = code << 1 | bits.ReadBit();

			if (code >= _minCode[length] && code <= _maxCode[length])
				return _symbols[_firstSymbol[length] + code - _minCode[length]];
		}

		throw new InvalidDataException("JPEG Huffman code is not in the table.");
	}
}
