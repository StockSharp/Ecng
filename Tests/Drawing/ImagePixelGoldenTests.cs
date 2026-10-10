namespace Ecng.Tests.Drawing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Ecng.Drawing;

/// <summary>
/// Pixel-for-pixel regression tests with an independent PNG fixture builder and
/// reader. These don't compare one production code path against another.
/// No imaging, font, or native packages are used in test execution.
/// </summary>
[TestClass]
public class ImagePixelGoldenTests : BaseTestClass
{
	private static readonly byte[] _jpegFixture = Convert.FromBase64String(
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAIBAQIBAQICAgICAgICAwUDAwMDAwYEBAMFBwYHBwcGBwcICQsJCAgKCAcHCg0KCgsMDAwMBwkODw0MDgsMDAz/2wBDAQICAgMDAwYDAwYMCAcIDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAz/wAARCAAIAAgDAREAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDE/wCCZvw8/wCF/f8ACbf6Z/ZP9k/Yf+WXn+b5n2n3XGPL9859q/MvpccDf8Qs/sn9/wDW/rft/s+y5PZex/vVObm9p5Wt1vp6XjPxx/xMF9T/AHH9nf2d7T7X1j2n1jk/u0eTk9j/AHubm+zy6//Z");

	// Eight-by-eight RGBA output decoded independently with Pillow 12.3.0
	// (JPEG quality 94, 4:4:4, four differently colored quadrants).
	// The oracle is checked into source: CI has no Python/Pillow dependency.
	private static readonly byte[] _jpegPixelsReference = Convert.FromBase64String(
		"8RQz/+8UMv/wEDT/8RY0/yTGMv8nzDL/Jsg0/yTIM//xEi3/6hg1//QUNv/oFC3/Msgz/ybHLP8vxCz/Kso2//MUM//vEjP/8REp//AWMf8nxTL/Jsk6/yjIMP8jxi//8hMw/+sWNP/zFDP/8hMw/yrKNv8nyjP/L8gy/yjKNv8KR93/DUPb/wpF3f8GSNz/3c4b/9rQGf/X0xz/2s4a/whG2f8JRtz/DEfp/whF2//g1CD/3NET/97RIP/f0iH/C0jf/w5B2P8IRNr/EEff/9XQHP/c0yD/19Yj/9nOGv8KRtr/CUTc/w5H3P8HRdj/4tId/9vPGf/g0hr/3dEd/w==");

	[TestMethod]
	public void ConvertToPng_RgbaEveryPixelMatchesExpected()
	{
		var expected = new byte[]
		{
			255,0,0,255, 0,255,0,255,
			0,0,255,255, 255,255,255,255,
			10,20,30,0, 220,120,40,128,
		};
		var original = CreateRgbaPng(2, 3, expected);
		var originalCopy = (byte[])original.Clone();

		var actual = Pixels(original.ConvertToPng(), 2, 3);
		ComparePixels(expected, actual, 2, 3);
		original.SequenceEqual(originalCopy).AssertTrue();
	}

	[TestMethod]
	public void ResizeImage_OpaqueBilinearPixelsMatchExactGolden()
	{
		// Center-aligned 2x2 -> 1x1 must average all four source pixels.
		var input = CreateRgbaPng(2, 2,
		[
			255,0,0,255, 0,255,0,255,
			0,0,255,255, 255,255,255,255,
		]);

		ComparePixels([128,128,128,255], Pixels(input.ResizeImage(1, 1), 1, 1), 1, 1);
	}

	[TestMethod]
	public void ResizeImage_TransparentColorsDoNotCreateDarkOrRedHalos()
	{
		// Transparent red/green must not leak into blue when downscaling.
		var input = CreateRgbaPng(2, 2,
		[
			255,0,0,0, 0,0,255,255,
			0,255,0,0, 0,0,255,255,
		]);

		ComparePixels([0,0,255,128], Pixels(input.ResizeImage(1, 1), 1, 1), 1, 1);
	}

	[TestMethod]
	public void ResizeImage_FourByFourToTwoByTwoHasExactFourReferencePixels()
	{
		byte[] corners =
		[
			255,30,0,255, 0,200,50,255,
			30,40,240,255, 250,250,200,255,
		];

		var input = new byte[4 * 4 * 4];
		for (var y = 0; y < 4; y++)
			for (var x = 0; x < 4; x++)
				Array.Copy(corners, ((y / 2) * 2 + (x / 2)) * 4, input, (y * 4 + x) * 4, 4);

		ComparePixels(corners, Pixels(CreateRgbaPng(4, 4, input).ResizeImage(2, 2), 2, 2), 2, 2);
	}

	[TestMethod]
	public void ResizeImage_NoUpscalingPreservesEveryPixelNotJustDimensions()
	{
		var expected = new byte[]
		{
			5,17,31,255, 91,72,53,90,
			200,180,160,0, 7,8,9,255,
		};
		var input = CreateRgbaPng(2, 2, expected);
		ComparePixels(expected, Pixels(input.ResizeImage(200, 200), 2, 2), 2, 2);
	}

	[TestMethod]
	public void JpegDecode_CompareEveryPixelAgainstIndependentDecoderWithSmallTolerance()
	{
		var actual = Pixels(_jpegFixture.ConvertToPng(), 8, 8);
		ComparePixels(_jpegPixelsReference, actual, 8, 8, tolerance: 3);
	}

	[TestMethod]
	public void Watermark_FixedSyntheticTrueTypeGlyph_MatchesExactPixelMask()
	{
		// Programmatically construct a tiny original TTF with one rectangular I glyph.
		// This avoids bundling a font or depending on OS-specific Verdana versions.
		var file = Path.Combine(Path.GetTempPath(), $"ecng-pixel-test-{Guid.NewGuid():N}.ttf");
		File.WriteAllBytes(file, BuildTestTrueType());

		try
		{
			var originalPixels = new byte[64 * 48 * 4];
			for (var i = 3; i < originalPixels.Length; i += 4)
				originalPixels[i] = 255; // opaque black

			var image = CreateRgbaPng(64, 48, originalPixels);
			var output = image.AddTextWatermark("I", fontSize: 18, opacity: 200,
				margin: 8, fontFilePath: file);
			var actual = Pixels(output, 64, 48);

			var expected = (byte[])originalPixels.Clone();
			// Font: UPEM=1000, ascent=800, descent=-200, advance=1000.
			// At 18pt / 96dpi: scale=.024, x-origin=32, baseline=35.2.
			// Glyph outline is a rectangle x=100..900, y=0..800 font units.
			const double left = 34.4;
			const double right = 53.6;
			const double top = 16.0;
			const double bottom = 35.2;

			for (var y = 0; y < 48; y++)
				for (var x = 0; x < 64; x++)
				{
					var hits = 0;
					for (var sy = 0; sy < 2; sy++)
						for (var sx = 0; sx < 2; sx++)
						{
							var xx = x + (sx + .5) / 2;
							var yy = y + (sy + .5) / 2;
							if (xx >= left && xx < right && yy >= top && yy < bottom)
								hits++;
						}

					var white = (byte)(200 * hits / 4);
					var dest = (y * 64 + x) * 4;
					expected[dest] = white;
					expected[dest + 1] = white;
					expected[dest + 2] = white;
				}

			ComparePixels(expected, actual, 64, 48);
		}
		finally
		{
			File.Delete(file);
		}
	}

	[TestMethod]
	public void Watermark_WithSameFontAndInput_IsPixelDeterministic()
	{
		var file = Path.Combine(Path.GetTempPath(), $"ecng-determinism-{Guid.NewGuid():N}.ttf");
		File.WriteAllBytes(file, BuildTestTrueType());
		try
		{
			var opaqueBlack = new byte[32 * 32 * 4];
			for (var i = 3; i < opaqueBlack.Length; i += 4) opaqueBlack[i] = 255;
			var source = CreateRgbaPng(32, 32, opaqueBlack);
			var a = Pixels(source.AddTextWatermark("I", fontSize: 12, opacity: 85, margin: 3, fontFilePath: file), 32, 32);
			var b = Pixels(source.AddTextWatermark("I", fontSize: 12, opacity: 85, margin: 3, fontFilePath: file), 32, 32);
			ComparePixels(a, b, 32, 32);
		}
		finally
		{
			File.Delete(file);
		}
	}

	// An independently constructed PNG, not PngCodec.Encode.
	private static byte[] CreateRgbaPng(int width, int height, byte[] pixels)
	{
		if (pixels.Length != checked(width * height * 4))
			throw new ArgumentException("Fixture dimensions differ from RGBA pixel count.");

		using var stream = new MemoryStream();
		stream.Write([137, 80, 78, 71, 13, 10, 26, 10]);
		var ihdr = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0, 4), width);
		BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4, 4), height);
		ihdr[8] = 8;
		ihdr[9] = 6;
		WriteChunk(stream, "IHDR", ihdr);

		using var compressed = new MemoryStream();
		using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
			for (var y = 0; y < height; y++)
			{
				zlib.WriteByte(0);
				zlib.Write(pixels, y * width * 4, width * 4);
			}

		WriteChunk(stream, "IDAT", compressed.ToArray());
		WriteChunk(stream, "IEND", []);
		return stream.ToArray();
	}

	// An independent decoding oracle for the canonical RGBA8/filter-0 files
	// produced by the public API; never invokes PngCodec.Decode.
	private static byte[] Pixels(byte[] png, int width, int height)
	{
		if (png.GetPngSize().Width != width || png.GetPngSize().Height != height)
			Assert.Fail($"Unexpected image dimensions, wanted {width}x{height}.");

		using var idat = new MemoryStream();
		for (var position = 8; position + 12 <= png.Length;)
		{
			var size = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position, 4));
			if (size < 0 || size > png.Length - position - 12)
				Assert.Fail("Output PNG has an invalid chunk length.");
			if (png.AsSpan(position + 4, 4).SequenceEqual("IDAT"u8))
				idat.Write(png, position + 8, size);
			position += size + 12;
		}

		idat.Position = 0;
		using var stream = new MemoryStream();
		using (var zlib = new ZLibStream(idat, CompressionMode.Decompress))
			zlib.CopyTo(stream);

		var data = stream.ToArray();
		var result = new byte[width * height * 4];
		if (data.Length != height * (1 + width * 4))
			Assert.Fail("Output PNG decompressed data length mismatch.");

		for (var y = 0; y < height; y++)
		{
			var start = y * (width * 4 + 1);
			if (data[start] != 0)
				Assert.Fail($"Unexpected PNG scanline filter at y={y}: {data[start]}");
			Array.Copy(data, start + 1, result, y * width * 4, width * 4);
		}

		return result;
	}

	private static void ComparePixels(byte[] expected, byte[] actual, int width, int height, int tolerance = 0)
	{
		if (expected.Length != width * height * 4 || actual.Length != expected.Length)
			Assert.Fail($"Expected {width * height * 4} RGBA bytes; actual {actual.Length}.");

		for (var i = 0; i < expected.Length; i++)
			if (Math.Abs(expected[i] - actual[i]) > tolerance)
			{
				var pixel = i / 4;
				var channel = "RGBA"[i % 4];
				Assert.Fail($"RGBA mismatch at pixel ({pixel % width}, {pixel / width}), channel {channel}: " +
					$"expected {expected[i]}, actual {actual[i]}, tolerance {tolerance}.");
			}
	}

	private static void WriteChunk(Stream stream, string name, byte[] content)
	{
		Span<byte> number = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(number, content.Length);
		stream.Write(number);

		var tag = Encoding.ASCII.GetBytes(name);
		stream.Write(tag);
		stream.Write(content);

		uint crc = 0xFFFFFFFF;
		foreach (var value in tag.Concat(content))
		{
			crc ^= value;
			for (var n = 0; n < 8; n++)
				crc = (crc & 1) != 0 ? crc >> 1 ^ 0xEDB88320 : crc >> 1;
		}
		BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
		stream.Write(number);
	}

	internal static byte[] BuildTestTrueType()
	{
		// A hand-built minimal .ttf with glyph 0 empty and glyph 1 = a rectangle,
		// cmap Unicode 'I' -> glyph 1. No third-party font binary is distributed.
		var head = new byte[54];
		Put16(head, 18, 1000); // unitsPerEm
		Put16(head, 50, 0);    // short loca

		var hhea = new byte[36];
		Put16(hhea, 4, 800);            // ascent
		Put16(hhea, 6, unchecked((ushort)-200)); // descent
		Put16(hhea, 34, 2);             // two horizontal metrics

		var maxp = new byte[6];
		Put32(maxp, 0, 0x00010000);
		Put16(maxp, 4, 2); // glyph count

		var cmap = new byte[44];
		Put16(cmap, 2, 1);  // one Unicode cmap
		Put16(cmap, 4, 3);  // Microsoft
		Put16(cmap, 6, 1);  // BMP Unicode
		Put32(cmap, 8, 12); // format-4 subtable offset

		Put16(cmap, 12, 4); // cmap format 4
		Put16(cmap, 14, 32);
		Put16(cmap, 18, 4); // segCountX2 (2 segments)
		Put16(cmap, 20, 4); // searchRange
		Put16(cmap, 22, 1); // entrySelector
		Put16(cmap, 24, 0); // rangeShift
		Put16(cmap, 26, (ushort)'I'); Put16(cmap, 28, 0xFFFF); // endCode
		Put16(cmap, 32, (ushort)'I'); Put16(cmap, 34, 0xFFFF); // startCode
		Put16(cmap, 36, unchecked((ushort)(1 - 'I')));
		Put16(cmap, 38, 1); // sentinel delta
		// idRangeOffset zeros

		var loca = new byte[6];
		Put16(loca, 4, 17); // glyph #1 is 34 bytes

		var glyf = new byte[34];
		Put16(glyf, 0, 1);   // 1 contour
		Put16(glyf, 2, 100); Put16(glyf, 4, 0);   // bounds min
		Put16(glyf, 6, 900); Put16(glyf, 8, 800); // bounds max
		Put16(glyf, 10, 3);  // 4 on-curve points
		// zero instructions at offset 12
		glyf[14] = glyf[15] = glyf[16] = glyf[17] = 1; // flags: on curve
		Put16(glyf, 18, 100); Put16(glyf, 20, 800); Put16(glyf, 22, 0);
		Put16(glyf, 24, unchecked((ushort)-800)); // x-deltas
		Put16(glyf, 26, 0); Put16(glyf, 28, 0);
		Put16(glyf, 30, 800); Put16(glyf, 32, 0); // y-deltas

		var hmtx = new byte[8];
		Put16(hmtx, 0, 1000);
		Put16(hmtx, 4, 1000);

		(string tag, byte[] data)[] tables =
		[
			("head", head), ("hhea", hhea), ("maxp", maxp),
			("cmap", cmap), ("loca", loca), ("glyf", glyf), ("hmtx", hmtx),
		];

		using var font = new MemoryStream();
		font.SetLength(12 + 16 * tables.Length);
		var header = new byte[12];
		Put32(header, 0, 0x00010000);
		Put16(header, 4, (ushort)tables.Length);
		font.Position = 0;
		font.Write(header);

		var directory = 12;
		foreach (var (tag, data) in tables)
		{
			font.Position = font.Length;
			var offset = (int)font.Position;
			font.Write(data);

			var record = new byte[16];
			Encoding.ASCII.GetBytes(tag).CopyTo(record, 0);
			Put32(record, 8, (uint)offset);
			Put32(record, 12, (uint)data.Length);
			font.Position = directory;
			font.Write(record);
			directory += 16;
		}

		return font.ToArray();
	}

	private static void Put16(byte[] data, int index, ushort value)
		=> BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(index, 2), value);

	private static void Put32(byte[] data, int index, uint value)
		=> BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(index, 4), value);

	[TestMethod]
	public void Watermark_PixelExactSourceOverBlending_ForOpaqueAndTransparentBackgrounds()
	{
		// Analytically computed Porter-Duff source-over goldens, not an
		// ImageHelper-vs-ImageHelper round trip. Glyph interior at (40,25).
		var fontPath = Path.Combine(Path.GetTempPath(), $"ecng-alpha-{Guid.NewGuid():N}.ttf");
		File.WriteAllBytes(fontPath, BuildTestTrueType());
		try
		{
			foreach (var backgroundAlpha in new byte[] { 0, 1, 32, 127, 254, 255 })
				foreach (var watermarkOpacity in new byte[] { 1, 64, 160, 254, 255 })
				{
					var sourcePixels = new byte[64 * 48 * 4];
					for (var index = 0; index < sourcePixels.Length; index += 4)
					{
						sourcePixels[index] = 30;
						sourcePixels[index + 1] = 75;
						sourcePixels[index + 2] = 120;
						sourcePixels[index + 3] = backgroundAlpha;
					}

					var input = CreateRgbaPng(64, 48, sourcePixels);
					var copy = (byte[])input.Clone();
					var actual = Pixels(input.AddTextWatermark("I", fontSize: 18,
						opacity: watermarkOpacity, margin: 8, fontFilePath: fontPath), 64, 48);

					input.SequenceEqual(copy).AssertTrue();
					for (var channel = 0; channel < 4; channel++)
						actual[channel].AssertEqual(sourcePixels[channel]); // outside the glyph

					var destination = (25 * 64 + 40) * 4;
					var foregroundAlpha = watermarkOpacity / 255.0;
					var oldAlpha = backgroundAlpha / 255.0;
					var combinedAlpha = foregroundAlpha + oldAlpha * (1 - foregroundAlpha);
					for (var channel = 0; channel < 3; channel++)
					{
						var expected = (byte)Math.Clamp((int)Math.Round(
							(255 * foregroundAlpha + sourcePixels[destination + channel] *
							oldAlpha * (1 - foregroundAlpha)) / combinedAlpha), 0, 255);
						actual[destination + channel].AssertEqual(expected);
					}
					actual[destination + 3].AssertEqual(
						(byte)Math.Clamp((int)Math.Round(combinedAlpha * 255), 0, 255));
				}
		}
		finally
		{
			File.Delete(fontPath);
		}
	}

	[TestMethod]
	public void Watermark_ZeroOpacityDoesNotNeedInstalledOrProvidedFont()
	{
		var pixelBytes = new byte[32 * 16 * 4];
		for (var i = 0; i < pixelBytes.Length; i++)
			pixelBytes[i] = (byte)(i * 11);
		var png = CreateRgbaPng(32, 16, pixelBytes);
		var output = png.AddTextWatermark("Invisible", opacity: 0, margin: 1,
			fontFilePath: Path.Combine(Path.GetTempPath(), "missing-ecng-invisible.ttf"));
		ComparePixels(pixelBytes, Pixels(output, 32, 16), 32, 16);
	}

}
