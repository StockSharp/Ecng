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
