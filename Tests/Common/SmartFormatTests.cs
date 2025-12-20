namespace Ecng.Tests.Common;

using System.Globalization;

using SmartFormat;

[TestClass]
public class SmartFormatTests
{
	private static readonly Dictionary<string, string> CultureCodes = new()
	{
		{ "en", "en-US" },
		{ "ar", "ar-SA" },
		{ "bn", "bn-BD" },
		{ "ca", "ca-ES" },
		{ "cs", "cs-CZ" },
		{ "da", "da-DK" },
		{ "de", "de-DE" },
		{ "el", "el-GR" },
		{ "es", "es-ES" },
		{ "fa", "fa-IR" },
		{ "fi", "fi-FI" },
		{ "fr", "fr-FR" },
		{ "he", "he-IL" },
		{ "hi", "hi-IN" },
		{ "hu", "hu-HU" },
		{ "it", "it-IT" },
		{ "ja", "ja-JP" },
		{ "jv", "jv-ID" },
		{ "ko", "ko-KR" },
		{ "ms", "ms-MY" },
		{ "nl", "nl-NL" },
		{ "no", "no-NO" },
		{ "pa", "pa-IN" },
		{ "pl", "pl-PL" },
		{ "pt", "pt-BR" },
		{ "ro", "ro-RO" },
		{ "ru", "ru-RU" },
		{ "sk", "sk-SK" },
		{ "sr", "sr-RS" },
		{ "sv", "sv-SE" },
		{ "ta", "ta-IN" },
		{ "th", "th-TH" },
		{ "tr", "tr-TR" },
		{ "uk", "uk-UA" },
		{ "uz", "uz-UZ" },
		{ "ky", "ky-KG" },
		{ "vi", "vi-VN" },
		{ "zh", "zh-CN" },
	};

	private static readonly Dictionary<string, string> Formats = new()
	{
		{ "en", "{0} {0:day|days}" },
		{ "ar", "{0} {0:يوم|يومان|أيام قليلة|أيام كثيرة|أيام|أيام}" },
		{ "bn", "{0} {0:দিন|দিন}" },
		{ "ca", "{0} {0:dia|dies}" },
		{ "cs", "{0} {0:den|dny|dnů}" },
		{ "da", "{0} {0:dag|dage}" },
		{ "de", "{0} {0:Tag|Tage}" },
		{ "el", "{0} {0:ημέρα|ημέρες}" },
		{ "es", "{0} {0:día|días}" },
		{ "fa", "{0} {0:روز|روزها}" },
		{ "fi", "{0} {0:päivä|päivää}" },
		{ "fr", "{0} {0:jour|jours}" },
		{ "he", "{0} {0:יום|ימים}" },
		{ "hi", "{0} {0:दिन|दिनों}" },
		{ "hu", "{0} {0:nap|napok}" },
		{ "it", "{0} {0:giorno|giorni}" },
		{ "ja", "{0} {0:日|日}" },
		{ "jv", "{0} {0:dina|dina}" },
		{ "ko", "{0} {0:일|일}" },
		{ "ms", "{0} {0:hari|hari}" },
		{ "nl", "{0} {0:dag|dagen}" },
		{ "no", "{0} {0:dag|dager}" },
		{ "pa", "{0} {0:ਦਿਨ|ਦਿਨ}" },
		{ "pl", "{0} {0:dzień|dni|dni}" },
		{ "pt", "{0} {0:dia|dias}" },
		{ "ro", "{0} {0:zi|zile|zile}" },
		{ "ru", "{0} {0:день|дня|дней}" },
		{ "sk", "{0} {0:deň|dni|dní}" },
		{ "sr", "{0} {0:дан|дана|дана}" },
		{ "sv", "{0} {0:dag|dagar}" },
		{ "ta", "{0} {0:நாள்|நாட்கள்}" },
		{ "th", "{0} {0:วัน|วัน}" },
		{ "tr", "{0} {0:gün|günler}" },
		{ "uk", "{0} {0:день|дні|днів}" },
		{ "uz", "{0} {0:kun|kun}" },
		{ "ky", "{0} {0:күн|күн}" },
		{ "vi", "{0} {0:ngày|ngày}" },
		{ "zh", "{0} {0:天|天}" },
	};

	[TestMethod]
	public void PluralEnglish()
	{
		var ci = CultureInfo.GetCultureInfo(CultureCodes["en"]);
		var format = Formats["en"];

		Smart.Format(ci, format, 0).AssertEqual("0 days");
		Smart.Format(ci, format, 1).AssertEqual("1 day");
		Smart.Format(ci, format, 2).AssertEqual("2 days");
		Smart.Format(ci, format, 5).AssertEqual("5 days");
		Smart.Format(ci, format, 21).AssertEqual("21 days");
		Smart.Format(ci, format, 100).AssertEqual("100 days");
	}

	[TestMethod]
	public void PluralRussian()
	{
		var ci = CultureInfo.GetCultureInfo(CultureCodes["ru"]);
		var format = Formats["ru"];

		Smart.Format(ci, format, 0).AssertEqual("0 дней");
		Smart.Format(ci, format, 1).AssertEqual("1 день");
		Smart.Format(ci, format, 2).AssertEqual("2 дня");
		Smart.Format(ci, format, 5).AssertEqual("5 дней");
		Smart.Format(ci, format, 11).AssertEqual("11 дней");
		Smart.Format(ci, format, 21).AssertEqual("21 день");
		Smart.Format(ci, format, 22).AssertEqual("22 дня");
		Smart.Format(ci, format, 25).AssertEqual("25 дней");
	}

	[TestMethod]
	public void PluralGerman()
	{
		var ci = CultureInfo.GetCultureInfo(CultureCodes["de"]);
		var format = Formats["de"];

		Smart.Format(ci, format, 0).AssertEqual("0 Tage");
		Smart.Format(ci, format, 1).AssertEqual("1 Tag");
		Smart.Format(ci, format, 2).AssertEqual("2 Tage");
		Smart.Format(ci, format, 5).AssertEqual("5 Tage");
	}

	[TestMethod]
	public void PluralPolish()
	{
		var ci = CultureInfo.GetCultureInfo(CultureCodes["pl"]);
		var format = Formats["pl"];

		Smart.Format(ci, format, 0).AssertEqual("0 dni");
		Smart.Format(ci, format, 1).AssertEqual("1 dzień");
		Smart.Format(ci, format, 2).AssertEqual("2 dni");
		Smart.Format(ci, format, 5).AssertEqual("5 dni");
		Smart.Format(ci, format, 22).AssertEqual("22 dni");
	}

	[TestMethod]
	public void PluralAllLanguagesNoExceptions()
	{
		foreach (var (key, code) in CultureCodes)
		{
			var ci = CultureInfo.GetCultureInfo(code);
			var format = Formats[key];

			for (var day = 0; day < 100; day++)
			{
				var result = Smart.Format(ci, format, day);
				result.AssertNotNull();
				result.Length.AssertGreater(0);
				result.AssertContains(day.ToString());
			}
		}
	}

	[TestMethod]
	public void PluralResultContainsNumber()
	{
		foreach (var (key, code) in CultureCodes)
		{
			var ci = CultureInfo.GetCultureInfo(code);
			var format = Formats[key];

			var result = Smart.Format(ci, format, 42);
			result.AssertContains("42");
		}
	}
}