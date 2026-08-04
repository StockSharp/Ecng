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

	// Random is not thread-safe: concurrent draws corrupt its state and it starts handing out
	// zeros forever. A seeded source is meant to be held and passed around - by a strategy whose
	// pipeline spans threads, by an emulator - so the draws are serialised. The order they come
	// out in across threads is not promised; that they are valid values is.
	private readonly Lock _sync = new();

	/// <inheritdoc />
	public virtual int Next(int min, int max)
	{
		using (_sync.EnterScope())
			return _random.NextInclusive(min, max);
	}

	/// <inheritdoc />
	public virtual long NextLong(long min, long max)
	{
		using (_sync.EnterScope())
			return _random.NextInclusive(min, max);
	}

	/// <inheritdoc />
	public virtual double NextDouble()
	{
		using (_sync.EnterScope())
			return _random.NextDouble();
	}

	/// <inheritdoc />
	public virtual void NextBytes(byte[] buffer)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		using (_sync.EnterScope())
			_random.NextBytes(buffer);
	}
}

/// <summary>
/// The source used where nobody needs the series back.
/// </summary>
/// <remarks>
/// Drawn from the same place <see cref="RandomGen"/> draws from, which is thread-safe by
/// construction: this one is handed to anything with no seed to keep, so the same instance ends
/// up being drawn from by several threads at once and a plain <see cref="Random"/> would lose
/// its state. Anything that wants to repeat its run takes <see cref="SeededRandomProvider"/>.
/// </remarks>
public class DefaultRandomProvider : IRandomProvider
{
#if NET6_0_OR_GREATER
	private static Random Random => Random.Shared;
#else
	[ThreadStatic]
	private static Random _threadRandom;
	private static long _globalSeed = DateTime.UtcNow.Ticks;

	private static Random Random => _threadRandom ??= new((int)(Interlocked.Increment(ref _globalSeed) ^ Environment.TickCount ^ Environment.CurrentManagedThreadId ^ (DateTime.UtcNow.Ticks >> 32)));
#endif

	/// <inheritdoc />
	public int Next(int min, int max) => Random.NextInclusive(min, max);

	/// <inheritdoc />
	public long NextLong(long min, long max) => Random.NextInclusive(min, max);

	/// <inheritdoc />
	public double NextDouble() => Random.NextDouble();

	/// <inheritdoc />
	public void NextBytes(byte[] buffer)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		Random.NextBytes(buffer);
	}

	/// <summary>
	/// The shared instance, for callers that only want a value and have nothing to reproduce.
	/// </summary>
	public static DefaultRandomProvider Instance { get; } = new();
}
