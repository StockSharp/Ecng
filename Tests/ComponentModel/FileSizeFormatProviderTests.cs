namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class FileSizeFormatProviderTests
{
	[TestMethod]
	public void GetFormat()
	{
		var provider = new FileSizeFormatProvider();
		((IFormatProvider)provider).GetFormat(typeof(ICustomFormatter)).AssertSame(provider);
		((IFormatProvider)provider).GetFormat(typeof(string)).AssertNull();
	}

	[TestMethod]
	public void FormatBytes()
	{
		var provider = new FileSizeFormatProvider();
		string.Format(provider, "{0:fs}", 512).AssertEqual("512.00b");
	}

	[TestMethod]
	public void FormatKilobytes()
	{
		var provider = new FileSizeFormatProvider();
		string.Format(provider, "{0:fs}", FileSizes.KB).AssertEqual("1.00kb");
	}

	[TestMethod]
	public void FormatMegabytes()
	{
		var provider = new FileSizeFormatProvider();
		string.Format(provider, "{0:fs}", FileSizes.MB).AssertEqual("1.00mb");
	}

	[TestMethod]
	public void FormatPrecision()
	{
		var provider = new FileSizeFormatProvider();
		string.Format(provider, "{0:fs1}", (decimal)FileSizes.MB * 1.5m).AssertEqual("1.5mb");
	}

	[TestMethod]
	public void NotNumber()
	{
		var provider = new FileSizeFormatProvider();
		string.Format(provider, "{0:fs}", "test").AssertEqual("test");
	}
}
