namespace Ecng.Drawing;

using System;
using System.Buffers;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Ecng.Drawing.Codecs.Jpeg;

/// <summary>
/// Managed baseline 8-bit JPEG encoder: YCbCr without chroma subsampling, the quantization tables of
/// the standard scaled by a quality, and Huffman tables built for the picture being written.
/// </summary>
internal static class JpegEncoder
{
	/// <summary>
	/// The lowest quality: the coarsest quantization.
	/// </summary>
	public const int MinQuality = 1;

	/// <summary>
	/// The highest quality: every coefficient is kept to the nearest whole number.
	/// </summary>
	public const int MaxQuality = 100;

	// An APP2 segment holds this much of a profile beside the 14 bytes that name and number it.
	private const int _profileChunk = 65519;

	private const int _symbols = 256;
	private const int _maxCacheBytes = 8 * 1024 * 1024;

	private const int _dcLuminance = 0;
	private const int _dcChrominance = 1;
	private const int _acLuminance = 2;
	private const int _acChrominance = 3;

	// Sixteen zeros in a row, and the rest of the block being zeros.
	private const int _zeroRun = 0xF0;
	private const int _endOfBlock = 0x00;
	private static readonly uint[] _valueTokens = CreateValueTokens();

	// Tables K.1 and K.2 of the standard, row by row.
	private static ReadOnlySpan<byte> Luminance =>
	[
		16, 11, 10, 16, 24, 40, 51, 61,
		12, 12, 14, 19, 26, 58, 60, 55,
		14, 13, 16, 24, 40, 57, 69, 56,
		14, 17, 22, 29, 51, 87, 80, 62,
		18, 22, 37, 56, 68, 109, 103, 77,
		24, 35, 55, 64, 81, 104, 113, 92,
		49, 64, 78, 87, 103, 121, 120, 101,
		72, 92, 95, 98, 112, 100, 103, 99,
	];

	private static ReadOnlySpan<byte> Chrominance =>
	[
		17, 18, 24, 47, 99, 99, 99, 99,
		18, 21, 26, 66, 99, 99, 99, 99,
		24, 26, 56, 99, 99, 99, 99, 99,
		47, 66, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
		99, 99, 99, 99, 99, 99, 99, 99,
	];

	// The symbols of the scan go here twice: the first time they are counted, the second time written
	// with the codes the counts gave.
	private sealed class Scan
	{
		private readonly MemoryStream _output;
		private readonly uint[] _tokens;
		private readonly uint[] _codeWords = new uint[4 * _symbols];
		private readonly byte[] _bytes = new byte[4096];
		private int _byteCount;
		private ulong _buffer;
		private int _pending;
		private int _tokenCount;

		public Scan(MemoryStream output, uint[] tokens)
		{
			_output = output;
			_tokens = tokens;

			for (var table = 0; table < Frequencies.Length; table++)
				Frequencies[table] = new int[_symbols];
		}

		public int[][] Frequencies { get; } = new int[4][];

		public bool IsWriting { get; private set; }

