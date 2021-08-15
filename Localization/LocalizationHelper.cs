namespace Ecng.Localization
{
	using System;

	using Ecng.Common;
	using Ecng.Configuration;

	public static class LocalizationHelper
	{
		[Obsolete]
		public static string TranslateOld(this string text, Languages from = Languages.English, Languages? to = null)
		{
			var fromStr = from switch
			{
				Languages.English => LangCodes.En,
				Languages.Russian => LangCodes.Ru,
				Languages.Chinese => "ch",
				Languages.Indian => "in",
				_ => throw new ArgumentOutOfRangeException(nameof(from)),
			};

			var toStr = to switch
			{
				null => null,
				Languages.English => LangCodes.En,
				Languages.Russian => LangCodes.Ru,
				Languages.Chinese => "ch",
				Languages.Indian => "in",
				_ => throw new ArgumentOutOfRangeException(nameof(from)),
			};

			return text.Translate(fromStr, toStr);
		}

		public static string Translate(this string text, string from = LangCodes.En, string to = null)
		{
			var manager = ConfigManager.TryGetService<LocalizationManager>();

			if (manager is null)
				return text;

			if (to.IsEmpty())
				to = manager.ActiveLanguage;

			return manager.Translate(text, from, to);
		}
	}
}