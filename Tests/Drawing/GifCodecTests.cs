namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// GIF decoding against hand-written files, and GIF encoding read back by <see cref="GifFile"/>.
/// </summary>
[TestClass]
public class GifCodecTests : BaseTestClass
{
	private static readonly (byte r, byte g, byte b, byte a) _none = (0, 0, 0, 0);
	private static readonly (byte r, byte g, byte b, byte a) _red = (255, 0, 0, 255);
	private static readonly (byte r, byte g, byte b, byte a) _black = (0, 0, 0, 255);

	// Index 0 white, 1 red, 2 blue, 3 black.
	private static readonly byte[] _palette = GifBuilder.Palette((255, 255, 255), (255, 0, 0), (0, 0, 255), (0, 0, 0));

	[TestMethod]
	public void TransparentPixel_StaysTransparentWhateverTheBackgroundIndexIs()
	{
		// The background index points at opaque blue; the transparent one is another.
		var gif = new GifBuilder(2, 1, _palette, background: 2)
			.Frame(0, 0, 2, 1, [1, 0], transparent: 0)
			.Build();

		AssertFrame(gif, 0, 2, 1, _red, _none);
	}

	[TestMethod]
	public void ScreenAroundTheFirstFrame_IsTransparent()
	{
		var gif = new GifBuilder(4, 1, _palette, background: 2)
			.Frame(1, 0, 2, 1, [1, 1])
			.Build();

		AssertFrame(gif, 0, 4, 1, _none, _red, _red, _none);
	}

	[TestMethod]
	public void RestoringToBackground_LeavesTheAreaTransparent()
	{
		var gif = new GifBuilder(2, 1, _palette, background: 2)
			.Frame(0, 0, 2, 1, [1, 1], disposal: 2)
			.Frame(0, 0, 1, 1, [3])
			.Build();

		AssertFrame(gif, 0, 2, 1, _red, _red);
		AssertFrame(gif, 1, 2, 1, _black, _none);
	}

	[TestMethod]
	public void BytesAfterTheTrailer_AreIgnored()
	{
		var gif = new GifBuilder(2, 1, _palette)
			.Frame(0, 0, 2, 1, [1, 3])
			.Build(afterTrailer: [0, 1, 2, 0x3B]);

		AssertFrame(gif, 0, 2, 1, _red, _black);
	}

	[TestMethod]
	public void FileWithoutTheTrailer_IsInvalidData()
	{
		var gif = new GifBuilder(2, 1, _palette)
			.Frame(0, 0, 2, 1, [1, 3])
			.Build();

		ThrowsExactly<InvalidDataException>(() => gif[..^1].GetGifFrames());
	}

	[TestMethod]
	public void PixelDataWithoutTheEndCode_IsAccepted()
	{
		var gif = new GifBuilder(2, 1, _palette)
			.Frame(0, 0, 2, 1, [1, 3], endCode: false)
			.Build();

		AssertFrame(gif, 0, 2, 1, _red, _black);
	}

	[TestMethod]
	public void CodesAfterTheLastPixel_AreIgnored()
	{
		var gif = new GifBuilder(2, 1, _palette)
			.Frame(0, 0, 2, 1, [1, 3], extraCodes: 5)
			.Build();

		AssertFrame(gif, 0, 2, 1, _red, _black);
	}

	[TestMethod]
	public void PixelDataThatEndsEarly_IsInvalidData()
	{
		// Three pixels are declared, two are there.
		var gif = new GifBuilder(3, 1, _palette)
			.Frame(0, 0, 2, 1, [1, 3])
			.Build();

		// The image descriptor follows the 13-byte header, the 12-byte table and the 8-byte control extension.
		gif[13 + 12 + 8].AssertEqual((byte)0x2C);
		gif[13 + 12 + 8 + 5] = 3;

		ThrowsExactly<InvalidDataException>(() => gif.GetGifFrames());
	}

	[TestMethod]
	public void FrameReachingBeyondTheScreen_IsClipped()
	{
		var gif = new GifBuilder(2, 2, _palette)
			.Frame(1, 1, 2, 2, [1, 2, 3, 0])
			.Build();

		AssertFrame(gif, 0, 2, 2, _none, _none, _none, _red);
	}

