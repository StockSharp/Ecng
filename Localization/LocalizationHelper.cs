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
			string fromStr;

			switch (from)
			{
				case Languages.English:
					fromStr = LangCodes.En;
					break;
				case Languages.Russian:
					fromStr = LangCodes.Ru;
					break;
				case Languages.Chinese:
					fromStr = "ch";
					break;
				case Languages.Indian:
					fromStr = "in";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(from));
			}

			string toStr;

			switch (to)
			{
				case null:
					toStr = null;
					break;
				case Languages.English:
					toStr = LangCodes.En;
					break;
				case Languages.Russian:
					toStr = LangCodes.Ru;
					break;
				case Languages.Chinese:
					toStr = "ch";
					break;
				case Languages.Indian:
					toStr = "in";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(from));
			}

			return text.Translate(fromStr, toStr);
		}

		public static string Translate(this string text, string from = LangCodes.En, string to = null)
		{
			var manager = ConfigManager.TryGetService<LocalizationManager>();

			if (manager == null)
				return text;

			if (to.IsEmpty())
				to = manager.ActiveLanguage;

			return manager.Translate(text, from, to);
		}
	}
}