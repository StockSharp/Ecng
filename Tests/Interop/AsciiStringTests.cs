namespace Ecng.Tests.Interop;

using System.Text;

using Ecng.Interop;

[TestClass]
public class AsciiStringTests
{
	private readonly Encoding _ascii = Encoding.ASCII;

	[TestMethod]
	public void WriteAndReadAsciiString1()
	{
		var value = "A";
		var str1 = (AsciiString1)value;
		unsafe
		{
			var expectedBytes = _ascii.GetBytes(value);
			for (var i = 0; i < 1; i++)
				str1.Value[i].AssertEqual(expectedBytes[i]);
		}
		string result = str1;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadAsciiString16()
	{
		var value = "Hello, World!";
		var str16 = (AsciiString16)value;
		unsafe
		{
			var expectedBytes = _ascii.GetBytes(value);
			for (var i = 0; i < value.Length; i++)
				str16.Value[i].AssertEqual(expectedBytes[i]);
			for (var i = value.Length; i < 16; i++)
				str16.Value[i].AssertEqual((byte)0);
		}
		string result = str16;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadAsciiString32()
	{
		var value = "This is a longer test string!";
		var str32 = (AsciiString32)value;
		unsafe
		{
			var expectedBytes = _ascii.GetBytes(value);
			for (var i = 0; i < value.Length; i++)
				str32.Value[i].AssertEqual(expectedBytes[i]);
			for (var i = value.Length; i < 32; i++)
				str32.Value[i].AssertEqual((byte)0);
		}
		string result = str32;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadEmptyString()
	{
		var value = string.Empty;
		var str16 = (AsciiString16)value;
		unsafe
		{
			for (var i = 0; i < 16; i++)
				str16.Value[i].AssertEqual((byte)0);
		}
		string result = str16;
		result.AssertEqual(value, nullAsEmpty: true);
	}

	[TestMethod]
	public void WriteAndReadNullString()
	{
		string value = null;
		var str16 = (AsciiString16)value;
		unsafe
		{
			for (var i = 0; i < 16; i++)
				str16.Value[i].AssertEqual((byte)0);
		}
		string result = str16;
		result.AssertEqual(value, nullAsEmpty: true);
	}

	[TestMethod]
	public void WriteStringTooLongForAsciiString16()
	{
		var value = "This string is too long for AsciiString16!";
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			var s = (AsciiString16)value;
		});
	}

	[TestMethod]
	public void WriteAndReadAsciiString64()
	{
		var value = "This is a test string for AsciiString64.";
		var str64 = (AsciiString64)value;
		unsafe
		{
			var expectedBytes = _ascii.GetBytes(value);
			for (var i = 0; i < value.Length; i++)
				str64.Value[i].AssertEqual(expectedBytes[i]);
			for (var i = value.Length; i < 64; i++)
				str64.Value[i].AssertEqual((byte)0);
		}
		string result = str64;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadNonAsciiCharacters()
	{
		var value = "Hello©World"; // Contains non-ASCII character ©
		var str16 = (AsciiString16)value;
		unsafe
		{
			var expectedBytes = _ascii.GetBytes(value); // Non-ASCII characters are replaced with '?'
			for (var i = 0; i < value.Length; i++)
				str16.Value[i].AssertEqual(expectedBytes[i]);
		}
		string result = str16;
		result.AssertEqual(_ascii.GetString(_ascii.GetBytes(value))); // Expect ASCII-encoded string
	}
}