namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// Independent Pillow-created BMP/GIF fixtures and byte-level APNG/GIF assertions.
/// </summary>
[TestClass]
public class BmpGifTddTests : BaseTestClass
{
	private static readonly byte[] _rgbBmp = Convert.FromBase64String("Qk1OAAAAAAAAADYAAAAoAAAAAwAAAAIAAAABABgAAAAAABgAAADEDgAAxA4AAAAAAAAAAAAAAP////8A/wD/AAAAAAD/AP8A/wAAAAAA");
	private static readonly byte[] _rgbaBmp = Convert.FromBase64String("Qk1OAAAAAAAAADYAAAAoAAAAAwAAAAIAAAABACAAAAAAABgAAADEDgAAxA4AAAAAAAAAAAAAAP//ZP//AAD/AP/cAAD//wD/AP//AAD/");
	// GIF89a, four independently encoded indexed frames, durations 50/120/200/70ms,
	// loop=2, disposal modes 2/3/2/1. Generated using Pillow, not Ecng codec.
	internal static byte[] AnimatedFixture => _animated;
	internal static byte[] WatermarkFixture => _watermarkGif;

	private static readonly byte[] _animated = Convert.FromBase64String(
		"R0lGODlhBAAEAIEAAAAAAP8AAAAAAAAAACH/C05FVFNDQVBFMi4wAwECAAAh+QQJBQAAACwAAAAABAAEAAAIDQADABgokOBAAAUBBAQAIfkEDQwAAAAsAQAAAAEABACBAAAAAP8AAAAAAAAACAYAAwgMEBAAIfkECRQAAAAsAQAAAAIABACBAAAAAAD/AAAAAAAACAgAAQQQSDBAQAAh+QQFBwAAACwDAAAAAQAEAIEAAAD//wAAAAAAAAAIBgADCAwQEAA7");
	private static readonly byte[] _watermarkGif = Convert.FromBase64String(
		"R0lGODlhQAAwAIEAAAAAAP8AAAAAAAAAACH/C05FVFNDQVBFMi4wAwEAAAAh+QQJBAAAACwAAAAAQAAwAAAI/wADCBwoEIDBgwgTAiBIUKHDgwwHPoxY8KFDigEsXqQ4kaPGhBg/gvSoMKRIiCRPmiyZUuRKlS1RRjwpkyFNgy8R5vy4U2PPhTF9BrX48ybQmTeLGt2IlKbSpSObwpQKVShVnkOrRrWZNKvWmg27Xv26NaxTr2SflpVI9ijXs2OJDlWrE+3aiizjdtTL9O3djGL9upxrty7fvILBsoWb2Gpjt2anPt77mK5ivJIxRh58+C/lzVg7G65c+DJgxqBxllYt2vTnxZlhcybdmvXkvqkhyw59G3Fuy7Zzv8asFbhu4m1HC8e9O3nw5nJrLzVu3Cj11dOxVw+83Df05NelDyU/HRu549/a08cN39szc/PR2yuHzr67e+/wx1snLL0+ffXymRYQACH5BA0HAAAALAAAAABAADAAgQAAAAD/AAAAAAAAAAj/AAEIHEiwIIAACBMiNMhwoEKFDSNKLPgw4USKFQNc3Mgwo0aOBzOCHBmy4kiPJEGiVCky5caVHGG6lCjzYs2ZHVvG1InzJk2eNoE6FPrTJEujBn1GVNpQKdOcSF8SLfnw5NSmU58mvQq1aleIR73ujEpQK0ayEz2aLct1K1qBa4e+XZq17VmxUufGhWuXrd6+cvEG/TsXa+GvFhEvDAt2rODAjfM+LjqZ5F6qkXEq/ug4s+a7nimH/sz3sNvKpC9ftmoadGLSp0cbRv1ZNWDRryXL3jx7d+zcgyvbbu1X+G3MwNPWJQ45Oe7Fv6Hrdk6XMO3onKdLL+7b9Xblaq97Ks8efPRw8dypn++e/ntp9M3dVzfO/D37+OTxs4ZvX/3x9f7Vh5x8vVEXEAAh+QQFCQAAACwAAAAAQAAwAIEAAAAAAP8AAAAAAAAI/wABCBwoMIDBgwYJKlwIACFChhAbOkzIcCLFiAotBsC4UCNEjxwJggxZ0OJHkyRLTkyp0uHJlSxHkpQpEmVKmhxxtnwY0+ZMnzVh3gSaUyNRjDojJk360mVPoRmPKpXalGdHqhWxXoUa1OlQrkXB7jzIUqLYqWeZZj1blexarz/ZvrVaNqrcrXDrhs27l65epFrt8v071+3XwYQF++1rOHHbi3EROx4LOaRax5cVNyacuetiwGkDe97MuPLAzqdFpw59V7Np0IhRU974NLbq2WWX3jYrufBr3p/R9sZrlDTs4I9pEzcuHLlv5a6hW96tu/Xo38mz13a+/Lds4My1dyqXXpr8cebf0+8WH5l7YvXW/8If/p76+u3hn09unn/8fvbTxafXfO4RFBAAOw==");