	[TestMethod]
	public void FramesBeyondTheWorkBudget_AreRefused()
	{
		// Sixteen one-pixel frames on a nine-megapixel screen: 144 megapixels to composite, from 400 bytes.
		var gif = Huge(loop: -1);

		(gif.Length < 1000).AssertTrue();
		ThrowsExactly<InvalidDataException>(() => gif.GetGifFrames());
		ThrowsExactly<InvalidDataException>(() => gif.ResizeImage(10, 10));
		ThrowsExactly<InvalidDataException>(() => gif.ConvertToPng());
		ThrowsExactly<InvalidDataException>(() => gif.AddTextWatermark("x", opacity: 0, margin: 0));
	}

	[TestMethod]
	public void LoopCount_IsReadWithoutDecodingTheFrames()
	{
		var gif = Huge(loop: 5);
		var before = GC.GetAllocatedBytesForCurrentThread();

		gif.GetGifLoopCount().AssertEqual(5);
		gif.GetAnimationLoopCount().AssertEqual(6);

		(GC.GetAllocatedBytesForCurrentThread() - before < 100_000).AssertTrue("reading the loop count allocated frame buffers");
	}

	[TestMethod]
	public void LoopCount_CountsRepetitionsInAGifAndPlaysInAnAnimation()
	{
		// NETSCAPE2.0 says how many times to repeat after the first run; no extension means no repeating.
		foreach (var (loop, plays) in new[] { (-1, 1), (0, 0), (1, 2), (5, 6), (65535, 65536) })
		{
			var builder = new GifBuilder(2, 1, _palette);

			if (loop >= 0)
				builder.Loop(loop);

			var gif = builder.Frame(0, 0, 2, 1, [1, 3], delay: 7).Frame(0, 0, 2, 1, [3, 1], delay: 9).Build();

			gif.GetGifLoopCount().AssertEqual(loop);
			gif.GetAnimationLoopCount().AssertEqual(plays);

			// A re-encoded GIF repeats as many times, and the APNG made of it plays as many.
			GifFile.Read(Reencode(gif)).LoopCount.AssertEqual(loop);

			var apng = gif.ConvertToPng();
			ReadApng(apng).Plays.AssertEqual(plays);
			apng.GetAnimationLoopCount().AssertEqual(plays);
		}
	}

	[TestMethod]
	public void Reencoded_FlatFrames_AreCompressed()
	{
		const int width = 200;
		const int height = 150;

		var gif = new GifBuilder(width, height, _palette)
			.Frame(0, 0, width, height, Filled(width * height, 1), delay: 4)
			.Frame(0, 0, width, height, Filled(width * height, 2), delay: 6)
			.Frame(0, 0, width, height, Filled(width * height, 3), delay: 8)
			.Build();

		var result = Reencode(gif);
		var file = GifFile.Read(result);

		file.Frames.Count.AssertEqual(3);
		file.Frames.Select(f => f.Delay).ToArray().AssertEqual(new[] { 4, 6, 8 });

		byte[][] colors = [[255, 0, 0, 255], [0, 0, 255, 255], [0, 0, 0, 255]];

		for (var i = 0; i < 3; i++)
		{
			var frame = file.Frames[i];

			frame.HasEndCode.AssertTrue();
			AssertPixels(Rgba(width, height, (_, _) => (colors[i][0], colors[i][1], colors[i][2], 255)), frame.ToRgba(), width);

			// 30 000 equal pixels: a few hundred bytes of LZW against 33 750 for nine bits a pixel.
			(frame.CompressedLength < 1000).AssertTrue($"frame {i} takes {frame.CompressedLength} bytes");
		}

		(result.Length < 3000).AssertTrue($"the file takes {result.Length} bytes");
	}

	[TestMethod]
	public void Reencoded_NoisyFrame_OutgrowsTheCodeTableAndStaysExact()
	{
		// Nothing repeats, so every code adds a string: the table of 4096 fills several times over.
		const int side = 128;
		var random = new Random(9173);
		var indices = new byte[side * side];
		random.NextBytes(indices);

		// All 256 colors are in use, so they are kept as they are.
		for (var i = 0; i < 256; i++)
			indices[i] = (byte)i;

		var gif = new GifBuilder(side, side, GifBuilder.GrayPalette(256))
			.Frame(0, 0, side, side, indices)
			.Build();

		var frame = GifFile.Read(Reencode(gif)).Frames.Single();

		frame.HasEndCode.AssertTrue();
		AssertPixels(Rgba(side, side, (x, y) => (indices[y * side + x], indices[y * side + x], indices[y * side + x], 255)), frame.ToRgba(), side);
	}

