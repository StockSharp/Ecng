namespace Ecng.Localization;

using Ecng.Common;

public static class LocalizedStrings
{
	public static ILocalizer Localizer { get; set; }

	public const string InheritedKey = nameof(InheritedKey);
	public const string VerboseKey = nameof(VerboseKey);
	public const string DebugKey = nameof(DebugKey);
	public const string InfoKey = nameof(InfoKey);
	public const string WarningsKey = nameof(WarningsKey);
	public const string ErrorsKey = nameof(ErrorsKey);
	public const string OffKey = nameof(OffKey);
	public const string IdKey = nameof(IdKey);
	public const string LoggingKey = nameof(LoggingKey);
	public const string NameKey = nameof(NameKey);
	public const string LogSourceNameKey = nameof(LogSourceNameKey);
	public const string LogLevelKey = nameof(LogLevelKey);
	public const string LogLevelDescKey = nameof(LogLevelDescKey);

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