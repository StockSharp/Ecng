namespace Ecng.Tests.Drawing;

using System;
using System.Buffers.Binary;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Ecng.Drawing;

[TestClass]
public class ImageHelperTests : BaseTestClass
{
	// Fixtures encoded by an independent encoder (Pillow), so the tests do not merely
	// round-trip our encoder and decoder through the same implementation.
	private static readonly byte[] _rgba = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAABAAAAAICAYAAADwdn+XAAAAIUlEQVR42mNkYAhg4GdgQMb/0fh4MQuDJANFYNSAwWAAABDMAv2PDdyuAAAAAElFTkSuQmCC");

	private static readonly byte[] _jpeg = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAAIABADASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAb/xAAZEAABBQAAAAAAAAAAAAAAAAAABAYjMaH/xAAVAQEBAAAAAAAAAAAAAAAAAAACBf/EABkRAAIDAQAAAAAAAAAAAAAAAAAEAwUhUf/aAAwDAQACEQMRAD8Ai0LSqPCkQtKo8AJrTs3Q0Nixmn//2Q==");

	private static readonly byte[] _gray = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAAAsAAAAHCAAAAAD7BeiBAAAAEElEQVR42mOsZIADJgbqsAEp2wCHVwHWfwAAAABJRU5ErkJggg==");

	private static readonly byte[] _palette = Convert.FromBase64String(
		"iVBORw0KGgoAAAANSUhEUgAAAAgAAAAFAQMAAABCXz8WAAAABlBMVEUAAAD/AAAb/40iAAAAAnRSTlMA/1uRIrUAAAASSURBVHjaYwhlCmVmYAplZgAABokBCjJzwdAAAAAASUVORK5CYII=");

	private static readonly byte[] _large = CreateCanvasPng(320, 160);

	[TestMethod]
	public void GetImageSize_PngAndJpegAcrossOperatingSystems()
	{
		_rgba.GetImageSize().AssertEqual(new Size(16, 8));
		_jpeg.GetImageSize().AssertEqual(new Size(16, 8));
		ReadOnlySpan<byte> span = _gray;
		span.GetImageSize().AssertEqual(new Size(11, 7));
	}

