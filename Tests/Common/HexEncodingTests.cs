namespace Ecng.Tests.Common;

using System.Text;

[TestClass]
public class HexEncodingTests
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
		Assert.ThrowsExactly<ArgumentNullException>(() => enc.GetByteCount((string)null));
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
		bytes.SequenceEqual([(byte)0xA1, (byte)0xB2]).AssertTrue();

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
		enc.GetMaxByteCount(2).AssertEqual(4);
		enc.GetMaxCharCount(3).AssertEqual(1);

		var bytes = HexEncoding.GetBytes("A1Z".ToCharArray(), 0, 3, out var discard);
		discard.AssertEqual(1);
		bytes.SequenceEqual([(byte)0xA1]).AssertTrue();
	}
}