namespace Ecng.Tests.Common;

[TestClass]
public class FastDateTimeParserTests : BaseTestClass
{
	[TestMethod]
	public void TwoDigitYear()
	{
		var parser = new FastDateTimeParser("yy-MM-dd");
		var dt = parser.Parse("98-01-01");

		dt.Year.AssertEqual(1998);
		dt.Month.AssertEqual(1);
		dt.Day.AssertEqual(1);
	}

	[TestMethod]
	public void ShortInput()
	{
		var parser = new FastDateTimeParser("yyyy-MM-dd HH:mm:ss.fff");
		var shortInput = "2024-01-02 03:04:0"; // too short for seconds+millis

		ThrowsExactly<FormatException>(() => parser.Parse(shortInput));
	}
}

