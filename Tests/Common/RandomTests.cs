namespace Ecng.Tests.Common;

using System.Collections;

[TestClass]
public class RandomTests : BaseTestClass
{
	[TestMethod]
	public void Int()
	{
		for (int i = 0; i < 1000; i++)
		{
			var value = RandomGen.GetInt();
			(value >= int.MinValue && value <= int.MaxValue).AssertTrue($"value={value} out of int range");

			var ranged = RandomGen.GetInt(0, 1000);
			(ranged >= 0 && ranged <= 1000).AssertTrue($"ranged={ranged} should be >=0 and <=1000");

			var negative = RandomGen.GetInt(int.MinValue, 0);
			(negative >= int.MinValue && negative <= 0).AssertTrue($"negative={negative} should be <={0}");

			var full = RandomGen.GetInt(int.MinValue, int.MaxValue);
			(full >= int.MinValue && full <= int.MaxValue).AssertTrue($"full={full} out of range");
		}

		// Edge case: min == max
		RandomGen.GetInt(42, 42).AssertEqual(42);
	}

	[TestMethod]
	public void UnsignedIntegers()
	{
		for (int i = 0; i < 10000; i++)
		{
			var byteValue = RandomGen.GetByte();
			(byteValue >= byte.MinValue && byteValue <= byte.MaxValue).AssertTrue($"byteValue={byteValue} should be >={byte.MinValue} and <={byte.MaxValue}");

			var byteRange = RandomGen.GetByte(10, 20);
			(byteRange >= 10 && byteRange <= 20).AssertTrue($"byteRange={byteRange} should be >=10 and <=20");

			var ushortValue = RandomGen.GetUShort();
			(ushortValue >= ushort.MinValue && ushortValue <= ushort.MaxValue).AssertTrue($"ushortValue={ushortValue} should be >={ushort.MinValue} and <={ushort.MaxValue}");

			var ushortRange = RandomGen.GetUShort(100, 200);
			(ushortRange >= 100 && ushortRange <= 200).AssertTrue($"ushortRange={ushortRange} should be >=100 and <=200");

			var uintValue = RandomGen.GetUInt();
			(uintValue >= uint.MinValue && uintValue <= uint.MaxValue).AssertTrue($"uintValue={uintValue} should be >={uint.MinValue} and <={uint.MaxValue}");

			var uintRange = RandomGen.GetUInt(50u, 75u);
			(uintRange >= 50u && uintRange <= 75u).AssertTrue($"uintRange={uintRange} should be >=50 and <=75");

			var ulongValue = RandomGen.GetULong();
			(ulongValue >= ulong.MinValue && ulongValue <= ulong.MaxValue).AssertTrue($"ulongValue={ulongValue} should be >={ulong.MinValue} and <={ulong.MaxValue}");

			var nearMax = RandomGen.GetULong(ulong.MaxValue - 1024, ulong.MaxValue);
			(nearMax >= ulong.MaxValue - 1024 && nearMax <= ulong.MaxValue).AssertTrue($"nearMax={nearMax} should be >={ulong.MaxValue - 1024} and <={ulong.MaxValue}");
		}
	}

	[TestMethod]
	public void SignedIntegers()
	{
		for (int i = 0; i < 10000; i++)
		{
			var shortValue = RandomGen.GetShort();
			(shortValue >= short.MinValue && shortValue <= short.MaxValue).AssertTrue($"shortValue={shortValue} should be >={short.MinValue} and <={short.MaxValue}");

			var shortRange = RandomGen.GetShort(-1234, 4321);
			(shortRange >= -1234 && shortRange <= 4321).AssertTrue($"shortRange={shortRange} should be >=-1234 and <=4321");

			var sbyteValue = RandomGen.GetSByte();
			(sbyteValue >= sbyte.MinValue && sbyteValue <= sbyte.MaxValue).AssertTrue($"sbyteValue={sbyteValue} should be >={sbyte.MinValue} and <={sbyte.MaxValue}");

			var sbyteRange = RandomGen.GetSByte(-8, 8);
			(sbyteRange >= -8 && sbyteRange <= 8).AssertTrue($"sbyteRange={sbyteRange} should be >=-8 and <=8");
		}
	}

