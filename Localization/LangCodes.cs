namespace Ecng.Localization;

using System;

public static class LangCodes
{
	public const string Ru = "ru";
	public const string En = "en";

	public static readonly string[] Codes = { En, Ru };

	public static int GetId(string langCode, bool throwErr = false)
	{
		return langCode switch
		{
			En => 0,
			Ru => 1,
			_ => !throwErr ? -1 : throw new ArgumentOutOfRangeException(nameof(langCode))
		};
	}
}