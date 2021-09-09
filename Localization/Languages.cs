namespace Ecng.Localization
{
	using System;

	[Obsolete("Use LangCodes code.")]
	public enum Languages
	{
		English,
		Russian,
		Chinese,
		Indian,
	}

	public static class LangCodes
	{
		public const string Ru = "ru";
		public const string En = "en";
	}
}