	[TestMethod]
	public void Reencoded_Frame_GetsATableNoLargerThanItsColors()
	{
		var gif = new GifBuilder(2, 2, _palette)
			.Frame(0, 0, 2, 2, [1, 2, 3, 1])
			.Build();

		var frame = GifFile.Read(Reencode(gif)).Frames.Single();

		(frame.Palette.Length <= 4 * 3).AssertTrue($"{frame.Palette.Length / 3} entries for three colors");
		frame.MinCodeSize.AssertEqual(2);
		frame.Transparent.AssertEqual(-1);
		AssertPixels(Rgba(2, 2, (x, y) => (x + y) switch { 0 or 2 => _red, _ when x == 1 => ((byte)0, (byte)0, (byte)255, (byte)255), _ => _black }), frame.ToRgba(), 2);
	}

	[TestMethod]
	public void Reencoded_TwoHundredFiftySixOpaqueColors_AreExact()
	{
		var indices = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

		var gif = new GifBuilder(16, 16, GifBuilder.GrayPalette(256))
			.Frame(0, 0, 16, 16, indices)
			.Build();

		var frame = GifFile.Read(Reencode(gif)).Frames.Single();

		frame.Transparent.AssertEqual(-1);
		AssertPixels(Rgba(16, 16, (x, y) => ((byte)(y * 16 + x), (byte)(y * 16 + x), (byte)(y * 16 + x), 255)), frame.ToRgba(), 16);
	}

	[TestMethod]
	public void Reencoded_TransparentPixels_StayTransparentBesideExactColors()
	{
		var gif = new GifBuilder(2, 2, _palette)
			.Frame(0, 0, 2, 2, [1, 0, 0, 3], transparent: 0)
			.Build();

		var frame = GifFile.Read(Reencode(gif)).Frames.Single();

		(frame.Transparent >= 0).AssertTrue();
		AssertPixels(Rgba(2, 2, (x, y) => (x, y) switch { (0, 0) => _red, (1, 1) => _black, _ => _none }), frame.ToRgba(), 2);
	}

	[TestMethod]
	public void Reencoded_MoreColorsThanATableHolds_StayCloseToTheSource()
	{
		// The first frame fills the screen with a gray ramp over a black band and stays; the second
		// lays a color ramp over its right half. Composited, that is about five hundred colors.
		const int width = 32;
		const int height = 24;

		var gray = new byte[width * height];

		for (var y = 0; y < 16; y++)
			for (var x = 0; x < width; x++)
				gray[y * width + x] = (byte)(y * 16 + x % 16);

		var colors = new byte[256 * 3];

		for (var i = 0; i < 256; i++)
		{
			colors[i * 3] = (byte)i;
			colors[i * 3 + 1] = (byte)(255 - i);
			colors[i * 3 + 2] = 128;
		}

		var gif = new GifBuilder(width, height, GifBuilder.GrayPalette(256))
			.Frame(0, 0, width, height, gray, disposal: 1)
			.Frame(16, 0, 16, 16, Enumerable.Range(0, 256).Select(i => (byte)i).ToArray(), palette: colors)
			.Build();

		var expected = Rgba(width, height, (x, y) =>
		{
			if (y >= 16)
				return _black;

			var i = (byte)(y * 16 + x % 16);
			return x < 16 ? (i, i, i, (byte)255) : (i, (byte)(255 - i), (byte)128, (byte)255);
		});

		var frame = GifFile.Read(Reencode(gif)).Frames[1];
		var actual = IndependentlyComposited(GifFile.Read(Reencode(gif)))[1];

		// Two neighbors of a ramp may share a table entry; nothing moves further than that.
		AssertPixels(expected, actual, width, tolerance: 6);

		// The band is a quarter of the picture: it has an entry of its own.
		for (var y = 16; y < height; y++)
			for (var x = 0; x < width; x++)
				Pixel(actual, width, x, y).AssertEqual(_black, $"black band at ({x}, {y})");
	}

