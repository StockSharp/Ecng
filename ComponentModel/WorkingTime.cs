namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Ecng.Common;
using Ecng.Localization;
using Ecng.Serialization;

/// <summary>
/// Working schedule settings.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WorkScheduleKey,
	Description = LocalizedStrings.WorkScheduleDescKey)]
public class WorkingTime : Cloneable<WorkingTime>, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WorkingTime"/>.
	/// </summary>
	public WorkingTime()
	{
	}

	/// <summary>
	/// Whether the schedule is active.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ActiveKey,
		Description = LocalizedStrings.TaskOnKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public bool IsEnabled { get; set; }

	private List<WorkingTimePeriod> _periods = [];

	/// <summary>
	/// Schedule validity periods.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodsKey,
		Description = LocalizedStrings.PeriodsDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public List<WorkingTimePeriod> Periods
	{
		get => _periods;
		set => _periods = value ?? throw new ArgumentNullException(nameof(value));
	}

	private IDictionary<DateTime, Range<TimeSpan>[]> _specialDays = new Dictionary<DateTime, Range<TimeSpan>[]>();

	/// <summary>
	/// Special working days (holidays, shortened days, etc.).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SpecialDaysKey,
		Description = LocalizedStrings.SpecialDaysDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	public IDictionary<DateTime, Range<TimeSpan>[]> SpecialDays
	{
		get => _specialDays;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			CheckDates(value.Keys);

			_specialDays = value;
		}
	}

	/// <summary>
	/// Special working days (non-standard working days, e.g., Saturday working days).
	/// </summary>
	public IEnumerable<DateTime> SpecialWorkingDays
	{
		get => SpecialDays.Where(p => p.Value.Length > 0).Select(p => p.Key);
		set
		{
			var holidays = SpecialHolidays.ToHashSet();

			_specialDays = (value ?? throw new ArgumentNullException(nameof(value)))
				.Select(d => d.Date)
				.Concat(holidays)
				.Distinct()
				.ToDictionary(
					d => d,
					d => holidays.Contains(d) ? [] : SpecialDays.TryGetValue(d, out var r) ? r : [new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay)]);
		}
	}

	/// <summary>
	/// Special holidays.
	/// </summary>
	public IEnumerable<DateTime> SpecialHolidays
	{
		get => SpecialDays.Where(p => p.Value.Length == 0).Select(p => p.Key);
		set
		{
			var workDays = SpecialWorkingDays.ToHashSet();

			_specialDays = (value ?? throw new ArgumentNullException(nameof(value)))
				.Select(d => d.Date)
				.Concat(workDays)
				.Distinct()
				.ToDictionary(
					d => d,
					d => workDays.Contains(d) ? SpecialDays.TryGetValue(d, out var r) ? r : [new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay)] : []);
		}
	}

	private static void CheckDates(IEnumerable<DateTime> dates)
	{
		var unique = new HashSet<DateTime>();

		foreach (var date in dates)
		{
			if (!unique.Add(date.Date))
				throw new ArgumentException($"Collection has duplicate dates.");
		}
	}

	/// <inheritdoc />
	public override WorkingTime Clone()
	{
		return new()
		{
			IsEnabled = IsEnabled,
			_periods = [.. Periods.Select(p => p.Clone())],
			_specialDays = SpecialDays.ToDictionary(
				p => p.Key,
				p => p.Value.Select(r => r.Clone()).ToArray())
		};
	}

	/// <inheritdoc />
	public void Load(SettingsStorage storage)
	{
		IsEnabled = storage.GetValue<bool>(nameof(IsEnabled));

		Periods = [.. storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Periods)).Select(s =>
		{
			var p = new WorkingTimePeriod();
			p.Load(s);
			return p;
		})];

		_specialDays = storage.GetValue<IDictionary<string, SettingsStorage[]>>(nameof(SpecialDays))
			?.ToDictionary(
				p => p.Key.To<DateTime>(),
				p => p.Value.Select(s => s.ToRange<TimeSpan>()).ToArray())
			?? [];
	}

	/// <inheritdoc />
	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(IsEnabled), IsEnabled)
			.Set(nameof(Periods), Periods.Select(p =>
			{
				var s = new SettingsStorage();
				p.Save(s);
				return s;
			}).ToArray())
			.Set(nameof(SpecialDays), SpecialDays.ToDictionary(
				p => p.Key.To<string>(),
				p => p.Value.Select(r => r.ToStorage()).ToArray()));
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"Enabled={IsEnabled}, Periods={Periods.Count}, SpecialDays={SpecialDays.Count}";
}