	[TestMethod]
	public void Bmp_IndependentPillowRgb24_DimensionsAndEveryPixel()
	{
		_rgbBmp.GetImageSize().Width.AssertEqual(3);
		_rgbBmp.GetImageSize().Height.AssertEqual(2);
		var png = _rgbBmp.ConvertToPng();
		var expected = new byte[]
		{
			255,0,0,255,0,255,0,255,0,0,255,255,
			255,255,0,255,0,255,255,255,255,0,255,255
		};
		AssertPixels(expected, ReadPng(png, 3, 2), 3);
	}

	[TestMethod]
	public void Bmp_PillowRgb24_ResizePreservesFormat_AndNoUpscaleIsByteIdentical()
	{
		var source = (byte[])_rgbBmp.Clone();
		var same = source.ResizeImage(100, 100);
		same.AssertEqual(source);
		var resized = source.ResizeImage(2, 2);
		(resized[0] == 'B' && resized[1] == 'M').AssertTrue();
		resized.GetImageSize().Width.AssertEqual(2);
		resized.GetImageSize().Height.AssertEqual(1);
		source.AssertEqual(_rgbBmp);
	}

	[TestMethod]
	public void Bmp_PillowRgba32_WrittenAgain_KeepsEveryPixel()
	{
		var source = (byte[])_rgbaBmp.Clone();
		var written = BmpCodec.Encode(BmpCodec.Decode(source));
		(written[0] == 'B' && written[1] == 'M').AssertTrue();

		// The picture of _rgbBmp in 32 bits a pixel: their fourth byte is reserved, not alpha.
		var expected = new byte[]
		{
			255,0,0,255,0,255,0,255,0,0,255,255,
			255,255,0,255,0,255,255,255,255,0,255,255
		};
		AssertPixels(expected, ReadPng(source.ConvertToPng(), 3, 2), 3);
		AssertPixels(expected, ReadPng(written.ConvertToPng(), 3, 2), 3);
		source.AssertEqual(_rgbaBmp);
	}

	[TestMethod]
	public void Gif_PillowFourFrameFixture_InspectEveryCompositedFrameAndTiming()
	{
		_animated.GetImageSize().Width.AssertEqual(4);
		_animated.GetImageSize().Height.AssertEqual(4);
		var frames = _animated.GetGifFrames();
		frames.Count.AssertEqual(4);
		var times = new[] { 50, 120, 200, 70 };
		byte[][] colors =
		[
			[255,0,0,255], [0,255,0,255],
			[0,0,255,255], [255,255,0,255]
		];

		for (var frame = 0; frame < 4; frame++)
		{
			frames[frame].delayMilliseconds.AssertEqual(times[frame]);
			var rgba = ReadPng(frames[frame].png, 4, 4);
			for (var y = 0; y < 4; y++)
				for (var x = 0; x < 4; x++)
				{
					var offset = (y * 4 + x) * 4;
					var wanted = x == frame ? colors[frame] : new byte[] { 0, 0, 0, 0 };
					for (var c = 0; c < 4; c++)
						rgba[offset + c].AssertEqual(wanted[c]);
				}
		}
		_animated.GetGifLoopCount().AssertEqual(2);
	}

