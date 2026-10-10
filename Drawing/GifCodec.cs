namespace Ecng.Drawing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Fully managed animated GIF decoder/encoder. Supports global/local palettes,
/// LZW, interlace, transparent indices, disposal 0/1/2/3, Netscape loop count,
/// arbitrary image subrectangles, delay and full frame compositing.
/// GIF output has 1-bit alpha and at most 256 colors per frame by format definition.
/// </summary>
internal static class GifCodec
{
	private const int _maxInputLength = 128 * 1024 * 1024;

	private const byte _trailer = 0x3B;
	private const byte _extension = 0x21;
	private const byte _imageSeparator = 0x2C;
	private const byte _graphicControl = 0xF9;
	private const byte _application = 0xFF;

	private const int _disposeToBackground = 2;
	private const int _disposeToPrevious = 3;

	// Codes are at most twelve bits long.
	private const int _maxCodes = 4096;

	// One image of a file: where it goes, how it is coded, and what the graphic control extension before it said.
	private sealed class Image
	{
		public int Left;
		public int Top;
		public int Width;
		public int Height;
		public bool IsInterlaced;

		// The color table in force, as a range of the file.
		public int PaletteStart;
		public int PaletteLength;

		// Index of the transparent color, -1 for none.
		public int Transparent;

		// Hundredths of a second.
		public int Delay;

		public int Disposal;
		public int MinCodeSize;

		// Offset of the first data sub-block.
		public int DataStart;
	}

	// Walks the blocks of a file. Pixel data is stepped over, not unpacked.
	private sealed class Reader
	{
		private readonly byte[] _data;
		private readonly int _globalPaletteStart;
		private readonly int _globalPaletteLength;
		private int _position;
		private int _transparent = -1;
		private int _delay;
		private int _disposal;

		public Reader(byte[] data)
		{
			(Width, Height) = ReadSize(data);

			if (data.Length > _maxInputLength)
				throw new InvalidDataException("GIF input exceeds supported size.");

			_data = data;
			_position = 13;

			var packed = data[10];

			if ((packed & 0x80) != 0)
			{
				_globalPaletteStart = _position;
				_globalPaletteLength = TakePalette(packed);
			}
		}

		public int Width { get; }
		public int Height { get; }

		/// <summary>
		/// The loop count of the NETSCAPE2.0 extension if one has been met, otherwise -1.
		/// </summary>
		public int LoopCount { get; private set; } = -1;

		/// <summary>
		/// Moves to the next image of the file.
		/// </summary>
		/// <returns>The image, or <see langword="null"/> at the trailer.</returns>
		public Image NextImage()
		{
			while (true)
			{
				if (_position >= _data.Length)
					throw new InvalidDataException("GIF has no trailer.");

				var block = _data[_position++];

				switch (block)
				{
					// Whatever follows the trailer is not part of the picture.
					case _trailer:
						return null;

					case _extension:
						ReadExtension();
						break;

					case _imageSeparator:
						return ReadImage();

					default:
						throw new InvalidDataException($"Unexpected GIF block 0x{block:X2}.");
				}
			}
		}

		private void ReadExtension()
		{
			if (_position >= _data.Length)
				throw new InvalidDataException("Truncated GIF extension.");

			switch (_data[_position++])
			{
				case _graphicControl:
				{
					if (_position + 6 > _data.Length || _data[_position] != 4 || _data[_position + 5] != 0)
						throw new InvalidDataException("Malformed GIF graphic-control extension.");

					var packed = _data[_position + 1];
					_delay = _data[_position + 2] | _data[_position + 3] << 8;
					_transparent = (packed & 1) != 0 ? _data[_position + 4] : -1;
					_disposal = packed >> 2 & 7;
					_position += 6;

					if (_disposal > _disposeToPrevious)
						throw new NotSupportedException("Unsupported GIF disposal method.");

					break;
				}

				case _application:
				{
					if (_position >= _data.Length)
						throw new InvalidDataException("GIF application extension is truncated.");

					var size = _data[_position++];

					if (size > _data.Length - _position)
						throw new InvalidDataException("Truncated GIF application name.");

					var name = Encoding.ASCII.GetString(_data, _position, size);
					_position += size;

					if (name is "NETSCAPE2.0" or "ANIMEXTS1.0")
					{
						var content = ReadSubBlocks(_data, ref _position);

						if (content.Length >= 3 && content[0] == 1)
							LoopCount = content[1] | content[2] << 8;
					}
					else
						SkipSubBlocks();

					break;
				}

				default:
					SkipSubBlocks();
					break;
			}
		}

