namespace Ecng.Common;

/// <summary>
/// A source of random values.
/// </summary>
/// <remarks>
/// Taken as a dependency by anything whose result is worth reproducing - an optimisation run, a
/// generated series a measurement was taken on, an allocation - so that the same run can be asked
/// for twice. Code with no such need can keep using <see cref="RandomGen"/>, which is this with
/// nobody holding the seed.
///
/// Deliberately small: everything else - a decimal in a range, a bool, an element of a collection,
/// a value of an enum - is built from these four, so an implementation has four things to get right
/// rather than fifty.
/// </remarks>
public interface IRandomProvider
{
	/// <summary>
	/// Returns a value in the range, both ends included.
	/// </summary>
	/// <param name="min">Smallest value that may be returned.</param>
	/// <param name="max">Largest value that may be returned.</param>
	/// <returns>A value between <paramref name="min"/> and <paramref name="max"/>.</returns>
	int Next(int min, int max);

	/// <summary>
	/// Returns a value in the range, both ends included.
	/// </summary>
	/// <param name="min">Smallest value that may be returned.</param>
	/// <param name="max">Largest value that may be returned.</param>
	/// <returns>A value between <paramref name="min"/> and <paramref name="max"/>.</returns>
	long NextLong(long min, long max);

	/// <summary>
	/// Returns a value in [0, 1).
	/// </summary>
	/// <returns>A value from zero up to but not including one.</returns>
	double NextDouble();

	/// <summary>
	/// Fills the buffer with random bytes.
	/// </summary>
	/// <param name="buffer">Buffer to fill.</param>
	void NextBytes(byte[] buffer);
}

/// <summary>
/// A source that starts from a stated point, so the same series can be had again.
/// </summary>
/// <param name="seed">Where the series starts.</param>
public class SeededRandomProvider(int seed) : IRandomProvider
{
	private readonly Random _random = new(seed);

	/// <inheritdoc />
	public virtual int Next(int min, int max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "The lower bound is above the upper one.");

		// Random.Next excludes its upper bound; callers of this interface mean it to be reachable,
		// so a range of 5 to 7 offers three values rather than two. int.MaxValue as the upper bound
		// cannot be widened, and is handed to the underlying call as-is.
		return max == int.MaxValue ? _random.Next(min, max) : _random.Next(min, max + 1);
	}

	/// <inheritdoc />
	public virtual long NextLong(long min, long max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, "The lower bound is above the upper one.");

		return max == long.MaxValue ? _random.NextInt64(min, max) : _random.NextInt64(min, max + 1);
	}

	/// <inheritdoc />
	public virtual double NextDouble() => _random.NextDouble();

	/// <inheritdoc />
	public virtual void NextBytes(byte[] buffer)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		_random.NextBytes(buffer);
	}
}

/// <summary>
/// The source used where nobody needs the series back.
/// </summary>
/// <remarks>
/// Seeded from the clock, so two of these do not agree - which is the point. Anything that would
/// want to repeat its run should take <see cref="SeededRandomProvider"/> instead.
/// </remarks>
public class DefaultRandomProvider : SeededRandomProvider
{
	// The clock alone does not separate two instances built in the same tick, and thread id does
	// not separate two built on the same thread - so a counter carries what neither can.
	private static int _counter;

	/// <summary>
	/// Initializes a new instance of the <see cref="DefaultRandomProvider"/>.
	/// </summary>
	public DefaultRandomProvider()
		: base(Interlocked.Increment(ref _counter) ^ Environment.TickCount ^ Environment.CurrentManagedThreadId ^ (int)(DateTime.UtcNow.Ticks >> 32))
	{
	}

	/// <summary>
	/// The shared instance, for callers that only want a value and have nothing to reproduce.
	/// </summary>
	public static DefaultRandomProvider Instance { get; } = new();
}
