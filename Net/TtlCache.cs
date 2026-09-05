namespace Ecng.Net;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Keeps the answers to a remote question for as long as they are worth keeping.
/// </summary>
/// <remarks>
/// For a caller that asks the same question far more often than the answer changes: a burst of asks
/// reaches the source once, and a change at the source is picked up within the lifetime. What an
/// answer is looked up by is the caller's own: a REST client keys by the request it asked with,
/// another by whatever it resolves by.
/// <para>
/// What has gone stale is dropped rather than left, so a key asked about once and never again does
/// not sit here for the life of the process. The sweep runs on the ask that was going to reach the
/// source anyway, and at most once per lifetime, so it costs nothing next to the call it accompanies.
/// </para>
/// </remarks>
/// <typeparam name="TKey">What an answer is looked up by.</typeparam>
/// <typeparam name="TValue">The answer.</typeparam>
public class TtlCache<TKey, TValue>
{
	private readonly TimeSpan _ttl;
	private readonly TimeSpan _maxAge;
	private readonly TimeProvider _time;
	private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime Expires, DateTime Deadline)> _entries;

	// What is being asked of the source right now, so a burst on a cold key reaches the source once.
	private readonly ConcurrentDictionary<TKey, Task<TValue>> _pending;

	private DateTime _nextSweep;

	/// <summary>
	/// Initializes a new instance of the <see cref="TtlCache{TKey, TValue}"/>.
	/// </summary>
	/// <param name="ttl">How long an answer is kept after it was last asked for.</param>
	/// <param name="maxAge">The oldest an answer may be, however often it is asked for.</param>
	/// <param name="time">The clock an answer's age is measured on.</param>
	public TtlCache(TimeSpan ttl, TimeSpan maxAge, TimeProvider time)
		: this(ttl, maxAge, time, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TtlCache{TKey, TValue}"/>.
	/// </summary>
	/// <param name="ttl">How long an answer is kept after it was last asked for.</param>
	/// <param name="maxAge">The oldest an answer may be, however often it is asked for.</param>
	/// <param name="time">The clock an answer's age is measured on.</param>
	/// <param name="comparer">How keys are compared.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> or <paramref name="maxAge"/> is not positive.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="time"/> is null.</exception>
	public TtlCache(TimeSpan ttl, TimeSpan maxAge, TimeProvider time, IEqualityComparer<TKey> comparer)
	{
		CheckPositive(ttl, nameof(ttl));
		CheckPositive(maxAge, nameof(maxAge));

		_ttl = ttl;
		_maxAge = maxAge;
		_time = time ?? throw new ArgumentNullException(nameof(time));
		_entries = comparer is null ? new() : new(comparer);
		_pending = comparer is null ? new() : new(comparer);
		_nextSweep = Now + ttl;
	}

	/// <summary>
	/// Gets the number of answers that have not gone stale.
	/// </summary>
	public int Count
	{
		get
		{
			var now = Now;
			return _entries.Count(entry => entry.Value.Expires > now);
		}
	}

	/// <summary>
	/// Gets the number of answers held, stale ones included.
	/// </summary>
	public int HeldCount => _entries.Count;

	/// <summary>
	/// Serves the answer for a key, asking <paramref name="resolve"/> when there is none to serve.
	/// </summary>
	/// <param name="key">What the answer is looked up by.</param>
	/// <param name="resolve">Where the answer comes from.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The answer.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="resolve"/> is null.</exception>
	public ValueTask<TValue> GetAsync(TKey key, Func<TKey, CancellationToken, ValueTask<TValue>> resolve, CancellationToken cancellationToken)
		=> GetAsync(key, resolve, _ttl, _maxAge, cancellationToken);

	/// <summary>
	/// Serves the answer for a key, asking <paramref name="resolve"/> when there is none to serve, and holds
	/// what it answered for a lifetime of this call's choosing.
	/// </summary>
	/// <param name="key">What the answer is looked up by.</param>
	/// <param name="resolve">Where the answer comes from.</param>
	/// <param name="ttl">How long this answer is served before the source is asked again.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The answer.</returns>
	/// <remarks>
	/// For a caller whose lifetime is not its own to fix -- a site that reads it from a setting somebody can
	/// change while it runs, and wants the change to apply to what is stored next.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="resolve"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> is not positive.</exception>
	public async ValueTask<TValue> GetAsync(TKey key, Func<TKey, CancellationToken, ValueTask<TValue>> resolve, TimeSpan ttl, TimeSpan maxAge, CancellationToken cancellationToken)
	{
		if (resolve is null)
			throw new ArgumentNullException(nameof(resolve));

		CheckPositive(ttl, nameof(ttl));
		CheckPositive(maxAge, nameof(maxAge));

		if (TryGet(key, ttl, out var held))
			return held;

		var promise = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
		var pending = _pending.GetOrAdd(key, promise.Task);

		// Somebody is already asking the source for this key: wait for their answer rather than asking
		// again. Waiting honours this caller's cancellation, so one caller giving up does not free the
		// others and does not cancel the work they are waiting on.
		if (!ReferenceEquals(pending, promise.Task))
			return await pending.WaitAsync(cancellationToken);

		try
		{
			var value = await resolve(key, cancellationToken);
			var now = Now;

			_entries[key] = (value, (now + ttl).Min(now + maxAge), now + maxAge);

			promise.TrySetResult(value);

			Sweep(now);

			return value;
		}
		catch (Exception ex)
		{
			// Those waiting hear the same failure, and the next ask starts afresh.
			promise.TrySetException(ex);
			throw;
		}
		finally
		{
			_pending.TryRemove(key, out _);
		}
	}

	/// <summary>
	/// Serves the answer held for a key, for a caller that puts them there itself.
	/// </summary>
	/// <param name="key">What the answer is looked up by.</param>
	/// <param name="value">The answer.</param>
	/// <returns>true if one is held and has not gone stale; otherwise, false.</returns>
	public bool TryGet(TKey key, out TValue value)
		=> TryGet(key, _ttl, out value);

	private bool TryGet(TKey key, TimeSpan ttl, out TValue value)
	{
		if (_entries.TryGetValue(key, out var entry))
		{
			var now = Now;

			if (entry.Expires > now)
			{
				// Being asked for is what keeps an answer: one nobody wants any more stops being kept, and a
				// busy one stops reaching the source. Never past the deadline, though, or the answer everybody
				// wants would be the one that never notices the source changing.
				var extended = (now + ttl).Min(entry.Deadline);

				if (extended > entry.Expires)
					_entries.TryUpdate(key, (entry.Value, extended, entry.Deadline), entry);

				value = entry.Value;
				return true;
			}

			// Asked for and stale: it is of no use to anybody, so it goes now rather than at the
			// next sweep.
			_entries.TryRemove(new(key, entry));
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Holds an answer for a key, for the lifetime this cache was made with.
	/// </summary>
	/// <param name="key">What the answer is looked up by.</param>
	/// <param name="value">The answer.</param>
	public void Set(TKey key, TValue value)
		=> Set(key, value, _ttl, _maxAge);

	/// <summary>
	/// Holds an answer for a key, for a lifetime of this call's choosing.
	/// </summary>
	/// <param name="key">What the answer is looked up by.</param>
	/// <param name="value">The answer.</param>
	/// <param name="ttl">How long it is kept after it was last asked for.</param>
	/// <param name="maxAge">The oldest it may be, however often it is asked for.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> or <paramref name="maxAge"/> is not positive.</exception>
	public void Set(TKey key, TValue value, TimeSpan ttl, TimeSpan maxAge)
	{
		CheckPositive(ttl, nameof(ttl));
		CheckPositive(maxAge, nameof(maxAge));

		var now = Now;

		_entries[key] = (value, (now + ttl).Min(now + maxAge), now + maxAge);

		Sweep(now);
	}

	/// <summary>
	/// Drops the answer held for a key, for a caller that knows it has changed.
	/// </summary>
	/// <param name="key">What the answer is looked up by.</param>
	/// <returns>true if an answer was held; otherwise, false.</returns>
	public bool Remove(TKey key) => _entries.TryRemove(key, out _);

	/// <summary>
	/// Drops every answer whose key the caller names.
	/// </summary>
	/// <param name="match">What the keys to drop have in common.</param>
	/// <returns>How many were dropped.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
	public int RemoveWhere(Func<TKey, bool> match)
	{
		if (match is null)
			throw new ArgumentNullException(nameof(match));

		var removed = 0;

		foreach (var key in _entries.Keys)
		{
			if (match(key) && _entries.TryRemove(key, out _))
				removed++;
		}

		return removed;
	}

	/// <summary>
	/// Drops every answer the caller recognises by what was cached.
	/// </summary>
	/// <param name="match">What the answers to drop have in common.</param>
	/// <returns>How many were dropped.</returns>
	/// <remarks>
	/// What has to go is often known by the answer rather than by the key it was cached under -- everything
	/// holding the record that just changed, whatever question it was the answer to.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
	public int RemoveWhere(Func<TKey, TValue, bool> match)
	{
		if (match is null)
			throw new ArgumentNullException(nameof(match));

		var removed = 0;

		foreach (var (key, entry) in _entries)
		{
			if (match(key, entry.Value) && _entries.TryRemove(new(key, entry)))
				removed++;
		}

		return removed;
	}

	/// <summary>
	/// Gets the answers held that have not gone stale.
	/// </summary>
	/// <remarks>
	/// A snapshot: what it lists was held when it was taken, and may be replaced or dropped while it is read.
	/// </remarks>
	public IEnumerable<TValue> Values
	{
		get
		{
			var now = Now;

			foreach (var (_, entry) in _entries)
			{
				if (entry.Expires > now)
					yield return entry.Value;
			}
		}
	}

	/// <summary>
	/// Drops every answer held.
	/// </summary>
	public void Clear() => _entries.Clear();

	private static void CheckPositive(TimeSpan value, string name)
	{
		if (value <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(name, value, "Must be positive.");
	}

	private DateTime Now => _time.GetUtcNow().UtcDateTime;

	private void Sweep(DateTime now)
	{
		if (now < _nextSweep)
			return;

		_nextSweep = now + _ttl;

		foreach (var (key, entry) in _entries)
		{
			// By the value it was seen with: an entry refreshed by another caller in between is the
			// live one and is not this sweep's to take.
			if (entry.Expires <= now)
				_entries.TryRemove(new(key, entry));
		}
	}
}
