namespace Ecng.Drawing;

using System;
using System.IO;

/// <summary>
/// Managed baseline 8-bit JPEG encoder, using deterministic 4:4:4 YCbCr,
/// fixed legal (incomplete) Huffman tables, and separable forward DCT.
/// No OS imaging APIs or third-party packages.
/// </summary>
internal static class JpegEncoder
{
	private static readonly int[] _zigzag =
	[
		0,1,8,16,9,2,3,10,17,24,32,25,18,11,4,5,
		12,19,26,33,40,48,41,34,27,20,13,6,7,14,21,
		28,35,42,49,56,57,50,43,36,29,22,15,23,30,37,
		44,51,58,59,52,45,38,31,39,46,53,60,61,54,47,55,
		62,63
	];

	// DC symbols 0..11: all Huffman codes are 4 bits long.
	// AC symbols 0 (EOB), F0 (16 zeros), then each (run=0..15,size=1..10).
	// All AC Huffman codes are 8 bits. A deliberately incomplete tree avoids
	// JPEG's forbidden all-ones Huffman code while remaining baseline-compliant.
	private static readonly int[] _acSymbols = CreateAcSymbols();
	private static readonly int[] _acCodes = CreateAcCodes();
	private static readonly double[,] _basis = Basis();

	private sealed class BitWriter(Stream stream)
	{
		private int _bits;
		private int _word;
		public void Write(int code, int length)
		{
			for (var i = length - 1; i >= 0; i--)
			{
				_word = _word << 1 | (code >> i & 1);
				if (++_bits != 8) continue;
				stream.WriteByte((byte)_word);
				if (_word == 0xFF) stream.WriteByte(0);
				_word = _bits = 0;
			}
		}
		public void Flush()
		{
			if (_bits > 0)
			{
				var last = (_word << (8 - _bits)) | ((1 << (8 - _bits)) - 1);
				stream.WriteByte((byte)last);
				if (last == 0xFF) stream.WriteByte(0);
				_word = _bits = 0;
			}
		}
	}

	public static byte[] Encode(RasterImage image)
	{
		ArgumentNullException.ThrowIfNull(image);
		if (image.Width > ushort.MaxValue || image.Height > ushort.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(image), "JPEG dimensions are limited to 65535 pixels.");

		using var stream = new MemoryStream();
		Marker(stream, 0xD8); // SOI
		Segment(stream, 0xE0, [0x4A,0x46,0x49,0x46,0,1,1,0,0,1,0,1,0,0]); // JFIF

		// Fixed quality ~80 tables: chosen to keep entropy magnitudes inside
		// baseline Huffman symbols even on high-contrast images.
		var yQ = new int[64];
		var cQ = new int[64];
		for (var i = 0; i < 64; i++) { yQ[i] = 12; cQ[i] = 14; }

		using (var q = new MemoryStream())
		{
			q.WriteByte(0);
			foreach (var i in _zigzag) q.WriteByte((byte)yQ[i]);
			q.WriteByte(1);
			foreach (var i in _zigzag) q.WriteByte((byte)cQ[i]);
			Segment(stream, 0xDB, q.ToArray());
		}

		using (var sof = new MemoryStream())
		{
			sof.WriteByte(8);
			U16(sof, image.Height);
			U16(sof, image.Width);
			sof.WriteByte(3);
			for (var c = 0; c < 3; c++)
			{
				sof.WriteByte((byte)(c + 1));
				sof.WriteByte(0x11); // 4:4:4 sampling
				sof.WriteByte((byte)(c == 0 ? 0 : 1));
			}
			Segment(stream, 0xC0, sof.ToArray());
		}

		using (var tables = new MemoryStream())
		{
			tables.WriteByte(0x00); // DC, table 0
			for (var length = 1; length <= 16; length++)
				tables.WriteByte((byte)(length == 4 ? 12 : 0));
			for (var i = 0; i < 12; i++) tables.WriteByte((byte)i);

			tables.WriteByte(0x10); // AC, table 0
			for (var length = 1; length <= 16; length++)
				tables.WriteByte((byte)(length == 8 ? _acSymbols.Length : 0));
			foreach (var symbol in _acSymbols)
				tables.WriteByte((byte)symbol);

			Segment(stream, 0xC4, tables.ToArray());
		}

		// Start of Scan: Y, Cb and Cr share Huffman tables 0/0.
		Segment(stream, 0xDA, [3,1,0,2,0,3,0,0,63,0]);

		var bits = new BitWriter(stream);
		var predictor = new int[3];
		var spatial = new double[64];
		var transformed = new double[64];
		var coeff = new int[64];

		for (var my = 0; my < image.Height; my += 8)
			for (var mx = 0; mx < image.Width; mx += 8)
				for (var channel = 0; channel < 3; channel++)
				{
					for (var j = 0; j < 8; j++)
						for (var i = 0; i < 8; i++)
						{
							var x = Math.Min(image.Width - 1, mx + i);
							var y = Math.Min(image.Height - 1, my + j);
							var offset = (y * image.Width + x) * 4;
							var alpha = image.Pixels[offset + 3] / 255.0;
							var red = image.Pixels[offset] * alpha + 255 * (1 - alpha);
							var green = image.Pixels[offset + 1] * alpha + 255 * (1 - alpha);
							var blue = image.Pixels[offset + 2] * alpha + 255 * (1 - alpha);
							spatial[j * 8 + i] = channel switch
							{
								0 => 0.299 * red + 0.587 * green + 0.114 * blue - 128,
								1 => -0.168736 * red - 0.331264 * green + 0.5 * blue,
								_ => 0.5 * red - 0.418688 * green - 0.081312 * blue
							};
						}

					ForwardDct(spatial, transformed);
					for (var i = 0; i < 64; i++)
						coeff[i] = (int)Math.Round(transformed[i] / (channel == 0 ? 12 : 14), MidpointRounding.AwayFromZero);

					var diff = coeff[0] - predictor[channel];
					predictor[channel] = coeff[0];
					WriteAmplitude(bits, diff, dc: true, zeros: 0);

					var zeros = 0;
					for (var i = 1; i < 64; i++)
					{
						var amplitude = coeff[_zigzag[i]];
						if (amplitude == 0) { zeros++; continue; }
						while (zeros >= 16)
						{
							bits.Write(_acCodes[0xF0], 8);
							zeros -= 16;
						}

						WriteAmplitude(bits, amplitude, dc: false, zeros: zeros);
						zeros = 0;
					}
					if (zeros != 0)
						bits.Write(_acCodes[0], 8);
				}

		bits.Flush();
		Marker(stream, 0xD9);
		return stream.ToArray();
	}

