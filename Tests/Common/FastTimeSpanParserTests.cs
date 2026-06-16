namespace Ecng.Tests.Common;

[TestClass]
public class FastTimeSpanParserTests : BaseTestClass
{
	[TestMethod]
	public void TrailingLiteral_PreservedInToString()
	{
		// A template ending in a literal must round-trip through ToString. The ctor used to
		// stop appending parts at the last placeholder, dropping the trailing 'Z'.
		var parser = new FastTimeSpanParser("hh:mm:ssZ");

		var ts = new TimeSpan(0, 1, 2, 3);

		parser.ToString(ts).AssertEqual("01:02:03Z");
	}
}