	[TestMethod]
	public void Resized_Frames_AreTheAveragedSourcePixels()
	{
		// Red left half, blue right half; the second frame the other way round.
		var gif = new GifBuilder(4, 4, _palette)
			.Loop(0)
			.Frame(0, 0, 4, 4, [1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2], delay: 3)
			.Frame(0, 0, 4, 4, [2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1], delay: 5)
			.Build();

		var file = GifFile.Read(gif.ResizeImage(2, 2));
		(byte, byte, byte, byte) blue = (0, 0, 255, 255);

		file.Width.AssertEqual(2);
		file.Height.AssertEqual(2);
		file.LoopCount.AssertEqual(0);
		file.Frames.Count.AssertEqual(2);
		file.Frames[0].Delay.AssertEqual(3);
		file.Frames[1].Delay.AssertEqual(5);
		AssertPixels(Rgba(2, 2, (x, _) => x == 0 ? _red : blue), file.Frames[0].ToRgba(), 2);
		AssertPixels(Rgba(2, 2, (x, _) => x == 0 ? blue : _red), file.Frames[1].ToRgba(), 2);
	}


	[TestMethod]
	public void DifferenceFrames_SixtySparseUpdates_AreSubrectanglesAndMuchSmaller()
	{
		const int width = 200, height = 120;
		var builder = new GifBuilder(width, height, _palette)
			.Loop(0)
			.Frame(0, 0, width, height, Filled(width * height, 3), delay: 5);

		for (var i = 0; i < 60; i++)
			builder.Frame(i * 3, i % 35, 1, 1, [1], delay: 2 + i % 7);

		var original = builder.Build();
		var encoded = Reencode(original);
		var independent = GifFile.Read(encoded);

		independent.Frames.Count.AssertEqual(61);
		independent.LoopCount.AssertEqual(0);
		(encoded.Length < 15_000).AssertTrue($"Sparse animation unexpectedly takes {encoded.Length} bytes.");

		for (var i = 1; i < 61; i++)
		{
			var frame = independent.Frames[i];
			frame.Width.AssertEqual(1);
			frame.Height.AssertEqual(1);
			frame.Left.AssertEqual((i - 1) * 3);
			frame.Top.AssertEqual((i - 1) % 35);
			frame.Disposal.AssertEqual(1);
			frame.Delay.AssertEqual(2 + (i - 1) % 7);
			frame.HasEndCode.AssertTrue();
		}
		AssertEveryFrameLikeSource(GifFile.Read(original), independent);
	}

	[TestMethod]
	public void DifferenceFrames_UnchangedHolesInBoundingRectangle_UseTransparentPaletteIndex()
	{
		var gif = new GifBuilder(7, 4, _palette)
			.Frame(0, 0, 7, 4, Filled(28, 3), delay: 2)
			.Frame(1, 1, 5, 2,
			[
				1, 0, 0, 0, 0,
				0, 0, 0, 0, 2
			], transparent: 0, delay: 9)
			.Build();

		var output = GifFile.Read(Reencode(gif));
		var changed = output.Frames[1];

		changed.Left.AssertEqual(1);
		changed.Top.AssertEqual(1);
		changed.Width.AssertEqual(5);
		changed.Height.AssertEqual(2);
		(changed.Transparent >= 0).AssertTrue();
		(changed.Indices[0] != changed.Transparent).AssertTrue();
		(changed.Indices[9] != changed.Transparent).AssertTrue();
		for (var index = 1; index < 9; index++)
			changed.Indices[index].AssertEqual((byte)changed.Transparent);
		changed.Disposal.AssertEqual(1);

		AssertEveryFrameLikeSource(GifFile.Read(gif), output);
	}

	[TestMethod]
	public void DifferenceFrames_OpaqueToTransparent_ClearsTheFullCanvas()
	{
		var gif = new GifBuilder(5, 3, _palette)
			.Frame(0, 0, 5, 3, Filled(15, 1), disposal: 2, delay: 4)
			.Frame(2, 1, 1, 1, [3], delay: 7)
			.Build();

		var output = GifFile.Read(Reencode(gif));
		output.Frames[0].Disposal.AssertEqual(2);
		output.Frames[0].Width.AssertEqual(5);
		output.Frames[0].Height.AssertEqual(3);
		output.Frames[1].Left.AssertEqual(2);
		output.Frames[1].Top.AssertEqual(1);
		output.Frames[1].Width.AssertEqual(1);
		output.Frames[1].Height.AssertEqual(1);
		AssertEveryFrameLikeSource(GifFile.Read(gif), output);
	}

