namespace Ecng.Tests.Drawing;

using Ecng.Drawing;

using static ImageTestHelper;

/// <summary>
/// The limits of the PNG decoder: how much it takes on the word of a header.
/// </summary>
[TestClass]
public class PngCodecTests : BaseTestClass
{
	[TestMethod]
	public void SingleIdatDecode_AllocatesPixelsAndScanlineBuffersWithoutCopyingTheInput()
	{
		const int width = 512;
		const int height = 384;
		var expected = SyntheticImageCorpus.Pixels("noise", width, height);
		var source = CreatePng(width, height, expected);
		for (var warmup = 0; warmup < 4; warmup++) PngCodec.Decode(source);
		long largest = 0;
		for (var sample = 0; sample < 3; sample++)
		{
			CancellationToken.ThrowIfCancellationRequested();
			var before = GC.GetAllocatedBytesForCurrentThread();
			var decoded = PngCodec.Decode(source);
			largest = Math.Max(largest, GC.GetAllocatedBytesForCurrentThread() - before);
			AssertPixels(expected, decoded.Pixels, width);
		}
		IsTrue(largest <= expected.Length + 128 * 1024,
			$"PNG decoding allocated {largest} bytes for {expected.Length} output bytes and a {source.Length}-byte input.");
	}

	[TestMethod]
	[DataRow(0, 0)]
	[DataRow(1, 0)]
	[DataRow(0, 1)]
	[DataRow(2, 2)]
	public void EmptyIdatChunks_PreserveTheConcatenatedStream(int leading, int trailing)
	{
		const int width = 37;
		const int height = 23;
		var expected = SyntheticImageCorpus.Pixels("texture", width, height);
		var source = ReadChunks(CreatePng(width, height, expected));
		var chunks = new List<(string, byte[])> { source[0] };
		for (var i = 0; i < leading; i++) chunks.Add(("IDAT", []));
		var compressed = source.Single(c => c.tag == "IDAT").data;
		var split = compressed.Length / 2;
		chunks.Add(("IDAT", compressed[..split]));
		chunks.Add(("IDAT", []));
		chunks.Add(("IDAT", compressed[split..]));
		for (var i = 0; i < trailing; i++) chunks.Add(("IDAT", []));
		chunks.Add(("IEND", []));
		AssertPixels(expected, PngCodec.Decode(WriteChunks(chunks)).Pixels, width);
	}

	[TestMethod]
	public void HeaderDeclaringMoreThanTheDataCanHold_IsRefusedBeforeABufferIsAllocated()
	{
		// A hundred megabytes of scanlines are announced; the data unpacks to a hundred bytes.
		var png = WriteChunks(
		[
			("IHDR", Header(5000, 5000, 8, 6)),
			("IDAT", Deflate(new byte[100])),
			("IEND", []),
		]);

		var before = GC.GetAllocatedBytesForCurrentThread();

		ThrowsExactly<InvalidDataException>(() => png.ConvertToPng());

		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		(allocated < 1_000_000).AssertTrue($"{allocated} bytes were allocated for a file of {png.Length}");
	}

	[TestMethod]
	public void ScanlinesShorterOrLongerThanTheHeaderCallsFor_AreInvalidData()
	{
		foreach (var length in new[] { 2 * 9 - 1, 2 * 9 + 1 })
		{
			var png = WriteChunks(
			[
				("IHDR", Header(2, 2, 8, 6)),
				("IDAT", Deflate(new byte[length])),
				("IEND", []),
			]);

			ThrowsExactly<InvalidDataException>(() => png.ConvertToPng());
		}
	}

	[TestMethod]
	public void ScanlineFilterOutsideTheFiveTypes_IsInvalidData()
	{
		var raw = new byte[2 * 9];
		raw[9] = 5;

		var png = WriteChunks(
		[
			("IHDR", Header(2, 2, 8, 6)),
			("IDAT", Deflate(raw)),
			("IEND", []),
		]);

		ThrowsExactly<InvalidDataException>(() => png.ConvertToPng());
	}
}
