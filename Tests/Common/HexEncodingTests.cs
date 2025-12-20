namespace Ecng.Tests.Common;

[TestClass]
public class HexEncodingTests : BaseTestClass
{
	[TestMethod]
	public void ConvertRoundTrip()
	{
		var hex = "48656C6C6F";
		var enc = new HexEncoding();
		var bytes = enc.GetBytes(hex);
		bytes.ASCII().AssertEqual("Hello");
		enc.GetString(bytes).AssertEqual(hex);
	}
	[TestMethod]
	public void StaticConversion()
	{
		var chars = "A1Z!".ToCharArray();
		var bytes = HexEncoding.GetBytes(chars, 0, chars.Length, out var discarded);
		discarded.AssertEqual(2);
		bytes.Length.AssertEqual(1);
		bytes[0].AssertEqual((byte)0xA1);
	}

	[TestMethod]
	public void Helpers()
	{
		HexEncoding.IsHexDigit('F').AssertTrue();
		HexEncoding.IsHexDigit('g').AssertFalse();
	}

	[TestMethod]
	public void GetByteCountString()
	{
		var enc = new HexEncoding();
		enc.GetByteCount("A1").AssertEqual(1);
		enc.GetByteCount("A1Z").AssertEqual(1);
		ThrowsExactly<ArgumentNullException>(() => enc.GetByteCount((string)null));
	}

	[TestMethod]
	public void CharArrayConversions()
	{
		var enc = new HexEncoding();
		var chars = "A1b2".ToCharArray();
		var count = enc.GetByteCount(chars, 0, chars.Length);
		count.AssertEqual(2);

		var bytes = new byte[2];
		enc.GetBytes(chars, 0, chars.Length, bytes, 0).AssertEqual(2);
		bytes.AssertEqual([(byte)0xA1, (byte)0xB2]);

		var charCnt = enc.GetCharCount(bytes, 0, bytes.Length);
		charCnt.AssertEqual(4);

		var dest = new char[4];
		enc.GetChars(bytes, 0, bytes.Length, dest, 0).AssertEqual(4);
		new string(dest).AssertEqual("A1B2");
	}

	[TestMethod]
	public void MaxCountsAndStatic()
	{
		var enc = new HexEncoding();
		// 2 hex chars = 1 byte, so GetMaxByteCount(2) = 1
		enc.GetMaxByteCount(2).AssertEqual(1);
		// 1 byte = 2 hex chars, so GetMaxCharCount(3) = 6
		enc.GetMaxCharCount(3).AssertEqual(6);

		var bytes = HexEncoding.GetBytes("A1Z".ToCharArray(), 0, 3, out var discard);
		discard.AssertEqual(1);
		bytes.AssertEqual([(byte)0xA1]);
	}

	[TestMethod]
	public void KnownVectors_BytesToHex()
	{
		var enc = new HexEncoding();

		// Single bytes - boundary values
		enc.GetString([0x00]).AssertEqual("00");
		enc.GetString([0xFF]).AssertEqual("FF");
		enc.GetString([0x0F]).AssertEqual("0F");
		enc.GetString([0xF0]).AssertEqual("F0");

		// Multiple bytes - known ASCII
		enc.GetString("Hello"u8.ToArray()).AssertEqual("48656C6C6F");
		enc.GetString("ABC"u8.ToArray()).AssertEqual("414243");

		// Sequential bytes
		enc.GetString([0x01, 0x02, 0x03]).AssertEqual("010203");
		enc.GetString([0xDE, 0xAD, 0xBE, 0xEF]).AssertEqual("DEADBEEF");
	}

	[TestMethod]
	public void KnownVectors_HexToBytes()
	{
		var enc = new HexEncoding();

		enc.GetBytes("00").AssertEqual([0x00]);
		enc.GetBytes("FF").AssertEqual([0xFF]);
		enc.GetBytes("ff").AssertEqual([0xFF]); // lowercase
		enc.GetBytes("DEADBEEF").AssertEqual([0xDE, 0xAD, 0xBE, 0xEF]);
		enc.GetBytes("deadbeef").AssertEqual([0xDE, 0xAD, 0xBE, 0xEF]);
	}

	[TestMethod]
	public void GetCharCount_ReturnsDoubleByteCount()
	{
		var enc = new HexEncoding();

		// Each byte produces exactly 2 hex characters
		enc.GetCharCount([0x00], 0, 1).AssertEqual(2);
		enc.GetCharCount([0x00, 0x00], 0, 2).AssertEqual(4);
		enc.GetCharCount(new byte[100], 0, 100).AssertEqual(200);
		enc.GetCharCount(new byte[1000], 0, 1000).AssertEqual(2000);
	}

	[TestMethod]
	public void LargeData_RoundTrip()
	{
		var enc = new HexEncoding();

		// Generate deterministic test data
		var original = new byte[10000];
		for (int i = 0; i < original.Length; i++)
			original[i] = (byte)(i % 256);

		var hex = enc.GetString(original);
		var decoded = enc.GetBytes(hex);

		decoded.AssertEqual(original);
	}

	[TestMethod]
	public void GetBytes_FiltersNonHexCharacters()
	{
		// Non-hex characters should be discarded
		// "A1 B2\tC3\nD4" = A1, space, B2, tab, C3, newline, D4
		// 8 hex chars (4 bytes) + 3 non-hex chars (discarded)
		var chars = "A1 B2\tC3\nD4".ToCharArray();
		var bytes = HexEncoding.GetBytes(chars, 0, chars.Length, out var discarded);

		discarded.AssertEqual(3); // space, tab, newline
		bytes.AssertEqual([0xA1, 0xB2, 0xC3, 0xD4]);
	}
}