namespace Ecng.Tests.Common;

using Ecng.Common;

/// <summary>
/// A source of randomness that can be asked for the same sequence twice.
/// </summary>
/// <remarks>
/// Randomness that cannot be repeated is fine for a nonce and wrong for anything whose result is
/// worth looking at again: an optimisation run, a generated series a backtest was measured on, an
/// allocation of somebody's money. Those need to be handed the source rather than reaching for a
/// global one, and the source needs to be able to start from a stated point.
/// </remarks>
[TestClass]
public class RandomProviderTests
{
	[TestMethod]
	public void SameSeed_SameSequence()
	{
		var a = new SeededRandomProvider(12345);
		var b = new SeededRandomProvider(12345);

		for (var i = 0; i < 100; i++)
		{
			a.Next(0, 1000).AssertEqual(b.Next(0, 1000));
			a.NextDouble().AssertEqual(b.NextDouble());
			a.NextLong(0, long.MaxValue).AssertEqual(b.NextLong(0, long.MaxValue));
		}
	}

	[TestMethod]
	public void DifferentSeeds_DifferentSequences()
	{
		var a = new SeededRandomProvider(1);
		var b = new SeededRandomProvider(2);

		var differed = false;

		for (var i = 0; i < 100 && !differed; i++)
			differed = a.Next(0, int.MaxValue) != b.Next(0, int.MaxValue);

		differed.AssertTrue("two seeds that differ must not walk the same path");
	}

	[TestMethod]
	public void SameSeed_SameBytes()
	{
		var a = new byte[64];
		var b = new byte[64];

		new SeededRandomProvider(777).NextBytes(a);
		new SeededRandomProvider(777).NextBytes(b);

		a.SequenceEqual(b).AssertTrue();
	}

	[TestMethod]
	public void Next_StaysWithinTheRangeAndReachesBothEnds()
	{
		var provider = new SeededRandomProvider(42);

		var sawMin = false;
		var sawMax = false;

		for (var i = 0; i < 10000; i++)
		{
			var value = provider.Next(5, 7);

			(value is >= 5 and <= 7).AssertTrue($"{value} is outside the range asked for");

			sawMin |= value == 5;
			sawMax |= value == 7;
		}

		// Inclusive on both ends: a caller asking for 5 to 7 means three values, not two.
		sawMin.AssertTrue("the lower bound must be reachable");
		sawMax.AssertTrue("the upper bound must be reachable");
	}

	[TestMethod]
	public void Next_SingleValueRange_IsThatValue()
	{
		var provider = new SeededRandomProvider(42);

		for (var i = 0; i < 10; i++)
			provider.Next(3, 3).AssertEqual(3);
	}

	[TestMethod]
	public void NextDouble_IsBetweenZeroAndOne()
	{
		var provider = new SeededRandomProvider(42);

		for (var i = 0; i < 1000; i++)
		{
			var value = provider.NextDouble();
			(value is >= 0 and < 1).AssertTrue($"{value} is outside [0, 1)");
		}
	}

	[TestMethod]
	public void NextLong_StaysWithinTheRange()
	{
		var provider = new SeededRandomProvider(42);

		for (var i = 0; i < 1000; i++)
		{
			var value = provider.NextLong(-100, 100);
			(value is >= -100 and <= 100).AssertTrue($"{value} is outside the range asked for");
		}
	}

	[TestMethod]
	public void Default_IsNotTiedToASeed()
	{
		// The unseeded provider is what code without a reproducibility requirement gets. Two of
		// them must not agree, or every process would draw the same "random" series.
		var a = new DefaultRandomProvider();
		var b = new DefaultRandomProvider();

		var differed = false;

		for (var i = 0; i < 100 && !differed; i++)
			differed = a.Next(0, int.MaxValue) != b.Next(0, int.MaxValue);

		differed.AssertTrue();
	}

	[TestMethod]
	public void Default_Instance_IsShared()
		=> DefaultRandomProvider.Instance.AssertSame(DefaultRandomProvider.Instance);

	[TestMethod]
	public void RandomArray_FromTheSameSeed_IsTheSameArray()
	{
		// The generators of order books, order logs and trades draw through RandomArray, so a
		// backtest measured on generated data could not be run twice on the same series. Handing
		// the array a source is what makes that possible.
		var a = new RandomArray<int>(1, 1000, 500, new SeededRandomProvider(2024));
		var b = new RandomArray<int>(1, 1000, 500, new SeededRandomProvider(2024));

		for (var i = 0; i < 500; i++)
			a.Next().AssertEqual(b.Next());
	}

	[TestMethod]
	public void RandomArray_WithoutASource_StillVaries()
	{
		// Left alone it behaves as it always did: two arrays with nobody holding a seed disagree.
		var a = new RandomArray<int>(1, int.MaxValue, 200);
		var b = new RandomArray<int>(1, int.MaxValue, 200);

		var differed = false;

		for (var i = 0; i < 200 && !differed; i++)
			differed = a.Next() != b.Next();

		differed.AssertTrue();
	}

	[TestMethod]
	public void RandomArray_RangeIsHonoured()
	{
		var array = new RandomArray<int>(5, 7, 1000, new SeededRandomProvider(1));

		for (var i = 0; i < 1000; i++)
		{
			var value = array.Next();
			(value is >= 5 and <= 7).AssertTrue($"{value} is outside the range asked for");
		}
	}

	[TestMethod]
	public void Next_InvertedRange_Throws()
		=> Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SeededRandomProvider(1).Next(10, 5));

	[TestMethod]
	public void NextBytes_NullBuffer_Throws()
		=> Assert.ThrowsExactly<ArgumentNullException>(() => new SeededRandomProvider(1).NextBytes(null));
}
