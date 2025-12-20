namespace Ecng.Tests.Localization;

using Ecng.ComponentModel;
using Ecng.Localization;
using Ecng.Logging;

[TestClass]
[DoNotParallelize]
public class LocalizeTests : BaseTestClass
{
	private class DefaultLocalizer : ILocalizer
	{
		string ILocalizer.Localize(string enStr) => enStr;
		string ILocalizer.LocalizeByKey(string key) => key;
	}

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
	public void DefaultLocalization()
	{
		// Ensure we have a default (passthrough) localizer for this test
		LocalizedStrings.Localizer = new DefaultLocalizer();

		LocalizedStrings.Name.AssertEqual("Name");
		LogLevels.Warning.GetFieldDisplayName().AssertEqual("Warnings");
	}

	[TestMethod]
	public void CustomLocalization()
	{
		LocalizedStrings.Localizer = new MockLocalizer();

		LocalizedStrings.Name.AssertEqual("Имя");
		LogLevels.Warning.GetFieldDisplayName().AssertEqual("Варнинги");
	}

	[TestMethod]
	public void NullLocalizerThrowsException()
	{
		// The implementation explicitly rejects null localizers
		ThrowsExactly<ArgumentNullException>(() => LocalizedStrings.Localizer = null);
	}

	[TestMethod]
	public void LocalizerReturnsOriginalIfNotFound()
	{
		LocalizedStrings.Localizer = new MockLocalizer();

		var unknownKey = "SomeUnknownKey";
		unknownKey.Localize().AssertEqual(unknownKey);
	}
}