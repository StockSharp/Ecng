namespace Ecng.Tests.Common;

using System.Collections;

[TestClass]
public class RandomTests
{
	[TestMethod]
	public void Int()
	{
		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetInt();
			RandomGen.GetInt(0, 1000);
			RandomGen.GetInt(int.MinValue, 0);
			RandomGen.GetInt(int.MinValue, int.MaxValue);
		}
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
		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetLong();
			RandomGen.GetLong(0, 1000);
			RandomGen.GetLong(long.MinValue, 0);
			RandomGen.GetLong(long.MinValue, long.MaxValue);
		}

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
		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetDate();
			RandomGen.GetDate(new DateTime(2020, 1, 1), DateTime.UtcNow);
		}
	}

	[TestMethod]
	public void Time()
	{
		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetTime();
			RandomGen.GetTime(TimeSpan.Zero, TimeSpan.MaxValue);
			RandomGen.GetTime(TimeSpan.MinValue, TimeSpan.Zero);
			RandomGen.GetTime(TimeSpan.MinValue, TimeSpan.MaxValue);
		}
	}

	[TestMethod]
	public void String()
	{
		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetString(0, RandomGen.GetInt(0, 1000));
		}
	}

	[TestMethod]
	public void Decimal()
	{
		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetDecimal(RandomGen.GetInt(1, 8), RandomGen.GetInt(0, 8));
		}
	}

	[TestMethod]
	public void Enum()
	{
		var seed = Enumerator.GetValues<CurrencyTypes>();
		var max = seed.Last();

		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetEnum(default, max);
		}
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
		RandomGen.GetInt(int.MaxValue);
		RandomGen.GetUInt(uint.MaxValue);
		RandomGen.GetULong(ulong.MaxValue);
	}

	[TestMethod]
	public void EmptyEnumerable()
	{
		var empty = Array.Empty<CurrencyTypes>();
		Assert.ThrowsExactly<InvalidOperationException>(() => RandomGen.GetEnum(empty));
	}

	[TestMethod]
	public void EmptyEnumerable2()
	{
		var empty = Array.Empty<int>();
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomGen.GetElement(empty));
	}

	[TestMethod]
	public void EnumeratesMultipleTimes()
	{
		var source = new CountingEnumerable<int>([1, 2, 3]);
		RandomGen.GetElement(source);
		source.GetEnumeratorCalls.AssertEqual(1);
	}
}
