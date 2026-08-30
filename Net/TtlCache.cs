namespace Ecng.Net;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Keeps the answers to a remote question for as long as they are worth keeping.
/// </summary>
/// <remarks>
/// For a caller that asks the same question far more often than the answer changes: a burst of asks
/// reaches the source once, and a change at the source is picked up within the lifetime. Unlike
/// <see cref="InMemoryRestApiClientCache"/>, which keys what a REST client asked by the request it
/// asked with, this is keyed by whatever the caller resolves by.
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
	private readonly TimeProvider _time;
	private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime Expires)> _entries;

	private DateTime _nextSweep;

	/// <summary>
	/// Initializes a new instance of the <see cref="TtlCache{TKey, TValue}"/>.
	/// </summary>
	/// <param name="ttl">How long an answer is served before the source is asked again.</param>
	/// <param name="time">The clock an answer's age is measured on.</param>
	public TtlCache(TimeSpan ttl, TimeProvider time)
		: this(ttl, time, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TtlCache{TKey, TValue}"/>.
	/// </summary>
	/// <param name="ttl">How long an answer is served before the source is asked again.</param>
	/// <param name="time">The clock an answer's age is measured on.</param>
	/// <param name="comparer">How keys are compared.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> is not positive.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="time"/> is null.</exception>
	public TtlCache(TimeSpan ttl, TimeProvider time, IEqualityComparer<TKey> comparer)
	{
		if (ttl <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(ttl), ttl, "Must be positive.");

		_ttl = ttl;
		_time = time ?? throw new ArgumentNullException(nameof(time));
		_entries = comparer is null ? new() : new(comparer);
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
	public async ValueTask<TValue> GetAsync(TKey key, Func<TKey, CancellationToken, ValueTask<TValue>> resolve, CancellationToken cancellationToken)
	{
		if (resolve is null)
			throw new ArgumentNullException(nameof(resolve));

		if (TryGet(key, out var held))
			return held;

		var value = await resolve(key, cancellationToken);
		var now = Now;

		_entries[key] = (value, now + _ttl);

		Sweep(now);

		return value;
	}

	/// <summary>
	/// Serves the answer held for a key, for a caller that puts them there itself.
	/// </summary>
	/// <param name="key">What the answer is looked up by.</param>
	/// <param name="value">The answer.</param>
	/// <returns>true if one is held and has not gone stale; otherwise, false.</returns>
	public bool TryGet(TKey key, out TValue value)
	{
		if (_entries.TryGetValue(key, out var entry))
		{
			if (entry.Expires > Now)
			{
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
	{
		var now = Now;

		_entries[key] = (value, now + _ttl);

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
	/// Drops every answer held.
	/// </summary>
	public void Clear() => _entries.Clear();

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
