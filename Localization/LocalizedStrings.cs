namespace Ecng.Localization;

using System;

using Ecng.Common;

/// <summary>
/// The localized strings.
/// </summary>
public static class LocalizedStrings
{
	private class NullLocalizer : ILocalizer
	{
		string ILocalizer.Localize(string enStr) => enStr;
		string ILocalizer.LocalizeByKey(string key) => key;
	}

	private static ILocalizer _localizer = new NullLocalizer();

	/// <summary>
	/// The localizer.
	/// </summary>
	public static ILocalizer Localizer
	{
		get => _localizer;
		set => _localizer = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// </summary>
	public const string InheritedKey = nameof(Inherited);
	/// <summary>
	/// </summary>
	public const string VerboseKey = nameof(Verbose);
	/// <summary>
	/// </summary>
	public const string DebugKey = nameof(Debug);
	/// <summary>
	/// </summary>
	public const string InfoKey = nameof(Info);
	/// <summary>
	/// </summary>
	public const string WarningsKey = nameof(Warnings);
	/// <summary>
	/// </summary>
	public const string ErrorsKey = nameof(Errors);
	/// <summary>
	/// </summary>
	public const string OffKey = nameof(Off);
	/// <summary>
	/// </summary>
	public const string IdKey = nameof(Id);
	/// <summary>
	/// </summary>
	public const string LoggingKey = nameof(Logging);
	/// <summary>
	/// </summary>
	public const string NameKey = nameof(Name);
	/// <summary>
	/// </summary>
	public const string LogSourceNameKey = nameof(LogSourceName);
	/// <summary>
	/// </summary>
	public const string LogLevelKey = nameof(LogLevel);
	/// <summary>
	/// </summary>
	public const string LogLevelDescKey = nameof(LogLevelDesc);
	/// <summary>
	/// </summary>
	public const string PreventWorkKey = nameof(PreventWork);
	/// <summary>
	/// </summary>
	public const string PreventUpgradeKey = nameof(PreventUpgrade);

	/// <summary>
	/// </summary>
	public static string Inherited => LocalizeByKey(InheritedKey);
	/// <summary>
	/// </summary>
	public static string Verbose => LocalizeByKey(VerboseKey);
	/// <summary>
	/// </summary>
	public static string Debug => LocalizeByKey(DebugKey);
	/// <summary>
	/// </summary>
	public static string Info => LocalizeByKey(InfoKey);
	/// <summary>
	/// </summary>
	public static string Warnings => LocalizeByKey(WarningsKey);
	/// <summary>
	/// </summary>
	public static string Errors => LocalizeByKey(ErrorsKey);
	/// <summary>
	/// </summary>
	public static string Off => LocalizeByKey(OffKey);
	/// <summary>
	/// </summary>
	public static string Id => LocalizeByKey(IdKey);
	/// <summary>
	/// </summary>
	public static string Logging => LocalizeByKey(LoggingKey);
	/// <summary>
	/// </summary>
	public static string Name => LocalizeByKey(NameKey);
	/// <summary>
	/// </summary>
	public static string LogSourceName => LocalizeByKey(LogSourceNameKey);
	/// <summary>
	/// </summary>
	public static string LogLevel => LocalizeByKey(LogLevelKey);
	/// <summary>
	/// </summary>
	public static string LogLevelDesc => LocalizeByKey(LogLevelDescKey);
	/// <summary>
	/// </summary>
	public static string PreventWork => LocalizeByKey(PreventWorkKey);
	/// <summary>
	/// </summary>
	public static string PreventUpgrade => LocalizeByKey(PreventUpgradeKey);

	/// <summary>
	/// Localizes the string.
	/// </summary>
	/// <param name="enStr">The string to localize on English.</param>
	/// <returns>The localized string.</returns>
	public static string Localize(this string enStr)
		=> Localizer.Localize(enStr);

	/// <summary>
	/// Localizes the string.
	/// </summary>
	/// <param name="key">The key of the string to localize.</param>
	/// <returns>The localized string.</returns>
	public static string LocalizeByKey(this string key)
		=> Localizer.LocalizeByKey(key);
}