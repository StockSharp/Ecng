namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Ecng.Common;
using Ecng.Localization;
using Ecng.Serialization;

/// <summary>
/// Schedule validity period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ScheduleValidityPeriodKey,
	Description = LocalizedStrings.ScheduleValidityPeriodKey)]
public class WorkingTimePeriod : Cloneable<WorkingTimePeriod>, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WorkingTimePeriod"/>.
	/// </summary>
	public WorkingTimePeriod()
	{
	}

	/// <summary>
	/// Schedule expiration date.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TillKey,
		Description = LocalizedStrings.WorkingTimeTillKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public DateTime Till { get; set; }

	private List<Range<TimeSpan>> _times = [];

	/// <summary>
	/// Schedule working time intervals within a day.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ScheduleKey,
		Description = LocalizedStrings.ScheduleKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 1)]
	public List<Range<TimeSpan>> Times
	{
		get => _times;
		set => _times = value ?? throw new ArgumentNullException(nameof(value));
	}

	private IDictionary<DayOfWeek, Range<TimeSpan>[]> _specialDays = new Dictionary<DayOfWeek, Range<TimeSpan>[]>();

	/// <summary>
	/// Special working days with custom schedules by day of week.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SpecialDaysKey,
		Description = LocalizedStrings.SpecialDaysDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	public IDictionary<DayOfWeek, Range<TimeSpan>[]> SpecialDays
	{
		get => _specialDays;
		set => _specialDays = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public override WorkingTimePeriod Clone()
	{
		return new WorkingTimePeriod
		{
			Till = Till,
			Times = [.. Times.Select(t => t.Clone())],
			SpecialDays = SpecialDays.ToDictionary(
				p => p.Key,
				p => p.Value.Select(r => r.Clone()).ToArray()),
		};
	}

	/// <inheritdoc />
	public void Load(SettingsStorage storage)
	{
		Till = storage.GetValue<DateTime>(nameof(Till));
		Times = [.. storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Times)).Select(s => s.ToRange<TimeSpan>())];
		SpecialDays = storage.GetValue<IDictionary<string, SettingsStorage[]>>(nameof(SpecialDays))
			?.ToDictionary(
				p => p.Key.To<DayOfWeek>(),
				p => p.Value.Select(s => s.ToRange<TimeSpan>()).ToArray())
			?? [];
	}

	/// <inheritdoc />
	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Till), Till)
			.Set(nameof(Times), Times.Select(t => t.ToStorage()).ToArray())
			.Set(nameof(SpecialDays), SpecialDays.ToDictionary(
				p => p.Key.To<string>(),
				p => p.Value.Select(r => r.ToStorage()).ToArray()));
	}

	/// <inheritdoc />
	public override string ToString() => Till.ToString();
}
