namespace Ecng.Tests.Interop;

using System.Text;

using Ecng.Interop;

[TestClass]
public class Utf8StringTests
{
	[TestMethod]
	public void WriteAndReadUtf8String1()
	{
		var value = "A";
		var str1 = (Utf8String1)value;
		unsafe
		{
			var expectedBytes = value.UTF8();
			for (var i = 0; i < 1; i++)
				str1.Value[i].AssertEqual(expectedBytes[i]);
		}
		string result = str1;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadUtf8String16()
	{
		var value = "Hello, World!";
		var str16 = (Utf8String16)value;
		unsafe
		{
			var expectedBytes = value.UTF8();
			for (var i = 0; i < expectedBytes.Length; i++)
				str16.Value[i].AssertEqual(expectedBytes[i]);
			for (var i = expectedBytes.Length; i < 16; i++)
				str16.Value[i].AssertEqual((byte)0);
		}
		string result = str16;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadUtf8String32()
	{
		var value = "This is a longer test string!";
		var str32 = (Utf8String32)value;
		unsafe
		{
			var expectedBytes = value.UTF8();
			for (var i = 0; i < expectedBytes.Length; i++)
				str32.Value[i].AssertEqual(expectedBytes[i]);
			for (var i = expectedBytes.Length; i < 32; i++)
				str32.Value[i].AssertEqual((byte)0);
		}
		string result = str32;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadEmptyString()
	{
		var value = string.Empty;
		var str16 = (Utf8String16)value;
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
		var str16 = (Utf8String16)value;
		unsafe
		{
			for (var i = 0; i < 16; i++)
				str16.Value[i].AssertEqual((byte)0);
		}
		string result = str16;
		result.AssertEqual(value, nullAsEmpty: true);
	}

	[TestMethod]
	public void WriteStringTooLongForUtf8String16()
	{
		var value = "This string is too long for Utf8String16!";
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			var s = (Utf8String16)value;
		});
	}

	[TestMethod]
	public void WriteAndReadUtf8String64()
	{
		var value = "This is a test string for Utf8String64.";
		var str64 = (Utf8String64)value;
		unsafe
		{
			var expectedBytes = value.UTF8();
			for (var i = 0; i < expectedBytes.Length; i++)
				str64.Value[i].AssertEqual(expectedBytes[i]);
			for (var i = expectedBytes.Length; i < 64; i++)
				str64.Value[i].AssertEqual((byte)0);
		}
		string result = str64;
		result.AssertEqual(value);
	}

	[TestMethod]
	public void WriteAndReadUnicodeCharacters()
	{
		var value = "Привет, 🌍!";
		var str19 = (Utf8String19)value;
		unsafe
		{
			var expectedBytes = value.UTF8();
			for (var i = 0; i < expectedBytes.Length; i++)
				str19.Value[i].AssertEqual(expectedBytes[i]);
		}
		string result = str19;
		result.AssertEqual(value);
	}
}