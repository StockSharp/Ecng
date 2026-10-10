namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// Animated PNG: files assembled chunk by chunk go in, and what comes out is read by <see cref="ImageTestHelper.ReadApng"/>.
/// </summary>
[TestClass]
public class ApngCodecTests : BaseTestClass
{
	private const byte _disposeNone = 0;
	private const byte _disposeBackground = 1;
	private const byte _disposePrevious = 2;
	private const byte _blendSource = 0;
	private const byte _blendOver = 1;

	private static readonly (byte r, byte g, byte b, byte a) _none = (0, 0, 0, 0);
	private static readonly (byte r, byte g, byte b, byte a) _red = (255, 0, 0, 255);
	private static readonly (byte r, byte g, byte b, byte a) _blue = (0, 0, 255, 255);

	[TestMethod]
	public void Delay_IsTheFractionOfASecondTheFrameDeclares()
	{
		var apng = Timed();

		// 1/30 s, 1/1000 s, 700 s, and 3 over a zero denominator, which stands for hundredths.
		apng.GetAnimationFrames().Select(f => f.delayMilliseconds).ToArray().AssertEqual(new[] { 33, 1, 700_000, 30 });
	}

	[TestMethod]
	public void Delay_ComesThroughResizingAndEncodingAsItWas()
	{
		var apng = Timed();

		foreach (var result in new[] { apng.ResizeImage(2, 2), ApngEncoder.Encode(ApngDecoder.Open(apng), []) })
		{
			var frames = ReadApng(result).Frames;

			frames.Select(f => (f.DelayNumerator, f.DelayDenominator)).ToArray()
				.AssertEqual(new (ushort, ushort)[] { (1, 30), (1, 1000), (700, 1), (3, 100) });
		}
	}

	[TestMethod]
	public void FramesBeyondTheWorkBudget_AreRefused()
	{
		// Sixteen one-pixel frames on a nine-megapixel canvas.
		var apng = Huge();

		(apng.Length < 50_000).AssertTrue();
		ThrowsExactly<InvalidDataException>(() => apng.GetAnimationFrames());
		ThrowsExactly<InvalidDataException>(() => apng.ResizeImage(10, 10));
		ThrowsExactly<InvalidDataException>(() => apng.ConvertToPng());
	}

	[TestMethod]
	public void LoopCount_IsReadWithoutDecodingTheFrames()
	{
		var apng = Huge();
		var before = GC.GetAllocatedBytesForCurrentThread();

		apng.GetAnimationLoopCount().AssertEqual(7);

		(GC.GetAllocatedBytesForCurrentThread() - before < 100_000).AssertTrue("reading the loop count allocated frame buffers");
	}

	[TestMethod]
	public void FullCanvasFramesWithinTheBudget_AreDecoded()
	{
		// Eleven frames of four megapixels each, one bit a pixel to keep the file and the test small.
		const int side = 2000;
		var flat = Deflate(new byte[side * (1 + side / 8)]);

		var chunks = new List<(string tag, byte[] data)>
		{
			("IHDR", Header(side, side, 1, 0)),
			("acTL", Counts(11, 0)),
			("fcTL", FrameControl(0, side, side, 0, 0, 1, 10, _disposeNone, _blendSource)),
			("IDAT", flat),
		};

		for (var i = 1u; i < 11; i++)
		{
			chunks.Add(("fcTL", FrameControl(i * 2 - 1, side, side, 0, 0, 1, 10, _disposeNone, _blendSource)));
			chunks.Add(("fdAT", Sequenced(i * 2, flat)));
		}

		chunks.Add(("IEND", []));

		var apng = WriteChunks(chunks);

		// Every frame is decoded to prove the file sound before it is handed back.
		apng.ConvertToPng().Length.AssertEqual(apng.Length);
	}

