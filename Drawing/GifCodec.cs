namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>Decoded GIF89a frame, composited onto the logical screen, including its original delay.</summary>
internal sealed class GifFrame(RasterImage image, int delayCentiseconds)
{
	public RasterImage Image { get; } = image;
	public int DelayCentiseconds { get; } = delayCentiseconds;
}

/// <summary>All GIF frames and the Netscape loop count (0=infinite, -1=not specified).</summary>
internal sealed class GifAnimation(int width, int height)
{
	public int Width { get; } = width;
	public int Height { get; } = height;
	public int LoopCount { get; set; } = -1;
	public List<GifFrame> Frames { get; } = [];
}

/// <summary>
/// Fully managed animated GIF decoder/encoder. Supports global/local palettes,
/// LZW, interlace, transparent indices, disposal 0/1/2/3, Netscape loop count,
/// arbitrary image subrectangles, delay and full frame compositing.
/// GIF output has 1-bit alpha and <=256 colors per frame by format definition.
/// </summary>
internal static class GifCodec
{
	public static bool IsGif(ReadOnlySpan<byte> bytes) =>
		bytes.Length >= 6 && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8));

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> bytes)
	{
		if (!IsGif(bytes) || bytes.Length < 13)
			throw new InvalidDataException("Invalid GIF header.");
		var width = U16(bytes, 6);
		var height = U16(bytes, 8);
		if (width == 0 || height == 0 || (long)width * height > 25_000_000)
			throw new InvalidDataException("GIF logical dimensions exceed 25 megapixels.");
		return (width, height);
	}

	public static GifAnimation Decode(byte[] data)
	{
		var (width, height) = ReadSize(data);
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("GIF input exceeds supported size.");

		var bytes = data.AsSpan();
		var packed = data[10];
		var backgroundIndex = data[11];
		var position = 13;
		var globalPalette = (packed & 0x80) != 0 ?
			Palette(bytes, ref position, 1 << ((packed & 7) + 1)) : Array.Empty<byte>();

		var animation = new GifAnimation(width, height);
		var canvas = new RasterImage(width, height);
		var initialized = false;
		var previousDisposal = 0;
		var previousX = 0;
		var previousY = 0;
		var previousWidth = 0;
		var previousHeight = 0;
		var previousClearAlpha = (byte)0;
		byte[] savedBeforePrevious = null;

		var transparency = -1;
		var delay = 0;
		var disposal = 0;
		var ended = false;
		long totalFramePixels = 0;

		while (position < data.Length)
		{
			var tag = data[position++];
			if (tag == 0x3B) { ended = true; break; }
			if (tag == 0x21)
			{
				if (position >= data.Length) throw new InvalidDataException("Truncated GIF extension.");
				var extension = data[position++];
				if (extension == 0xF9)
				{
					if (position + 6 > data.Length || data[position++] != 4)
						throw new InvalidDataException("Malformed GIF graphic-control extension.");
					var gce = data[position++];
					delay = U16(data, position); position += 2;
					transparency = (gce & 1) != 0 ? data[position] : -1;
					position++;
					if (data[position++] != 0)
						throw new InvalidDataException("GIF graphic-control terminator is missing.");
					disposal = (gce >> 2) & 7;
					if (disposal > 3)
						throw new NotSupportedException("Unsupported GIF disposal method.");
				}
				else if (extension == 0xFF)
				{
					if (position >= data.Length) throw new InvalidDataException("GIF application extension is truncated.");
					var size = data[position++];
					if (size > data.Length - position) throw new InvalidDataException("Truncated GIF application name.");
					var name = System.Text.Encoding.ASCII.GetString(data, position, size);
					position += size;
					var payload = SubBlocks(data, ref position);
					if ((name == "NETSCAPE2.0" || name == "ANIMEXTS1.0") &&
						payload.Length >= 3 && payload[0] == 1)
						animation.LoopCount = payload[1] | payload[2] << 8;
				}
				else
					_ = SubBlocks(data, ref position);
				continue;
			}
			if (tag != 0x2C)
				throw new InvalidDataException($"Unexpected GIF block 0x{tag:X2}.");
			if (position + 9 > data.Length)
				throw new InvalidDataException("Truncated GIF image descriptor.");

			var left = U16(data, position);
			var top = U16(data, position + 2);
			var w = U16(data, position + 4);
			var h = U16(data, position + 6);
			var flags = data[position + 8];
			position += 9;

			if (w == 0 || h == 0 || left + w > width || top + h > height)
				throw new InvalidDataException("GIF image rectangle exceeds logical screen.");
			if (++totalFramePixels > 40_000_000 || animation.Frames.Count >= 512)
				throw new InvalidDataException("GIF total animation pixel budget exceeded.");

			var palette = (flags & 0x80) != 0
				? Palette(bytes, ref position, 1 << ((flags & 7) + 1))
				: globalPalette;
			if (palette.Length == 0)
				throw new InvalidDataException("GIF image does not have a color table.");

			if (position >= data.Length)
				throw new InvalidDataException("Missing GIF LZW minimum code size.");
			var minSize = data[position++];
			var compressed = SubBlocks(data, ref position);
			var indices = Decompress(compressed, minSize, w * h);
			if ((flags & 0x40) != 0)
				indices = Deinterlace(indices, w, h);

			// GIF logical background is transparent when its index is declared
			// transparent by the initial frame, otherwise it is the global color.
			if (!initialized)
			{
				if (globalPalette.Length > backgroundIndex * 3 && transparency != backgroundIndex)
					Fill(canvas.Pixels, globalPalette, backgroundIndex, 255);
				initialized = true;
			}

			// Disposal of the preceding frame happens before painting this one.
			if (previousDisposal == 2)
			{
				for (var y = previousY; y < previousY + previousHeight; y++)
					for (var x = previousX; x < previousX + previousWidth; x++)
					{
						var offset = (y * width + x) * 4;
						if (previousClearAlpha == 0)
							Array.Clear(canvas.Pixels, offset, 4);
						else
							AssignBackground(canvas.Pixels, offset, globalPalette, backgroundIndex);
					}
			}
			else if (previousDisposal == 3 && savedBeforePrevious != null)
				Array.Copy(savedBeforePrevious, canvas.Pixels, canvas.Pixels.Length);

			var backup = disposal == 3 ? (byte[])canvas.Pixels.Clone() : null;
			for (var y = 0; y < h; y++)
				for (var x = 0; x < w; x++)
				{
					var index = indices[y * w + x];
					if (index == transparency) continue;
					if (index * 3 + 2 >= palette.Length)
						throw new InvalidDataException("GIF palette index outside color table.");
					var at = ((top + y) * width + left + x) * 4;
					canvas.Pixels[at] = palette[index * 3];
					canvas.Pixels[at + 1] = palette[index * 3 + 1];
					canvas.Pixels[at + 2] = palette[index * 3 + 2];
					canvas.Pixels[at + 3] = 255;
				}
			var composited = new RasterImage(width, height);
			Array.Copy(canvas.Pixels, composited.Pixels, canvas.Pixels.Length);
			animation.Frames.Add(new GifFrame(composited, delay));

			previousDisposal = disposal;
			previousX = left; previousY = top;
			previousWidth = w; previousHeight = h;
			previousClearAlpha = (byte)(transparency == backgroundIndex ? 0 : 255);
			savedBeforePrevious = backup;
			transparency = -1; delay = 0; disposal = 0;
		}

		if (!ended || animation.Frames.Count == 0 || position != data.Length)
			throw new InvalidDataException("GIF animation is truncated or contains trailing bytes.");
		return animation;
	}

	public static byte[] Encode(GifAnimation animation)
	{
		if (animation.Frames.Count == 0)
			throw new InvalidDataException("Cannot encode an empty GIF.");

		using var output = new MemoryStream();
		output.Write("GIF89a"u8);
		Word(output, animation.Width);
		Word(output, animation.Height);
		output.WriteByte(0x70); // no global table, 8-bit source precision
		output.WriteByte(0); // background index: transparent
		output.WriteByte(0);

		if (animation.LoopCount >= 0)
		{
			output.Write([0x21,0xFF,0x0B]);
			output.Write("NETSCAPE2.0"u8);
			output.WriteByte(3);
			output.WriteByte(1);
			Word(output, animation.LoopCount);
			output.WriteByte(0);
		}

		foreach (var frame in animation.Frames)
		{
			if (frame.Image.Width != animation.Width || frame.Image.Height != animation.Height)
				throw new InvalidDataException("GIF frame size must equal logical canvas.");

			var (indexed, palette) = Quantize(frame.Image);
			var delay = Math.Clamp(frame.DelayCentiseconds, 0, ushort.MaxValue);
			// Full-canvas source frames. Restore-to-transparent background
			// before each next frame so frames are independent of each other.
			output.Write([0x21,0xF9,4,0x09]);
			Word(output, delay);
			output.WriteByte(0);
			output.WriteByte(0);
			output.WriteByte(0x2C);
			Word(output, 0); Word(output, 0);
			Word(output, animation.Width); Word(output, animation.Height);
			output.WriteByte(0x87); // local 256-color palette
			output.Write(palette);
			output.WriteByte(8); // LZW minimum size
			var compressed = Compress(indexed);
			for (var pos = 0; pos < compressed.Length;)
			{
				var n = Math.Min(255, compressed.Length - pos);
				output.WriteByte((byte)n);
				output.Write(compressed, pos, n);
				pos += n;
			}
			output.WriteByte(0);
		}
		output.WriteByte(0x3B);
		return output.ToArray();
	}

	private static (byte[] indexed, byte[] palette) Quantize(RasterImage image)
	{
		var indexed = new byte[image.Width * image.Height];
		var palette = new byte[256 * 3];
		var colors = new Dictionary<int, byte>();
		var tooManyColors = false;
		for (var i = 0; i < indexed.Length; i++)
		{
			var pos = i * 4;
			if (image.Pixels[pos + 3] < 128) continue;
			var key = image.Pixels[pos] << 16 | image.Pixels[pos + 1] << 8 | image.Pixels[pos + 2];
			if (!colors.ContainsKey(key))
			{
				if (colors.Count == 255) { tooManyColors = true; break; }
				var index = checked((byte)(colors.Count + 1));
				colors.Add(key, index);
				palette[index * 3] = image.Pixels[pos];
				palette[index * 3 + 1] = image.Pixels[pos + 1];
				palette[index * 3 + 2] = image.Pixels[pos + 2];
			}
		}

		if (tooManyColors)
		{
			colors.Clear();
			for (var index = 1; index <= 255; index++)
			{
				palette[index * 3] = (byte)((index >> 5) * 255 / 7);
				palette[index * 3 + 1] = (byte)(((index >> 2) & 7) * 255 / 7);
				palette[index * 3 + 2] = (byte)((index & 3) * 255 / 3);
			}
		}

		for (var i = 0; i < indexed.Length; i++)
		{
			var pos = i * 4;
			if (image.Pixels[pos + 3] < 128) { indexed[i] = 0; continue; }
			if (tooManyColors)
			{
				var colorCode = (image.Pixels[pos] >> 5) << 5 |
					(image.Pixels[pos + 1] >> 5) << 2 | image.Pixels[pos + 2] >> 6;
				indexed[i] = (byte)Math.Max(1, colorCode);
			}
			else
			{
				var key = image.Pixels[pos] << 16 | image.Pixels[pos + 1] << 8 | image.Pixels[pos + 2];
				indexed[i] = colors[key];
			}
		}

		return (indexed, palette);
	}

	// Valid and deliberately bounded LZW encoding: emit a CLEAR every 200
	// literal symbols, keeping code widths at 9 bits. This prioritizes simple,
	// deterministic and interoperable output over the smallest GIF filesize.
	private static byte[] Compress(byte[] values)
	{
		using var output = new MemoryStream();
		var buffer = 0u;
		var available = 0;
		void Emit(int code)
		{
			buffer |= (uint)code << available;
			available += 9;
			while (available >= 8)
			{
				output.WriteByte((byte)buffer);
				buffer >>= 8;
				available -= 8;
			}
		}

		Emit(256); // clear
		var sinceClear = 0;
		foreach (var value in values)
		{
			if (sinceClear == 200) { Emit(256); sinceClear = 0; }
			Emit(value);
			sinceClear++;
		}
		Emit(257); // EOI
		if (available > 0) output.WriteByte((byte)buffer);
		return output.ToArray();
	}

	private static byte[] Decompress(byte[] input, int minimum, int expected)
	{
		if (minimum < 2 || minimum > 8)
			throw new InvalidDataException("GIF LZW minimum code size is invalid.");

		var clear = 1 << minimum;
		var end = clear + 1;
		var next = end + 1;
		var codeSize = minimum + 1;
		var prefixes = new int[4096];
		var suffixes = new byte[4096];
		var stack = new byte[4096];
		var pixels = new byte[expected];
		var cursor = 0; var bit = 0;
		var old = -1; byte first = 0;
		var ended = false;

		int ReadCode()
		{
			if ((long)bit + codeSize > input.Length * 8L)
				throw new InvalidDataException("Truncated GIF LZW stream.");
			var value = 0;
			for (var i = 0; i < codeSize; i++)
				value |= (input[(bit + i) / 8] >> ((bit + i) & 7) & 1) << i;
			bit += codeSize;
			return value;
		}

		while (true)
		{
			var code = ReadCode();
			if (code == clear)
			{
				next = end + 1;
				codeSize = minimum + 1;
				old = -1;
				continue;
			}
			if (code == end) { ended = true; break; }
			if (code > next || code >= 4096)
				throw new InvalidDataException("Invalid GIF LZW dictionary code.");

			var decoded = code;
			var top = 0;
			if (code == next)
			{
				if (old < 0) throw new InvalidDataException("Invalid initial GIF LZW code.");
				stack[top++] = first;
				decoded = old;
			}

			var steps = 0;
			while (decoded >= clear)
			{
				if (decoded >= next || top == stack.Length || ++steps > 4096)
					throw new InvalidDataException("Cyclic or oversized GIF LZW dictionary.");
				stack[top++] = suffixes[decoded];
				decoded = prefixes[decoded];
			}
			if (decoded >= clear)
				throw new InvalidDataException("GIF LZW literal out of range.");

			first = (byte)decoded;
			stack[top++] = first;
			while (top > 0)
			{
				if (cursor >= expected)
					throw new InvalidDataException("GIF LZW produced extra pixels.");
				pixels[cursor++] = stack[--top];
			}

			if (old >= 0 && next < 4096)
			{
				prefixes[next] = old;
				suffixes[next] = first;
				next++;
				if (next == (1 << codeSize) && codeSize < 12)
					codeSize++;
			}
			old = code;
		}

		if (!ended || cursor != expected)
			throw new InvalidDataException("GIF LZW did not decode expected pixel count.");
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

	private static byte[] SubBlocks(byte[] data, ref int cursor)
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

	private static byte[] Palette(ReadOnlySpan<byte> data, ref int cursor, int count)
	{
		var length = checked(count * 3);
		if (cursor + length > data.Length)
			throw new InvalidDataException("Truncated GIF color table.");
		var colors = data.Slice(cursor, length).ToArray();
		cursor += length;
		return colors;
	}

	private static void Fill(byte[] target, byte[] palette, int index, byte alpha)
	{
		for (var at = 0; at < target.Length; at += 4)
		{
			target[at] = palette[index * 3];
			target[at + 1] = palette[index * 3 + 1];
			target[at + 2] = palette[index * 3 + 2];
			target[at + 3] = alpha;
		}
	}

	private static void AssignBackground(byte[] target, int at, byte[] palette, int index)
	{
		if (palette.Length <= index * 3 + 2) { Array.Clear(target, at, 4); return; }
		target[at] = palette[index * 3];
		target[at + 1] = palette[index * 3 + 1];
		target[at + 2] = palette[index * 3 + 2];
		target[at + 3] = 255;
	}

	private static ushort U16(ReadOnlySpan<byte> data, int at)
	{
		if (at < 0 || data.Length - at < 2)
			throw new InvalidDataException("Truncated GIF data.");
		return BinaryPrimitives.ReadUInt16LittleEndian(data[at..]);
	}
	private static void Word(Stream output, int value)
	{
		if (value < 0 || value > ushort.MaxValue)
			throw new InvalidDataException("GIF field exceeds 16-bit range.");
		output.WriteByte((byte)value);
		output.WriteByte((byte)(value >> 8));
	}
}
