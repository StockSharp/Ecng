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
	public void Long()
	{
		for (int i = 0; i < 10000; i++)
		{
			RandomGen.GetLong();
			RandomGen.GetLong(0, 1000);
			RandomGen.GetLong(long.MinValue, 0);
			RandomGen.GetLong(long.MinValue, long.MaxValue);
		}
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