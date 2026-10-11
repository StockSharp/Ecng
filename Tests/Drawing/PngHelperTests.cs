namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;
using System.Drawing;

using Ecng.Drawing;

[TestClass]
public class PngHelperTests : BaseTestClass
{
	// Two real pictures, one bit a pixel and all black: 3x2, and 513x258 - a size whose bytes all differ, so one
	// read in the wrong byte order shows.
	private static readonly byte[] _small = "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACAQAAAAC1D1u3AAAADElEQVR42mNgYGAAAAAEAAHI6uv5AAAAAElFTkSuQmCC".Base64();
	private static readonly byte[] _large = "iVBORw0KGgoAAAANSUhEUgAAAgEAAAECAQAAAABP4Tn/AAAAJklEQVR42u3BAQEAAACCIP+vbkhAAQAAAAAAAAAAAAAAAAAAAPBuQoQAAehHS4YAAAAASUVORK5CYII=".Base64();

	// How a JPEG picture starts.
	private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08];

	[TestMethod]
	public void GetPngSize_ReadsTheSizeThePictureStates()
	{
		_small.GetPngSize().AssertEqual(new Size(3, 2));
		_large.GetPngSize().AssertEqual(new Size(513, 258));
	}

	[TestMethod]
	public void GetPngSize_OfASpan_ReadsTheSame()
	{
		ReadOnlySpan<byte> picture = _large;

		picture.GetPngSize().AssertEqual(new Size(513, 258));
	}

	// The size is in the header, so the beginning of a picture is enough - what a download that is still going holds.
	[TestMethod]
	public void GetPngSize_NeedsTheHeaderAlone()
	{
		var header = _large[..24];

		header.GetPngSize().AssertEqual(new Size(513, 258));
	}

	[TestMethod]
	public void IsPng_TellsAPngByHowItStarts()
	{
		_small.IsPng().AssertTrue();
		_large.IsPng().AssertTrue();

		_jpeg.IsPng().AssertFalse();
		"GIF89a".ASCII().IsPng().AssertFalse();
		Array.Empty<byte>().IsPng().AssertFalse();
		_small[..4].IsPng().AssertFalse();
	}

	[TestMethod]
	public void TryGetPngSize_RefusesWhatIsNotAPngHeader()
	{
		_jpeg.TryGetPngSize(out _).AssertFalse();
		Array.Empty<byte>().TryGetPngSize(out _).AssertFalse();

		// The signature alone, and a header cut a byte short of its height.
		_small[..8].TryGetPngSize(out _).AssertFalse();
		_small[..23].TryGetPngSize(out _).AssertFalse();
	}

	// A PNG whose first chunk is not the header chunk is not one a size can be read from.
	[TestMethod]
	public void TryGetPngSize_RefusesAPictureThatDoesNotOpenWithItsHeaderChunk()
	{
		var broken = (byte[])_small.Clone();
		broken[12] = (byte)'t';

		broken.IsPng().AssertTrue();
		broken.TryGetPngSize(out var size).AssertFalse();
		size.AssertEqual(Size.Empty);
	}

	[TestMethod]
	[DataRow(0, 2)]
	[DataRow(3, 0)]
	[DataRow(-1, 2)]
	[DataRow(3, -1)]
	public void TryGetPngSize_RefusesInvalidDimensions(int width, int height)
	{
		var header = _small[..24];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16), width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(20), height);

		AssertInvalidHeader(header);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(12)]
	[DataRow(14)]
	[DataRow(-1)]
	public void TryGetPngSize_RefusesInvalidHeaderChunkLength(int length)
	{
		var header = _small[..24];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8), length);

		AssertInvalidHeader(header);
	}

	[TestMethod]
	public void GetPngSize_LargeDimensions_DoNotRequireDecoding()
	{
		var header = _small[..24];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(16), int.MaxValue);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(20), int.MaxValue);

		header.GetPngSize().AssertEqual(new Size(int.MaxValue, int.MaxValue));
	}

	[TestMethod]
	public void GetPngSize_OfWhatIsNotAPng_Throws()
	{
		ThrowsExactly<InvalidDataException>(() => _jpeg.GetPngSize());
		ThrowsExactly<InvalidDataException>(() => Array.Empty<byte>().GetPngSize());
	}

	[TestMethod]
	public void ANullArray_IsRefused()
	{
		byte[] nothing = null;

		ThrowsExactly<ArgumentNullException>(() => nothing.IsPng());
		ThrowsExactly<ArgumentNullException>(() => nothing.TryGetPngSize(out _));
		ThrowsExactly<ArgumentNullException>(() => nothing.GetPngSize());
	}

	private static void AssertInvalidHeader(byte[] header)
	{
		header.TryGetPngSize(out var size).AssertFalse();
		size.AssertEqual(Size.Empty);
		((ReadOnlySpan<byte>)header).TryGetPngSize(out size).AssertFalse();
		size.AssertEqual(Size.Empty);

		ThrowsExactly<InvalidDataException>(() => header.GetPngSize());
		ThrowsExactly<InvalidDataException>(() => ((ReadOnlySpan<byte>)header).GetPngSize());
	}
}
