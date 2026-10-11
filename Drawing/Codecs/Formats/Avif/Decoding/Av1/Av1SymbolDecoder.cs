namespace Ecng.Drawing.Codecs.Formats.Avif.Decoding.Av1;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Image = Ecng.Drawing.Codecs.CodecImage;
using PixelFormat = Ecng.Drawing.Codecs.CodecPixelFormats;
using ImageOrientation = Ecng.Drawing.Codecs.ImageOrientations;
using MetadataProfileKind = Ecng.Drawing.Codecs.MetadataProfileKinds;


internal sealed class Av1SymbolDecoder
{
	private const int EcProbShift = 6;
	private const int EcMinProb = 4;

	private readonly Av1BitReader _bits;
	private readonly bool _disableCdfUpdate;

	private uint _symbolValue;
	private uint _symbolRange;
	private int _symbolMaxBits;

	public Av1SymbolDecoder(byte[] data, int start, int length, bool disableCdfUpdate)
	{
		_bits = new Av1BitReader(data, start, length);
		_disableCdfUpdate = disableCdfUpdate;

		int numBits = Math.Min(length * 8, 15);
		uint buf = _bits.ReadBits(numBits);
		uint paddedBuf = buf << (15 - numBits);
		_symbolValue = ((1u << 15) - 1) ^ paddedBuf;
		_symbolRange = 1u << 15;
		_symbolMaxBits = (8 * length) - 15;
	}
	public int SymbolMaxBits => _symbolMaxBits;
	public int ReadBool()
	{
		Span<ushort> cdf = [1 << 14, 1 << 15, 0];
		return ReadSymbolCore(cdf, adapt: false);
	}
	public uint ReadLiteral(int n)
	{
		uint x = 0;
		for (int i = 0; i < n; i++)
		{
			x = (2 * x) + (uint)ReadBool();
		}

		return x;
	}
	public int ReadSymbol(Span<ushort> cdf) => ReadSymbolCore(cdf, adapt: !_disableCdfUpdate);

	private int ReadSymbolCore(Span<ushort> cdf, bool adapt)
	{
		int n = cdf.Length - 1;

		uint cur = _symbolRange;
		uint prev;
		int symbol = -1;

		do
		{
			symbol++;
			prev = cur;
			uint f = (1u << 15) - cdf[symbol];
			cur = ((_symbolRange >> 8) * (f >> EcProbShift)) >> (7 - EcProbShift);
			cur += (uint)(EcMinProb * (n - symbol - 1));
		}
		while (_symbolValue < cur);

		_symbolRange = prev - cur;
		_symbolValue -= cur;

		Renormalize();

		if (adapt)
		{
			Av1CdfAdaptation.AdaptCdf(cdf, n, symbol);
		}

		return symbol;
	}
	private void Renormalize()
	{
		int bits = 15 - FloorLog2(_symbolRange);
		_symbolRange <<= bits;

		int numBits = Math.Min(bits, Math.Max(0, _symbolMaxBits));
		uint newData = numBits > 0 ? _bits.ReadBits(numBits) : 0;
		uint paddedData = newData << (bits - numBits);

		_symbolValue = paddedData ^ (((_symbolValue + 1) << bits) - 1);
		_symbolMaxBits -= bits;
	}

	private static int FloorLog2(uint x) => Av1CdfAdaptation.FloorLog2(x);
}
