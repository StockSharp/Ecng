namespace Ecng.Net;

using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Generic sliding-window rate limiter. Tracks timestamped events per key and reports
/// a key as limited once it accumulates at least <c>maxAttempts</c> events within the
/// trailing <c>window</c>. Empty trackers are evicted on read so memory does not grow
/// without bound for keys that fire a few times and then go quiet.
/// </summary>
/// <typeparam name="TKey">Key type (e.g. an IP string, a user id).</typeparam>
public class SlidingWindowRateLimiter<TKey>
{
	private readonly int _maxAttempts;
	private readonly TimeSpan _window;
	private readonly Func<DateTime> _now;
	private readonly Dictionary<TKey, Tracker> _trackers;

	/// <summary>
	/// Initializes a new instance of the <see cref="SlidingWindowRateLimiter{TKey}"/> class.
	/// </summary>
	/// <param name="maxAttempts">Maximum events allowed within the window before a key is limited.</param>
	/// <param name="window">Trailing time window. Defaults to one minute.</param>
	/// <param name="comparer">Optional key comparer.</param>
	/// <param name="now">Optional clock (UTC). Defaults to <see cref="DateTime.UtcNow"/>; injectable for tests.</param>
	public SlidingWindowRateLimiter(int maxAttempts = 5, TimeSpan? window = null, IEqualityComparer<TKey> comparer = null, Func<DateTime> now = null)
	{
		if (maxAttempts <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "Invalid value.");

		_maxAttempts = maxAttempts;
		_window = window ?? TimeSpan.FromMinutes(1);
		_now = now ?? (() => DateTime.UtcNow);
		_trackers = new(comparer);
	}

	private readonly Lock _sync = new();

	/// <summary>
	/// Number of keys currently tracked. Exposed for diagnostics / tests to verify that
	/// empty trackers are evicted and do not accumulate.
	/// </summary>
	public int TrackedCount
	{
		get
		{
			using (_sync.EnterScope())
				return _trackers.Count;
		}
	}

	/// <summary>
	/// Records one event (e.g. a failed attempt) for the key.
	/// </summary>
	public void Record(TKey key)
	{
		if (key is null)
			return;

		using (_sync.EnterScope())
		{
			if (!_trackers.TryGetValue(key, out var tracker))
				_trackers[key] = tracker = new();

			tracker.Add(_now());
		}
	}

	/// <summary>
	/// Whether the key has reached the limit within the trailing window. Prunes expired
	/// events first and evicts the tracker entirely when nothing recent remains.
	/// </summary>
	public bool IsLimited(TKey key)
	{
		if (key is null)
			return false;

		using (_sync.EnterScope())
		{
			if (!_trackers.TryGetValue(key, out var tracker))
				return false;

			var count = tracker.Prune(_now() - _window);

			if (count == 0)
			{
				_trackers.Remove(key);
				return false;
			}

			return count >= _maxAttempts;
		}
	}

	/// <summary>
	/// Clears all tracked events for the key (e.g. after a success).
	/// </summary>
	public void Reset(TKey key)
	{
		if (key is null)
			return;

		using (_sync.EnterScope())
			_trackers.Remove(key);
	}

	private sealed class Tracker
	{
		private readonly List<DateTime> _events = [];

		public void Add(DateTime time) => _events.Add(time);

		public int Prune(DateTime cutoff)
		{
			_events.RemoveAll(t => t < cutoff);
			return _events.Count;
		}
	}
}