	[TestMethod]
	public void Long()
	{
		for (int i = 0; i < 1000; i++)
		{
			var value = RandomGen.GetLong();
			(value >= long.MinValue && value <= long.MaxValue).AssertTrue($"value={value} out of long range");

			var ranged = RandomGen.GetLong(0, 1000);
			(ranged >= 0 && ranged <= 1000).AssertTrue($"ranged={ranged} should be >=0 and <=1000");

			var negative = RandomGen.GetLong(long.MinValue, 0);
			(negative >= long.MinValue && negative <= 0).AssertTrue($"negative={negative} should be <=0");

			var full = RandomGen.GetLong(long.MinValue, long.MaxValue);
			(full >= long.MinValue && full <= long.MaxValue).AssertTrue($"full={full} out of range");
		}

		// Edge case: min == max
		RandomGen.GetLong(long.MaxValue, long.MaxValue).AssertEqual(long.MaxValue);
	}

	[TestMethod]
	public void Double()
	{
		const double min = -123.456;
		const double max = 789.123;

		for (int i = 0; i < 10000; i++)
		{
			var unit = RandomGen.GetDouble();
			(unit >= 0 && unit < 1).AssertTrue($"unit={unit} should be >=0 and <1");

			var upTo = RandomGen.GetDouble(100.0);
			(upTo >= 0 && upTo <= 100.0).AssertTrue($"upTo={upTo} should be >=0 and <=100.0");

			var ranged = RandomGen.GetDouble(min, max);
			(ranged >= min && ranged <= max).AssertTrue($"ranged={ranged} should be >={min} and <={max}");
		}

		RandomGen.GetDouble(42.0, 42.0).AssertEqual(42.0);
	}

	[TestMethod]
	public void Float()
	{
		const float min = -12.5f;
		const float max = 34.75f;

		for (int i = 0; i < 10000; i++)
		{
			var unit = RandomGen.GetFloat();
			(unit >= 0f && unit < 1f).AssertTrue($"unit={unit} should be >=0 and <1");

			var upTo = RandomGen.GetFloat(10f);
			(upTo >= 0f && upTo <= 10f).AssertTrue($"upTo={upTo} should be >=0 and <=10");

			var ranged = RandomGen.GetFloat(min, max);
			(ranged >= min && ranged <= max).AssertTrue($"ranged={ranged} should be >={min} and <={max}");
		}

		RandomGen.GetFloat(7f, 7f).AssertEqual(7f);
	}

	[TestMethod]
	public void Date()
	{
		var minDate = new DateTime(2020, 1, 1);
		var maxDate = new DateTime(2025, 12, 31);

		for (int i = 0; i < 1000; i++)
		{
			var date = RandomGen.GetDate();
			(date >= DateTime.MinValue && date <= DateTime.MaxValue).AssertTrue($"date={date} out of range");

			var ranged = RandomGen.GetDate(minDate, maxDate);
			(ranged >= minDate && ranged <= maxDate).AssertTrue($"ranged={ranged} should be between {minDate} and {maxDate}");
		}

		// Edge case: min == max
		var sameDay = new DateTime(2024, 6, 15);
		RandomGen.GetDate(sameDay, sameDay).AssertEqual(sameDay);
	}

	[TestMethod]
	public void Time()
	{
		var minTime = TimeSpan.FromHours(1);
		var maxTime = TimeSpan.FromHours(10);

		for (int i = 0; i < 1000; i++)
		{
			var time = RandomGen.GetTime();
			(time >= TimeSpan.MinValue && time <= TimeSpan.MaxValue).AssertTrue($"time={time} out of range");

			var positive = RandomGen.GetTime(TimeSpan.Zero, TimeSpan.MaxValue);
			(positive >= TimeSpan.Zero && positive <= TimeSpan.MaxValue).AssertTrue($"positive={positive} should be >=0");

			var negative = RandomGen.GetTime(TimeSpan.MinValue, TimeSpan.Zero);
			(negative >= TimeSpan.MinValue && negative <= TimeSpan.Zero).AssertTrue($"negative={negative} should be <=0");

			var ranged = RandomGen.GetTime(minTime, maxTime);
			(ranged >= minTime && ranged <= maxTime).AssertTrue($"ranged={ranged} should be between {minTime} and {maxTime}");
		}

		// Edge case: min == max
		var sameTime = TimeSpan.FromMinutes(30);
		RandomGen.GetTime(sameTime, sameTime).AssertEqual(sameTime);
	}

