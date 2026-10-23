namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;
using System.IO.Compression;

using Ecng.Drawing;
using Ecng.IO;

using StbImageSharp;

/// <summary>
/// Builds and reads pictures for the Drawing tests without going through the codecs under test.
/// </summary>
internal static class ImageTestHelper
{
	private static readonly byte[] _pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

	/// <summary>
	/// Unpacks a Base64 string holding zlib-compressed bytes.
	/// </summary>
	public static byte[] Inflate(string base64)
	{
		using var input = new MemoryStream(Convert.FromBase64String(base64));
		using var zlib = new ZLibStream(input, CompressionMode.Decompress);
		using var output = new MemoryStream();
		zlib.CopyTo(output);
		return output.ToArray();
	}

	/// <summary>
	/// Unpacks a zlib stream.
	/// </summary>
	public static byte[] Inflate(byte[] compressed)
	{
		using var input = new MemoryStream(compressed);
		using var zlib = new ZLibStream(input, CompressionMode.Decompress);
		using var output = new MemoryStream();
		zlib.CopyTo(output);
		return output.ToArray();
	}

	/// <summary>
	/// Fills an RGBA8 buffer pixel by pixel.
	/// </summary>
	public static byte[] Rgba(int width, int height, Func<int, int, (byte r, byte g, byte b, byte a)> pixel)
	{
		var result = new byte[width * height * 4];

		for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var (r, g, b, a) = pixel(x, y);
				var at = (y * width + x) * 4;
				result[at] = r;
				result[at + 1] = g;
				result[at + 2] = b;
				result[at + 3] = a;
			}