	[TestMethod]
	public void Gif_Resize_AllFourFramesStayAnimated_PreservesDelayAndLoops()
	{
		var output = _animated.ResizeImage(2, 2);
		(output[0] == 'G' && output[1] == 'I' && output[2] == 'F').AssertTrue();
		output.GetImageSize().Width.AssertEqual(2);
		output.GetImageSize().Height.AssertEqual(2);
		output.GetGifLoopCount().AssertEqual(2);
		var frames = output.GetGifFrames();
		frames.Count.AssertEqual(4);
		var delays = new[] { 50, 120, 200, 70 };
		for (var i = 0; i < 4; i++)
		{
			frames[i].delayMilliseconds.AssertEqual(delays[i]);
			frames[i].png.GetImageSize().Width.AssertEqual(2);
			frames[i].png.GetImageSize().Height.AssertEqual(2);
		}
	}

	[TestMethod]
	public void Gif_ConvertToPng_PreservesEveryFrameAsApng()
	{
		var apng = _animated.ConvertToPng();
		apng.GetImageSize().Width.AssertEqual(4);
		apng.GetImageSize().Height.AssertEqual(4);
		var chunks = ReadChunks(apng);
		var animation = chunks.Single(x => x.tag == "acTL").data;
		BinaryPrimitives.ReadUInt32BigEndian(animation.AsSpan(0, 4)).AssertEqual((uint)4);
		// The GIF repeats twice after its first run: three plays in all.
		BinaryPrimitives.ReadUInt32BigEndian(animation.AsSpan(4, 4)).AssertEqual((uint)3);
		chunks.Count(x => x.tag == "fcTL").AssertEqual(4);
		chunks.Count(x => x.tag == "fdAT").AssertEqual(3);
		AssertPixels(ReadPng(_animated.GetGifFrames()[0].png, 4, 4), ReadPng(apng, 4, 4), 4);
	}

	[TestMethod]
	public void Apng_OutputFromGif_ResizeMustPreserveAllFourFramesAndDelays()
	{
		var apng = _animated.ConvertToPng();
		var resized = apng.ResizeImage(2, 2);
		var chunks = ReadChunks(resized);
		var acTl = chunks.Single(x => x.tag == "acTL").data;
		BinaryPrimitives.ReadUInt32BigEndian(acTl.AsSpan(0, 4)).AssertEqual((uint)4);
		// The GIF repeats twice after its first run: three plays in all.
		BinaryPrimitives.ReadUInt32BigEndian(acTl.AsSpan(4, 4)).AssertEqual((uint)3);
		chunks.Count(x => x.tag == "fcTL").AssertEqual(4);
		chunks.Count(x => x.tag == "fdAT").AssertEqual(3);
		resized.GetImageSize().Width.AssertEqual(2);
		resized.GetImageSize().Height.AssertEqual(2);
		var delays = new[] { 5, 12, 20, 7 };
		var controls = chunks.Where(x => x.tag == "fcTL").ToArray();
		for (var i = 0; i < 4; i++)
			BinaryPrimitives.ReadUInt16BigEndian(controls[i].data.AsSpan(20, 2))
				.AssertEqual((ushort)delays[i]);
	}

	[TestMethod]
	public void Gif_ApngExport_CheckEveryFramePayloadAndControlAgainstIndependentColors()
	{
		var apng = ReadApng(_animated.ConvertToPng());
		var expectedDurations = new[] { 5, 12, 20, 7 }; // centiseconds
		byte[][] colors =
		[
			[255,0,0,255], [0,255,0,255],
			[0,0,255,255], [255,255,0,255]
		];

		apng.Frames.Count.AssertEqual(4);

		for (var index = 0; index < 4; index++)
		{
			var frame = apng.Frames[index];
			frame.Width.AssertEqual(4);
			frame.Height.AssertEqual(4);
			frame.DelayNumerator.AssertEqual((ushort)expectedDurations[index]);
			frame.DelayDenominator.AssertEqual((ushort)100);
			frame.Disposal.AssertEqual((byte)0);
			frame.Blend.AssertEqual((byte)0);

			// The frame is its own color in the column of its number and transparent elsewhere.
			var column = index;
			var color = colors[index];
			AssertPixels(Rgba(4, 4, (x, _) => x == column ? (color[0], color[1], color[2], color[3]) : default), frame.Rgba, 4);
		}
	}

