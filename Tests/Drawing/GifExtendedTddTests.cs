namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// Independent GIF89a fixture builder with literal LZW codes. Exercises
/// interlaced rows, local palettes, control and comment extensions, and rejects
/// corrupt GIF headers and streams without using production GIF encoders.
/// </summary>
[TestClass]
public class GifExtendedTddTests : BaseTestClass
{
	[TestMethod]
	public void Gif_InterlacedLocalPalette_AllRowsAndChannelsExactlyMatchOracle()
	{
		var gif = BuildInterlacedGif(comment: false);
		var frames = gif.GetGifFrames();
		frames.Count.AssertEqual(1);
		frames[0].delayMilliseconds.AssertEqual(50);
		gif.GetGifLoopCount().AssertEqual(-1);
		gif.GetImageSize().Width.AssertEqual(4);
		gif.GetImageSize().Height.AssertEqual(8);

		var output = ReadPng(frames[0].png, 4, 8);
		byte[][] palette = [[0, 0, 0, 0], [255, 0, 0, 255], [0, 255, 0, 255], [0, 0, 255, 255]];
		for (var y = 0; y < 8; y++)
			for (var x = 0; x < 4; x++)
				for (var c = 0; c < 4; c++)
					output[(y * 4 + x) * 4 + c].AssertEqual(palette[y % 3 + 1][c]);
	}

	[TestMethod]
	public void Gif_UnknownCommentExtensionIsSkippedWithoutChangingFrames()
	{
		var plain = BuildInterlacedGif(false);
		var annotated = BuildInterlacedGif(true);
		var a = plain.GetGifFrames();
		var b = annotated.GetGifFrames();
		a.Count.AssertEqual(b.Count);
		a[0].delayMilliseconds.AssertEqual(b[0].delayMilliseconds);
		var pa = ReadPng(a[0].png, 4, 8);
		var pb = ReadPng(b[0].png, 4, 8);
		pa.SequenceEqual(pb).AssertTrue();
	}

	[TestMethod]
	public void Gif_NoUpscalingClonesBytesNotOnlyCanvasAndFrames()
	{
		var source = BuildInterlacedGif(false);
		var resized = source.ResizeImage(40, 80);
		source.SequenceEqual(resized).AssertTrue();
		ReferenceEquals(source, resized).AssertFalse();
	}

	[TestMethod]
	public void Gif_InvalidDisposalAndTruncatedLocalColorTable_AreRejected()
	{
		var valid = BuildInterlacedGif(false);
		var gce = FindSequence(valid, [0x21, 0xF9, 4]);
		(gce >= 0).AssertTrue();
		var invalid = (byte[])valid.Clone();
		invalid[gce + 3] = (byte)(4 << 2 | 1);
		ThrowsExactly<NotSupportedException>(() => invalid.GetGifFrames());

		var local = FindSequence(valid, [0x2C]);
		(local >= 0).AssertTrue();
		var truncated = valid[..(local + 10 + 4)]; // fewer than four palette entries
		ThrowsExactly<InvalidDataException>(() => truncated.GetGifFrames());
	}

	[TestMethod]
	public void Gif_LzwIncorrectMinimumCodeSizeAndBrokenTerminator_AreRejected()
	{
		var gif = BuildInterlacedGif(false);
		var local = FindSequence(gif, [0x2C]);
		var lzwSize = local + 10 + 12; // descriptor + 4-entry RGB palette
		gif[lzwSize].AssertEqual((byte)8);
		var wrong = (byte[])gif.Clone();
		wrong[lzwSize] = 9;
		ThrowsExactly<InvalidDataException>(() => wrong.GetGifFrames());

		var broken = (byte[])gif.Clone();
		broken[^1] = 0;
		ThrowsExactly<InvalidDataException>(() => broken.GetGifFrames());
	}

	[TestMethod]
	public void Gif_ConvertInterlacedToApng_HasOneFrameAndItsPixels()
	{
		var original = BuildInterlacedGif(true);
		var apng = original.ConvertToPng();
		var rgba = ReadPng(apng, 4, 8);
		var expected = ReadPng(original.GetGifFrames()[0].png, 4, 8);
		rgba.SequenceEqual(expected).AssertTrue();
		EncodingAsciiContains(apng, "acTL").AssertTrue();
		EncodingAsciiContains(apng, "fcTL").AssertTrue();
	}

	private static byte[] BuildInterlacedGif(bool comment)
	{
		using var file = new MemoryStream();
		file.Write("GIF89a"u8);
		U16(file, 4);
		U16(file, 8);
		file.WriteByte(0x80); // global two-color palette
		file.WriteByte(0);
		file.WriteByte(0);
		file.Write([0, 0, 0, 255, 255, 255]);

		if (comment)
			file.Write([0x21, 0xFE, 3, (byte)'H', (byte)'i', (byte)'!', 0]);

		file.Write([0x21, 0xF9, 4, 1, 5, 0, 0, 0]); // transparent index 0, 50ms
		file.WriteByte(0x2C);
		U16(file, 0);
		U16(file, 0);
		U16(file, 4);
		U16(file, 8);
		file.WriteByte(0xC1); // interlace + local four-color palette
		file.Write([0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255]);

		var indices = new byte[4 * 8];
		var next = 0;
		foreach (var (start, step) in new (int start, int step)[] { (0, 8), (4, 8), (2, 4), (1, 2) })
			for (var y = start; y < 8; y += step)
				for (var x = 0; x < 4; x++)
					indices[next++] = (byte)(y % 3 + 1);

		file.WriteByte(8); // LZW literal code width 8
		var data = EncodeLzwLiteralFixture(indices);
		for (var at = 0; at < data.Length;)
		{
			var take = Math.Min(255, data.Length - at);
			file.WriteByte((byte)take);
			file.Write(data, at, take);
			at += take;
		}
		file.WriteByte(0);
		file.WriteByte(0x3B);
		return file.ToArray();
	}

	private static byte[] EncodeLzwLiteralFixture(byte[] pixels)
	{
		using var compressed = new MemoryStream();
		uint accumulator = 0;
		var bits = 0;
		void Append(int code)
		{
			accumulator |= (uint)code << bits;
			bits += 9;
			while (bits >= 8)
			{
				compressed.WriteByte((byte)accumulator);
				accumulator >>= 8;
				bits -= 8;
			}
		}
		Append(256);
		foreach (var pixel in pixels)
			Append(pixel);
		Append(257);
		if (bits > 0)
			compressed.WriteByte((byte)accumulator);
		return compressed.ToArray();
	}

	private static int FindSequence(byte[] bytes, byte[] sequence)
	{
		for (var i = 0; i <= bytes.Length - sequence.Length; i++)
			if (bytes.AsSpan(i, sequence.Length).SequenceEqual(sequence))
				return i;
		return -1;
	}

	private static bool EncodingAsciiContains(byte[] bytes, string word) =>
		Encoding.ASCII.GetString(bytes).Contains(word, StringComparison.Ordinal);

	private static void U16(Stream stream, int value)
	{
		stream.WriteByte((byte)value);
		stream.WriteByte((byte)(value >> 8));
	}
}