	private static void ForwardDct(double[] spatial, double[] result)
	{
		Span<double> tmp = stackalloc double[64];
		for (var y = 0; y < 8; y++)
			for (var u = 0; u < 8; u++)
			{
				var sum = 0.0;
				for (var x = 0; x < 8; x++)
					sum += spatial[y * 8 + x] * _basis[x, u];
				tmp[y * 8 + u] = sum;
			}

		for (var v = 0; v < 8; v++)
			for (var u = 0; u < 8; u++)
			{
				var sum = 0.0;
				for (var y = 0; y < 8; y++)
					sum += tmp[y * 8 + u] * _basis[y, v];
				result[v * 8 + u] = sum / 4.0;
			}
	}

	private static double[,] Basis()
	{
		var basis = new double[8, 8];
		for (var x = 0; x < 8; x++)
			for (var u = 0; u < 8; u++)
				basis[x, u] = (u == 0 ? 1.0 / Math.Sqrt(2) : 1.0) *
					Math.Cos((2 * x + 1) * u * Math.PI / 16);
		return basis;
	}

	private static void WriteAmplitude(BitWriter bits, int value, bool dc, int zeros)
	{
		var abs = Math.Abs(value);
		var size = 0;
		while (abs > 0) { abs >>= 1; size++; }

		if (dc)
		{
			if (size > 11)
				throw new InvalidDataException("JPEG DC coefficient exceeds baseline limits.");
			bits.Write(size, 4);
		}
		else
		{
			if (size < 1 || size > 10)
				throw new InvalidDataException("JPEG AC coefficient exceeds baseline limits.");
			bits.Write(_acCodes[(zeros << 4) | size], 8);
		}

		if (size > 0)
			bits.Write(value < 0 ? value + (1 << size) - 1 : value, size);
	}

	private static int[] CreateAcSymbols()
	{
		var symbols = new int[162];
		symbols[0] = 0;
		symbols[1] = 0xF0;
		var index = 2;
		for (var run = 0; run < 16; run++)
			for (var length = 1; length <= 10; length++)
				symbols[index++] = run << 4 | length;
		return symbols;
	}

	private static int[] CreateAcCodes()
	{
		var codes = new int[256];
		for (var i = 0; i < _acSymbols.Length; i++)
			codes[_acSymbols[i]] = i;
		return codes;
	}

	private static void Marker(Stream output, int marker)
	{
		output.WriteByte(0xFF);
		output.WriteByte((byte)marker);
	}

	private static void Segment(Stream output, int marker, ReadOnlySpan<byte> content)
	{
		Marker(output, marker);
		U16(output, checked(content.Length + 2));
		output.Write(content);
	}

	private static void U16(Stream output, int value)
	{
		output.WriteByte((byte)(value >> 8));
		output.WriteByte((byte)value);
	}
}