	[TestMethod]
	public void Gif_Watermark_EachOfThreeFramesChanged_AnimationPreserved()
	{
		var file = TempFile("font.ttf", ImagePixelGoldenTests.BuildTestTrueType());
		var source = (byte[])_watermarkGif.Clone();
		var expected = source.GetGifFrames();
		var result = source.AddTextWatermark("I", fontSize: 18, opacity: 255,
			margin: 8, fontFilePath: file);
		(result[0] == 'G' && result[1] == 'I' && result[2] == 'F').AssertTrue();
		result.GetGifLoopCount().AssertEqual(0);
		var frames = result.GetGifFrames();
		frames.Count.AssertEqual(3);
		for (var i = 0; i < frames.Count; i++)
		{
			frames[i].delayMilliseconds.AssertEqual(expected[i].delayMilliseconds);
			CompareNonEqual(ReadPng(expected[i].png, 64, 48), ReadPng(frames[i].png, 64, 48));
		}
		source.AssertEqual(_watermarkGif);
	}

	[TestMethod]
	public void Gif_WrittenAgain_ShowsInEveryFrameWhatTheSourceShows()
	{
		var source = _animated.GetGifFrames();
		var frames = GifCodec.Encode(GifCodec.Open(_animated)).GetGifFrames();

		frames.Count.AssertEqual(source.Count);

		for (var i = 0; i < source.Count; i++)
		{
			frames[i].delayMilliseconds.AssertEqual(source[i].delayMilliseconds);
			AssertPixels(ReadPng(source[i].png, 4, 4), ReadPng(frames[i].png, 4, 4), 4);
		}
	}

	[TestMethod]
	public void Gif_WrittenAgain_ClearsAFrameTheNextOneIsTransparentOver()
	{
		// Red on the left and nothing on the right; then nothing on the left and blue on the right.
		var palette = GifBuilder.Palette((255, 255, 255), (255, 0, 0), (0, 0, 255), (0, 0, 0));

		var gif = new GifBuilder(2, 1, palette)
			.Frame(0, 0, 2, 1, [1, 0], transparent: 0, disposal: 2, delay: 3)
			.Frame(0, 0, 2, 1, [0, 2], transparent: 0, disposal: 2, delay: 5)
			.Build();

		var written = GifCodec.Encode(GifCodec.Open(gif));
		var frames = written.GetGifFrames();

		frames.Count.AssertEqual(2);
		AssertPixels([255, 0, 0, 255, 0, 0, 0, 0], ReadPng(frames[0].png, 2, 1), 2);
		AssertPixels([0, 0, 0, 0, 0, 0, 255, 255], ReadPng(frames[1].png, 2, 1), 2);

		// The file says so itself, to a reader that is not the library: drawing over red cannot make it nothing.
		GifFile.Read(written).Frames[0].Disposal.AssertEqual(2);
	}

	[TestMethod]
	public void Gif_TruncatedAndCorruptFiles_FailInControlledManner()
	{
		foreach (var length in new[] { 1, 6, 13, 25, _animated.Length / 2, _animated.Length - 1 })
		{
			try
			{
				_animated[..length].ConvertToPng();
				Fail($"Truncated GIF of {length} bytes was accepted.");
			}
			catch (InvalidDataException) { }
			catch (NotSupportedException) { } // <6-byte data is unrecognizable as GIF.
		}

		var random = new Random(18057);
		for (var iter = 0; iter < 100; iter++)
		{
			var bytes = (byte[])_animated.Clone();
			var index = random.Next(13, bytes.Length - 1);
			bytes[index] ^= (byte)(1 << random.Next(8));
			try
			{
				var frames = bytes.GetGifFrames();
				(frames.Count > 0 && frames.Count <= 512).AssertTrue();
			}
			catch (InvalidDataException) { }
			catch (NotSupportedException) { }
		}
	}

	[TestMethod]
	public void Apng_ReimportEveryGifFrame_AgreesWithIndependentPillowPixelOracle()
	{
		var animation = _animated.ConvertToPng();
		var frames = animation.GetAnimationFrames();
		frames.Count.AssertEqual(4);
		// The GIF repeats twice after its first run: three plays in all.
		animation.GetAnimationLoopCount().AssertEqual(3);
		var original = _animated.GetGifFrames();
		for (var i = 0; i < 4; i++)
		{
			frames[i].delayMilliseconds.AssertEqual(original[i].delayMilliseconds);
			AssertPixels(ReadPng(original[i].png, 4, 4), ReadPng(frames[i].png, 4, 4), 4);
		}
	}

