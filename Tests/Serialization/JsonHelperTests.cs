namespace Ecng.Tests.Serialization;

using System.Text;

using Ecng.Serialization;

[TestClass]
public class JsonHelperTests
{
	private static readonly byte[] _payload = [0x31, 0x32, 0x33]; // "123"

	[TestMethod]
	public void Utf8_Preamble_Bytes_Are_EF_BB_BF()
	{
		var bom = Encoding.UTF8.GetPreamble();
		bom.Length.AssertEqual(3);
		bom[0].AssertEqual((byte)0xEF);
		bom[1].AssertEqual((byte)0xBB);
		bom[2].AssertEqual((byte)0xBF);
	}

	[TestMethod]
	public void SkipBom_ByteArray_WithUtf8Bom_RemovesPrefix()
	{
		var bom = Encoding.UTF8.GetPreamble();
		var input = new byte[bom.Length + _payload.Length];
		Array.Copy(bom, 0, input, 0, bom.Length);
		Array.Copy(_payload, 0, input, bom.Length, _payload.Length);

		var result = input.SkipBom();

		// When BOM exists, a new array is returned with payload only
		ReferenceEquals(input, result).AssertFalse();
		result.AssertEqual(_payload);
	}

	[TestMethod]
	public void SkipBom_ByteArray_WithoutBom_ReturnsSameInstance()
	{
		var result = _payload.SkipBom();
		ReferenceEquals(_payload, result).AssertTrue();
		result.AssertEqual(_payload);
	}

	[TestMethod]
	public void SkipBom_ByteArray_ShortArrays_Unchanged()
	{
		var a1 = Array.Empty<byte>();
		ReferenceEquals(a1, a1.SkipBom()).AssertTrue();

		var a2 = new byte[] { 0xEF };
		ReferenceEquals(a2, a2.SkipBom()).AssertTrue();

		var a3 = new byte[] { 0xEF, 0xBB };
		ReferenceEquals(a3, a3.SkipBom()).AssertTrue();
	}

	[TestMethod]
	public void SkipBom_ByteArray_FromBomCharStream_RemovesPrefix()
	{
		using var ms = new MemoryStream();
		// Use UTF8 without preamble; we will write the BOM char explicitly as content.
		using (var writer = new StreamWriter(ms, JsonHelper.UTF8NoBom, 1024, leaveOpen: true))
		{
			writer.Write('\uFEFF');
			writer.Write("123");
			writer.Flush();
		}

		var bytes = ms.ToArray();
		// Ensure the stream actually starts with BOM bytes then payload
		bytes.Length.AssertEqual(Encoding.UTF8.GetPreamble().Length + _payload.Length);

		var result = bytes.SkipBom();
		ReferenceEquals(bytes, result).AssertFalse();
		result.AssertEqual(_payload);
	}

	[TestMethod]
	public void SkipBom_String_WithBom_RemovesLeadingChar()
	{
		var withBom = "\uFEFFhello";
		var result = withBom.SkipBom();
		result.AreEqual("hello");
	}

	[TestMethod]
	public void SkipBom_String_WithoutBom_ReturnsSameReference()
	{
		var s = "hello";
		var result = s.SkipBom();
		ReferenceEquals(s, result).AssertTrue();
	}

	[TestMethod]
	public void SkipBom_String_NullOrEmpty_ReturnsOriginal()
	{
		string nullStr = null;
		nullStr.SkipBom().AssertNull();

		var empty = string.Empty;
		ReferenceEquals(empty, JsonHelper.SkipBom(empty)).AssertTrue();
	}

	[TestMethod]
	public void SkipBom_String_BomInMiddle_NotRemoved()
	{
		var s = "he\uFEFFllo";
		var result = s.SkipBom();
		ReferenceEquals(s, result).AssertTrue();
	}
}