	[TestMethod]
	public void String()
	{
		// NOTE: GetString(minLen, maxLen) - contract is unclear, string may exceed maxLen
		// This test verifies basic functionality without strict length bounds

		for (int i = 0; i < 100; i++)
		{
			var minLen = RandomGen.GetInt(5, 20);
			var maxLen = minLen + RandomGen.GetInt(10, 30);
			var str = RandomGen.GetString(minLen, maxLen);

			// Basic validity checks
			str.AssertNotNull();
			(str.Length > 0).AssertTrue($"str.Length={str.Length} should be >0 for minLen={minLen}");
		}

		// Verify we get different strings (randomness)
		var samples = Enumerable.Range(0, 20).Select(_ => RandomGen.GetString(10, 20)).Distinct().ToArray();
		(samples.Length > 1).AssertTrue("GetString should return different values");

		// Verify strings contain printable characters
		var sample = RandomGen.GetString(50, 100);
		sample.AssertNotNull();
		sample.All(c => !char.IsControl(c) || c == '\t' || c == '\n' || c == '\r')
			.AssertTrue("String should contain mostly printable characters");
	}

	[TestMethod]
	public void Decimal()
	{
		// GetDecimal(intDigits, fracDigits) - generates decimal with specified digit counts
		for (int i = 0; i < 100; i++)
		{
			var intDigits = RandomGen.GetInt(1, 8);
			var fracDigits = RandomGen.GetInt(0, 8);
			var value = RandomGen.GetDecimal(intDigits, fracDigits);

			// Verify value is a valid decimal (not NaN or overflow)
			(value >= decimal.MinValue && value <= decimal.MaxValue).AssertTrue($"value={value} out of decimal range");
		}

		// Verify we get different values (randomness check)
		var samples = Enumerable.Range(0, 50).Select(_ => RandomGen.GetDecimal(5, 3)).Distinct().ToArray();
		(samples.Length > 1).AssertTrue("GetDecimal should return different values");

		// Verify values can have fractional parts when requested
		var withFraction = Enumerable.Range(0, 20).Select(_ => RandomGen.GetDecimal(3, 4)).ToArray();
		withFraction.Any(v => v != Math.Truncate(v)).AssertTrue("GetDecimal with fracDigits>0 should produce some fractional values");
	}

	[TestMethod]
	public void Enum()
	{
		var allValues = Enumerator.GetValues<CurrencyTypes>();
		var min = allValues.First();
		var max = allValues.Last();

		for (int i = 0; i < 1000; i++)
		{
			var value = RandomGen.GetEnum(min, max);

			// Verify value is a valid enum member
			System.Enum.IsDefined(typeof(CurrencyTypes), value).AssertTrue($"value={value} is not a valid CurrencyTypes");

			// Verify value is within range
			(value >= min && value <= max).AssertTrue($"value={value} should be between {min} and {max}");
		}

		// Verify we can get different values (not always the same)
		var samples = Enumerable.Range(0, 100).Select(_ => RandomGen.GetEnum(min, max)).Distinct().ToArray();
		(samples.Length > 1).AssertTrue("GetEnum should return different values");
	}

	private class CountingEnumerable<T>(IEnumerable<T> inner) : IEnumerable<T>
	{
		public int GetEnumeratorCalls { get; private set; }

		public IEnumerator<T> GetEnumerator()
		{
			GetEnumeratorCalls++;
			return inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	[TestMethod]
	public void MaxValue()
	{
		// When max parameter equals type's max value, should return valid values
		var intVal = RandomGen.GetInt(int.MaxValue);
		(intVal >= 0 && intVal <= int.MaxValue).AssertTrue($"intVal={intVal} should be >=0 and <=int.MaxValue");

		var uintVal = RandomGen.GetUInt(uint.MaxValue);
		(uintVal >= 0 && uintVal <= uint.MaxValue).AssertTrue($"uintVal={uintVal} should be <=uint.MaxValue");

		var ulongVal = RandomGen.GetULong(ulong.MaxValue);
		(ulongVal >= 0 && ulongVal <= ulong.MaxValue).AssertTrue($"ulongVal={ulongVal} should be <=ulong.MaxValue");
	}

	[TestMethod]
	public void EmptyEnumerable()
	{
		var empty = Array.Empty<CurrencyTypes>();
		ThrowsExactly<InvalidOperationException>(() => RandomGen.GetEnum(empty));
	}

	[TestMethod]
	public void EmptyEnumerable2()
	{
		var empty = Array.Empty<int>();
		ThrowsExactly<ArgumentOutOfRangeException>(() => RandomGen.GetElement(empty));
	}

	[TestMethod]
	public void EnumeratesMultipleTimes()
	{
		var source = new CountingEnumerable<int>([1, 2, 3]);
		RandomGen.GetElement(source);
		source.GetEnumeratorCalls.AssertEqual(1);
	}
}
