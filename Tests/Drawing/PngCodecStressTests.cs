namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using StbImageSharp;

using static ImageTestHelper;

/// <summary>
/// Independent byte-level PNG encoder generates adversarial and legal fixtures.
/// Production decoder is never used to create expected pixels.
/// </summary>
/// <remarks>
/// A fixture that is RGBA8, as the PNG the library writes is, would read right off a conversion that
/// handed it back as it came. So the decoder is asked for the pixels itself, and the PNG the library
/// makes of the fixture has to hold the same ones: see <see cref="Decoded"/>.
/// </remarks>
[TestClass]
public class PngCodecStressTests : BaseTestClass
{
	private static readonly (int x, int y, int dx, int dy)[] _adam7 =
	[
		(0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4),
		(0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2)
	];

	[TestMethod]
	public void Png_AllFiveFilterTypes_RestoreEveryRgbaChannelExactly()
	{
		var expected = Pattern(19, 13);
		for (var filter = 0; filter <= 4; filter++)
		{
			var png = Pack(19, 13, expected, filter, interlace: false);
			AssertPixels(expected, Decoded(png, 19, 13), 19);
		}
	}

	[TestMethod]
	public void Png_Adam7InterlacedOddSizes_EqualExpectedAtEveryPixel()
	{
		foreach (var (w, h) in new[] { (1, 1), (2, 3), (5, 4), (13, 11), (17, 15) })
		{
			var pixels = Pattern(w, h);
			var png = Pack(w, h, pixels, 4, interlace: true);
			AssertPixels(pixels, Decoded(png, w, h), w);
		}
	}

	[TestMethod]
	public void Png_MultipleScanlineFiltersInSingleImage_EveryPixelMatches()
	{
		var source = Pattern(17, 19);
		var png = Pack(17, 19, source, -1, interlace: false);
		AssertPixels(source, Decoded(png, 17, 19), 17);
	}

	[TestMethod]
	public void Png_SmallRandomizedRgbaPictures_LosslessExactPixelRoundTrips()
	{
		var rand = new Random(87234);
		for (var iteration = 0; iteration < 160; iteration++)
		{
			var w = rand.Next(1, 48);
			var h = rand.Next(1, 45);
			var pixels = new byte[w * h * 4];
			rand.NextBytes(pixels);
			var source = Pack(w, h, pixels, iteration % 5, interlace: iteration % 4 == 0);
			var copy = (byte[])source.Clone();
			AssertPixels(pixels, Decoded(source, w, h), w);
			source.SequenceEqual(copy).AssertTrue();
		}
	}

	[TestMethod]
	public void Png_TransparencyAndPaletteFourBitAllIndicesAreExact()
	{
		// Indexed 4-bit 4x2 with four RGBA palette entries including 0/128/255 alpha.
		byte[] indices = [0, 1, 2, 3, 3, 2, 1, 0];
		byte[] palette = [250, 0, 0, 0, 250, 0, 0, 0, 250, 220, 120, 40];
		byte[] alpha = [0, 127, 255, 220];
		byte[] raw = [0, 0x01, 0x23, 0, 0x32, 0x10];
		var png = RawPng(4, 2, 4, 3, 0, raw, palette, alpha);
		var expected = new byte[4 * 2 * 4];
		for (var i = 0; i < indices.Length; i++)
		{
			var idx = indices[i];
			Array.Copy(palette, idx * 3, expected, i * 4, 3);
			expected[i * 4 + 3] = alpha[idx];
		}
		AssertPixels(expected, Decoded(png, 4, 2), 4);
	}

	[TestMethod]
	public void Png_Rgba16Bit_ConvertsEveryChannelToHighOrderByte()
	{
		byte[] samples = [12, 34, 56, 78, 100, 120, 140, 160, 201, 217, 233, 249, 77, 33, 22, 11];
		var raw = new byte[2 * (1 + 2 * 4 * 2)];
		for (var i = 0; i < 16; i++)
		{
			var at = 1 + (i / 8) * 17 + (i % 8) * 2;
			raw[at] = samples[i];
			raw[at + 1] = (byte)(i * 11);
		}
		var png = RawPng(2, 2, 16, 6, 0, raw, [], []);
		// Four pixels, each RGBA channel retained via high byte.
		AssertPixels(samples, Decoded(png, 2, 2), 2);
	}

