namespace Ecng.Localization
{
	public static class LocalizationHelper
	{
		public static LocalizationManager DefaultManager { get; set; } = new LocalizationManager();

		public static string Translate(this string text, Languages? from = null, Languages? to = null)
		{
			var manager = DefaultManager;

			if (manager == null)
				return text;

			return manager.Translate(text, from ?? Languages.English, to ?? manager.ActiveLanguage);
		}

		public const string Ru = "ru-RU";
		public const string En = "en-US";
	}
}