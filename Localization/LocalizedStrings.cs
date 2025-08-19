namespace Ecng.Localization;

using Ecng.Common;

/// <summary>
/// The localized strings.
/// </summary>
public static class LocalizedStrings
{
	/// <summary>
	/// The localizer.
	/// </summary>
	public static ILocalizer Localizer { get; set; }

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
	public static string Inherited => Localize(nameof(Inherited));
	/// <summary>
	/// </summary>
	public static string Verbose => Localize(nameof(Verbose));
	/// <summary>
	/// </summary>
	public static string Debug => Localize(nameof(Debug));
	/// <summary>
	/// </summary>
	public static string Info => Localize(nameof(Info));
	/// <summary>
	/// </summary>
	public static string Warnings => Localize(nameof(Warnings));
	/// <summary>
	/// </summary>
	public static string Errors => Localize(nameof(Errors));
	/// <summary>
	/// </summary>
	public static string Off => Localize(nameof(Off));
	/// <summary>
	/// </summary>
	public static string Id => Localize(nameof(Id));
	/// <summary>
	/// </summary>
	public static string Logging => Localize(nameof(Logging));
	/// <summary>
	/// </summary>
	public static string Name => Localize(nameof(Name));
	/// <summary>
	/// </summary>
	public static string LogSourceName => Localize(nameof(LogSourceName));
	/// <summary>
	/// </summary>
	public static string LogLevel => Localize(nameof(LogLevel));
	/// <summary>
	/// </summary>
	public static string LogLevelDesc => Localize(nameof(LogLevelDesc));
	/// <summary>
	/// </summary>
	public static string PreventWork => Localize(nameof(PreventWork));
	/// <summary>
	/// </summary>
	public static string PreventUpgrade => Localize(nameof(PreventUpgrade));

	/// <summary>
	/// Localizes the string.
	/// </summary>
	/// <param name="enStr">The string to localize on English.</param>
	/// <returns>The localized string.</returns>
	public static string Localize(this string enStr)
		=> (Localizer?.Localize(enStr)).IsEmpty(enStr);
}