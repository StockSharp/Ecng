namespace Ecng.Common;

/// <summary>
/// Draws from a <see cref="Random"/> over a range that includes both of its ends.
/// </summary>
/// <remarks>
/// <see cref="Random"/> excludes its upper bound, and widening the range by one to include it
/// stops working at the top of the type - exactly where a caller asking for the whole range
/// needs it. Getting that right, and getting a value into a range without favouring part of it,
/// is the fiddly part of every source of randomness here, so it is written once.
///
/// Named apart from <see cref="Random.Next(int, int)"/> on purpose: an extension method cannot
/// win against an instance method of the same name, so one called Next would silently never be
/// the one called.
/// </remarks>
public static class RandomExtensions
{
	/// <summary>
	/// Draws sixty-four uniform bits.
	/// </summary>
	/// <param name="random">Source.</param>
	/// <returns>The bits.</returns>
	[CLSCompliant(false)]
	public static ulong NextUInt64(this Random random)
	{
		if (random is null)
			throw new ArgumentNullException(nameof(random));

		Span<byte> buffer = stackalloc byte[8];
		random.NextBytes(buffer);
		return BitConverter.ToUInt64(buffer);
	}

	/// <summary>
	/// Draws a value in the range, both ends included.
	/// </summary>
	/// <param name="random">Source.</param>
	/// <param name="min">Smallest value that may be returned.</param>
	/// <param name="max">Largest value that may be returned.</param>
	/// <returns>A value between <paramref name="min"/> and <paramref name="max"/>.</returns>
	public static long NextInclusive(this Random random, long min, long max)
	{
		if (random is null)
			throw new ArgumentNullException(nameof(random));

		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "The lower bound is above the upper one.");

		if (min == max)
			return min;

		// NextInt64 excludes its upper bound, so the span is widened by one to make the top value
		// reachable - which is what an inclusive range means.
		if (max != long.MaxValue)
			return random.NextInt64(min, max + 1);

		// At long.MaxValue there is no room to widen. A whole 64-bit value is drawn and reduced
		// into the span instead, discarding the tail that does not divide evenly - taking the
		// remainder alone would quietly favour the start of the range.
		var range = unchecked((ulong)max - (ulong)min);

		if (range == ulong.MaxValue)
			return unchecked((long)random.NextUInt64());

		range++;

		var limit = ulong.MaxValue - (ulong.MaxValue % range);

		ulong value;

		do
		{
			value = random.NextUInt64();
		}
		while (value >= limit);

		return unchecked((long)(value % range) + min);
	}

	/// <summary>
	/// Draws a value in the range, both ends included.
	/// </summary>
	/// <param name="random">Source.</param>
	/// <param name="min">Smallest value that may be returned.</param>
	/// <param name="max">Largest value that may be returned.</param>
	/// <returns>A value between <paramref name="min"/> and <paramref name="max"/>.</returns>
	public static int NextInclusive(this Random random, int min, int max)
	{
		if (random is null)
			throw new ArgumentNullException(nameof(random));

		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "The lower bound is above the upper one.");

		// Same widening as above; at int.MaxValue the wider type has the room this one lacks.
		return max == int.MaxValue ? (int)random.NextInclusive(min, (long)max) : random.Next(min, max + 1);
	}
}
