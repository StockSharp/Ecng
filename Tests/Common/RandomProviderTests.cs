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
	public void Default_SurvivesConcurrentUse()
	{
		// The shared source is drawn from by everything that has no seed to keep - generators,
		// strategies, several tests at once. System.Random is not thread-safe: concurrent draws
		// corrupt its state and it collapses towards returning zero, which shows up as "random"
		// values that are suddenly all the same. RandomGen has always guarded against this;
		// a provider handed round the same way has to as well.
		var trues = 0;
		var draws = 0;

		Parallel.For(0, 16, _ =>
		{
			var localTrues = 0;

			for (var i = 0; i < 25000; i++)
			{
				if (DefaultRandomProvider.Instance.GetBool())
					localTrues++;
			}

			Interlocked.Add(ref trues, localTrues);
			Interlocked.Add(ref draws, 25000);
		});

		var share = (double)trues / draws;
		(share is > 0.45 and < 0.55).AssertTrue($"{share:P1} of draws came back true - the shared source lost its state under threads");
	}

	[TestMethod]
	public void Seeded_SurvivesConcurrentUse()
	{
		// A seeded source shared between threads cannot promise the order draws come out in, but
		// it must still come out with values: a corrupted Random returns zero forever.
		var provider = new SeededRandomProvider(11);
		var zeros = 0;
		var draws = 0;

		Parallel.For(0, 16, _ =>
		{
			var localZeros = 0;

			for (var i = 0; i < 25000; i++)
			{
				var value = provider.Next(0, 1000);

				(value is >= 0 and <= 1000).AssertTrue($"{value} is outside the range asked for");

				if (value == 0)
					localZeros++;
			}

			Interlocked.Add(ref zeros, localZeros);
			Interlocked.Add(ref draws, 25000);
		});

		var share = (double)zeros / draws;
		(share < 0.01).AssertTrue($"{share:P1} of draws came back zero - the source lost its state under threads");
	}

	[TestMethod]
	public void Next_MaxValue_IsReachable()
	{
		// "Both ends included" has to hold at the top of the type too, or a caller asking for the
		// whole range silently never sees its last value. RandomGen has always reached it.
		var provider = new SeededRandomProvider(5);
		var reached = false;

		for (var i = 0; i < 10000 && !reached; i++)
			reached = provider.Next(int.MaxValue - 1, int.MaxValue) == int.MaxValue;

		reached.AssertTrue("int.MaxValue must be reachable");
	}

	[TestMethod]
	public void NextLong_MaxValue_IsReachable()
	{
		var provider = new SeededRandomProvider(5);
		var reached = false;

		for (var i = 0; i < 10000 && !reached; i++)
			reached = provider.NextLong(long.MaxValue - 1, long.MaxValue) == long.MaxValue;

		reached.AssertTrue("long.MaxValue must be reachable");
	}

	[TestMethod]
	public void GetULong_ReachesTheTopOfItsRange()
	{
		// Scaling by a value below one can never land on the upper bound, so a range of 0 to 9
		// quietly offered nine values rather than ten.
		var provider = new SeededRandomProvider(5);
		var reached = false;

		for (var i = 0; i < 10000 && !reached; i++)
			reached = provider.GetULong(0, 9) == 9;

		reached.AssertTrue("the upper bound must be reachable");
	}

	[TestMethod]
	public void GetULong_UsesTheWholeWidthOfTheType()
	{
		// A double carries 53 bits, so scaling one across the whole range leaves the bottom of
		// every value barely varying - draws that look random and are quietly coarse. Over 2000
		// values a uniform source shows nearly all 256 low bytes; the scaled one showed 14.
		var provider = new SeededRandomProvider(5);
		var lowBytes = new HashSet<byte>();

		for (var i = 0; i < 2000; i++)
			lowBytes.Add((byte)(provider.GetULong() & 0xFF));

		(lowBytes.Count > 200).AssertTrue($"only {lowBytes.Count} of 256 low bytes ever appeared");
	}

	[TestMethod]
	public void GetInt_IsNotNegative()
	{
		// The no-argument form means "a non-negative int", as it always has on RandomGen.
		var provider = new SeededRandomProvider(5);

		for (var i = 0; i < 1000; i++)
			(provider.GetInt() >= 0).AssertTrue("the no-argument form must not go negative");
	}

	[TestMethod]
	public void GetDouble_RejectsValuesThatAreNotNumbers()
	{
		var provider = new SeededRandomProvider(5);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => provider.GetDouble(double.NaN, 1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => provider.GetDouble(0, double.NaN));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => provider.GetDouble(double.NegativeInfinity, 1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => provider.GetDouble(0, double.PositiveInfinity));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => provider.GetDouble(1, 0));
	}

	[TestMethod]
	public void NextInclusive_ReachesBothEnds()
	{
		// The extension the providers are built on, exercised directly - it is public, so it is
		// somebody else's to call too.
		var random = new Random(9);
		var sawMin = false;
		var sawMax = false;

		for (var i = 0; i < 10000; i++)
		{
			var value = random.NextInclusive(5, 7);

			(value is >= 5 and <= 7).AssertTrue($"{value} is outside the range asked for");

			sawMin |= value == 5;
			sawMax |= value == 7;
		}

		sawMin.AssertTrue("the lower bound must be reachable");
		sawMax.AssertTrue("the upper bound must be reachable");
	}

	[TestMethod]
	public void NextInclusive_ReachesTheTopOfTheType()
	{
		var random = new Random(9);
		var reachedInt = false;
		var reachedLong = false;

		for (var i = 0; i < 10000 && !(reachedInt && reachedLong); i++)
		{
			reachedInt |= random.NextInclusive(int.MaxValue - 1, int.MaxValue) == int.MaxValue;
			reachedLong |= random.NextInclusive(long.MaxValue - 1, long.MaxValue) == long.MaxValue;
		}

		reachedInt.AssertTrue("int.MaxValue must be reachable");
		reachedLong.AssertTrue("long.MaxValue must be reachable");
	}

	[TestMethod]
	public void NextInclusive_InvertedRange_Throws()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Random(9).NextInclusive(10, 5));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Random(9).NextInclusive(10L, 5L));
	}

	[TestMethod]
	public void Next_InvertedRange_Throws()
		=> Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SeededRandomProvider(1).Next(10, 5));

	[TestMethod]
	public void NextBytes_NullBuffer_Throws()
		=> Assert.ThrowsExactly<ArgumentNullException>(() => new SeededRandomProvider(1).NextBytes(null));
}