		private Image ReadImage()
		{
			if (_position + 9 > _data.Length)
				throw new InvalidDataException("Truncated GIF image descriptor.");

			var image = new Image
			{
				Left = _data[_position] | _data[_position + 1] << 8,
				Top = _data[_position + 2] | _data[_position + 3] << 8,
				Width = _data[_position + 4] | _data[_position + 5] << 8,
				Height = _data[_position + 6] | _data[_position + 7] << 8,
				Transparent = _transparent,
				Delay = _delay,
				Disposal = _disposal,
				PaletteStart = _globalPaletteStart,
				PaletteLength = _globalPaletteLength,
			};

			var flags = _data[_position + 8];
			_position += 9;

			if (image.Width == 0 || image.Height == 0)
				throw new InvalidDataException("GIF image has no size.");

			RasterImage.EnsureDecodable(image.Width, image.Height);

			image.IsInterlaced = (flags & 0x40) != 0;

			if ((flags & 0x80) != 0)
			{
				image.PaletteStart = _position;
				image.PaletteLength = TakePalette(flags);
			}

			if (image.PaletteLength == 0)
				throw new InvalidDataException("GIF image does not have a color table.");

			if (_position >= _data.Length)
				throw new InvalidDataException("Missing GIF LZW minimum code size.");

			image.MinCodeSize = _data[_position++];

			if (image.MinCodeSize is < 2 or > 8)
				throw new InvalidDataException("GIF LZW minimum code size is invalid.");

			image.DataStart = _position;
			SkipSubBlocks();

			// A graphic control extension speaks for the one image that follows it.
			_transparent = -1;
			_delay = 0;
			_disposal = 0;

			return image;
		}

		// Steps over a color table whose size is in the low three bits of a flags byte. Returns its length.
		private int TakePalette(int flags)
		{
			var length = 3 << ((flags & 7) + 1);

			if (length > _data.Length - _position)
				throw new InvalidDataException("Truncated GIF color table.");

			_position += length;
			return length;
		}

		private void SkipSubBlocks()
		{
			while (true)
			{
				if (_position >= _data.Length)
					throw new InvalidDataException("Truncated GIF data sub-block.");

				var length = _data[_position++];

				if (length == 0)
					return;

				if (length > _data.Length - _position)
					throw new InvalidDataException("GIF sub-block exceeds input.");

				_position += length;
			}
		}
	}

