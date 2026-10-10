namespace Ecng.Tests.Drawing;

/// <summary>
/// A GIF file as its blocks describe it, read by a decoder that belongs to the tests:
/// what the library writes is checked without the library reading it back.
/// </summary>
internal sealed class GifFile
{
	/// <summary>
	/// One image of the file with the graphic control extension that preceded it.
	/// </summary>
	public sealed class Frame
	{
		public int Left { get; init; }
		public int Top { get; init; }
		public int Width { get; init; }
		public int Height { get; init; }
		public int Disposal { get; init; }

		/// <summary>
		/// Delay in hundredths of a second.
		/// </summary>
		public int Delay { get; init; }

		/// <summary>
		/// Index of the transparent color, -1 if the frame has none.
		/// </summary>
		public int Transparent { get; init; }

		/// <summary>
		/// The color table the frame uses, three bytes per color.
		/// </summary>
		public byte[] Palette { get; init; }

		public bool HasLocalPalette { get; init; }
		public int MinCodeSize { get; init; }

		/// <summary>
		/// Length of the compressed pixel data, without the sub-block length bytes.
		/// </summary>
		public int CompressedLength { get; init; }

		/// <summary>
		/// Whether the pixel data closed with the end-of-information code.
		/// </summary>
		public bool HasEndCode { get; init; }

		public byte[] Indices { get; init; }

		/// <summary>
		/// The frame as RGBA8: transparent pixels are all zeros.
		/// </summary>
		public byte[] ToRgba()
		{
			var rgba = new byte[Indices.Length * 4];

			for (var i = 0; i < Indices.Length; i++)
			{
				var index = Indices[i];

				if (index == Transparent)
					continue;

				(index * 3 + 2 < Palette.Length).AssertTrue($"color index {index} is outside the table");
				rgba[i * 4] = Palette[index * 3];
				rgba[i * 4 + 1] = Palette[index * 3 + 1];
				rgba[i * 4 + 2] = Palette[index * 3 + 2];
				rgba[i * 4 + 3] = 255;
			}

			return rgba;
		}
	}

	public int Width { get; private set; }
	public int Height { get; private set; }

	/// <summary>
	/// The NETSCAPE2.0 loop count, -1 if the file has no such extension.
	/// </summary>
	public int LoopCount { get; private set; } = -1;

	public List<Frame> Frames { get; } = [];

	/// <summary>
	/// Number of bytes that follow the trailer.
	/// </summary>
	public int BytesAfterTrailer { get; private set; }

	public static GifFile Read(byte[] data)
	{
		(data.AsSpan(0, 6).SequenceEqual("GIF89a"u8) || data.AsSpan(0, 6).SequenceEqual("GIF87a"u8)).AssertTrue("GIF signature");

		var file = new GifFile { Width = data[6] | data[7] << 8, Height = data[8] | data[9] << 8 };
		var at = 13;
		byte[] globalPalette = null;

		if ((data[10] & 0x80) != 0)
		{
			globalPalette = data.AsSpan(at, 3 << ((data[10] & 7) + 1)).ToArray();
			at += globalPalette.Length;
		}

		var transparent = -1;
		var delay = 0;
		var disposal = 0;

		while (true)
		{
			var block = data[at++];

			if (block == 0x3B)
				break;

			if (block == 0x21)
			{
				var label = data[at++];

				if (label == 0xF9)
				{
					data[at].AssertEqual((byte)4, "graphic control extension size");
					disposal = data[at + 1] >> 2 & 7;
					transparent = (data[at + 1] & 1) != 0 ? data[at + 4] : -1;
					delay = data[at + 2] | data[at + 3] << 8;
					data[at + 5].AssertEqual((byte)0, "graphic control extension terminator");
					at += 6;
				}
				else if (label == 0xFF)
				{
					var name = Encoding.ASCII.GetString(data, at + 1, data[at]);
					at += 1 + data[at];
					var content = SubBlocks(data, ref at);

					if (name == "NETSCAPE2.0" && content.Length >= 3 && content[0] == 1)
						file.LoopCount = content[1] | content[2] << 8;
				}
				else
					SubBlocks(data, ref at);

				continue;
			}

			block.AssertEqual((byte)0x2C, $"GIF block at {at - 1}");

			var left = data[at] | data[at + 1] << 8;
			var top = data[at + 2] | data[at + 3] << 8;
			var width = data[at + 4] | data[at + 5] << 8;
			var height = data[at + 6] | data[at + 7] << 8;
			var flags = data[at + 8];
			at += 9;

			(flags & 0x40).AssertEqual(0, "interlaced frames are not read here");

			var palette = globalPalette;

			if ((flags & 0x80) != 0)
			{
				palette = data.AsSpan(at, 3 << ((flags & 7) + 1)).ToArray();
				at += palette.Length;
			}

			var minCodeSize = data[at++];
			var compressed = SubBlocks(data, ref at);
			var indices = Decompress(compressed, minCodeSize, width * height, out var hasEndCode);

			file.Frames.Add(new()
			{
				Left = left,
				Top = top,
				Width = width,
				Height = height,
				Disposal = disposal,
				Delay = delay,
				Transparent = transparent,
				Palette = palette,
				HasLocalPalette = (flags & 0x80) != 0,
				MinCodeSize = minCodeSize,
				CompressedLength = compressed.Length,
				HasEndCode = hasEndCode,
				Indices = indices,
			});

			transparent = -1;
			delay = 0;
			disposal = 0;
		}

		file.BytesAfterTrailer = data.Length - at;
		return file;
	}

	private static byte[] SubBlocks(byte[] data, ref int at)
	{
		var content = new List<byte>();

		while (data[at] != 0)
		{
			content.AddRange(data.AsSpan(at + 1, data[at]).ToArray());
			at += 1 + data[at];
		}

		at++;
		return [.. content];
	}

	// Textbook LZW: every code after the first adds the previous string plus the first byte of the current one.
	private static byte[] Decompress(byte[] input, int minCodeSize, int count, out bool hasEndCode)
	{
		var clear = 1 << minCodeSize;
		var end = clear + 1;
		var table = new List<byte[]>();

		void Reset()
		{
			table.Clear();

			for (var i = 0; i < clear; i++)
				table.Add([(byte)i]);

			table.Add([]);
			table.Add([]);
		}

		Reset();

		var output = new List<byte>(count);
		var codeSize = minCodeSize + 1;
		var bit = 0;
		byte[] previous = null;
		hasEndCode = false;

		while (bit + codeSize <= input.Length * 8)
		{
			var code = 0;

			for (var i = 0; i < codeSize; i++, bit++)
				code |= (input[bit / 8] >> (bit % 8) & 1) << i;

			if (code == clear)
			{
				Reset();
				codeSize = minCodeSize + 1;
				previous = null;
				continue;
			}

			if (code == end)
			{
				hasEndCode = true;
				break;
			}

			byte[] entry;

			if (code < table.Count)
				entry = table[code];
			else
			{
				(code == table.Count && previous is not null).AssertTrue($"LZW code {code} is not in the table of {table.Count}");
				entry = [.. previous, previous[0]];
			}

			output.AddRange(entry);

			if (previous is not null && table.Count < 4096)
			{
				table.Add([.. previous, entry[0]]);

				if (table.Count == 1 << codeSize && codeSize < 12)
					codeSize++;
			}

			previous = entry;
		}

		output.Count.AssertEqual(count, "number of pixels the LZW data unpacks to");
		return [.. output];
	}
}
