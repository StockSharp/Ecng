namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>Managed PNG decoder/encoder (all standard color types, 1/2/4/8/16-bit samples, Adam7).</summary>
internal static class PngCodec
{
	// The most a deflate stream can expand what it holds.
	private const int _maxDeflateRatio = 1032;

	// Bytes a pixel of the pictures that are written takes.
	private const int _pixel = 4;

	// Scanlines are handed to zlib this many bytes at a time, or one by one if they are longer:
	// handed row by row they pack worse.
	private const int _block = 64 * 1024;

	// The name a profile gets in a PNG when it comes from a format that does not name it.
	private const string _profileName = "ICC profile";

	// The chunks that say what colors the samples are, in the order they are written.
	private const int _primaries = 0;
	private const int _gamma = 1;
	private const int _profile = 2;
	private const int _standard = 3;
	private const int _colorChunkKinds = 4;

	public static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];
	private static readonly uint[] _crcTable = CreateCrcTable();
	private static readonly (int x, int y, int dx, int dy)[] _passes =
	[
		(0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4),
		(0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2),
	];

	public static bool IsPng(ReadOnlySpan<byte> data) => data.StartsWith(Signature);

	public static (int width, int height) ReadSize(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length < 24 || !IsPng(bytes) || !bytes.Slice(12, 4).SequenceEqual("IHDR"u8) ||
			BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8, 4)) != 13)
			throw new InvalidDataException("Invalid PNG IHDR header.");

		var width = BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(16, 4));
		var height = BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(20, 4));

		if (width <= 0 || height <= 0)
			throw new InvalidDataException("PNG has no size.");

		return (width, height);
	}

	public static RasterImage Decode(byte[] data)
		=> Decode(data, ImageProcessingLimits.Default);

	public static RasterImage Decode(byte[] data, ImageProcessingLimits limits)
		=> Decode(data, 0, 0, limits);

	public static RasterImage DecodeDownscaled(byte[] data, int width, int height)
		=> Decode(data, width, height, ImageProcessingLimits.Default);

	public static RasterImage DecodeDownscaled(byte[] data, int width, int height, ImageProcessingLimits limits)
		=> Decode(data, width, height, limits);

	private static RasterImage Decode(byte[] data, int targetWidth, int targetHeight, ImageProcessingLimits limits)
	{
		var (width, height) = ReadSize(data);
		var isDownscaled = targetWidth != 0;
		if (!isDownscaled)
			RasterImage.EnsureDecodable(width, height, limits);
		if (data.Length > 128 * 1024 * 1024)
			throw new InvalidDataException("PNG input exceeds the supported size.");

		var depth = 0;
		var color = 0;
		var interlace = 0;
		var palette = Array.Empty<byte>();
		var transparency = Array.Empty<byte>();
		var gotHeader = false;
		var gotData = false;
		var dataClosed = false;
		var ended = false;
		using var compressed = new MemoryStream();
		var firstDataOffset = -1;
		var firstDataLength = 0;
		var offset = 8;

		while (offset <= data.Length - 12)
		{
			var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
			if (length > int.MaxValue || length > data.Length - offset - 12)
				throw new InvalidDataException("Invalid PNG chunk length.");

			var tag = data.AsSpan(offset + 4, 4);
			var content = data.AsSpan(offset + 8, (int)length);
			var actualCrc = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8 + (int)length, 4));
			if (actualCrc != Crc(data.AsSpan(offset + 4, (int)length + 4)))
				throw new InvalidDataException("PNG CRC mismatch.");

			// PNG requires all IDAT chunks to appear consecutively.
			// Ancillary chunks may follow IDAT, but cannot split its sequence.
			if (gotData && !tag.SequenceEqual("IDAT"u8) && !tag.SequenceEqual("IEND"u8))
				dataClosed = true;

			if (tag.SequenceEqual("IHDR"u8))
			{
				if (gotHeader || offset != 8 || length != 13)
					throw new InvalidDataException("Invalid PNG IHDR placement.");

				gotHeader = true;
				depth = content[8];
				color = content[9];
				interlace = content[12];
				if (content[10] != 0 || content[11] != 0 || interlace > 1 || !ValidFormat(depth, color))
					throw new NotSupportedException("Unsupported PNG bit depth, color type, or interlace method.");
			}
			else if (tag.SequenceEqual("PLTE"u8))
			{
				if (!gotHeader || gotData || length == 0 || length > 768 || length % 3 != 0)
					throw new InvalidDataException("Invalid PNG palette.");

				palette = content.ToArray();
			}
			else if (tag.SequenceEqual("tRNS"u8))
			{
				if (!gotHeader || gotData)
					throw new InvalidDataException("Invalid PNG transparency chunk.");

				transparency = content.ToArray();
			}
			else if (tag.SequenceEqual("IDAT"u8))
			{
				if (dataClosed)
					throw new InvalidDataException("PNG IDAT chunks must be contiguous.");
				if (!gotHeader)
					throw new InvalidDataException("PNG data precedes header.");
				gotData = true;
				if (firstDataOffset < 0)
				{
					firstDataOffset = offset + 8;
					firstDataLength = content.Length;
				}
				else
				{
					if (compressed.Length == 0)
						compressed.Write(data.AsSpan(firstDataOffset, firstDataLength));
					compressed.Write(content);
				}
			}
			else if (tag.SequenceEqual("IEND"u8))
			{
				if (length != 0) throw new InvalidDataException("Invalid IEND.");
				ended = true;
				break;
			}
			else if ((tag[0] & 0x20) == 0)
				throw new NotSupportedException("Unknown critical PNG chunk.");

			offset = checked(offset + 12 + (int)length);
		}

		if (!gotHeader || !gotData || !ended)
			throw new InvalidDataException("PNG is incomplete.");
		if (color == 3 && (palette.Length == 0 || transparency.Length > palette.Length / 3))
			throw new InvalidDataException("Invalid indexed PNG palette.");
		if ((color == 0 && transparency.Length != 0 && transparency.Length != 2) ||
			(color == 2 && transparency.Length != 0 && transparency.Length != 6) ||
			(color != 0 && color != 2 && color != 3 && transparency.Length != 0))
			throw new InvalidDataException("Invalid PNG transparency.");

		// A single IDAT can be read from the validated input; only split streams need concatenation.
		using var singleChunk = compressed.Length == 0 ? new MemoryStream(data, firstDataOffset, firstDataLength, writable: false) : null;
		var payload = singleChunk ?? compressed;

		var channels = color switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new InvalidDataException() };
		var rowSize = ((long)width * channels * depth + 7) / 8;
		if (rowSize > 128 * 1024 * 1024 || (long)width * 4 > 128 * 1024 * 1024)
			throw new InvalidDataException("PNG scanline exceeds the supported size.");
		var rowBytes = (int)rowSize;
		var needed = interlace == 0 ? checked((long)(rowBytes + 1) * height) : 0L;

		if (interlace == 1)
			foreach (var (px, py, dx, dy) in _passes)
			{
				var w = PassSize(width, px, dx);
				var h = PassSize(height, py, dy);
				if (w != 0 && h != 0)
					needed += (((long)w * channels * depth + 7) / 8 + 1) * h;
			}

		// The scanlines are unpacked straight into a buffer of the size the header calls for, so the
		// header is not taken at its word: data too short to expand to that size is refused first.
		if (needed > payload.Length * _maxDeflateRatio)
			throw new InvalidDataException("PNG scanlines have an invalid length.");

		var workingBytes = rowSize * 2 + (long)(isDownscaled ? targetWidth : width) * (isDownscaled ? targetHeight : height) * 4;
		if (isDownscaled)
			workingBytes += (long)width * 4 + (long)targetWidth * (interlace == 0 ? 80 : 48) +
				(interlace == 0 ? 0 : (long)targetWidth * targetHeight * 32);
		limits.EnsureWorkingMemory(workingBytes);

		var downscaler = isDownscaled && interlace == 0 ? new ImageDownscaler(width, height, targetWidth, targetHeight, limits) : null;
		var accumulator = isDownscaled && interlace != 0 ? new ImageAreaAccumulator(width, height, targetWidth, targetHeight, limits) : null;
		var result = downscaler?.Image ?? accumulator?.Image ?? new RasterImage(width, height, limits);
		payload.Position = 0;
		using var zlib = new ZLibStream(payload, CompressionMode.Decompress, leaveOpen: true);

		if (interlace == 0)
			DecodePass(result, downscaler, null, zlib, width, height, 0, 0, 1, 1,
				channels, depth, color, palette, transparency);
		else
			foreach (var (px, py, dx, dy) in _passes)
			{
				var w = PassSize(width, px, dx);
				var h = PassSize(height, py, dy);
				if (w != 0 && h != 0)
					DecodePass(result, null, accumulator, zlib, w, h, px, py, dx, dy,
						channels, depth, color, palette, transparency);
			}

		if (zlib.ReadByte() >= 0)
			throw new InvalidDataException("PNG decompression exceeded expected row data.");

		accumulator?.Complete();
		return result;
	}

	/// <summary>
	/// Writes a picture as an RGBA8 PNG.
	/// </summary>
	/// <param name="image">The picture.</param>
	/// <param name="colorChunks">
	/// The chunks that say what colors the samples are, whole and ready to be written: what
	/// <see cref="ReadColorChunks"/> or <see cref="ProfileChunk"/> gave. Empty for none.
	/// </param>
	public static byte[] Encode(RasterImage image, byte[] colorChunks)
	{
		using var output = new MemoryStream();
		output.Write(Signature);
		var header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), image.Width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), image.Height);
		header[8] = 8;
		header[9] = 6; // RGBA8
		WriteChunk(output, "IHDR"u8, header);
		output.Write(colorChunks);

		WriteChunk(output, "IDAT"u8, Compress(image));
		WriteChunk(output, "IEND"u8, []);
		return output.ToArray();
	}

	/// <summary>
	/// Reads the chunks of a PNG that say what colors its samples are - cHRM, gAMA, iCCP, sRGB - so that
	/// a picture made of it can say the same.
	/// </summary>
	/// <returns>
	/// The chunks, whole, one after another. A profile is among them only if it can go into a re-encoded
	/// picture (see <see cref="IccProfile.IsRgb"/>), and sRGB only if there is no profile: a file holds one or the other.
	/// </returns>
	public static byte[] ReadColorChunks(ReadOnlySpan<byte> data)
	{
		// Where each chunk is in the file and how long; the first of a kind is the one that counts.
		Span<int> starts = stackalloc int[_colorChunkKinds];
		Span<int> lengths = stackalloc int[_colorChunkKinds];
		Span<bool> isMet = stackalloc bool[_colorChunkKinds];

		for (var offset = Signature.Length; offset <= data.Length - 12;)
		{
			var length = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));

			if (length > (uint)(data.Length - offset - 12))
				break;

			var tag = data.Slice(offset + 4, 4);
			var content = data.Slice(offset + 8, (int)length);
			var whole = 12 + (int)length;

			// The color of the samples is said before the samples.
			if (tag.SequenceEqual("IDAT"u8) || tag.SequenceEqual("IEND"u8))
				break;

			var kind =
				tag.SequenceEqual("cHRM"u8) ? _primaries :
				tag.SequenceEqual("gAMA"u8) ? _gamma :
				tag.SequenceEqual("iCCP"u8) ? _profile :
				tag.SequenceEqual("sRGB"u8) ? _standard : -1;

			if (kind >= 0 && !isMet[kind])
			{
				isMet[kind] = true;

				// The checksum first: a profile has to be unpacked to be looked at.
				var isWhole = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 8 + (int)length, 4)) == Crc(data.Slice(offset + 4, (int)length + 4));

				var isSound = isWhole && kind switch
				{
					_primaries => length == 32,
					_gamma => length == 4,
					_standard => length == 1,
					_ => UnpackProfile(content).Length != 0,
				};

				if (isSound)
				{
					starts[kind] = offset;
					lengths[kind] = whole;
				}
			}

			offset += whole;
		}

		if (lengths[_profile] != 0)
			lengths[_standard] = 0;

		var chunks = new byte[lengths[_primaries] + lengths[_gamma] + lengths[_profile] + lengths[_standard]];
		var to = 0;

		for (var kind = 0; kind < _colorChunkKinds; kind++)
		{
			data.Slice(starts[kind], lengths[kind]).CopyTo(chunks.AsSpan(to));
			to += lengths[kind];
		}

		return chunks;
	}

	/// <summary>
	/// Reads the ICC profile of a PNG, unpacked from the iCCP chunk that <see cref="ReadColorChunks"/> carries over.
	/// </summary>
	/// <returns>
	/// The profile if it is one that can go into a re-encoded picture (see <see cref="IccProfile.IsRgb"/>);
	/// otherwise - and if the file has none - empty.
	/// </returns>
	public static byte[] ReadProfile(ReadOnlySpan<byte> data)
	{
		ReadOnlySpan<byte> chunks = ReadColorChunks(data);

		// Whole chunks one after another: the length, the tag, the content and the checksum.
		for (var offset = 0; offset < chunks.Length;)
		{
			var length = (int)BinaryPrimitives.ReadUInt32BigEndian(chunks[offset..]);

			if (chunks.Slice(offset + 4, 4).SequenceEqual("iCCP"u8))
				return UnpackProfile(chunks.Slice(offset + 8, length));

			offset += 12 + length;
		}

		return [];
	}

	/// <summary>
	/// Makes the iCCP chunk that holds an ICC profile.
	/// </summary>
	/// <param name="profile">The profile; empty for none.</param>
	/// <returns>The chunk, whole; empty if there is no profile.</returns>
	public static byte[] ProfileChunk(byte[] profile)
	{
		if (profile.Length == 0)
			return [];

		// The name, a zero, the compression method - the only one there is - and the profile deflated.
		using var content = new MemoryStream();
		content.Write(Encoding.Latin1.GetBytes(_profileName));
		content.WriteByte(0);
		content.WriteByte(0);

		using (var zlib = new ZLibStream(content, CompressionLevel.Optimal, leaveOpen: true))
			zlib.Write(profile);

		using var chunk = new MemoryStream();
		WriteChunk(chunk, "iCCP"u8, content.GetBuffer().AsSpan(0, (int)content.Length));
		return chunk.ToArray();
	}

	// The profile the content of an iCCP chunk holds - a name of 1 to 79 characters, a zero, the compression method,
	// the profile deflated - if it is one for RGB; empty otherwise.
	private static byte[] UnpackProfile(ReadOnlySpan<byte> content)
	{
		var name = content.IndexOf((byte)0);

		if (name is < 1 or > 79 || content.Length < name + 2 || content[name + 1] != 0)
			return [];

		// One byte beyond the largest profile that is carried is enough to tell that it is too large.
		var profile = new byte[Math.Min(IccProfile.MaxLength + 1L, (long)(content.Length - name - 2) * _maxDeflateRatio)];
		var filled = 0;

		try
		{
			using var zlib = new ZLibStream(new MemoryStream(content[(name + 2)..].ToArray()), CompressionMode.Decompress);

			while (filled < profile.Length)
			{
				var read = zlib.Read(profile, filled, profile.Length - filled);

				if (read == 0)
					break;

				filled += read;
			}
		}
		catch (InvalidDataException)
		{
			// What does not unpack is no profile: the picture is whole without it.
			return [];
		}

		return IccProfile.IsRgb(profile.AsSpan(0, filled)) ? profile.AsSpan(0, filled).ToArray() : [];
	}

	/// <summary>
	/// Packs the pixels the way an IDAT chunk of an RGBA8 picture holds them: scanlines in a zlib stream,
	/// filtered if that makes the stream shorter.
	/// </summary>
	public static byte[] Compress(RasterImage image)
	{
		// A filter is chosen for a row by the row alone, and that suits a photograph. The rows of a
		// screenshot repeat one another, and those often pack better left as they are.
		var filtered = Pack(image, isFiltered: true, int.MaxValue);
		var plain = Pack(image, isFiltered: false, filtered.Length);

		return plain is null || filtered.Length < plain.Length ? filtered : plain;
	}

	// Packs the scanlines each filtered the way that leaves it the smallest values, or all of them as they are.
	private static byte[] Pack(RasterImage image, bool isFiltered, int maximumLength)
	{
		var rowBytes = image.Width * _pixel;

		// The row above the first one counts as zeros.
		var zeros = new byte[rowBytes];
		var candidates = new byte[5][];

		for (var filter = 1; isFiltered && filter < candidates.Length; filter++)
			candidates[filter] = new byte[rowBytes];

		var block = new byte[Math.Max(rowBytes + 1, (int)Math.Min(_block, (long)image.Height * (rowBytes + 1)))];
		var filled = 0;

		using var compressed = new MemoryStream();
		using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
		{
			for (var y = 0; y < image.Height; y++)
			{
				ReadOnlySpan<byte> row = image.Pixels.AsSpan(y * rowBytes, rowBytes);
				var filter = isFiltered ? Filter(row, y == 0 ? zeros : image.Pixels.AsSpan((y - 1) * rowBytes, rowBytes), candidates) : 0;

				if (filled + rowBytes + 1 > block.Length)
				{
					zlib.Write(block, 0, filled);
					if (compressed.Length > maximumLength)
						return null;
					filled = 0;
				}

				block[filled++] = (byte)filter;
				(filter == 0 ? row : candidates[filter]).CopyTo(block.AsSpan(filled));
				filled += rowBytes;
			}

			zlib.Write(block, 0, filled);
		}

		return compressed.ToArray();
	}

	// Writes a row as each of the four filters leaves it - every byte less what the filter predicts for
	// it from the byte a pixel to the left, the byte above and the byte above that one - and returns the
	// filter whose output is smallest when its bytes are read as signed: the heuristic of libpng.
	// Of equals the first wins, 0 being the row as it is.
	private static int Filter(ReadOnlySpan<byte> row, ReadOnlySpan<byte> above, byte[][] candidates)
	{
		var sub = candidates[1];
		var up = candidates[2];
		var average = candidates[3];
		var paeth = candidates[4];

		long none = 0, subWeight = 0, upWeight = 0, averageWeight = 0, paethWeight = 0;

		for (var i = 0; i < row.Length; i++)
		{
			if (i == _pixel)
			{
				i = PngRowFilter.SumVectors(row, above, candidates, i, ref none, ref subWeight, ref upWeight, ref averageWeight, ref paethWeight);
				if (i == row.Length) break;
			}
			int value = row[i];
			int top = above[i];
			var left = 0;
			var corner = 0;

			if (i >= _pixel)
			{
				left = row[i - _pixel];
				corner = above[i - _pixel];
			}

			none += Magnitude(value);
			subWeight += Magnitude(sub[i] = unchecked((byte)(value - left)));
			upWeight += Magnitude(up[i] = unchecked((byte)(value - top)));
			averageWeight += Magnitude(average[i] = unchecked((byte)(value - (left + top) / 2)));
			paethWeight += Magnitude(paeth[i] = unchecked((byte)(value - Paeth(left, top, corner))));
		}

		var best = 0;
		var least = none;

		if (subWeight < least)
			(best, least) = (1, subWeight);

		if (upWeight < least)
			(best, least) = (2, upWeight);

		if (averageWeight < least)
			(best, least) = (3, averageWeight);

		return paethWeight < least ? 4 : best;
	}

	private static int Magnitude(int value)
		=> value < 128 ? value : 256 - value;

	private static void DecodePass(RasterImage image, ImageDownscaler downscaler, ImageAreaAccumulator accumulator, Stream raw,
		int width, int height, int px, int py, int dx, int dy,
		int channels, int depth, int color, byte[] palette, byte[] transparency)
	{
		var rowLen = checked((int)(((long)width * channels * depth + 7) / 8));
		var stride = Math.Max(1, (channels * depth + 7) / 8);
		var previous = new byte[rowLen];
		var row = new byte[rowLen];
		var rowPixels = downscaler is null && accumulator is null ? null : new byte[width * 4];
		var pixels = rowPixels ?? image.Pixels;

		for (var y = 0; y < height; y++)
		{
			var filter = raw.ReadByte();
			if (filter is < 0 or > 4)
				throw new InvalidDataException("Unsupported or missing PNG scanline filter.");
			for (var filled = 0; filled < row.Length;)
			{
				var read = raw.Read(row, filled, row.Length - filled);
				if (read == 0)
					throw new InvalidDataException("PNG scanlines have an invalid length.");
				filled += read;
			}

			if (filter != 0)
				for (var i = 0; i < rowLen; i++)
				{
					var a = i >= stride ? row[i - stride] : 0;
					var b = previous[i];
					var c = i >= stride ? previous[i - stride] : 0;
					var prediction = filter switch
					{
						0 => 0,
						1 => a,
						2 => b,
						3 => (a + b) / 2,
						4 => Paeth(a, b, c),
						_ => throw new InvalidDataException("Unsupported PNG scanline filter."),
					};
					row[i] = unchecked((byte)(row[i] + prediction));
				}

			if (color == 6 && depth == 8 && dx == 1)
				row.CopyTo(pixels, rowPixels is null ? ((py + y * dy) * image.Width + px) * 4 : 0);
			else
				for (var x = 0; x < width; x++)
				{
					var dest = rowPixels is null ? ((py + y * dy) * image.Width + px + x * dx) * 4 : x * 4;
					var sample = x * channels;
					switch (color)
					{
						case 0:
							var grayRaw = Sample(row, sample, depth);
							var gray = Normalize(grayRaw, depth);
							var ga = transparency.Length == 2 &&
								grayRaw == BinaryPrimitives.ReadUInt16BigEndian(transparency) ? (byte)0 : (byte)255;
							Assign(pixels, dest, gray, gray, gray, ga);
							break;

						case 2:
							var rr = Sample(row, sample, depth);
							var gg = Sample(row, sample + 1, depth);
							var bb = Sample(row, sample + 2, depth);
							var ra = transparency.Length == 6 &&
								rr == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0, 2)) &&
								gg == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2, 2)) &&
								bb == BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4, 2)) ? (byte)0 : (byte)255;
							Assign(pixels, dest, Normalize(rr, depth), Normalize(gg, depth), Normalize(bb, depth), ra);
							break;

						case 3:
							var index = Sample(row, x, depth);
							if (index >= palette.Length / 3)
								throw new InvalidDataException("PNG palette index outside palette.");
							Assign(pixels, dest, palette[index * 3], palette[index * 3 + 1],
								palette[index * 3 + 2], index < transparency.Length ? transparency[index] : (byte)255);
							break;

						case 4:
							var l = Normalize(Sample(row, sample, depth), depth);
							Assign(pixels, dest, l, l, l, Normalize(Sample(row, sample + 1, depth), depth));
							break;

						case 6:
							Assign(pixels, dest,
								Normalize(Sample(row, sample, depth), depth),
								Normalize(Sample(row, sample + 1, depth), depth),
								Normalize(Sample(row, sample + 2, depth), depth),
								Normalize(Sample(row, sample + 3, depth), depth));
							break;
					}
				}

			downscaler?.AddRow(rowPixels, y > 0 && row.AsSpan().SequenceEqual(previous));
			accumulator?.AddRow(rowPixels, px, py + y * dy, dx);
			(row, previous) = (previous, row);
		}
	}

	private static void Assign(byte[] p, int offset, byte r, byte g, byte b, byte a)
	{
		p[offset] = r;
		p[offset + 1] = g;
		p[offset + 2] = b;
		p[offset + 3] = a;
	}

	private static int Sample(byte[] row, int index, int depth)
	{
		if (depth == 8) return row[index];
		if (depth == 16) return BinaryPrimitives.ReadUInt16BigEndian(row.AsSpan(index * 2, 2));

		var offset = index * depth;
		return row[offset / 8] >> (8 - depth - offset % 8) & ((1 << depth) - 1);
	}

	private static byte Normalize(int sample, int depth)
		=> depth switch
		{
			8 => (byte)sample,
			16 => (byte)(sample >> 8),
			_ => (byte)(sample * 255 / ((1 << depth) - 1)),
		};

	private static bool ValidFormat(int depth, int color) => color switch
	{
		0 => depth is 1 or 2 or 4 or 8 or 16,
		2 => depth is 8 or 16,
		3 => depth is 1 or 2 or 4 or 8,
		4 => depth is 8 or 16,
		6 => depth is 8 or 16,
		_ => false,
	};

	private static int PassSize(int total, int start, int step)
		=> total <= start ? 0 : (total - start + step - 1) / step;

	private static int Paeth(int a, int b, int c)
	{
		var p = a + b - c;
		var da = Math.Abs(p - a);
		var db = Math.Abs(p - b);
		var dc = Math.Abs(p - c);
		return da <= db && da <= dc ? a : db <= dc ? b : c;
	}

	public static void WriteChunk(Stream output, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> content)
	{
		Span<byte> length = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(length, content.Length);
		output.Write(length);
		output.Write(tag);
		output.Write(content);
		BinaryPrimitives.WriteUInt32BigEndian(length, ~Crc(Crc(0xFFFFFFFF, tag), content));
		output.Write(length);
	}

	/// <summary>
	/// The checksum of a chunk: of its tag and its content together.
	/// </summary>
	public static uint Crc(ReadOnlySpan<byte> content)
		=> ~Crc(0xFFFFFFFF, content);

	private static uint Crc(uint crc, ReadOnlySpan<byte> content)
	{
		ref uint table = ref MemoryMarshal.GetArrayDataReference(_crcTable);
		while (content.Length >= 8)
		{
			var first = BinaryPrimitives.ReadUInt32LittleEndian(content) ^ crc;
			var second = BinaryPrimitives.ReadUInt32LittleEndian(content[4..]);
			crc = Unsafe.Add(ref table, 1792 + (byte)first) ^ Unsafe.Add(ref table, 1536 + (byte)(first >> 8))
				^ Unsafe.Add(ref table, 1280 + (byte)(first >> 16)) ^ Unsafe.Add(ref table, 1024 + (byte)(first >> 24))
				^ Unsafe.Add(ref table, 768 + (byte)second) ^ Unsafe.Add(ref table, 512 + (byte)(second >> 8))
				^ Unsafe.Add(ref table, 256 + (byte)(second >> 16)) ^ Unsafe.Add(ref table, (byte)(second >> 24));
			content = content[8..];
		}
		foreach (var b in content)
			crc = Unsafe.Add(ref table, (byte)(crc ^ b)) ^ (crc >> 8);
		return crc;
	}

	private static uint[] CreateCrcTable()
	{
		var table = new uint[8 * 256];
		for (uint i = 0; i < 256; i++)
		{
			var c = i;
			for (var j = 0; j < 8; j++)
				c = (c & 1) == 1 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
			table[i] = c;
		}
		for (var i = 0; i < 256; i++)
		{
			var c = table[i];
			for (var slice = 1; slice < 8; slice++)
			{
				c = table[(byte)c] ^ (c >> 8);
				table[slice * 256 + i] = c;
			}
		}
		return table;
	}
}
