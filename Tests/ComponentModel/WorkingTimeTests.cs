namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;
using Ecng.Serialization;

[TestClass]
public class WorkingTimeTests : BaseTestClass
{
	#region WorkingTimePeriod

	[TestMethod]
	public void Period_Clone()
	{
		var period = new WorkingTimePeriod
		{
			Till = new DateTime(2025, 12, 31),
			Times = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(17))],
			SpecialDays = new Dictionary<DayOfWeek, Range<TimeSpan>[]>
			{
				[DayOfWeek.Saturday] = [new Range<TimeSpan>(TimeSpan.FromHours(10), TimeSpan.FromHours(14))]
			}
		};

		var clone = period.Clone();

		clone.Till.AssertEqual(period.Till);
		clone.Times.Count.AssertEqual(1);
		clone.Times[0].Min.AssertEqual(TimeSpan.FromHours(9));
		clone.Times[0].Max.AssertEqual(TimeSpan.FromHours(17));
		clone.SpecialDays.Count.AssertEqual(1);
		clone.SpecialDays[DayOfWeek.Saturday].Length.AssertEqual(1);

		// Verify deep clone (modifying clone doesn't affect original)
		clone.Times.Clear();
		period.Times.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Period_SaveLoad_Roundtrip()
	{
		var period = new WorkingTimePeriod
		{
			Till = new DateTime(2025, 6, 30),
			Times =
			[
				new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
				new Range<TimeSpan>(TimeSpan.FromHours(13), TimeSpan.FromHours(17))
			],
			SpecialDays = new Dictionary<DayOfWeek, Range<TimeSpan>[]>
			{
				[DayOfWeek.Friday] = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(15))]
			}
		};

		var storage = new SettingsStorage();
		period.Save(storage);

		var loaded = new WorkingTimePeriod();
		loaded.Load(storage);

		loaded.Till.AssertEqual(period.Till);
		loaded.Times.Count.AssertEqual(2);
		loaded.Times[0].Min.AssertEqual(TimeSpan.FromHours(9));
		loaded.Times[0].Max.AssertEqual(TimeSpan.FromHours(12));
		loaded.Times[1].Min.AssertEqual(TimeSpan.FromHours(13));
		loaded.Times[1].Max.AssertEqual(TimeSpan.FromHours(17));
		loaded.SpecialDays.Count.AssertEqual(1);
		loaded.SpecialDays[DayOfWeek.Friday][0].Min.AssertEqual(TimeSpan.FromHours(9));
		loaded.SpecialDays[DayOfWeek.Friday][0].Max.AssertEqual(TimeSpan.FromHours(15));
	}

	[TestMethod]
	public void Period_Times_NullThrows()
	{
		var period = new WorkingTimePeriod();
		ThrowsExactly<ArgumentNullException>(() => period.Times = null);
	}

	[TestMethod]
	public void Period_SpecialDays_NullThrows()
	{
		var period = new WorkingTimePeriod();
		ThrowsExactly<ArgumentNullException>(() => period.SpecialDays = null);
	}

	#endregion

	#region WorkingTime

	[TestMethod]
	public void WorkingTime_IsEnabled_DefaultFalse()
	{
		var wt = new WorkingTime();
		wt.IsEnabled.AssertFalse();
	}

	[TestMethod]
	public void WorkingTime_Periods_NullThrows()
	{
		var wt = new WorkingTime();
		ThrowsExactly<ArgumentNullException>(() => wt.Periods = null);
	}

	[TestMethod]
	public void WorkingTime_SpecialDays_NullThrows()
	{
		var wt = new WorkingTime();
		ThrowsExactly<ArgumentNullException>(() => wt.SpecialDays = null);
	}

	[TestMethod]
	public void WorkingTime_SpecialDays_DuplicateDatesThrow()
	{
		var wt = new WorkingTime();
		var dict = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			[new DateTime(2025, 1, 1)] = [],
		};

		// No exception for unique dates
		wt.SpecialDays = dict;

		// Duplicate dates (same Date, different time) via SpecialDays setter
		var duplicateDict = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			[new DateTime(2025, 5, 1, 0, 0, 0)] = [],
			[new DateTime(2025, 5, 1, 12, 0, 0)] = [],
		};
		ThrowsExactly<ArgumentException>(() => wt.SpecialDays = duplicateDict);
	}

	[TestMethod]
	public void WorkingTime_Clone()
	{
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2025, 12, 31),
					Times = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(17))]
				}
			],
		};
		wt.SpecialDays = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			[new DateTime(2025, 1, 1)] = []
		};

		var clone = wt.Clone();

		clone.IsEnabled.AssertTrue();
		clone.Periods.Count.AssertEqual(1);
		clone.Periods[0].Till.AssertEqual(new DateTime(2025, 12, 31));
		clone.SpecialDays.Count.AssertEqual(1);
		clone.SpecialDays.ContainsKey(new DateTime(2025, 1, 1)).AssertTrue();

		// Verify deep clone
		clone.Periods.Clear();
		wt.Periods.Count.AssertEqual(1);
	}

	[TestMethod]
	public void WorkingTime_SaveLoad_Roundtrip()
	{
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2025, 12, 31),
					Times = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(17))]
				}
			],
		};
		wt.SpecialDays = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			[new DateTime(2025, 5, 1)] = [],
			[new DateTime(2025, 5, 9)] = [new Range<TimeSpan>(TimeSpan.FromHours(10), TimeSpan.FromHours(14))]
		};

		var storage = new SettingsStorage();
		wt.Save(storage);

		var loaded = new WorkingTime();
		loaded.Load(storage);

		loaded.IsEnabled.AssertTrue();
		loaded.Periods.Count.AssertEqual(1);
		loaded.Periods[0].Till.AssertEqual(new DateTime(2025, 12, 31));
		loaded.Periods[0].Times.Count.AssertEqual(1);
		loaded.SpecialDays.Count.AssertEqual(2);
		loaded.SpecialDays[new DateTime(2025, 5, 1)].Length.AssertEqual(0);
		loaded.SpecialDays[new DateTime(2025, 5, 9)].Length.AssertEqual(1);
	}

	#endregion

	#region WorkingTimeExtensions

	[TestMethod]
	public void GetPeriod_FindsFirstMatching()
	{
		var wt = new WorkingTime
		{
			Periods =
			[
				new WorkingTimePeriod { Till = new DateTime(2025, 6, 30) },
				new WorkingTimePeriod { Till = new DateTime(2025, 12, 31) },
			]
		};

		var period = wt.GetPeriod(new DateTime(2025, 3, 1));
		period.AssertNotNull();
		period.Till.AssertEqual(new DateTime(2025, 6, 30));

		var period2 = wt.GetPeriod(new DateTime(2025, 7, 1));
		period2.AssertNotNull();
		period2.Till.AssertEqual(new DateTime(2025, 12, 31));

		var period3 = wt.GetPeriod(new DateTime(2026, 1, 1));
		period3.AssertNull();
	}

	[TestMethod]
	public void IsWorkingTime_Disabled_ReturnsTrue()
	{
		var wt = new WorkingTime { IsEnabled = false };

		wt.IsWorkingTime(DateTime.Now, out var isWorkingDay, out var period).AssertTrue();
		isWorkingDay.AssertNull();
		period.AssertNull();
	}

	[TestMethod]
	public void IsWorkingTime_InsideHours_ReturnsTrue()
	{
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2025, 12, 31),
					Times = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(17))]
				}
			]
		};

		// Monday at 10:00
		var dt = new DateTime(2025, 1, 6, 10, 0, 0);
		wt.IsWorkingTime(dt, out _, out var period).AssertTrue();
		period.AssertNotNull();
	}

	[TestMethod]
	public void IsWorkingTime_OutsideHours_ReturnsFalse()
	{
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2025, 12, 31),
					Times = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(17))]
				}
			]
		};

		// Monday at 20:00
		var dt = new DateTime(2025, 1, 6, 20, 0, 0);
		wt.IsWorkingTime(dt, out _, out _).AssertFalse();
	}

	[TestMethod]
	public void IsWorkingTime_Holiday_ReturnsFalse()
	{
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2025, 12, 31),
					Times = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(17))]
				}
			],
		};
		wt.SpecialDays = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			[new DateTime(2025, 1, 1)] = []
		};

		// Holiday
		var dt = new DateTime(2025, 1, 1, 10, 0, 0);
		wt.IsWorkingTime(dt, out var isWorkingDay, out _).AssertFalse();
		isWorkingDay.AssertEqual(false);
	}

	[TestMethod]
	public void IsWorkingTime_SpecialWorkDay_ReturnsCorrectly()
	{
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2025, 12, 31),
					Times = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(17))]
				}
			],
		};
		wt.SpecialDays = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			// Special shortened day
			[new DateTime(2025, 3, 7)] = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(14))]
		};

		// Inside special hours
		var dt1 = new DateTime(2025, 3, 7, 10, 0, 0);
		wt.IsWorkingTime(dt1, out var isWorkingDay1, out _).AssertTrue();
		isWorkingDay1.AssertEqual(true);

		// Outside special hours
		var dt2 = new DateTime(2025, 3, 7, 15, 0, 0);
		wt.IsWorkingTime(dt2, out var isWorkingDay2, out _).AssertFalse();
		isWorkingDay2.AssertEqual(true);
	}

	[TestMethod]
	public void IsWorkingDate_Holiday()
	{
		var wt = new WorkingTime { IsEnabled = true };
		wt.SpecialDays = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			[new DateTime(2025, 1, 1)] = []
		};

		wt.IsWorkingDate(new DateTime(2025, 1, 1)).AssertFalse();
	}

	[TestMethod]
	public void IsWorkingDate_SpecialWorkDay()
	{
		var wt = new WorkingTime { IsEnabled = true };
		wt.SpecialDays = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			// Saturday as working day
			[new DateTime(2025, 1, 4)] = [new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(14))]
		};

		wt.IsWorkingDate(new DateTime(2025, 1, 4)).AssertTrue();
	}

	[TestMethod]
	public void IsWorkingDate_Weekend()
	{
		var wt = new WorkingTime { IsEnabled = true };

		// Saturday Jan 4, 2025
		wt.IsWorkingDate(new DateTime(2025, 1, 4)).AssertFalse();
		// Sunday Jan 5, 2025
		wt.IsWorkingDate(new DateTime(2025, 1, 5)).AssertFalse();
		// Monday Jan 6, 2025
		wt.IsWorkingDate(new DateTime(2025, 1, 6)).AssertTrue();
	}

	[TestMethod]
	public void IsWorkingDate_Disabled_ReturnsTrue()
	{
		var wt = new WorkingTime { IsEnabled = false };

		// Even weekends return true when disabled
		wt.IsWorkingDate(new DateTime(2025, 1, 4)).AssertTrue();
	}

	#endregion

	#region Encode/Decode

	[TestMethod]
	public void EncodeDecode_Periods_Roundtrip()
	{
		var periods = new List<WorkingTimePeriod>
		{
			new()
			{
				Till = new DateTime(2025, 6, 30),
				Times =
				[
					new Range<TimeSpan>(TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
					new Range<TimeSpan>(TimeSpan.FromHours(13), TimeSpan.FromHours(17))
				]
			},
			new()
			{
				Till = new DateTime(2025, 12, 31),
				Times = [new Range<TimeSpan>(TimeSpan.FromHours(10), TimeSpan.FromHours(16))]
			}
		};

		var encoded = periods.EncodeToString();
		encoded.IsEmpty().AssertFalse();

		var decoded = encoded.DecodeToPeriods().ToArray();
		decoded.Length.AssertEqual(2);
		decoded[0].Till.AssertEqual(new DateTime(2025, 6, 30));
		decoded[0].Times.Count.AssertEqual(2);
		decoded[1].Till.AssertEqual(new DateTime(2025, 12, 31));
		decoded[1].Times.Count.AssertEqual(1);
	}

	[TestMethod]
	public void EncodeDecode_Empty_Roundtrip()
	{
		var periods = new List<WorkingTimePeriod>();
		var encoded = periods.EncodeToString();

		var decoded = encoded.DecodeToPeriods().ToArray();
		decoded.Length.AssertEqual(0);
	}

	[TestMethod]
	public void DecodeToSpecialDays_Roundtrip()
	{
		var encoded = "20250101=,20250509=09:00-14:00";
		var decoded = encoded.DecodeToSpecialDays();

		decoded.Count.AssertEqual(2);
		decoded[new DateTime(2025, 1, 1)].Length.AssertEqual(0);
		decoded[new DateTime(2025, 5, 9)].Length.AssertEqual(1);
		decoded[new DateTime(2025, 5, 9)][0].Min.AssertEqual(TimeSpan.FromHours(9));
		decoded[new DateTime(2025, 5, 9)][0].Max.AssertEqual(TimeSpan.FromHours(14));
	}

	[TestMethod]
	public void DecodeToPeriods_InvalidFormat_Throws()
	{
		ThrowsExactly<FormatException>(() => "invalid".DecodeToPeriods());
	}

	[TestMethod]
	public void DecodeToSpecialDays_InvalidFormat_Throws()
	{
		ThrowsExactly<FormatException>(() => "invalid".DecodeToSpecialDays());
	}

	#endregion

	#region EncodeToString / DecodeToSpecialDays

	[TestMethod]
	public void EncodeDecodeSpecialDays_Roundtrip()
	{
		var specialDays = new Dictionary<DateTime, Range<TimeSpan>[]>
		{
			{
				new DateTime(2025, 1, 1),
				new[] { new Range<TimeSpan>(new TimeSpan(10, 0, 0), new TimeSpan(14, 0, 0)) }
			},
		};

		var encoded = specialDays.EncodeToString();
		IsNotNull(encoded);
		IsFalse(encoded.IsEmpty());

		var decoded = encoded.DecodeToSpecialDays();
		decoded.Count.AssertEqual(1);
		IsTrue(decoded.ContainsKey(new DateTime(2025, 1, 1)));
		decoded[new DateTime(2025, 1, 1)].Length.AssertEqual(1);
	}

	[TestMethod]
	public void DecodeToSpecialDays_Empty_ReturnsEmpty()
	{
		var result = "".DecodeToSpecialDays();
		result.Count.AssertEqual(0);
	}

	#endregion

	#region GetPeriod

	[TestMethod]
	public void GetPeriod_FoundPeriod_ReturnsPeriod()
	{
		var wt = new WorkingTime
		{
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2025, 6, 1),
					Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
				},
				new WorkingTimePeriod
				{
					Till = new DateTime(2026, 1, 1),
					Times = [new Range<TimeSpan>(new TimeSpan(10, 0, 0), new TimeSpan(17, 0, 0))],
				},
			],
		};

		var period = wt.GetPeriod(new DateTime(2025, 3, 1));
		period.AssertNotNull();
		period.Till.AssertEqual(new DateTime(2025, 6, 1));
	}

	[TestMethod]
	public void GetPeriod_NoPeriod_ReturnsNull()
	{
		var wt = new WorkingTime
		{
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2020, 1, 1),
					Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
				},
			],
		};

		wt.GetPeriod(new DateTime(2025, 1, 1)).AssertNull();
	}

	#endregion
}