		public int RequiredEntropyBytes
		{
			get
			{
				long bits = 0;
				for (var table = 0; table < Frequencies.Length; table++)
					for (var symbol = 0; symbol < _symbols; symbol++)
						bits += (long)Frequencies[table][symbol] * ((_codeWords[table * _symbols + symbol] >> 16) + (table < 2 ? symbol : symbol & 15));
				return checked((int)((bits + 7) / 8));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public int CountBlock(ReadOnlySpan<short> block, int previous, int dcTable, int acTable)
		{
			var tokens = _tokens;
			var tokenCount = _tokenCount;
			var difference = block[0] - previous;
			var size = Size(difference);
			Frequencies[dcTable][size]++;
			Store(tokens, ref tokenCount, (uint)(dcTable << 29 | size << 24 | size << 16 | Bits(difference, size)));
			var frequencies = Frequencies[acTable];
			ref int frequencyStart = ref MemoryMarshal.GetArrayDataReference(frequencies);
			ref uint valueStart = ref MemoryMarshal.GetArrayDataReference(_valueTokens);
			var acPrefix = (uint)acTable << 29;
			var mask = Sse2.IsSupported ? NonzeroMask(block) & ~1UL : 0;
			if (Sse2.IsSupported && BitOperations.PopCount(mask) < 48)
			{
				var previousIndex = 0;
				while (mask != 0)
				{
					var index = BitOperations.TrailingZeroCount(mask);
					var skipped = index - previousIndex - 1;
					for (; skipped > 15; skipped -= 16)
					{
						frequencies[_zeroRun]++;
						Store(tokens, ref tokenCount, acPrefix | (uint)_zeroRun << 16);
					}
					var value = block[index];
					var token = acPrefix | Unsafe.Add(ref valueStart, (ushort)value) | (uint)skipped << 20;
					Unsafe.Add(ref frequencyStart, (int)(token >> 16 & 255))++;
					Store(tokens, ref tokenCount, token);
					previousIndex = index;
					mask &= mask - 1;
				}
				if (previousIndex < 63)
				{
					frequencies[_endOfBlock]++;
					Store(tokens, ref tokenCount, acPrefix);
				}
			}
			else
			{
				var zeros = 0;
				var last = LastNonzero(block);
				for (var i = 1; i <= last; i++)
				{
					var value = block[i];
					if (value == 0)
					{
						zeros++;
						continue;
					}
					for (; zeros > 15; zeros -= 16)
					{
						frequencies[_zeroRun]++;
						Store(tokens, ref tokenCount, acPrefix | (uint)_zeroRun << 16);
					}
					var token = acPrefix | Unsafe.Add(ref valueStart, (ushort)value) | (uint)zeros << 20;
					Unsafe.Add(ref frequencyStart, (int)(token >> 16 & 255))++;
					Store(tokens, ref tokenCount, token);
					zeros = 0;
				}
				if (last < 63)
				{
					frequencies[_endOfBlock]++;
					Store(tokens, ref tokenCount, acPrefix);
				}
			}
			_tokenCount = tokenCount;
			return block[0];
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public void WriteStored()
		{
			var buffer = _buffer;
			var pending = _pending;
			var tokenCount = _tokenCount;
			ref uint tokenSource = ref MemoryMarshal.GetArrayDataReference(_tokens);
			ref uint codeSource = ref MemoryMarshal.GetArrayDataReference(_codeWords);
			var byteCount = checked((int)_output.Position);
			// Reserve the stream's length before writing into its buffer: extending it afterward
			// would clear the bytes just written. Truncate the unused reservation when finished.
			_output.SetLength(_output.Capacity);
			var bytes = _output.GetBuffer();
			ref byte destination = ref MemoryMarshal.GetArrayDataReference(bytes);
			for (var i = 0; i < tokenCount; i++)
			{
				var token = Unsafe.Add(ref tokenSource, i);
				var word = Unsafe.Add(ref codeSource, (int)((token >> 21 & 768) | (token >> 16 & 255)));
				var count = (int)(token >> 24 & 31);
				var total = (int)(word >> 16) + count;
				buffer = buffer << total | (ulong)(word & 65535) << count | (token & 65535);
				pending += total;
				if (byteCount > bytes.Length - 8)
				{
					_output.SetLength(byteCount);
					_output.Capacity = checked(byteCount + Math.Max(8, byteCount / 2));
					_output.SetLength(_output.Capacity);
					bytes = _output.GetBuffer();
					destination = ref MemoryMarshal.GetArrayDataReference(bytes);
				}
				while (pending >= 8)
				{
					pending -= 8;
					var value = (byte)(buffer >> pending);
					Unsafe.Add(ref destination, byteCount++) = value;
					if (value == 255) Unsafe.Add(ref destination, byteCount++) = 0;
				}
			}
			_buffer = buffer;
			_pending = pending;
			_output.SetLength(byteCount);
			_output.Position = byteCount;
		}

		/// <summary>
		/// Turns from counting to writing: the symbols of a table get these codes.
		/// </summary>
		public void Use(int table, ushort[] codes, byte[] lengths)
		{
			for (var symbol = 0; symbol < _symbols; symbol++)
				_codeWords[table * _symbols + symbol] = (uint)lengths[symbol] << 16 | codes[symbol];
			IsWriting = true;
		}

		/// <summary>
		/// A symbol and the bits of the value it announces.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Put(int table, int symbol, int bits, int count)
		{
			var word = _codeWords[table * _symbols + symbol];
			var length = (int)(word >> 16);

			var total = length + count;
			_buffer = _buffer << total | (ulong)(word & 65535) << count | (uint)bits;
			_pending += total;

			while (_pending >= 8)
			{
				_pending -= 8;
				Write((byte)(_buffer >> _pending));
			}
		}

		/// <summary>
		/// Fills the last byte with ones.
		/// </summary>
		public void Flush()
		{
			if (_pending != 0)
			{
				var free = 8 - _pending;
				Write((byte)(_buffer << free | (1u << free) - 1));
			}
			_pending = 0;
			_output.Write(_bytes, 0, _byteCount);
			_byteCount = 0;
		}

		// A byte of all ones would read as the start of a marker: a zero after it says it is not.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Write(byte value)
		{
			Append(value);

			if (value == 0xFF)
				Append(0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Append(byte value)
		{
			_bytes[_byteCount++] = value;
			if (_byteCount == _bytes.Length)
			{
				_output.Write(_bytes, 0, _byteCount);
				_byteCount = 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Store(uint[] tokens, ref int index, uint token)
		{
			// A block emits at most 64 tokens, including its DC coefficient and any zero runs.
			// Bits 29..30 hold the table, 24..28 the magnitude length, 16..23 the symbol, 0..15 its bits.
			if (tokens is not null)
				Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(tokens), index++) = token;
		}
	}

	/// <summary>
	/// Writes a picture as a JPEG. Pixels that are not opaque are shown over white.
	/// </summary>
	/// <param name="image">The picture.</param>
	/// <param name="quality">From <see cref="MinQuality"/> to <see cref="MaxQuality"/>, on the scale of libjpeg.</param>
	/// <param name="profile">The ICC profile the colors of the picture are in; empty if it has none.</param>
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static byte[] Encode(RasterImage image, int quality, byte[] profile)
	{
		ArgumentNullException.ThrowIfNull(image);
		ArgumentNullException.ThrowIfNull(profile);

		if (quality is < MinQuality or > MaxQuality)
			throw new ArgumentOutOfRangeException(nameof(quality));

		if (image.Width > ushort.MaxValue || image.Height > ushort.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(image), "JPEG dimensions are limited to 65535 pixels.");

		var coefficientCount = (long)((image.Width + 7) / 8) * ((image.Height + 7) / 8) * 192;
		var tokens = coefficientCount * sizeof(uint) <= _maxCacheBytes ? ArrayPool<uint>.Shared.Rent((int)coefficientCount) : null;
		var coefficients = tokens is null && coefficientCount * sizeof(short) <= _maxCacheBytes ? ArrayPool<short>.Shared.Rent((int)coefficientCount) : null;
		try
		{
			return WriteImage(image, quality, profile, tokens, coefficients);
		}
		finally
		{
			if (coefficients is not null) ArrayPool<short>.Shared.Return(coefficients);
			if (tokens is not null) ArrayPool<uint>.Shared.Return(tokens);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static byte[] WriteImage(RasterImage image, int quality, byte[] profile, uint[] tokens, short[] coefficients)
	{
		var luminance = Scale(Luminance, quality);
		var chrominance = Scale(Chrominance, quality);
		var luminanceFactors = Factors(luminance);
		var chrominanceFactors = Factors(chrominance);

		using var stream = new MemoryStream();
		var scan = new Scan(stream, tokens);

		Walk(image, luminanceFactors, chrominanceFactors, scan, coefficients);

		Marker(stream, 0xD8);
		Segment(stream, 0xE0, [0x4A, 0x46, 0x49, 0x46, 0, 1, 1, 0, 0, 1, 0, 1, 0, 0]); // JFIF 1.1, square pixels
		WriteProfile(stream, profile);

		var tables = new byte[2 * 65];
		tables[0] = 0;
		tables[65] = 1;

		for (var i = 0; i < 64; i++)
		{
			tables[1 + i] = luminance[JpegTransforms.ZigZag[i]];
			tables[66 + i] = chrominance[JpegTransforms.ZigZag[i]];
		}

		Segment(stream, 0xDB, tables);

		Span<byte> frame = stackalloc byte[15];
		frame[0] = 8;
		frame[1] = (byte)(image.Height >> 8);
		frame[2] = (byte)image.Height;
		frame[3] = (byte)(image.Width >> 8);
		frame[4] = (byte)image.Width;
		frame[5] = 3;

		for (var component = 0; component < 3; component++)
		{
			frame[6 + component * 3] = (byte)(component + 1);
			frame[7 + component * 3] = 0x11; // one sample a pixel
			frame[8 + component * 3] = (byte)(component == 0 ? 0 : 1);
		}

		Segment(stream, 0xC0, frame);

		using (var huffman = new MemoryStream())
		{
			// Table class and number: DC 0 and 1, then AC 0 and 1.
			ReadOnlySpan<byte> ids = [0x00, 0x01, 0x10, 0x11];

			for (var table = 0; table < 4; table++)
			{
				huffman.WriteByte(ids[table]);
				WriteCodes(huffman, scan, table);
			}

			Segment(stream, 0xC4, huffman.GetBuffer().AsSpan(0, (int)huffman.Length));
		}

		// Y with tables 0/0, Cb and Cr with tables 1/1; the whole spectrum in one scan.
		Segment(stream, 0xDA, [3, 1, 0x00, 2, 0x11, 3, 0x11, 0, 63, 0]);
		var entropyBytes = scan.RequiredEntropyBytes;
		stream.Capacity = Math.Max(stream.Capacity, checked((int)stream.Length + entropyBytes + entropyBytes / 128 + 16));

		if (tokens is not null)
			scan.WriteStored();
		else
			Walk(image, luminanceFactors, chrominanceFactors, scan, coefficients);
		scan.Flush();

		Marker(stream, 0xD9);
		return stream.ToArray();
	}

	// The scale of libjpeg: 50 leaves the tables as they are, 100 turns them into ones.
	private static byte[] Scale(ReadOnlySpan<byte> table, int quality)
	{
		var percent = quality < 50 ? 5000 / quality : 200 - quality * 2;
		var scaled = new byte[64];

		for (var i = 0; i < 64; i++)
			scaled[i] = (byte)Math.Clamp((table[i] * percent + 50) / 100, 1, 255);

		return scaled;
	}

	// Quantization reciprocals in scan order.
	private static double[] Factors(byte[] table)
	{
		var factors = new double[64];

		for (var i = 0; i < 64; i++)
		{
			factors[i] = 1.0 / table[JpegTransforms.ZigZag[i]];
		}

		return factors;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void Walk(RasterImage image, double[] luminance, double[] chrominance, Scan scan, short[] coefficients)
	{
		Span<double> y = stackalloc double[64];
		Span<double> cb = stackalloc double[64];
		Span<double> cr = stackalloc double[64];
		Span<int> previous = stackalloc int[3];
		Span<short> temporary = stackalloc short[192];
		var yBlock = new JpegBlock8x8F();
		var cbBlock = new JpegBlock8x8F();
		var crBlock = new JpegBlock8x8F();
		var offset = 0;

		for (var top = 0; top < image.Height; top += 8)
			for (var left = 0; left < image.Width; left += 8)
			{
				var quantized = coefficients is null ? temporary : coefficients.AsSpan(offset, 192);
				if (!scan.IsWriting || coefficients is null)
				{
					if (JpegInputColorConverter.TryReadBlock(image, left, top, ref yBlock, ref cbBlock, ref crBlock))
					{
						QuantizeBlock(ref yBlock, luminance, quantized[..64]);
						QuantizeBlock(ref cbBlock, chrominance, quantized.Slice(64, 64));
						QuantizeBlock(ref crBlock, chrominance, quantized.Slice(128, 64));
					}
					else
					{
						ReadBlock(image, left, top, y, cb, cr);
						QuantizeBlock(y, luminance, quantized[..64]);
						QuantizeBlock(cb, chrominance, quantized.Slice(64, 64));
						QuantizeBlock(cr, chrominance, quantized.Slice(128, 64));
					}
				}
				previous[0] = PutBlock(scan, quantized[..64], previous[0], _dcLuminance, _acLuminance);
				previous[1] = PutBlock(scan, quantized.Slice(64, 64), previous[1], _dcChrominance, _acChrominance);
				previous[2] = PutBlock(scan, quantized.Slice(128, 64), previous[2], _dcChrominance, _acChrominance);
				offset += 192;
			}
	}

	// A block that reaches beyond the picture repeats its last column and its last row.
	internal static void ReadBlock(RasterImage image, int left, int top, Span<double> y, Span<double> cb, Span<double> cr)
	{
		if (JpegInputColorConverter.TryReadBlock(image, left, top, y, cb, cr)) return;
		var pixels = image.Pixels;

		for (var row = 0; row < 8; row++)
		{
			var line = Math.Min(image.Height - 1, top + row) * image.Width;

			for (var column = 0; column < 8; column++)
			{
				var at = (line + Math.Min(image.Width - 1, left + column)) * 4;
				double red = pixels[at];
				double green = pixels[at + 1];
				double blue = pixels[at + 2];
				var alpha = pixels[at + 3];

				if (alpha != 255)
				{
					var white = 255.0 - alpha;
					red = (red * alpha + 255 * white) / 255;
					green = (green * alpha + 255 * white) / 255;
					blue = (blue * alpha + 255 * white) / 255;
				}

				var to = row * 8 + column;
				y[to] = 0.299 * red + 0.587 * green + 0.114 * blue - 128;
				cb[to] = -0.168736 * red - 0.331264 * green + 0.5 * blue;
				cr[to] = 0.5 * red - 0.418688 * green - 0.081312 * blue;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	internal static void QuantizeBlock(Span<double> block, double[] factors, Span<short> quantized)
	{
		Unsafe.SkipInit(out JpegBlock8x8F source);
		var input = MemoryMarshal.CreateSpan(ref source.V0L.X, 64);
		if (Avx.IsSupported)
		{
			for (var i = 0; i < 64; i += 4)
			{
				var values = Unsafe.ReadUnaligned<Vector256<double>>(ref Unsafe.As<double, byte>(ref block[i]));
				Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref input[i]), Avx.ConvertToVector128Single(values));
			}
		}
		else
			for (var i = 0; i < 64; i++) input[i] = (float)block[i];
		QuantizeBlock(ref source, factors, quantized);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void QuantizeBlock(ref JpegBlock8x8F source, double[] factors, Span<short> quantized)
	{
		var constant = source[0];
		var flat = true;
		if (Avx.IsSupported)
		{
			ref var start = ref Unsafe.As<JpegBlock8x8F, float>(ref source);
			var expected = Vector256.Create(constant);
			for (var i = 0; i < 64 && flat; i += 8)
			{
				var values = Unsafe.ReadUnaligned<Vector256<float>>(ref Unsafe.As<float, byte>(ref Unsafe.Add(ref start, i)));
				flat = Avx.MoveMask(Avx.Compare(values, expected, FloatComparisonMode.OrderedEqualNonSignaling)) == 255;
			}
		}
		else
			for (var i = 1; i < 64 && flat; i++) flat = source[i] == constant;
		if (flat)
		{
			quantized.Clear();
			var value = constant * 8f * factors[0];
			quantized[0] = checked((short)(int)(value + (value < 0 ? -0.5 : 0.5)));
			return;
		}
		Unsafe.SkipInit(out JpegBlock8x8F output);
		Unsafe.SkipInit(out JpegBlock8x8F scratch);
		FastFloatingPointDCT.TransformFDCT(ref source, ref output, ref scratch);
		var transformed = MemoryMarshal.CreateReadOnlySpan(ref output.V0L.X, 64);
		JpegCoefficientQuantizer.Write(transformed, factors, quantized);
	}

	// Returns the DC coefficient of the block: the next block of the component is coded against it.
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static int PutBlock(Scan scan, ReadOnlySpan<short> block, int previous, int dcTable, int acTable)
	{
		if (!scan.IsWriting) return scan.CountBlock(block, previous, dcTable, acTable);
		var dc = block[0];
		var difference = dc - previous;
		var size = Size(difference);
		scan.Put(dcTable, size, Bits(difference, size), size);

		var zeros = 0;
		var last = LastNonzero(block);

		for (var i = 1; i <= last; i++)
		{
			var value = block[i];

			if (value == 0)
			{
				zeros++;
				continue;
			}

			for (; zeros > 15; zeros -= 16)
				scan.Put(acTable, _zeroRun, 0, 0);

			size = Size(value);
			scan.Put(acTable, zeros << 4 | size, Bits(value, size), size);
			zeros = 0;
		}

		if (last < 63)
			scan.Put(acTable, _endOfBlock, 0, 0);

		return dc;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong NonzeroMask(ReadOnlySpan<short> block)
	{
		ref short source = ref MemoryMarshal.GetReference(block);
		ulong result = 0;
		for (var index = 0; index < 64; index += 16)
		{
			var first = Unsafe.ReadUnaligned<Vector128<short>>(ref Unsafe.As<short, byte>(ref Unsafe.Add(ref source, index)));
			var second = Unsafe.ReadUnaligned<Vector128<short>>(ref Unsafe.As<short, byte>(ref Unsafe.Add(ref source, index + 8)));
			var zeros = Sse2.PackSignedSaturate(Sse2.CompareEqual(first, Vector128<short>.Zero), Sse2.CompareEqual(second, Vector128<short>.Zero));
			result |= (ulong)(ushort)~Sse2.MoveMask(zeros.AsByte()) << index;
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LastNonzero(ReadOnlySpan<short> block)
	{
		if (Sse2.IsSupported)
		{
			ref short start = ref MemoryMarshal.GetReference(block);
			for (var index = 56; index >= 0; index -= 8)
			{
				var values = Unsafe.ReadUnaligned<Vector128<short>>(ref Unsafe.As<short, byte>(ref Unsafe.Add(ref start, index)));
				var mask = (uint)(~Sse2.MoveMask(Sse2.CompareEqual(values, Vector128<short>.Zero).AsByte()) & 0xffff);
				if (mask != 0)
					return index + (31 - BitOperations.LeadingZeroCount(mask)) / 2;
			}
			return 0;
		}
		for (var index = 63; index > 0; index--)
			if (block[index] != 0)
				return index;
		return 0;
	}

	// How many bits the magnitude of a value takes.
	private static int Size(int value)
		=> 32 - BitOperations.LeadingZeroCount((uint)(value < 0 ? -value : value));

	// A negative value is written as the ones' complement of its magnitude.
	private static int Bits(int value, int size)
		=> value + ((value >> 31) & ((1 << size) - 1));

	// The amplitude bits and magnitude category depend only on the signed 16-bit coefficient.
	// Cache them once (256 KiB) rather than classify every AC coefficient again for each picture.
	private static uint[] CreateValueTokens()
	{
		var result = new uint[ushort.MaxValue + 1];
		for (var i = 0; i < result.Length; i++)
		{
			var value = (short)i;
			var size = Size(value);
			result[i] = (uint)(size << 24 | size << 16 | Bits(value, size));
		}
		return result;
	}

	// Writes what a DHT segment says of one table - how many codes there are of each length and the
	// symbols in the order of their codes - and hands the codes to the scan.
	private static void WriteCodes(Stream output, Scan scan, int table)
	{
		Span<byte> counts = stackalloc byte[16];
		var symbols = JpegHuffmanTable.Optimize(scan.Frequencies[table], counts);

		output.Write(counts);
		output.Write(symbols);

		var codes = new ushort[_symbols];
		var lengths = new byte[_symbols];
		var code = 0;
		var at = 0;

		for (var length = 1; length <= counts.Length; length++)
		{
			for (var i = 0; i < counts[length - 1]; i++)
			{
				codes[symbols[at]] = (ushort)code++;
				lengths[symbols[at]] = (byte)length;
				at++;
			}

			code <<= 1;
		}

		scan.Use(table, codes, lengths);
	}

	// A profile longer than a segment holds is split; every part says which it is and how many there are.
	private static void WriteProfile(Stream output, byte[] profile)
	{
		var parts = (profile.Length + _profileChunk - 1) / _profileChunk;

		if (parts > byte.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(profile), "The ICC profile is too large for a JPEG.");

		var segment = new byte[14 + Math.Min(profile.Length, _profileChunk)];
		JpegSegments.ProfileSignature.CopyTo(segment);

		for (var part = 0; part < parts; part++)
		{
			var from = part * _profileChunk;
			var length = Math.Min(_profileChunk, profile.Length - from);

			segment[12] = (byte)(part + 1);
			segment[13] = (byte)parts;
			profile.AsSpan(from, length).CopyTo(segment.AsSpan(14));

			Segment(output, JpegSegments.Profile, segment.AsSpan(0, 14 + length));
		}
	}

	private static void Marker(Stream output, int marker)
	{
		output.WriteByte(0xFF);
		output.WriteByte((byte)marker);
	}

	private static void Segment(Stream output, int marker, ReadOnlySpan<byte> content)
	{
		Marker(output, marker);
		output.WriteByte((byte)(content.Length + 2 >> 8));
		output.WriteByte((byte)(content.Length + 2));
		output.Write(content);
	}
}
