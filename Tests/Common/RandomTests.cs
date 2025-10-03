namespace Ecng.Tests.Common;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

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
			(byteValue >= byte.MinValue && byteValue <= byte.MaxValue).AssertTrue();

			var byteRange = RandomGen.GetByte(10, 20);
			(byteRange >= 10 && byteRange <= 20).AssertTrue();

			var ushortValue = RandomGen.GetUShort();
			(ushortValue >= ushort.MinValue && ushortValue <= ushort.MaxValue).AssertTrue();

			var ushortRange = RandomGen.GetUShort(100, 200);
			(ushortRange >= 100 && ushortRange <= 200).AssertTrue();

			var uintValue = RandomGen.GetUInt();
			(uintValue >= uint.MinValue && uintValue <= uint.MaxValue).AssertTrue();

			var uintRange = RandomGen.GetUInt(50u, 75u);
			(uintRange >= 50u && uintRange <= 75u).AssertTrue();

			var ulongValue = RandomGen.GetULong();
			(ulongValue >= ulong.MinValue && ulongValue <= ulong.MaxValue).AssertTrue();

			var nearMax = RandomGen.GetULong(ulong.MaxValue - 1024, ulong.MaxValue);
			(nearMax >= ulong.MaxValue - 1024 && nearMax <= ulong.MaxValue).AssertTrue();
		}
	}

	[TestMethod]
	public void SignedIntegers()
	{
		for (int i = 0; i < 10000; i++)
		{
			var shortValue = RandomGen.GetShort();
			(shortValue >= short.MinValue && shortValue <= short.MaxValue).AssertTrue();

			var shortRange = RandomGen.GetShort(-1234, 4321);
			(shortRange >= -1234 && shortRange <= 4321).AssertTrue();

			var sbyteValue = RandomGen.GetSByte();
			(sbyteValue >= sbyte.MinValue && sbyteValue <= sbyte.MaxValue).AssertTrue();

			var sbyteRange = RandomGen.GetSByte(-8, 8);
			(sbyteRange >= -8 && sbyteRange <= 8).AssertTrue();
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
			(unit >= 0 && unit < 1).AssertTrue();

			var upTo = RandomGen.GetDouble(100.0);
			(upTo >= 0 && upTo <= 100.0).AssertTrue();

			var ranged = RandomGen.GetDouble(min, max);
			(ranged >= min && ranged <= max).AssertTrue();
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
			(unit >= 0f && unit < 1f).AssertTrue();

			var upTo = RandomGen.GetFloat(10f);
			(upTo >= 0f && upTo <= 10f).AssertTrue();

			var ranged = RandomGen.GetFloat(min, max);
			(ranged >= min && ranged <= max).AssertTrue();
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