	[TestMethod]
	public void DifferenceFrames_UnchangedFrame_WritesOnlyOneTransparentPixel()
	{
		var gif = new GifBuilder(4, 3, _palette)
			.Frame(0, 0, 4, 3, Filled(12, 1), delay: 3)
			.Frame(0, 0, 4, 3, Filled(12, 1), delay: 11)
			.Build();

		var encoded = GifFile.Read(Reencode(gif));
		var duplicate = encoded.Frames[1];

		duplicate.Width.AssertEqual(1);
		duplicate.Height.AssertEqual(1);
		(duplicate.Transparent >= 0).AssertTrue();
		duplicate.Indices.Single().AssertEqual((byte)duplicate.Transparent);
		duplicate.Delay.AssertEqual(11);
		AssertEveryFrameLikeSource(GifFile.Read(gif), encoded);
	}

	[TestMethod]
	public void DifferenceFrames_LastFrameAtLoopBoundary_ClearsBeforeTransparentFirstFrame()
	{
		var gif = new GifBuilder(3, 1, _palette)
			.Loop(0)
			.Frame(0, 0, 3, 1, [0, 1, 0], transparent: 0, delay: 2)
			.Frame(0, 0, 3, 1, [1, 1, 1], delay: 5)
			.Build();

		var encoded = GifFile.Read(Reencode(gif));
		encoded.Frames[1].Disposal.AssertEqual(2);
		encoded.Frames[1].Width.AssertEqual(3);
		encoded.Frames[1].Height.AssertEqual(1);
		encoded.LoopCount.AssertEqual(0);
		AssertEveryFrameLikeSource(GifFile.Read(gif), encoded);
	}

	private static void AssertEveryFrameLikeSource(GifFile original, GifFile encoded)
	{
		var a = IndependentlyComposited(original);
		var b = IndependentlyComposited(encoded);
		a.Length.AssertEqual(b.Length);
		for (var i = 0; i < a.Length; i++)
			AssertPixels(a[i], b[i], original.Width);
	}

	// Test-only independent LZW decoding (GifFile) and disposal composition.
	// Never uses the production GIF decoder to read encoded output.
	private static byte[][] IndependentlyComposited(GifFile file)
	{
		var canvas = new byte[file.Width * file.Height * 4];
		var pictures = new List<byte[]>();
		GifFile.Frame previous = null;
		byte[] previousSaved = null;
		foreach (var frame in file.Frames)
		{
			if (previous?.Disposal == 2)
			{
				for (var y = previous.Top; y < previous.Top + previous.Height; y++)
					for (var x = previous.Left; x < previous.Left + previous.Width; x++)
						Array.Clear(canvas, (y * file.Width + x) * 4, 4);
			}
			else if (previous?.Disposal == 3)
				previousSaved.CopyTo(canvas, 0);

			var saved = frame.Disposal == 3 ? (byte[])canvas.Clone() : null;
			var patch = frame.ToRgba();
			for (var y = 0; y < frame.Height; y++)
				for (var x = 0; x < frame.Width; x++)
				{
					var pixel = (y * frame.Width + x) * 4;
					if (patch[pixel + 3] < 128) continue;
					var dest = ((y + frame.Top) * file.Width + x + frame.Left) * 4;
					Array.Copy(patch, pixel, canvas, dest, 4);
				}

			pictures.Add((byte[])canvas.Clone());
			previous = frame;
			previousSaved = saved;
		}
		return [.. pictures];
	}

	// Sixteen one-pixel frames on a 3000x3000 screen.
	private static byte[] Huge(int loop)
	{
		var builder = new GifBuilder(3000, 3000, _palette);

		if (loop >= 0)
			builder.Loop(loop);

		for (var i = 0; i < 16; i++)
			builder.Frame(i, i, 1, 1, [1]);

		return builder.Build();
	}

	private static byte[] Filled(int count, byte index)
	{
		var indices = new byte[count];
		Array.Fill(indices, index);
		return indices;
	}

	// The file as the encoder writes its frames, nothing drawn on them.
	private static byte[] Reencode(byte[] gif)
		=> GifCodec.Encode(GifCodec.Open(gif));

	private static void AssertFrame(byte[] gif, int frame, int width, int height, params (byte r, byte g, byte b, byte a)[] pixels)
	{
		var actual = ReadPng(gif.GetGifFrames()[frame].png, width, height);

		AssertPixels(Rgba(width, height, (x, y) => pixels[y * width + x]), actual, width);
	}
}