		return result;
	}

	/// <summary>
	/// Writes an RGBA8 PNG with unfiltered scanlines.
	/// </summary>
	public static byte[] CreatePng(int width, int height, byte[] rgba)
	{
		rgba.Length.AssertEqual(width * height * 4);

		var raw = new byte[height * (1 + width * 4)];

		for (var y = 0; y < height; y++)
			Array.Copy(rgba, y * width * 4, raw, y * (1 + width * 4) + 1, width * 4);

		return WriteChunks(
		[
			("IHDR", Header(width, height, 8, 6)),
			("IDAT", Deflate(raw)),
			("IEND", []),
		]);
	}

	/// <summary>
	/// Builds the 13 bytes of an IHDR chunk.
	/// </summary>
	public static byte[] Header(int width, int height, byte bitDepth, byte colorType)
	{
		var header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
		header[8] = bitDepth;
		header[9] = colorType;
		return header;
	}

	/// <summary>
	/// Compresses bytes into a zlib stream.
	/// </summary>
	public static byte[] Deflate(byte[] data)
	{
		using var output = new MemoryStream();

		using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
			zlib.Write(data);

		return output.ToArray();
	}

	/// <summary>
	/// Returns the predictor of PNG filter type 4: whichever of the left, upper and upper-left bytes is closest to left + up - upper-left.
	/// </summary>
	public static int Paeth(int left, int up, int upLeft)
	{
		var p = left + up - upLeft;
		var pa = Math.Abs(p - left);
		var pb = Math.Abs(p - up);
		var pc = Math.Abs(p - upLeft);
		return pa <= pb && pa <= pc ? left : pb <= pc ? up : upLeft;
	}

	/// <summary>
	/// Splits a PNG file into its chunks.
	/// </summary>
	public static (string tag, byte[] data)[] ReadChunks(byte[] png)
	{
		png.AsSpan(0, 8).SequenceEqual(_pngSignature).AssertTrue("PNG signature");

		var chunks = new List<(string tag, byte[] data)>();

		for (var at = 8; at < png.Length;)
		{
			var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at));
			(length >= 0 && length <= png.Length - at - 12).AssertTrue($"PNG chunk length at {at}");

			var crc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8 + length));
			crc.AssertEqual(Crc(png.AsSpan(at + 4, length + 4)), $"PNG chunk CRC at {at}");

			chunks.Add((Encoding.ASCII.GetString(png, at + 4, 4), png.AsSpan(at + 8, length).ToArray()));
			at += length + 12;
		}

		return [.. chunks];
	}

	/// <summary>
	/// Assembles a PNG file from chunks, computing their checksums.
	/// </summary>
	public static byte[] WriteChunks(IEnumerable<(string tag, byte[] data)> chunks)
	{
		using var output = new MemoryStream();
		output.Write(_pngSignature);

		foreach (var (tag, data) in chunks)
			WriteChunk(output, tag, data);

		return output.ToArray();
	}

	/// <summary>
	/// Appends one chunk to a PNG file that is being written, computing its checksum.
	/// </summary>
	public static void WriteChunk(Stream output, string tag, byte[] data)
	{
		var number = new byte[4];
		BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
		output.Write(number);

		var body = new byte[4 + data.Length];
		Encoding.ASCII.GetBytes(tag).CopyTo(body, 0);
		data.CopyTo(body, 4);
		output.Write(body);

		BinaryPrimitives.WriteUInt32BigEndian(number, Crc(body));
		output.Write(number);
	}

	/// <summary>
	/// Decodes an RGBA8 PNG - the only kind the library writes - whatever filters its scanlines use.
	/// For an animated PNG this is the default image. Also checks that the library reads
	/// the same size off the file as its header holds.
	/// </summary>
	public static (int width, int height, byte[] rgba) ReadPng(byte[] png)
	{
		var (width, height, raw) = ReadScanlines(png);
		raw.Length.AssertEqual(height * (1 + width * 4), "PNG scanline data length");
		var image = ImageResult.FromMemory(png, ColorComponents.RedGreenBlueAlpha);
		image.Width.AssertEqual(width, "independent PNG width");
		image.Height.AssertEqual(height, "independent PNG height");
		return (width, height, image.Data);
	}

	/// <summary>
	/// Returns the filter type of every scanline of an RGBA8 PNG.
	/// </summary>
	public static byte[] ReadFilters(byte[] png)
	{
		var (width, height, raw) = ReadScanlines(png);
		var rowLength = 1 + width * 4;

		raw.Length.AssertEqual(height * rowLength, "PNG scanline data length");

		var filters = new byte[height];

		for (var y = 0; y < height; y++)
			filters[y] = raw[y * rowLength];

		return filters;
	}

	/// <summary>
	/// Decodes an RGBA8 PNG and checks its size.
	/// </summary>
	public static byte[] ReadPng(byte[] png, int width, int height)
	{
		var (w, h, rgba) = ReadPng(png);
		w.AssertEqual(width, "PNG width");
		h.AssertEqual(height, "PNG height");
		return rgba;
	}

	/// <summary>
	/// Reads the animation of an RGBA8 APNG: every frame as its own rectangle, not composited.
	/// </summary>
	public static ApngFile ReadApng(byte[] png)
	{
		var chunks = ReadChunks(png);
		var header = chunks[0].data;
		header[8].AssertEqual((byte)8, "APNG bit depth");
		header[9].AssertEqual((byte)6, "APNG color type");

		var control = chunks.Single(c => c.tag == "acTL").data;

		var file = new ApngFile
		{
			Width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0)),
			Height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4)),
			FrameCount = (int)BinaryPrimitives.ReadUInt32BigEndian(control.AsSpan(0)),
			Plays = (int)BinaryPrimitives.ReadUInt32BigEndian(control.AsSpan(4)),
		};

		var sequence = 0u;
		ApngFile.Frame frame = null;
		var data = new List<byte>();

		void Finish()
		{
			if (frame is null)
				return;

			var png = WriteChunks(
			[
				("IHDR", Header(frame.Width, frame.Height, 8, 6)),
				("IDAT", [.. data]),
				("IEND", []),
			]);
			frame.Rgba = ReadPng(png, frame.Width, frame.Height);
			file.Frames.Add(frame);
			data.Clear();
		}

		foreach (var (tag, content) in chunks)
		{
			switch (tag)
			{
				case "fcTL":
					Finish();
					content.Length.AssertEqual(26);
					BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(0)).AssertEqual(sequence++, "fcTL sequence number");

					frame = new()
					{
						Width = (int)BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(4)),
						Height = (int)BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(8)),
						Left = (int)BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(12)),
						Top = (int)BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(16)),
						DelayNumerator = BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(20)),
						DelayDenominator = BinaryPrimitives.ReadUInt16BigEndian(content.AsSpan(22)),
						Disposal = content[24],
						Blend = content[25],
					};
					break;

				case "IDAT" when frame is not null:
					data.AddRange(content);
					break;

				case "fdAT":
					BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(0)).AssertEqual(sequence++, "fdAT sequence number");
					data.AddRange(content.AsSpan(4).ToArray());
					break;
			}
		}

		Finish();
		file.Frames.Count.AssertEqual(file.FrameCount, "APNG frame count");
		return file;
	}

	/// <summary>
	/// Builds the 26 bytes of an fcTL chunk.
	/// </summary>
	public static byte[] FrameControl(uint sequence, int width, int height, int left, int top, ushort delayNumerator, ushort delayDenominator, byte disposal, byte blend)
	{
		var control = new byte[26];
		BinaryPrimitives.WriteUInt32BigEndian(control.AsSpan(0), sequence);
		BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(4), width);
		BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(8), height);
		BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(12), left);
		BinaryPrimitives.WriteInt32BigEndian(control.AsSpan(16), top);
		BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(20), delayNumerator);
		BinaryPrimitives.WriteUInt16BigEndian(control.AsSpan(22), delayDenominator);
		control[24] = disposal;
		control[25] = blend;
		return control;
	}

	/// <summary>
	/// Compresses the RGBA8 pixels of one frame the way IDAT and fdAT carry them.
	/// </summary>
	public static byte[] FrameData(int width, int height, byte[] rgba)
	{
		var raw = new byte[height * (1 + width * 4)];

		for (var y = 0; y < height; y++)
			Array.Copy(rgba, y * width * 4, raw, y * (1 + width * 4) + 1, width * 4);

		return Deflate(raw);
	}

	/// <summary>
	/// Prefixes frame data with its sequence number, making the content of an fdAT chunk.
	/// </summary>
	public static byte[] Sequenced(uint sequence, byte[] data)
	{
		var result = new byte[4 + data.Length];
		BinaryPrimitives.WriteUInt32BigEndian(result, sequence);
		data.CopyTo(result, 4);
		return result;
	}

	/// <summary>
	/// Compares two pixel buffers channel by channel. A failure names the first channel that is off,
	/// how many are, and by how much at most.
	/// </summary>
	public static void AssertPixels(byte[] expected, byte[] actual, int width, int channels = 4, int tolerance = 0)
	{
		actual.Length.AssertEqual(expected.Length, "pixel buffer length");

		var first = -1;
		var count = 0;
		var largest = 0;

		for (var i = 0; i < expected.Length; i++)
		{
			var difference = Math.Abs(expected[i] - actual[i]);

			if (difference <= tolerance)
				continue;

			if (first < 0)
				first = i;

			count++;
			largest = Math.Max(largest, difference);
		}

		if (first < 0)
			return;

		var pixel = first / channels;
		false.AssertTrue($"Pixel ({pixel % width}, {pixel / width}), channel {first % channels}: expected {expected[first]}, actual {actual[first]}, tolerance {tolerance}. " +
			$"{count} of {expected.Length} channels are off, by {largest} at most.");
	}

	/// <summary>
	/// Returns the pixels of an opaque black picture.
	/// </summary>
	public static byte[] BlackRgba(int width, int height)
		=> Rgba(width, height, (_, _) => (0, 0, 0, 255));

	/// <summary>
	/// Lays white over opaque pixels wherever a box covers one of the four points a pixel is sampled at:
	/// a quarter of the opacity for every point covered.
	/// </summary>
	public static byte[] WhiteBoxes(byte[] background, int width, byte opacity, params (double left, double top, double right, double bottom)[] boxes)
		=> Rgba(width, background.Length / 4 / width, (x, y) =>
		{
			var (r, g, b, a) = Pixel(background, width, x, y);
			a.AssertEqual((byte)255, $"the background at ({x}, {y}) is not opaque");

			var hits = 0;

			for (var sample = 0; sample < 4; sample++)
			{
				var xx = x + (sample % 2 + 0.5) / 2;
				var yy = y + (sample / 2 + 0.5) / 2;

				if (boxes.Any(box => xx >= box.left && xx < box.right && yy >= box.top && yy < box.bottom))
					hits++;
			}

			var alpha = opacity * hits / 4;

			byte Over(byte color)
				=> (byte)Math.Round((255.0 * alpha + color * (255.0 - alpha)) / 255);

			return (Over(r), Over(g), Over(b), 255);
		});

	/// <summary>
	/// Returns one pixel of an RGBA8 buffer.
	/// </summary>
	public static (byte r, byte g, byte b, byte a) Pixel(byte[] rgba, int width, int x, int y)
	{
		var at = (y * width + x) * 4;
		return (rgba[at], rgba[at + 1], rgba[at + 2], rgba[at + 3]);
	}

	/// <summary>
	/// Expands gray samples to RGBA8.
	/// </summary>
	public static byte[] GrayToRgba(byte[] gray)
	{
		var result = new byte[gray.Length * 4];

		for (var i = 0; i < gray.Length; i++)
		{
			result[i * 4] = result[i * 4 + 1] = result[i * 4 + 2] = gray[i];
			result[i * 4 + 3] = 255;
		}

		return result;
	}

	/// <summary>
	/// Expands RGB samples to RGBA8.
	/// </summary>
	public static byte[] RgbToRgba(byte[] rgb)
	{
		var result = new byte[rgb.Length / 3 * 4];

		for (var i = 0; i < rgb.Length / 3; i++)
		{
			result[i * 4] = rgb[i * 3];
			result[i * 4 + 1] = rgb[i * 3 + 1];
			result[i * 4 + 2] = rgb[i * 3 + 2];
			result[i * 4 + 3] = 255;
		}

		return result;
	}

	/// <summary>
	/// Inserts a segment right after the SOI marker of a JPEG.
	/// </summary>
	public static byte[] InsertJpegSegment(byte[] jpeg, byte marker, byte[] payload)
	{
		var segment = new byte[4 + payload.Length];
		segment[0] = 0xFF;
		segment[1] = marker;
		BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(payload.Length + 2));
		payload.CopyTo(segment, 4);
		return [.. jpeg[..2], .. segment, .. jpeg[2..]];
	}

	/// <summary>
	/// Removes every segment with the given marker that precedes the first scan of a JPEG.
	/// </summary>
	public static byte[] RemoveJpegSegments(byte[] jpeg, byte marker)
	{
		var result = new List<byte>(jpeg.Length) { jpeg[0], jpeg[1] };

		for (var at = 2; at < jpeg.Length;)
		{
			jpeg[at].AssertEqual((byte)0xFF, $"JPEG marker at {at}");

			if (jpeg[at + 1] == 0xDA)
			{
				result.AddRange(jpeg[at..]);
				break;
			}

			var length = 2 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(at + 2));

			if (jpeg[at + 1] != marker)
				result.AddRange(jpeg.AsSpan(at, length).ToArray());

			at += length;
		}

		return [.. result];
	}

	/// <summary>
	/// Returns the payloads of the segments with the given marker that precede the first scan of a JPEG,
	/// in the order the file has them.
	/// </summary>
	public static List<byte[]> ReadJpegSegments(byte[] jpeg, byte marker)
	{
		var segments = new List<byte[]>();

		for (var at = 2; at < jpeg.Length;)
		{
			jpeg[at].AssertEqual((byte)0xFF, $"JPEG marker at {at}");

			if (jpeg[at + 1] == 0xDA)
				break;

			var length = BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(at + 2));

			if (jpeg[at + 1] == marker)
				segments.Add(jpeg.AsSpan(at + 4, length - 2).ToArray());

			at += 2 + length;
		}

		return segments;
	}

	/// <summary>
	/// Returns the offset of the first frame header (SOF0 or SOF2) payload of a JPEG.
	/// </summary>
	public static int FindJpegFrame(byte[] jpeg)
	{
		for (var at = 2; at < jpeg.Length;)
		{
			jpeg[at].AssertEqual((byte)0xFF, $"JPEG marker at {at}");

			if (jpeg[at + 1] is 0xC0 or 0xC2)
				return at + 4;

			at += 2 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(at + 2));
		}

		throw new InvalidDataException("The JPEG has no frame header.");
	}

	/// <summary>
	/// Splits a WebP file into its chunks, checking the RIFF header and that a chunk of an odd length is
	/// followed by a zero byte of padding.
	/// </summary>
	public static (string tag, byte[] data)[] ReadWebpChunks(byte[] webp)
	{
		(webp.Length >= 12 && webp.AsSpan(0, 4).SequenceEqual("RIFF"u8) && webp.AsSpan(8, 4).SequenceEqual("WEBP"u8)).AssertTrue("WebP signature");
		BinaryPrimitives.ReadUInt32LittleEndian(webp.AsSpan(4)).AssertEqual((uint)webp.Length - 8, "RIFF size");

		var chunks = new List<(string tag, byte[] data)>();

		for (var at = 12; at < webp.Length;)
		{
			(webp.Length - at >= 8).AssertTrue($"WebP chunk header at {at}");

			var length = BinaryPrimitives.ReadUInt32LittleEndian(webp.AsSpan(at + 4));
			(length <= webp.Length - at - 8).AssertTrue($"WebP chunk length at {at}");

			chunks.Add((Encoding.ASCII.GetString(webp, at, 4), webp.AsSpan(at + 8, (int)length).ToArray()));
			at += 8 + (int)length;

			if (length % 2 == 1)
			{
				(at < webp.Length && webp[at] == 0).AssertTrue($"WebP chunk padding at {at}");
				at++;
			}
		}

		return [.. chunks];
	}

	/// <summary>
	/// Assembles a WebP file from chunks, padding a chunk of an odd length.
	/// </summary>
	public static byte[] WriteWebpChunks(IEnumerable<(string tag, byte[] data)> chunks)
	{
		using var output = new MemoryStream();
		output.Write("RIFF\0\0\0\0WEBP"u8);

		var number = new byte[4];

		foreach (var (tag, data) in chunks)
		{
			output.Write(Encoding.ASCII.GetBytes(tag));
			BinaryPrimitives.WriteInt32LittleEndian(number, data.Length);
			output.Write(number);
			output.Write(data);

			if (data.Length % 2 == 1)
				output.WriteByte(0);
		}

		var webp = output.ToArray();
		BinaryPrimitives.WriteInt32LittleEndian(webp.AsSpan(4), webp.Length - 8);
		return webp;
	}

	/// <summary>
	/// Builds an APP1 payload holding an Exif block with nothing but the orientation tag.
	/// </summary>
	public static byte[] ExifOrientation(ushort orientation, bool bigEndian = true)
	{
		var exif = new byte[6 + 8 + 2 + 12 + 4];
		"Exif"u8.CopyTo(exif);

		var tiff = exif.AsSpan(6);

		if (bigEndian)
		{
			"MM"u8.CopyTo(tiff);
			BinaryPrimitives.WriteUInt16BigEndian(tiff[2..], 42);
			BinaryPrimitives.WriteUInt32BigEndian(tiff[4..], 8);
			BinaryPrimitives.WriteUInt16BigEndian(tiff[8..], 1);
			BinaryPrimitives.WriteUInt16BigEndian(tiff[10..], 0x0112);
			BinaryPrimitives.WriteUInt16BigEndian(tiff[12..], 3);
			BinaryPrimitives.WriteUInt32BigEndian(tiff[14..], 1);
			BinaryPrimitives.WriteUInt16BigEndian(tiff[18..], orientation);
		}
		else
		{
			"II"u8.CopyTo(tiff);
			BinaryPrimitives.WriteUInt16LittleEndian(tiff[2..], 42);
			BinaryPrimitives.WriteUInt32LittleEndian(tiff[4..], 8);
			BinaryPrimitives.WriteUInt16LittleEndian(tiff[8..], 1);
			BinaryPrimitives.WriteUInt16LittleEndian(tiff[10..], 0x0112);
			BinaryPrimitives.WriteUInt16LittleEndian(tiff[12..], 3);
			BinaryPrimitives.WriteUInt32LittleEndian(tiff[14..], 1);
			BinaryPrimitives.WriteUInt16LittleEndian(tiff[18..], orientation);
		}

		return exif;
	}

	/// <summary>
	/// Returns the path of a file in a new directory that is removed when the test run ends. The file itself is not created.
	/// </summary>
	public static string TempPath(string name)
		=> Path.Combine(LocalFileSystem.Instance.GetTempPath(), name);

	/// <summary>
	/// Writes bytes into a file of a new directory that is removed when the test run ends.
	/// </summary>
	public static string TempFile(string name, byte[] content)
	{
		var path = TempPath(name);
		File.WriteAllBytes(path, content);
		return path;
	}

	/// <summary>
	/// Writes files into a new directory that is removed when the test run ends.
	/// </summary>
	/// <returns>The path of the directory.</returns>
	public static string TempDirectory(params (string name, byte[] content)[] files)
	{
		var directory = LocalFileSystem.Instance.GetTempPath();

		foreach (var (name, content) in files)
			File.WriteAllBytes(Path.Combine(directory, name), content);

		return directory;
	}

	// Checks the header of an RGBA8 PNG, also against the size the library reads off the file,
	// and unpacks its scanlines, filter bytes included.
	private static (int width, int height, byte[] raw) ReadScanlines(byte[] png)
	{
		var chunks = ReadChunks(png);
		chunks[0].tag.AssertEqual("IHDR");

		var header = chunks[0].data;
		var width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0));
		var height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4));
		header[8].AssertEqual((byte)8, "PNG bit depth");
		header[9].AssertEqual((byte)6, "PNG color type");
		header[12].AssertEqual((byte)0, "PNG interlace method");

		var size = png.GetPngSize();
		size.Width.AssertEqual(width, "PNG width as the library reads it");
		size.Height.AssertEqual(height, "PNG height as the library reads it");

		return (width, height, Inflate(chunks.Where(c => c.tag == "IDAT").SelectMany(c => c.data).ToArray()));
	}

	private static uint Crc(ReadOnlySpan<byte> data)
	{
		var crc = 0xFFFFFFFFu;

		foreach (var value in data)
		{
			crc ^= value;

			for (var bit = 0; bit < 8; bit++)
				crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
		}

		return ~crc;
	}
}
