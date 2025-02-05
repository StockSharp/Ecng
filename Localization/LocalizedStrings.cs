namespace Ecng.Localization;

using Ecng.Common;

public static class LocalizedStrings
{
	public static ILocalizer Localizer { get; set; }

	public const string InheritedKey = nameof(Inherited);
	public const string VerboseKey = nameof(Verbose);
	public const string DebugKey = nameof(Debug);
	public const string InfoKey = nameof(Info);
	public const string WarningsKey = nameof(Warnings);
	public const string ErrorsKey = nameof(Errors);
	public const string OffKey = nameof(Off);
	public const string IdKey = nameof(Id);
	public const string LoggingKey = nameof(Logging);
	public const string NameKey = nameof(Name);
	public const string LogSourceNameKey = nameof(LogSourceName);
	public const string LogLevelKey = nameof(LogLevel);
	public const string LogLevelDescKey = nameof(LogLevelDesc);

	public static string Inherited => Localize(nameof(Inherited));
	public static string Verbose => Localize(nameof(Verbose));
	public static string Debug => Localize(nameof(Debug));
	public static string Info => Localize(nameof(Info));
	public static string Warnings => Localize(nameof(Warnings));
	public static string Errors => Localize(nameof(Errors));
	public static string Off => Localize(nameof(Off));
	public static string Id => Localize(nameof(Id));
	public static string Logging => Localize(nameof(Logging));
	public static string Name => Localize(nameof(Name));
	public static string LogSourceName => Localize(nameof(LogSourceName));
	public static string LogLevel => Localize(nameof(LogLevel));
	public static string LogLevelDesc => Localize(nameof(LogLevelDesc));

	public static string Localize(this string enStr)
		=> (Localizer?.Localize(enStr)).IsEmpty(enStr);
}