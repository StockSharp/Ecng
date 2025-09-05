namespace Ecng.Tests.Localization;

using Ecng.ComponentModel;
using Ecng.Localization;
using Ecng.Logging;

[TestClass]
public class LocalizeTests
{
	private class MockLocalizer : ILocalizer
	{
		string ILocalizer.Localize(string enStr)
		{
			if (enStr == "Warnings")
				return "Варнинги";
			else if (enStr == "Name")
				return "Имя";

			return enStr;
		}

		string ILocalizer.LocalizeByKey(string key)
		{
			if (key == "Warnings")
				return "Варнинги";
			else if (key == "Name")
				return "Имя";

			return key;
		}
	}

	[TestMethod]
	public void TestLocalize()
	{
		LocalizedStrings.Name.AssertEqual("Name");
		LogLevels.Warning.GetFieldDisplayName().AssertEqual("Warnings");
		
		LocalizedStrings.Localizer = new MockLocalizer();

		LocalizedStrings.Name.AssertEqual("Имя");
		LogLevels.Warning.GetFieldDisplayName().AssertEqual("Варнинги");
	}
}