	[TestMethod]
	public void Apng_FullFrameBlendSourceAndBlendOver_RespectTransparentPixels()
	{
		var source = ReadChunks(_animated.ConvertToPng());
		var controls = 0;
		foreach (var i in Enumerable.Range(0, source.Length))
			if (source[i].tag == "fcTL")
			{
				// First frame retains red column; second frame should composite
				// green over red instead of wiping transparent pixels.
				if (controls == 1)
					source[i].data[25] = 1;
				controls++;
			}
		var apng = WriteChunks(source);
		var frames = apng.GetAnimationFrames();
		var second = ReadPng(frames[1].png, 4, 4);
		for (var y = 0; y < 4; y++)
		{
			AssertPixels([255, 0, 0, 255], second.AsSpan((y * 4 + 0) * 4, 4).ToArray(), 1);
			AssertPixels([0, 255, 0, 255], second.AsSpan((y * 4 + 1) * 4, 4).ToArray(), 1);
			AssertPixels([0, 0, 0, 0], second.AsSpan((y * 4 + 2) * 4, 4).ToArray(), 1);
		}
	}

	[TestMethod]
	public void Apng_AllThreeDisposalModes_RenderExpectedFramePixels()
	{
		for (var disposal = 0; disposal < 3; disposal++)
		{
			var chunks = ReadChunks(_animated.ConvertToPng());
			var first = Array.FindIndex(chunks, c => c.tag == "fcTL");
			chunks[first].data[24] = (byte)disposal;
			var second = Array.FindIndex(chunks, first + 1, c => c.tag == "fcTL");
			chunks[second].data[25] = 1; // overlay on previous canvas
			var frames = WriteChunks(chunks).GetAnimationFrames();
			var next = ReadPng(frames[1].png, 4, 4);
			// NONE preserves red; BACKGROUND and PREVIOUS clear/restore it.
			for (var y = 0; y < 4; y++)
				next[(y * 4) * 4 + 3].AssertEqual((byte)(disposal == 0 ? 255 : 0));
		}
	}

	[TestMethod]
	public void Apng_MalformedFrameSequenceDimensionsCrcOrMetadata_FailsClosed()
	{
		var bytes = _animated.ConvertToPng();
		var changed = (byte[])bytes.Clone();
		changed[changed.Length - 24] ^= 0x80;
		ThrowsExactly<InvalidDataException>(() => changed.GetAnimationFrames());

		void Reject(Action<(string tag, byte[] data)[]> change)
		{
			var chunks = ReadChunks(bytes);
			change(chunks);
			ThrowsExactly<InvalidDataException>(() => WriteChunks(chunks).GetAnimationFrames());
		}
		Reject(chunks => BinaryPrimitives.WriteUInt32BigEndian(chunks.Single(c => c.tag == "acTL").data.AsSpan(0, 4), 8));
		Reject(chunks => BinaryPrimitives.WriteUInt32BigEndian(chunks.First(c => c.tag == "fcTL").data.AsSpan(0, 4), 100));
		Reject(chunks => BinaryPrimitives.WriteUInt32BigEndian(chunks.First(c => c.tag == "fdAT").data.AsSpan(0, 4), 100));
		Reject(chunks => BinaryPrimitives.WriteUInt32BigEndian(chunks.First(c => c.tag == "fcTL").data.AsSpan(4, 4), 5));
		Reject(chunks => { var d = chunks.First(c => c.tag == "fcTL").data; d[24] = 3; });
		Reject(chunks => { var d = chunks.First(c => c.tag == "fcTL").data; d[25] = 2; });
		Reject(chunks => { var at = Array.FindIndex(chunks, c => c.tag == "fdAT"); chunks[at] = (chunks[at].tag, Array.Empty<byte>()); });
	}



	private static void CompareNonEqual(byte[] expected, byte[] actual)
	{
		expected.Length.AssertEqual(actual.Length);
		if (expected.SequenceEqual(actual))
			Fail("Expected watermark to change the frame pixels.");
	}
}
