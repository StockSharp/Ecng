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
	public const string Line2Key = nameof(Line2);
	/// <summary>
	/// </summary>
	public const string NoGapLineKey = nameof(NoGapLine);
	/// <summary>
	/// </summary>
	public const string StepLineKey = nameof(StepLine);
	/// <summary>
	/// </summary>
	public const string BandKey = nameof(Band);
	/// <summary>
	/// </summary>
	public const string BandOneValueKey = nameof(BandOneValue);
	/// <summary>
	/// </summary>
	public const string DotStyleKey = nameof(DotStyle);
	/// <summary>
	/// </summary>
	public const string HistogramKey = nameof(Histogram);
	/// <summary>
	/// </summary>
	public const string BubbleKey = nameof(Bubble);
	/// <summary>
	/// </summary>
	public const string StackedBarKey = nameof(StackedBar);
	/// <summary>
	/// </summary>
	public const string DashedLineKey = nameof(DashedLine);
	/// <summary>
	/// </summary>
	public const string AreaKey = nameof(Area);
	/// <summary>
	/// </summary>
	public const string WorkScheduleKey = nameof(WorkSchedule);
	/// <summary>
	/// </summary>
	public const string WorkScheduleDescKey = nameof(WorkScheduleDesc);
	/// <summary>
	/// </summary>
	public const string ActiveKey = nameof(Active);
	/// <summary>
	/// </summary>
	public const string TaskOnKey = nameof(TaskOn);
	/// <summary>
	/// </summary>
	public const string GeneralKey = nameof(General);
	/// <summary>
	/// </summary>
	public const string PeriodsKey = nameof(Periods);
	/// <summary>
	/// </summary>
	public const string PeriodsDescKey = nameof(PeriodsDesc);
	/// <summary>
	/// </summary>
	public const string SpecialDaysKey = nameof(SpecialDays);
	/// <summary>
	/// </summary>
	public const string SpecialDaysDescKey = nameof(SpecialDaysDesc);
	/// <summary>
	/// </summary>
	public const string ScheduleKey = nameof(Schedule);
	/// <summary>
	/// </summary>
	public const string ScheduleValidityPeriodKey = nameof(ScheduleValidityPeriod);
	/// <summary>
	/// </summary>
	public const string TillKey = nameof(Till);
	/// <summary>
	/// </summary>
	public const string WorkingTimeTillKey = nameof(WorkingTimeTill);
	/// <summary>
	/// </summary>
	public const string WorkScheduleDayKey = nameof(WorkScheduleDay);

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
	/// </summary>
	public static string Line2 => LocalizeByKey(Line2Key);
	/// <summary>
	/// </summary>
	public static string NoGapLine => LocalizeByKey(NoGapLineKey);
	/// <summary>
	/// </summary>
	public static string StepLine => LocalizeByKey(StepLineKey);
	/// <summary>
	/// </summary>
	public static string Band => LocalizeByKey(BandKey);
	/// <summary>
	/// </summary>
	public static string BandOneValue => LocalizeByKey(BandOneValueKey);
	/// <summary>
	/// </summary>
	public static string DotStyle => LocalizeByKey(DotStyleKey);
	/// <summary>
	/// </summary>
	public static string Histogram => LocalizeByKey(HistogramKey);
	/// <summary>
	/// </summary>
	public static string Bubble => LocalizeByKey(BubbleKey);
	/// <summary>
	/// </summary>
	public static string StackedBar => LocalizeByKey(StackedBarKey);
	/// <summary>
	/// </summary>
	public static string DashedLine => LocalizeByKey(DashedLineKey);
	/// <summary>
	/// </summary>
	public static string Area => LocalizeByKey(AreaKey);
	/// <summary>
	/// </summary>
	public static string WorkSchedule => LocalizeByKey(WorkScheduleKey);
	/// <summary>
	/// </summary>
	public static string WorkScheduleDesc => LocalizeByKey(WorkScheduleDescKey);
	/// <summary>
	/// </summary>
	public static string Active => LocalizeByKey(ActiveKey);
	/// <summary>
	/// </summary>
	public static string TaskOn => LocalizeByKey(TaskOnKey);
	/// <summary>
	/// </summary>
	public static string General => LocalizeByKey(GeneralKey);
	/// <summary>
	/// </summary>
	public static string Periods => LocalizeByKey(PeriodsKey);
	/// <summary>
	/// </summary>
	public static string PeriodsDesc => LocalizeByKey(PeriodsDescKey);
	/// <summary>
	/// </summary>
	public static string SpecialDays => LocalizeByKey(SpecialDaysKey);
	/// <summary>
	/// </summary>
	public static string SpecialDaysDesc => LocalizeByKey(SpecialDaysDescKey);
	/// <summary>
	/// </summary>
	public static string Schedule => LocalizeByKey(ScheduleKey);
	/// <summary>
	/// </summary>
	public static string ScheduleValidityPeriod => LocalizeByKey(ScheduleValidityPeriodKey);
	/// <summary>
	/// </summary>
	public static string Till => LocalizeByKey(TillKey);
	/// <summary>
	/// </summary>
	public static string WorkingTimeTill => LocalizeByKey(WorkingTimeTillKey);
	/// <summary>
	/// </summary>
	public static string WorkScheduleDay => LocalizeByKey(WorkScheduleDayKey);

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