	[TestMethod]
	public void FrameRectangles_AreComposedWithTheirBlendAndDisposal()
	{
		(byte, byte, byte, byte) green = (0, 255, 0, 128);

		var apng = Apng(4, 2, 0,
			(0, 0, 4, 2, _disposeNone, _blendSource, Rgba(4, 2, (_, _) => _red)),
			// Half-transparent green over red, cleared after it has been shown.
			(1, 0, 2, 1, _disposeBackground, _blendOver, Rgba(2, 1, (_, _) => green)),
			// Shown and taken back.
			(3, 1, 1, 1, _disposePrevious, _blendSource, Rgba(1, 1, (_, _) => _blue)),
			// A transparent pixel written as it is, not blended.
			(0, 1, 1, 1, _disposeNone, _blendSource, Rgba(1, 1, (_, _) => _none)));

		var frames = apng.GetAnimationFrames();
		(byte, byte, byte, byte) mixed = (127, 128, 0, 255);

		frames.Count.AssertEqual(4);
		AssertPixels(Rgba(4, 2, (_, _) => _red), ReadPng(frames[0].png, 4, 2), 4);
		AssertPixels(Rgba(4, 2, (x, y) => y == 0 && x is 1 or 2 ? mixed : _red), ReadPng(frames[1].png, 4, 2), 4, tolerance: 1);
		AssertPixels(Rgba(4, 2, (x, y) => (x, y) switch { (1, 0) or (2, 0) => _none, (3, 1) => _blue, _ => _red }), ReadPng(frames[2].png, 4, 2), 4);
		AssertPixels(Rgba(4, 2, (x, y) => (x, y) switch { (1, 0) or (2, 0) or (0, 1) => _none, _ => _red }), ReadPng(frames[3].png, 4, 2), 4);
	}

	[TestMethod]
	public void ConvertToPng_OfAnAnimatedPng_ReturnsItAsItIs()
	{
		var apng = Timed();

		apng.ConvertToPng().AssertEqual(apng);
	}

	[TestMethod]
	public void ConvertToPng_OfAnAnimatedPngWithADamagedFrame_IsInvalidData()
	{
		var chunks = ReadChunks(Timed());
		var last = Array.FindLastIndex(chunks, c => c.tag == "fdAT");

		// The frame keeps its sequence number and loses its pixels.
		chunks[last] = ("fdAT", chunks[last].data[..4]);

		ThrowsExactly<InvalidDataException>(() => WriteChunks(chunks).ConvertToPng());
	}

	// Four 4x4 frames with delays of 1/30 s, 1/1000 s, 700 s and 3/0.
	private static byte[] Timed()
	{
		var pixels = Rgba(4, 4, (x, y) => ((byte)(x * 60), (byte)(y * 60), 90, 255));
		var data = FrameData(4, 4, pixels);

		return WriteChunks(
		[
			("IHDR", Header(4, 4, 8, 6)),
			("acTL", Counts(4, 3)),
			("fcTL", FrameControl(0, 4, 4, 0, 0, 1, 30, _disposeNone, _blendSource)),
			("IDAT", data),
			("fcTL", FrameControl(1, 4, 4, 0, 0, 1, 1000, _disposeNone, _blendSource)),
			("fdAT", Sequenced(2, data)),
			("fcTL", FrameControl(3, 4, 4, 0, 0, 700, 1, _disposeNone, _blendSource)),
			("fdAT", Sequenced(4, data)),
			("fcTL", FrameControl(5, 4, 4, 0, 0, 3, 0, _disposeNone, _blendSource)),
			("fdAT", Sequenced(6, data)),
			("IEND", []),
		]);
	}

	// Sixteen one-pixel frames on a 3000x3000 canvas, played seven times.
	private static byte[] Huge()
	{
		var frames = new (int, int, int, int, byte, byte, byte[])[16];

		for (var i = 0; i < frames.Length; i++)
			frames[i] = (i, i, 1, 1, _disposeNone, _blendSource, Rgba(1, 1, (_, _) => _red));

		return Apng(3000, 3000, 7, frames);
	}

	// The default image is not part of the animation, so the first frame may be any rectangle.
	private static byte[] Apng(int width, int height, int plays, params (int left, int top, int width, int height, byte disposal, byte blend, byte[] rgba)[] frames)
	{
		var chunks = new List<(string tag, byte[] data)>
		{
			("IHDR", Header(width, height, 8, 6)),
			("acTL", Counts(frames.Length, plays)),
			("IDAT", FrameData(width, height, new byte[width * height * 4])),
		};

		var sequence = 0u;

		foreach (var frame in frames)
		{
			chunks.Add(("fcTL", FrameControl(sequence++, frame.width, frame.height, frame.left, frame.top, 1, 10, frame.disposal, frame.blend)));
			chunks.Add(("fdAT", Sequenced(sequence++, FrameData(frame.width, frame.height, frame.rgba))));
		}

		chunks.Add(("IEND", []));
		return WriteChunks(chunks);
	}

	private static byte[] Counts(int frames, int plays)
	{
		var content = new byte[8];
		BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(0), frames);
		BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(4), plays);
		return content;
	}
}
