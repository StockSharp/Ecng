namespace Ecng.Drawing;

using System;
using System.IO;
using System.Numerics;

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

	private const int _dcLuminance = 0;
	private const int _dcChrominance = 1;
	private const int _acLuminance = 2;
	private const int _acChrominance = 3;

	// Sixteen zeros in a row, and the rest of the block being zeros.
	private const int _zeroRun = 0xF0;
	private const int _endOfBlock = 0x00;

	// cos(4pi/16), cos(6pi/16), and the difference and the sum of cos(2pi/16) and cos(6pi/16).
	private const double _c4 = 0.7071067811865476;
	private const double _c6 = 0.3826834323650898;
	private const double _c2MinusC6 = 0.541196100146197;
	private const double _c2PlusC6 = 1.3065629648763766;

	// The transform leaves every output multiplied by eight and by these, one for its row and one for its column.
	private static readonly double[] _outputScale =
	[
		1.0, 1.3870398453221475, 1.3065629648763766, 1.1758756024193586,
		1.0, 0.7856949583871021, 0.541196100146197, 0.275899379282943,
	];

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
		private readonly Stream _output;
		private readonly ushort[][] _codes = new ushort[4][];
		private readonly byte[][] _lengths = new byte[4][];
		private ulong _buffer;
		private int _pending;

		public Scan(Stream output)
		{
			_output = output;

			for (var table = 0; table < Frequencies.Length; table++)
				Frequencies[table] = new int[_symbols];
		}

		public int[][] Frequencies { get; } = new int[4][];

		public bool IsWriting { get; private set; }

		/// <summary>
		/// Turns from counting to writing: the symbols of a table get these codes.
		/// </summary>
		public void Use(int table, ushort[] codes, byte[] lengths)
		{
			_codes[table] = codes;
			_lengths[table] = lengths;
			IsWriting = true;
		}

		/// <summary>
		/// A symbol and the bits of the value it announces.
		/// </summary>
		public void Put(int table, int symbol, int bits, int count)
		{
			if (!IsWriting)
			{
				Frequencies[table][symbol]++;
				return;
			}

			var length = _lengths[table][symbol];

			_buffer = _buffer << length | _codes[table][symbol];
			_buffer = _buffer << count | (uint)bits;
			_pending += length + count;

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
			if (_pending == 0)
				return;

			var free = 8 - _pending;
			Write((byte)(_buffer << free | (1u << free) - 1));
			_pending = 0;
		}

		// A byte of all ones would read as the start of a marker: a zero after it says it is not.
		private void Write(byte value)
		{
			_output.WriteByte(value);

			if (value == 0xFF)
				_output.WriteByte(0);
		}
	}

	/// <summary>
	/// Writes a picture as a JPEG. Pixels that are not opaque are shown over white.
	/// </summary>
	/// <param name="image">The picture.</param>
	/// <param name="quality">From <see cref="MinQuality"/> to <see cref="MaxQuality"/>, on the scale of libjpeg.</param>
	/// <param name="profile">The ICC profile the colors of the picture are in; empty if it has none.</param>
	public static byte[] Encode(RasterImage image, int quality, byte[] profile)
	{
		ArgumentNullException.ThrowIfNull(image);
		ArgumentNullException.ThrowIfNull(profile);

		if (quality is < MinQuality or > MaxQuality)
			throw new ArgumentOutOfRangeException(nameof(quality));

		if (image.Width > ushort.MaxValue || image.Height > ushort.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(image), "JPEG dimensions are limited to 65535 pixels.");

		var luminance = Scale(Luminance, quality);
		var chrominance = Scale(Chrominance, quality);
		var luminanceFactors = Factors(luminance);
		var chrominanceFactors = Factors(chrominance);

		using var stream = new MemoryStream();
		var scan = new Scan(stream);

		// The picture is transformed twice rather than its coefficients kept: they would take six bytes a pixel.
		Walk(image, luminanceFactors, chrominanceFactors, scan);

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

		Walk(image, luminanceFactors, chrominanceFactors, scan);
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

	// What an output of the transform is multiplied by to give the quantized coefficient, in zigzag order.
	private static double[] Factors(byte[] table)
	{
		var factors = new double[64];

		for (var i = 0; i < 64; i++)
		{
			var at = JpegTransforms.ZigZag[i];
			factors[i] = 1 / (table[at] * _outputScale[at / 8] * _outputScale[at % 8] * 8);
		}

		return factors;
	}

	private static void Walk(RasterImage image, double[] luminance, double[] chrominance, Scan scan)
	{
		Span<double> y = stackalloc double[64];
		Span<double> cb = stackalloc double[64];
		Span<double> cr = stackalloc double[64];
		Span<int> previous = stackalloc int[3];

		for (var top = 0; top < image.Height; top += 8)
			for (var left = 0; left < image.Width; left += 8)
			{
				ReadBlock(image, left, top, y, cb, cr);

				previous[0] = PutBlock(scan, y, luminance, previous[0], _dcLuminance, _acLuminance);
				previous[1] = PutBlock(scan, cb, chrominance, previous[1], _dcChrominance, _acChrominance);
				previous[2] = PutBlock(scan, cr, chrominance, previous[2], _dcChrominance, _acChrominance);
			}
	}

	// A block that reaches beyond the picture repeats its last column and its last row.
	private static void ReadBlock(RasterImage image, int left, int top, Span<double> y, Span<double> cb, Span<double> cr)
	{
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

	// Returns the DC coefficient of the block: the next block of the component is coded against it.
	private static int PutBlock(Scan scan, Span<double> block, double[] factors, int previous, int dcTable, int acTable)
	{
		Transform(block);

		var dc = Quantize(block[0] * factors[0]);
		var difference = dc - previous;
		var size = Size(difference);
		scan.Put(dcTable, size, Bits(difference, size), size);

		var zeros = 0;

		for (var i = 1; i < 64; i++)
		{
			var value = Quantize(block[JpegTransforms.ZigZag[i]] * factors[i]);

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

		if (zeros > 0)
			scan.Put(acTable, _endOfBlock, 0, 0);

		return dc;
	}

	// To the nearest whole number, halves away from zero.
	private static int Quantize(double value)
		=> (int)(value + (value < 0 ? -0.5 : 0.5));

	// How many bits the magnitude of a value takes.
	private static int Size(int value)
		=> value == 0 ? 0 : BitOperations.Log2((uint)Math.Abs(value)) + 1;

	// A negative value is written as the ones' complement of its magnitude.
	private static int Bits(int value, int size)
		=> value < 0 ? value + (1 << size) - 1 : value;

	// The forward transform of Arai, Agui and Nakajima over rows, then over columns, in place.
	private static void Transform(Span<double> block)
	{
		for (var row = 0; row < 64; row += 8)
			Transform(block, row, 1);

		for (var column = 0; column < 8; column++)
			Transform(block, column, 8);
	}

	private static void Transform(Span<double> block, int start, int step)
	{
		ref var d0 = ref block[start];
		ref var d1 = ref block[start + step];
		ref var d2 = ref block[start + step * 2];
		ref var d3 = ref block[start + step * 3];
		ref var d4 = ref block[start + step * 4];
		ref var d5 = ref block[start + step * 5];
		ref var d6 = ref block[start + step * 6];
		ref var d7 = ref block[start + step * 7];

		var sum07 = d0 + d7;
		var difference07 = d0 - d7;
		var sum16 = d1 + d6;
		var difference16 = d1 - d6;
		var sum25 = d2 + d5;
		var difference25 = d2 - d5;
		var sum34 = d3 + d4;
		var difference34 = d3 - d4;

		// The even outputs.
		var outer = sum07 + sum34;
		var outerDifference = sum07 - sum34;
		var inner = sum16 + sum25;
		var innerDifference = sum16 - sum25;

		d0 = outer + inner;
		d4 = outer - inner;

		var rotated = (innerDifference + outerDifference) * _c4;
		d2 = outerDifference + rotated;
		d6 = outerDifference - rotated;

		// The odd outputs.
		var a = difference34 + difference25;
		var b = difference25 + difference16;
		var c = difference16 + difference07;

		var shared = (a - c) * _c6;
		var low = _c2MinusC6 * a + shared;
		var high = _c2PlusC6 * c + shared;
		var middle = b * _c4;

		var plus = difference07 + middle;
		var minus = difference07 - middle;

		d5 = minus + low;
		d3 = minus - low;
		d1 = plus + high;
		d7 = plus - high;
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
