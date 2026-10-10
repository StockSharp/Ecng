namespace Ecng.Tests.Drawing;

/// <summary>
/// Writes GIF files for tests byte by byte. Pixel data is left uncompressed - every code is a
/// literal - so that nothing of the library's own encoder goes into a fixture.
/// </summary>
internal sealed class GifBuilder
{
	private const int _minCodeSize = 8;
	private const int _clearCode = 1 << _minCodeSize;
	private const int _endCode = _clearCode + 1;

	// A decoder widens its codes once it has added 254 entries after a clear; clearing sooner keeps them at nine bits.
	private const int _literalsPerClear = 200;

	private readonly MemoryStream _stream = new();

	/// <param name="width">Width of the logical screen.</param>
	/// <param name="height">Height of the logical screen.</param>
	/// <param name="palette">The global color table, three bytes per color; its size must be a power of two. Null for none.</param>
	/// <param name="background">Index of the background color.</param>
	public GifBuilder(int width, int height, byte[] palette = null, byte background = 0)
	{
		_stream.Write("GIF89a"u8);
		WriteWord(width);
		WriteWord(height);
		_stream.WriteByte(palette is null ? (byte)0 : (byte)(0x80 | TableBits(palette)));
		_stream.WriteByte(background);
		_stream.WriteByte(0);

		if (palette is not null)
			_stream.Write(palette);
	}

	/// <summary>
	/// A gray ramp of the given number of colors: entry i is (i, i, i) scaled to the full range.
	/// </summary>
	public static byte[] GrayPalette(int count)
	{
		var palette = new byte[count * 3];

		for (var i = 0; i < count; i++)
			palette[i * 3] = palette[i * 3 + 1] = palette[i * 3 + 2] = (byte)(i * 255 / (count - 1));

		return palette;
	}

	/// <summary>
	/// Builds a color table out of colors, padded with black to a power of two.
	/// </summary>
	public static byte[] Palette(params (byte r, byte g, byte b)[] colors)
	{
		var size = 2;

		while (size < colors.Length)
			size *= 2;

		var palette = new byte[size * 3];

		for (var i = 0; i < colors.Length; i++)
		{
			palette[i * 3] = colors[i].r;
			palette[i * 3 + 1] = colors[i].g;
			palette[i * 3 + 2] = colors[i].b;
		}

		return palette;
	}

	/// <summary>
	/// Adds the NETSCAPE2.0 application extension.
	/// </summary>
	public GifBuilder Loop(int count)
	{
		_stream.Write([0x21, 0xFF, 11]);
		_stream.Write("NETSCAPE2.0"u8);
		_stream.Write([3, 1]);
		WriteWord(count);
		_stream.WriteByte(0);
		return this;
	}

	/// <summary>
	/// Adds an image with a graphic control extension before it.
	/// </summary>
	/// <param name="left">Column of the image on the logical screen.</param>
	/// <param name="top">Row of the image on the logical screen.</param>
	/// <param name="width">Width of the image.</param>
	/// <param name="height">Height of the image.</param>
	/// <param name="indices">Color indices, row by row.</param>
	/// <param name="transparent">Index of the transparent color, -1 for none.</param>
	/// <param name="disposal">Disposal method, 0 to 3.</param>
	/// <param name="delay">Delay in hundredths of a second.</param>
	/// <param name="palette">The local color table, or null to use the global one.</param>
	/// <param name="endCode">Whether the pixel data closes with the end-of-information code.</param>
	/// <param name="extraCodes">Codes written after the last pixel, before the end code.</param>
	public GifBuilder Frame(int left, int top, int width, int height, byte[] indices, int transparent = -1, int disposal = 0, int delay = 0,
		byte[] palette = null, bool endCode = true, int extraCodes = 0)
	{
		indices.Length.AssertEqual(width * height);

		_stream.Write([0x21, 0xF9, 4, (byte)(disposal << 2 | (transparent >= 0 ? 1 : 0))]);
		WriteWord(delay);
		_stream.WriteByte((byte)Math.Max(0, transparent));
		_stream.WriteByte(0);

		_stream.WriteByte(0x2C);
		WriteWord(left);
		WriteWord(top);
		WriteWord(width);
		WriteWord(height);
		_stream.WriteByte(palette is null ? (byte)0 : (byte)(0x80 | TableBits(palette)));

		if (palette is not null)
			_stream.Write(palette);

		_stream.WriteByte(_minCodeSize);

		var data = Literals(indices, endCode, extraCodes);

		for (var at = 0; at < data.Length; at += 255)
		{
			var length = Math.Min(255, data.Length - at);
			_stream.WriteByte((byte)length);
			_stream.Write(data, at, length);
		}

		_stream.WriteByte(0);
		return this;
	}

	/// <summary>
	/// Closes the file with the trailer and whatever is to follow it.
	/// </summary>
	public byte[] Build(byte[] afterTrailer = null)
	{
		_stream.WriteByte(0x3B);

		if (afterTrailer is not null)
			_stream.Write(afterTrailer);

		return _stream.ToArray();
	}

	private static byte[] Literals(byte[] indices, bool endCode, int extraCodes)
	{
		var output = new List<byte>();
		var buffer = 0u;
		var bits = 0;

		void Emit(int code)
		{
			buffer |= (uint)code << bits;
			bits += _minCodeSize + 1;

			while (bits >= 8)
			{
				output.Add((byte)buffer);
				buffer >>= 8;
				bits -= 8;
			}
		}

		var sinceClear = _literalsPerClear;

		foreach (var index in indices)
		{
			if (sinceClear == _literalsPerClear)
			{
				Emit(_clearCode);
				sinceClear = 0;
			}

			Emit(index);
			sinceClear++;
		}

		for (var i = 0; i < extraCodes; i++)
			Emit(0);

		if (endCode)
			Emit(_endCode);

		if (bits > 0)
			output.Add((byte)buffer);

		return [.. output];
	}

	private static int TableBits(byte[] palette)
	{
		var size = palette.Length / 3;
		var bits = 0;

		while (2 << bits < size)
			bits++;

		(2 << bits).AssertEqual(size, "color table size must be a power of two");
		return bits;
	}

	private void WriteWord(int value)
	{
		_stream.WriteByte((byte)value);
		_stream.WriteByte((byte)(value >> 8));
	}
}