	[TestMethod]
	public void Png_DamagedCrcAndTruncatedPayload_MustFailClosed()
	{
		var bytes = Pack(15, 11, Pattern(15, 11), 0, false);
		for (var i = 0; i < Math.Min(bytes.Length - 4, 150); i++)
		{
			var copy = (byte[])bytes.Clone();
			copy[i] ^= 0x20;
			try
			{
				copy.ConvertToPng();
				Fail($"Modified PNG byte {i} was accepted despite CRC or structural error.");
			}
			catch (InvalidDataException) { }
			catch (NotSupportedException) { }
		}
	}

	// What the decoder makes of a picture; the PNG the library writes of it is read back and compared.
	private static byte[] Decoded(byte[] png, int width, int height)
	{
		var image = PngCodec.Decode(png);

		(image.Width, image.Height).AssertEqual((width, height), "the size of the decoded picture");
		var reference = ImageResult.FromMemory(png, ColorComponents.RedGreenBlueAlpha);
		(reference.Width, reference.Height).AssertEqual((width, height), "the size from the independent decoder");
		AssertPixels(reference.Data, image.Pixels, width);
		AssertPixels(image.Pixels, ReadPng(png.ConvertToPng(), width, height), width);

		return image.Pixels;
	}

	private static byte[] Pattern(int width, int height)
	{
		var data = new byte[width * height * 4];
		for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++)
			{
				var p = (y * width + x) * 4;
				data[p] = (byte)(x * 17 + y * 11);
				data[p + 1] = (byte)(x * 7 + y * 31);
				data[p + 2] = (byte)(x * 53 + y * 3);
				data[p + 3] = (byte)(x * 29 + y * 19);
			}
		return data;
	}

	private static byte[] Pack(int w, int h, byte[] rgba, int chosenFilter, bool interlace)
	{
		using var raw = new MemoryStream();
		if (!interlace)
			WritePass(raw, rgba, w, w, h, 0, 0, 1, 1, chosenFilter);
		else
			foreach (var (x, y, dx, dy) in _adam7)
			{
				var pw = w <= x ? 0 : (w - x + dx - 1) / dx;
				var ph = h <= y ? 0 : (h - y + dy - 1) / dy;
				if (pw > 0 && ph > 0)
					WritePass(raw, rgba, w, pw, ph, x, y, dx, dy, chosenFilter);
			}

		return RawPng(w, h, 8, 6, (byte)(interlace ? 1 : 0), raw.ToArray(), [], []);
	}

	private static void WritePass(Stream raw, byte[] rgba, int stride, int width, int height,
		int startX, int startY, int stepX, int stepY, int chosenFilter)
	{
		var prev = new byte[width * 4];
		var row = new byte[width * 4];
		for (var y = 0; y < height; y++)
		{
			for (var x = 0; x < width; x++)
				Array.Copy(rgba, ((startY + y * stepY) * stride + startX + x * stepX) * 4, row, x * 4, 4);

			var filter = chosenFilter == -1 ? y % 5 : chosenFilter;
			raw.WriteByte((byte)filter);
			for (var i = 0; i < row.Length; i++)
			{
				var a = i >= 4 ? row[i - 4] : 0;
				var b = prev[i];
				var c = i >= 4 ? prev[i - 4] : 0;
				var prediction = filter switch
				{
					0 => 0,
					1 => a,
					2 => b,
					3 => (a + b) / 2,
					4 => Paeth(a, b, c),
					_ => throw new InvalidDataException()
				};
				raw.WriteByte(unchecked((byte)(row[i] - prediction)));
			}
			(row, prev) = (prev, row);
		}
	}

	private static byte[] RawPng(int w, int h, byte depth, byte color, byte interlace,
		byte[] uncompressed, byte[] palette, byte[] transparency)
	{
		var header = Header(w, h, depth, color);
		header[12] = interlace;

		var chunks = new List<(string tag, byte[] data)> { ("IHDR", header) };

		if (palette.Length != 0)
			chunks.Add(("PLTE", palette));

		if (transparency.Length != 0)
			chunks.Add(("tRNS", transparency));

		chunks.Add(("IDAT", Deflate(uncompressed)));
		chunks.Add(("IEND", []));
		return WriteChunks(chunks);
	}

	[TestMethod]
	public void Png_AllLegalColorTypesAndBitDepths_AgreeWithIndependentPixelOracle()
	{
		// All 15 legal (color type, bit depth) pairs, each with odd-width rows.
		foreach (var (color, depth) in new (byte color, byte depth)[]
		{
			(0, 1), (0, 2), (0, 4), (0, 8), (0, 16),
			(2, 8), (2, 16),
			(3, 1), (3, 2), (3, 4), (3, 8),
			(4, 8), (4, 16),
			(6, 8), (6, 16)
		})
		{
			try
			{
				const int width = 5, height = 3;
				var channels = color switch { 2 => 3, 4 => 2, 6 => 4, _ => 1 };
				var maxSample = depth == 16 ? 65535 : (1 << depth) - 1;
				byte[] palette = [], transparency = [];
				if (color == 3)
				{
					var count = 1 << depth;
					palette = new byte[count * 3];
					for (var index = 0; index < count; index++)
					{
						palette[3 * index] = (byte)(17 * index);
						palette[3 * index + 1] = (byte)(255 - index);
						palette[3 * index + 2] = (byte)(index ^ 0x5a);
					}
					transparency = Enumerable.Range(0, Math.Min(count, 5)).Select(i => (byte)(i * 43)).ToArray();
				}

				var samples = new int[width * height * channels];
				for (var p = 0; p < width * height; p++)
					for (var c = 0; c < channels; c++)
						samples[p * channels + c] = color == 3
							? p % (1 << depth)
							: (p * 37001 + c * 13111 + 17773) & maxSample;

				using var raw = new MemoryStream();
				for (var y = 0; y < height; y++)
				{
					raw.WriteByte((byte)(y % 5));
					// Build a zero-filtered row, then encode the appropriate PNG
					// filter separately for each byte (bpp is not always 4).
					var row = PackSamples(samples.AsSpan(y * width * channels, width * channels), depth);
					var bpp = Math.Max(1, (channels * depth + 7) / 8);
					var previous = y == 0 ? new byte[row.Length] :
						PackSamples(samples.AsSpan((y - 1) * width * channels, width * channels), depth);
					for (var j = 0; j < row.Length; j++)
					{
						var left = j >= bpp ? row[j - bpp] : 0;
						var up = previous[j];
						var upperLeft = j >= bpp ? previous[j - bpp] : 0;
						var predictor = (y % 5) switch
						{
							0 => 0,
							1 => left,
							2 => up,
							3 => (left + up) / 2,
							4 => Paeth(left, up, upperLeft),
							_ => 0
						};
						raw.WriteByte(unchecked((byte)(row[j] - predictor)));
					}
				}

				var expected = new byte[width * height * 4];
				for (var p = 0; p < width * height; p++)
				{
					int Read(int c) => samples[p * channels + c];
					byte Normal(int v) => depth == 16 ? (byte)(v >> 8) : depth == 8 ? (byte)v :
						(byte)(v * 255 / maxSample);
					var off = p * 4;
					switch (color)
					{
						case 0:
							expected[off] = expected[off + 1] = expected[off + 2] = Normal(Read(0));
							expected[off + 3] = 255;
							break;
						case 2:
							for (var c = 0; c < 3; c++)
								expected[off + c] = Normal(Read(c));
							expected[off + 3] = 255;
							break;
						case 3:
							var idx = Read(0);
							Array.Copy(palette, idx * 3, expected, off, 3);
							expected[off + 3] = idx < transparency.Length ? transparency[idx] : (byte)255;
							break;
						case 4:
							expected[off] = expected[off + 1] = expected[off + 2] = Normal(Read(0));
							expected[off + 3] = Normal(Read(1));
							break;
						case 6:
							for (var c = 0; c < 4; c++)
								expected[off + c] = Normal(Read(c));
							break;
					}
				}

				var source = RawPng(width, height, depth, color, 0, raw.ToArray(), palette, transparency);
				AssertPixels(expected, Decoded(source, width, height), width);
			}
			catch (Exception ex)
			{
				Fail($"PNG color={color}, depth={depth}: {ex}");
			}
		}
	}

	[TestMethod]
	public void Png_GrayscaleAndRgbTransparencyKeys_PreserveTransparentPixels()
	{
		foreach (var depth in new byte[] { 1, 2, 4, 8, 16 })
		{
			var value = depth == 16 ? 0x3040 : depth == 8 ? 37 : 1;
			byte[] raw = [0, .. PackSamples([0, value, value, depth == 16 ? 65535 : (1 << depth) - 1], depth)];
			byte[] key = [(byte)(value >> 8), (byte)value];
			var pixels = Decoded(RawPng(4, 1, depth, 0, 0, raw, [], key), 4, 1);
			for (var i = 0; i < 4; i++)
				pixels[4 * i + 3].AssertEqual((byte)(i is 1 or 2 || (i == 3 && depth == 1) ? 0 : 255));
		}

		foreach (var depth in new byte[] { 8, 16 })
		{
			var values = depth == 8 ? new[] { 17, 91, 250 } : new[] { 0x1234, 0x5678, 0x9abc };
			byte[] raw = [0, .. PackSamples([
				0, 0, 0, values[0], values[1], values[2],
				values[0], values[1], values[2], values[0], values[1], values[0]], depth)];
			var key = new byte[6];
			for (var j = 0; j < 3; j++)
			{
				key[2 * j] = (byte)(values[j] >> 8);
				key[2 * j + 1] = (byte)values[j];
			}
			var pixels = Decoded(RawPng(4, 1, depth, 2, 0, raw, [], key), 4, 1);
			for (var i = 0; i < 4; i++)
				pixels[4 * i + 3].AssertEqual((byte)(i is 1 or 2 ? 0 : 255));
		}
	}

	[TestMethod]
	public void Png_UnknownCriticalChunkRejected_AncillaryChunkIgnored()
	{
		var source = Pack(2, 2, Pattern(2, 2), 0, false);
		byte[] WithChunk(string tag)
		{
			using var output = new MemoryStream();
			output.Write(source, 0, 33); // signature + IHDR
			WriteChunk(output, tag, [11, 22, 33]);
			output.Write(source, 33, source.Length - 33);
			return output.ToArray();
		}
		ThrowsExactly<NotSupportedException>(() => WithChunk("UNKN").ConvertToPng());
		AssertPixels(Pattern(2, 2), Decoded(WithChunk("tEXt"), 2, 2), 2);
	}

	[TestMethod]
	public void Png_TruncatedAndMalformedContents_FailWithoutRuntimeIndexErrors()
	{
		var source = Pack(13, 7, Pattern(13, 7), 4, false);
		for (var length = 8; length < source.Length; length += Math.Max(1, source.Length / 37))
		{
			var cut = source[..length];
			try
			{
				cut.ConvertToPng();
				Fail($"Truncated PNG accepted at {length} bytes.");
			}
			catch (InvalidDataException) { }
			catch (NotSupportedException) { }
		}

		ThrowsExactly<NotSupportedException>(() =>
			RawPng(1, 1, 4, 6, 0, [0, 0, 0, 0, 0], [], []).ConvertToPng());
		ThrowsExactly<NotSupportedException>(() =>
			RawPng(1, 1, 8, 6, 2, [0, 0, 0, 0, 0], [], []).ConvertToPng());
		ThrowsExactly<InvalidDataException>(() =>
			RawPng(2, 1, 8, 3, 0, [0, 1, 2], [255, 0, 0], []).ConvertToPng());
		ThrowsExactly<InvalidDataException>(() =>
			RawPng(1, 1, 8, 6, 0, [0, 0, 0, 0, 0, 0, 255], [], []).ConvertToPng());
	}

	[TestMethod]
	public void Png_IendMustBeLastChunk_NotSilentlyIgnoreAppendedData()
	{
		var source = Pack(2, 2, Pattern(2, 2), 0, false);
		ThrowsExactly<InvalidDataException>(() => source.Concat(new byte[] { 1, 2, 3, 4 }).ToArray().ConvertToPng());
	}

	private static byte[] PackSamples(ReadOnlySpan<int> samples, int depth)
	{
		var result = new byte[(samples.Length * depth + 7) / 8];
		for (var i = 0; i < samples.Length; i++)
		{
			if (depth == 16)
			{
				result[i * 2] = (byte)(samples[i] >> 8);
				result[i * 2 + 1] = (byte)samples[i];
			}
			else if (depth == 8)
			{
				result[i] = (byte)samples[i];
			}
			else
			{
				var bit = i * depth;
				result[bit / 8] |= (byte)(samples[i] << (8 - depth - bit % 8));
			}
		}
		return result;
	}

	[TestMethod]
	public void Png_IdatChunksSeparatedByAncillaryChunk_MustBeRejected()
	{
		// Valid PNG compressors may split one zlib stream into multiple IDAT
		// chunks, but the chunks must be contiguous (PNG chunk ordering rule).
		var original = Pack(7, 5, Pattern(7, 5), 0, false);
		var originalIdatLength = BinaryPrimitives.ReadInt32BigEndian(original.AsSpan(33, 4));
		var compressed = original.AsSpan(41, originalIdatLength).ToArray();
		using var file = new MemoryStream();
		file.Write(original, 0, 33);
		var split = compressed.Length / 2;
		WriteChunk(file, "IDAT", compressed.AsSpan(0, split).ToArray());
		WriteChunk(file, "tEXt", [77, 97, 114, 107]);
		WriteChunk(file, "IDAT", compressed.AsSpan(split).ToArray());
		WriteChunk(file, "IEND", []);
		ThrowsExactly<InvalidDataException>(() => file.ToArray().ConvertToPng());
	}

	[TestMethod]
	public void Png_ConsecutiveIdatChunks_AreDecodedAsOneCompressedStream()
	{
		var original = Pack(7, 5, Pattern(7, 5), 0, false);
		var len = BinaryPrimitives.ReadInt32BigEndian(original.AsSpan(33, 4));
		var compressed = original.AsSpan(41, len).ToArray();
		using var file = new MemoryStream();
		file.Write(original, 0, 33);
		var split = compressed.Length / 3;
		WriteChunk(file, "IDAT", compressed.AsSpan(0, split).ToArray());
		WriteChunk(file, "IDAT", compressed.AsSpan(split, compressed.Length - split).ToArray());
		WriteChunk(file, "IEND", []);
		AssertPixels(Pattern(7, 5), Decoded(file.ToArray(), 7, 5), 7);
	}

	[TestMethod]
	public void Png_DuplicateIhdrOrMisorderedPalette_IsInvalid()
	{
		var original = Pack(3, 3, Pattern(3, 3), 0, false);
		using var duplicated = new MemoryStream();
		duplicated.Write(original, 0, 33);
		duplicated.Write(original, 8, 25); // second complete IHDR
		duplicated.Write(original, 33, original.Length - 33);
		ThrowsExactly<InvalidDataException>(() => duplicated.ToArray().ConvertToPng());

		using var misplaced = new MemoryStream();
		misplaced.Write(original, 0, original.Length - 12); // IHDR + IDAT
		WriteChunk(misplaced, "PLTE", [0, 0, 0, 255, 255, 255]);
		WriteChunk(misplaced, "IEND", []);
		ThrowsExactly<InvalidDataException>(() => misplaced.ToArray().ConvertToPng());
	}
}