	[TestMethod]
	public void GetImageSize_RejectsBadHeadersAndHugeDimensions()
	{
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.GetImageSize());
		ThrowsExactly<NotSupportedException>(() => new byte[] { 1, 2, 3 }.GetImageSize());
		var damaged = (byte[])_rgba.Clone();
		damaged[16] = damaged[17] = damaged[18] = damaged[19] = 0;
		ThrowsExactly<InvalidDataException>(() => damaged.GetImageSize());
	}

	[TestMethod]
	public void ConvertToPng_TrueAlphaAndSourceUnchanged()
	{
		var before = (byte[])_rgba.Clone();
		var result = _rgba.ConvertToPng();
		result.IsPng().AssertTrue();
		result.GetPngSize().AssertEqual(new Size(16, 8));
		_rgba.SequenceEqual(before).AssertTrue();

		var pixels = Pixels(result);
		Alpha(pixels, 16, 0, 0).AssertEqual((byte)0);
		Alpha(pixels, 16, 3, 4).AssertEqual((byte)0);
		Alpha(pixels, 16, 4, 4).AssertEqual((byte)255);
	}

	[TestMethod]
	public void ConvertToPng_DecodesGrayscaleAndPaletteWithTransparency()
	{
		var grayscale = _gray.ConvertToPng();
		var grayPixels = Pixels(grayscale);
		grayPixels[0].AssertEqual((byte)121);
		grayPixels[1].AssertEqual((byte)121);
		grayPixels[2].AssertEqual((byte)121);
		grayPixels[3].AssertEqual((byte)255);

		var indexed = _palette.ConvertToPng();
		indexed.GetPngSize().AssertEqual(new Size(8, 5));
		var colors = Pixels(indexed);
		Alpha(colors, 8, 0, 0).AssertEqual((byte)0);
		Alpha(colors, 8, 1, 0).AssertEqual((byte)255);
		colors[(0 * 8 + 1) * 4].AssertEqual((byte)255);
	}

	[TestMethod]
	public void ConvertToPng_DecodesBaselineJpegAndPixelColors()
	{
		var result = _jpeg.ConvertToPng();
		result.IsPng().AssertTrue();
		result.GetPngSize().AssertEqual(new Size(16, 8));
		var pixels = Pixels(result);
		var index = (4 * 16 + 8) * 4;
		(Math.Abs(pixels[index] - 120) < 45).AssertTrue();
		(Math.Abs(pixels[index + 1] - 100) < 45).AssertTrue();
		(Math.Abs(pixels[index + 2] - 80) < 45).AssertTrue();
		pixels[index + 3].AssertEqual((byte)255);
	}

	[TestMethod]
	public void ResizeImage_RespectsBothBoundsWithoutUpscaling()
	{
		_rgba.ResizeImage(8, 8).GetPngSize().AssertEqual(new Size(8, 4));
		_rgba.ResizeImage(4, 1).GetPngSize().AssertEqual(new Size(2, 1));
		_rgba.ResizeImage(100, 100).GetPngSize().AssertEqual(new Size(16, 8));
		_jpeg.ResizeImage(4, 4).GetPngSize().AssertEqual(new Size(4, 2));
	}

	[TestMethod]
	public void ResizeImage_PreservesAlphaAndDoesNotModifyInput()
	{
		var before = (byte[])_rgba.Clone();
		var output = _rgba.ResizeImage(8, 8);
		var pixels = Pixels(output);
		Alpha(pixels, 8, 0, 0).AssertEqual((byte)0);
		Alpha(pixels, 8, 7, 1).AssertEqual((byte)255);
		_rgba.SequenceEqual(before).AssertTrue();
	}

	[TestMethod]
	public void ResizeImage_RejectsInvalidDimensions()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.ResizeImage(0, 8));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.ResizeImage(8, -1));
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.ResizeImage(2, 2));
	}

	[TestMethod]
	public void CorruptPng_CrcIsNotIgnored()
	{
		var broken = (byte[])_rgba.Clone();
		broken[broken.Length - 18] ^= 0x10;
		ThrowsExactly<InvalidDataException>(() => broken.ConvertToPng());
	}

	[TestMethod]
	public void NoJpegOrTiffSilentlyGuessedFromArbitraryBytes()
	{
		ThrowsExactly<NotSupportedException>(() => new byte[] { 3, 2, 1 }.ConvertToPng());
		ThrowsExactly<NotSupportedException>(() => new byte[] { (byte)'I', (byte)'I', 42, 0 }.ResizeImage(5, 5));
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.ConvertToPng());
	}

	[TestMethod]
	public void Watermark_IsRealRenderedTextAndKeepsOriginalPixels()
	{
		var font = FindFont();
		if (font == null) return; // The machine has no TrueType fonts installed.

		var source = _large.ConvertToPng();
		var before = Pixels(source);
		var result = source.AddTextWatermark("StockSharp", fontSize: 24, margin: 12, opacity: 255, fontFilePath: font);
		var after = Pixels(result);
		result.GetPngSize().AssertEqual(new Size(320, 160));
		source.GetPngSize().AssertEqual(new Size(320, 160));
		after.SequenceEqual(before).AssertFalse();
	}

	[TestMethod]
	public void Watermark_UnicodeText()
	{
		var font = FindFont();
		if (font == null) return;

		var input = _large.ConvertToPng();
		var output = input.AddTextWatermark("S# Тест", fontSize: 24, margin: 12, fontFilePath: font);
		output.IsPng().AssertTrue();
		output.GetPngSize().AssertEqual(new Size(320, 160));
	}

	[TestMethod]
	public void Watermark_ZeroOpacityKeepsPixels()
	{
		var before = Pixels(_rgba.ConvertToPng());
		var result = _rgba.AddTextWatermark("No Change", opacity: 0, margin: 0);
		Pixels(result).SequenceEqual(before).AssertTrue();
	}

	[TestMethod]
	public void Watermark_ValidatesArguments()
	{
		ThrowsExactly<ArgumentException>(() => _rgba.AddTextWatermark(" "));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", fontSize: 0));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", fontSize: float.NaN));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", margin: -1));
		ThrowsExactly<ArgumentException>(() => _rgba.AddTextWatermark("test", fontFamily: " "));
		ThrowsExactly<ArgumentOutOfRangeException>(() => _rgba.AddTextWatermark("test", margin: 50));
		byte[] missing = null;
		ThrowsExactly<ArgumentNullException>(() => missing.AddTextWatermark("test"));
	}

	[TestMethod]
	public void Watermark_NoSilentFontSubstitution()
	{
		ThrowsExactly<FileNotFoundException>(() =>
			_rgba.AddTextWatermark("Text", fontSize: 5, margin: 1,
				fontFilePath: Path.Combine(Path.GetTempPath(), "missing-ecng-ttf-734289.ttf")));
	}

	// Our encoder produces RGBA8 PNG with filter type 0 on every scanline. Inspect
	// decoded pixel bytes using only the standard zlib implementation, independent
	// of ImageHelper's decoding path.

	// Construct the large canvas at runtime instead of maintaining a long Base64
	// fixture that is easy to corrupt. The raw PNG chunks and checksums are built
	// independently of the production image codec.
	private static byte[] CreateCanvasPng(int width, int height)
	{
		using var output = new MemoryStream();
		output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

		var header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
		header[8] = 8;
		header[9] = 6;
		WriteTestChunk(output, "IHDR", header);

		using var compressed = new MemoryStream();
		using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
		{
			var row = new byte[1 + width * 4];
			for (var x = 0; x < width; x++) row[1 + 4 * x + 3] = 255;

			for (var y = 0; y < height; y++)
				zlib.Write(row);
		}

		WriteTestChunk(output, "IDAT", compressed.ToArray());
		WriteTestChunk(output, "IEND", Array.Empty<byte>());
		return output.ToArray();
	}

	private static void WriteTestChunk(Stream stream, string type, byte[] data)
	{
		Span<byte> length = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
		stream.Write(length);

		var tag = System.Text.Encoding.ASCII.GetBytes(type);
		stream.Write(tag);
		stream.Write(data);

		uint crc = 0xffffffff;
		foreach (var value in tag.Concat(data))
		{
			crc ^= value;
			for (var bit = 0; bit < 8; bit++)
				crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
		}

		BinaryPrimitives.WriteUInt32BigEndian(length, crc ^ 0xffffffff);
		stream.Write(length);
	}

	private static byte[] Pixels(byte[] png)
	{
		using var compressed = new MemoryStream();
		for (var i = 8; i + 12 <= png.Length;)
		{
			var size = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(i, 4));
			if (size < 0 || size > png.Length - i - 12)
				throw new InvalidDataException("Invalid encoded chunk.");
			if (png.AsSpan(i + 4, 4).SequenceEqual("IDAT"u8))
				compressed.Write(png, i + 8, size);
			i += size + 12;
		}

		compressed.Position = 0;
		using var output = new MemoryStream();
		using (var zlib = new ZLibStream(compressed, CompressionMode.Decompress))
			zlib.CopyTo(output);
		var sizeResult = png.GetPngSize();
		var w = sizeResult.Width;
		var h = sizeResult.Height;
		var decoded = output.ToArray();
		var rgb = new byte[checked(w * h * 4)];
		for (var y = 0; y < h; y++)
		{
			var offset = y * (1 + w * 4);
			decoded[offset].AssertEqual((byte)0);
			Array.Copy(decoded, offset + 1, rgb, y * w * 4, w * 4);
		}
		return rgb;
	}

	private static byte Alpha(byte[] pixels, int width, int x, int y)
		=> pixels[(y * width + x) * 4 + 3];

	private static string FindFont()
	{
		var root = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
		var options = new[] { root, "/usr/share/fonts", "/System/Library/Fonts/Supplemental", "/Library/Fonts" };
		foreach (var option in options)
		{
			if (string.IsNullOrWhiteSpace(option) || !Directory.Exists(option)) continue;
			try
			{
				var found = Directory.EnumerateFiles(option, "*.ttf", SearchOption.AllDirectories)
					.FirstOrDefault(x => Path.GetFileName(x).Contains("Verdana", StringComparison.OrdinalIgnoreCase)
						|| Path.GetFileName(x).Contains("DejaVuSans", StringComparison.OrdinalIgnoreCase)
						|| Path.GetFileName(x).Contains("Arial", StringComparison.OrdinalIgnoreCase));
				if (found != null) return found;
			}
			catch (UnauthorizedAccessException) { }
			catch (IOException) { }
		}
		return null;
	}
}