	public static bool IsGif(ReadOnlySpan<byte> bytes) =>
		bytes.Length >= 6 && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8));

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> bytes)
	{
		if (!IsGif(bytes) || bytes.Length < 13)
			throw new InvalidDataException("Invalid GIF header.");
		var width = bytes[6] | bytes[7] << 8;
		var height = bytes[8] | bytes[9] << 8;
		if (width == 0 || height == 0)
			throw new InvalidDataException("GIF has no size.");
		return (width, height);
	}

	/// <summary>
	/// Reads the loop count of the NETSCAPE2.0 extension: how many times the animation is repeated after
	/// its first run, 0 for endlessly.
	/// </summary>
	/// <returns>The loop count, or -1 if the file has no such extension.</returns>
	public static int ReadLoopCount(byte[] data)
		=> Scan(data, out _, out _).LoopCount;

	/// <summary>
	/// Reads what the file says about itself and prepares its frames for decoding.
	/// </summary>
	public static Animation Open(byte[] data)
	{
		var reader = Scan(data, out var frames, out var pixels);

		RasterImage.EnsureDecodable(reader.Width, reader.Height);
		Animation.EnsureWithinBudget(frames, pixels);

		// The extension counts repetitions: a file without it plays once, a file with N plays N + 1 times.
		var plays = reader.LoopCount < 0 ? 1 : reader.LoopCount == 0 ? 0 : reader.LoopCount + 1;

		return new(reader.Width, reader.Height, plays, frames, ReadFrames(data));
	}

	public static byte[] Encode(Animation animation)
	{
		using var output = new MemoryStream();
		output.Write("GIF89a"u8);
		Word(output, animation.Width);
		Word(output, animation.Height);
		output.WriteByte(0x70); // no global table, 8-bit source precision
		output.WriteByte(0);
		output.WriteByte(0);

		if (animation.Plays != 1)
		{
			output.Write([_extension, _application, 0x0B]);
			output.Write("NETSCAPE2.0"u8);
			output.WriteByte(3);
			output.WriteByte(1);
			Word(output, animation.Plays == 0 ? 0 : Math.Min(ushort.MaxValue, animation.Plays - 1));
			output.WriteByte(0);
		}

		var count = 0;

		foreach (var frame in animation.Frames)
		{
			if (frame.Image.Width != animation.Width || frame.Image.Height != animation.Height)
				throw new InvalidDataException("GIF frame size must equal logical canvas.");

			WriteFrame(output, frame);
			count++;
		}

		if (count == 0)
			throw new InvalidDataException("Cannot encode an empty GIF.");

		output.WriteByte(_trailer);
		return output.ToArray();
	}

	// Walks the whole file without unpacking a pixel: checks its structure, counts the frames and
	// the pixels they take to composite.
	private static Reader Scan(byte[] data, out int frames, out long pixels)
	{
		var reader = new Reader(data);
		var canvas = (long)reader.Width * reader.Height;
		frames = 0;
		pixels = 0;

		while (reader.NextImage() is { } image)
		{
			frames++;
			pixels += Math.Max(canvas, (long)image.Width * image.Height);
		}

		if (frames == 0)
			throw new InvalidDataException("GIF has no image.");

		return reader;
	}

	// The canvas starts transparent and stays so wherever no frame has drawn: the background color
	// of the logical screen is not used, the way browsers do not use it.
	private static IEnumerable<AnimationFrame> ReadFrames(byte[] data)
	{
		var reader = new Reader(data);
		var canvas = new RasterImage(reader.Width, reader.Height);
		Image previous = null;

		// The canvas as it was before the previous frame, if that frame is to be taken back.
		byte[] saved = null;

		while (reader.NextImage() is { } image)
		{
			if (previous?.Disposal == _disposeToBackground)
				Clear(canvas, previous);
			else if (previous?.Disposal == _disposeToPrevious)
				saved.CopyTo(canvas.Pixels, 0);

			saved = image.Disposal == _disposeToPrevious ? (byte[])canvas.Pixels.Clone() : null;

			Draw(data, canvas, image);
			previous = image;

			var frame = new RasterImage(canvas.Width, canvas.Height);
			canvas.Pixels.CopyTo(frame.Pixels, 0);

			yield return new(frame, image.Delay, 100);
		}
	}

	private static void Clear(RasterImage canvas, Image image)
	{
		var right = Math.Min(canvas.Width, image.Left + image.Width);
		var bottom = Math.Min(canvas.Height, image.Top + image.Height);

		if (image.Left >= right)
			return;

		for (var y = image.Top; y < bottom; y++)
			canvas.Pixels.AsSpan((y * canvas.Width + image.Left) * 4, (right - image.Left) * 4).Clear();
	}

	// An image may reach beyond the logical screen; what does is not shown.
	private static void Draw(byte[] data, RasterImage canvas, Image image)
	{
		var position = image.DataStart;
		var indices = Decompress(ReadSubBlocks(data, ref position), image.MinCodeSize, image.Width * image.Height);

		if (image.IsInterlaced)
			indices = Deinterlace(indices, image.Width, image.Height);

		var right = Math.Min(canvas.Width, image.Left + image.Width);
		var bottom = Math.Min(canvas.Height, image.Top + image.Height);
		var colors = image.PaletteLength / 3;

		for (var y = image.Top; y < bottom; y++)
			for (var x = image.Left; x < right; x++)
			{
				var index = indices[(y - image.Top) * image.Width + x - image.Left];

				if (index == image.Transparent)
					continue;

				if (index >= colors)
					throw new InvalidDataException("GIF palette index outside color table.");

				var from = image.PaletteStart + index * 3;
				var to = (y * canvas.Width + x) * 4;
				canvas.Pixels[to] = data[from];
				canvas.Pixels[to + 1] = data[from + 1];
				canvas.Pixels[to + 2] = data[from + 2];
				canvas.Pixels[to + 3] = 255;
			}
	}

	private static void WriteFrame(Stream output, AnimationFrame frame)
	{
		var image = frame.Image;
		var pixels = image.Width * image.Height;
		var indices = new byte[pixels];

		var hasTransparency = false;

		for (var i = 0; i < pixels && !hasTransparency; i++)
			hasTransparency = image.Pixels[i * 4 + 3] < ColorQuantizer.OpaqueAlpha;

		// A frame with transparent pixels gives them index 0 and has one color fewer for the rest.
		var first = hasTransparency ? 1 : 0;
		var colors = ColorQuantizer.Quantize(image, 256 - first, indices, first);
		var used = first + colors.Length / 3;

		// The table holds a power of two of colors, two at the least.
		var bits = 1;

		while (1 << bits < used)
			bits++;

		var delay = Math.Clamp((int)Math.Round(frame.DelayNumerator * 100.0 / frame.DelayDenominator, MidpointRounding.AwayFromZero), 0, ushort.MaxValue);

		// Every frame is the whole canvas and is cleared before the next one, so that the transparent
		// pixels of a frame show nothing of the frame before it.
		output.Write([_extension, _graphicControl, 4, (byte)(_disposeToBackground << 2 | (hasTransparency ? 1 : 0))]);
		Word(output, delay);
		output.WriteByte(0);
		output.WriteByte(0);

		output.WriteByte(_imageSeparator);
		Word(output, 0);
		Word(output, 0);
		Word(output, image.Width);
		Word(output, image.Height);
		output.WriteByte((byte)(0x80 | (bits - 1)));

		var table = new byte[3 << bits];
		colors.CopyTo(table, first * 3);
		output.Write(table);

		var minCodeSize = Math.Max(2, bits);
		output.WriteByte((byte)minCodeSize);

		var compressed = Compress(indices, minCodeSize);

		for (var at = 0; at < compressed.Length; at += 255)
		{
			var length = Math.Min(255, compressed.Length - at);
			output.WriteByte((byte)length);
			output.Write(compressed, at, length);
		}

		output.WriteByte(0);
	}

	// LZW: a string that has been met before is written as the one code it was given then. The width
	// of a code follows what the decoder knows at that moment, and the decoder learns a string one
	// code later than the encoder does.
	private static byte[] Compress(byte[] indices, int minCodeSize)
	{
		var clear = 1 << minCodeSize;
		var end = clear + 1;

		using var output = new MemoryStream();
		var buffer = 0u;
		var bits = 0;

		// What the decoder holds: the width of the next code and the next code it will assign.
		var codeSize = minCodeSize + 1;
		var decoderNext = end + 1;
		var isFirstCode = true;

		// What the encoder holds: strings as (code of the string so far, next index), and the next code to assign.
		var strings = new Dictionary<int, int>();
		var encoderNext = end + 1;

		void Emit(int code)
		{
			buffer |= (uint)code << bits;
			bits += codeSize;

			while (bits >= 8)
			{
				output.WriteByte((byte)buffer);
				buffer >>= 8;
				bits -= 8;
			}
		}

		// The decoder adds a string for every code it reads but the first after a clear, and widens
		// its codes when the next one to assign no longer fits.
		void Follow()
		{
			if (isFirstCode)
			{
				isFirstCode = false;
				return;
			}

			if (decoderNext < _maxCodes && ++decoderNext == 1 << codeSize && codeSize < 12)
				codeSize++;
		}

		Emit(clear);

		int current = indices[0];

		for (var i = 1; i < indices.Length; i++)
		{
			var key = current << 8 | indices[i];

			if (strings.TryGetValue(key, out var known))
			{
				current = known;
				continue;
			}

			Emit(current);
			Follow();

			if (encoderNext < _maxCodes)
				strings.Add(key, encoderNext++);
			else
			{
				// The table is full: start over.
				Emit(clear);
				strings.Clear();
				encoderNext = end + 1;
				decoderNext = end + 1;
				codeSize = minCodeSize + 1;
				isFirstCode = true;
			}

			current = indices[i];
		}

		Emit(current);
		Follow();
		Emit(end);

		if (bits > 0)
			output.WriteByte((byte)buffer);

		return output.ToArray();
	}

	// Reads as many indices as the image has pixels. A stream that goes on after them, or stops there
	// without its end code, is taken as it is; one that stops before them is not a picture.
	private static byte[] Decompress(byte[] input, int minCodeSize, int expected)
	{
		var clear = 1 << minCodeSize;
		var end = clear + 1;
		var next = end + 1;
		var codeSize = minCodeSize + 1;
		var prefixes = new short[_maxCodes];
		var suffixes = new byte[_maxCodes];
		var stack = new byte[_maxCodes];
		var pixels = new byte[expected];
		var cursor = 0;
		var position = 0;
		var buffer = 0;
		var bits = 0;
		var old = -1;
		byte first = 0;

		while (cursor < expected)
		{
			while (bits < codeSize)
			{
				if (position >= input.Length)
					throw new InvalidDataException("GIF pixel data ends before the image is complete.");

				buffer |= input[position++] << bits;
				bits += 8;
			}

			var code = buffer & ((1 << codeSize) - 1);
			buffer >>= codeSize;
			bits -= codeSize;

			if (code == clear)
			{
				next = end + 1;
				codeSize = minCodeSize + 1;
				old = -1;
				continue;
			}

			if (code == end)
				throw new InvalidDataException("GIF pixel data ends before the image is complete.");

			if (code > next || (code == next && old < 0))
				throw new InvalidDataException("Invalid GIF LZW dictionary code.");

			var decoded = code;
			var top = 0;

			// A code the table does not hold yet can only be the previous string plus its own first index.
			if (code == next)
			{
				stack[top++] = first;
				decoded = old;
			}

			// Prefixes point at codes assigned earlier, so the chain is shorter than the table.
			while (decoded >= clear)
			{
				stack[top++] = suffixes[decoded];
				decoded = prefixes[decoded];
			}

			first = (byte)decoded;
			stack[top++] = first;

			while (top > 0 && cursor < expected)
				pixels[cursor++] = stack[--top];

			if (old >= 0 && next < _maxCodes)
			{
				prefixes[next] = (short)old;
				suffixes[next] = first;
				next++;

				if (next == 1 << codeSize && codeSize < 12)
					codeSize++;
			}

			old = code;
		}

		return pixels;
	}

	private static byte[] Deinterlace(byte[] indices, int width, int height)
	{
		var output = new byte[indices.Length];
		var source = 0;
		foreach (var (start, step) in new (int start, int step)[] { (0,8),(4,8),(2,4),(1,2) })
			for (var y = start; y < height; y += step)
			{
				Array.Copy(indices, source, output, y * width, width);
				source += width;
			}
		return output;
	}

	private static byte[] ReadSubBlocks(byte[] data, ref int cursor)
	{
		using var combined = new MemoryStream();
		while (true)
		{
			if (cursor >= data.Length)
				throw new InvalidDataException("Truncated GIF data sub-block.");
			var length = data[cursor++];
			if (length == 0) break;
			if (length > data.Length - cursor)
				throw new InvalidDataException("GIF sub-block exceeds input.");
			combined.Write(data, cursor, length);
			cursor += length;
		}
		return combined.ToArray();
	}

	private static void Word(Stream output, int value)
	{
		if (value < 0 || value > ushort.MaxValue)
			throw new InvalidDataException("GIF field exceeds 16-bit range.");
		output.WriteByte((byte)value);
		output.WriteByte((byte)(value >> 8));
	}
}
