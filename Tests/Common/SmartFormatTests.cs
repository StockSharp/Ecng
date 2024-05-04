namespace Ecng.Tests.Common;

using System.Collections.Generic;
using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SmartFormat;

[TestClass]
public class SmartFormatTests
{
	[TestMethod]
    public void Plural()
    {
		var cultureCodes = new Dictionary<string, string>
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

		var formats = new Dictionary<string, string>
		{
			{ "en", "{0} {0:day|days}" },
			{ "ar", "{0} {0:يوم|يومان|أيام قليلة|أيام كثيرة|أيام|أيام}" },
			{ "bn", "{0} {0:দিন|দিন|দিন}" },
			{ "ca", "{0} {0:dia|dies}" },
			{ "cs", "{0} {0:den|dny|dnů}" },
			{ "da", "{0} {0:dag|dage|dage}" },
			{ "de", "{0} {0:Tag|Tage|Tage}" },
			{ "el", "{0} {0:ημέρα|ημέρες|ημέρες}" },
			{ "es", "{0} {0:día|días|días}" },
			{ "fa", "{0} {0:روز|روز|روز}" },
			{ "fi", "{0} {0:päivä|päivää|päivää}" },
			{ "fr", "{0} {0:jour|jours|jours}" },
			{ "he", "{0} {0:יום|ימים|ימים}" },
			{ "hi", "{0} {0:दिन|दिनों|दिनों}" },
			{ "hu", "{0} {0:nap|nap|napok}" },
			{ "it", "{0} {0:giorno|giorni|giorni}" },
			{ "ja", "{0} {0:日|日|日}" },
			{ "jv", "{0} {0:dina|dina|dina}" },
			{ "ko", "{0} {0:일|일|일}" },
			{ "ms", "{0} {0:hari|hari|hari}" },
			{ "nl", "{0} {0:dag|dagen|dagen}" },
			{ "no", "{0} {0:dag|dager|dager}" },
			{ "pa", "{0} {0:ਦਿਨ|ਦਿਨ|ਦਿਨ}" },
			{ "pl", "{0} {0:dzień|dni|dni}" },
			{ "pt", "{0} {0:dia|dias|dias}" },
			{ "ro", "{0} {0:zi|zile|zile}" },
			{ "ru", "{0} {0:день|дня|дней}" },
			{ "sk", "{0} {0:deň|dni|dní}" },
			{ "sr", "{0} {0:дан|дана|дана}" },
			{ "sv", "{0} {0:dag|dagar|dagar}" },
			{ "ta", "{0} {0:நாள்|நாட்கள்|நாட்கள்}" },
			{ "th", "{0} {0:วัน|วัน|วัน}" },
			{ "tr", "{0} {0:gün|gün|gün}" },
			{ "uk", "{0} {0:день|дні|днів}" },
			{ "uz", "{0} {0:kun|kun|kun}" },
			{ "ky", "{0} {0:күн|күн|күн}" },
			{ "vi", "{0} {0:ngày|ngày|ngày}" },
			{ "zh", "{0} {0:天|天|天}" },
		};

		var days = new[] { 0, 1, 2, 3, 5, 10, 20, 100, 1000 };

		foreach (var (key, code) in cultureCodes)
		{
			var ci = CultureInfo.GetCultureInfo(code);

			foreach (var day in days)
			{
				Smart.Format(ci, formats[key], day);
			}
		}
	}
}