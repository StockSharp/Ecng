namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Ecng.Common;

/// <summary>
/// Calculates which periodic actions are due for execution at a given point in time.
/// This class does not run timers and does not marshal actions to any thread.
/// </summary>
/// <remarks>
/// Intended to be used from a host that owns a timer (for example, a background timer or a WPF <c>DispatcherTimer</c>).
/// On each timer tick, call <see cref="GetDueActions"/> and then execute/marshal returned actions as appropriate.
/// </remarks>
public class PeriodicActionPlanner
{
	private class Entry(Action action, TimeSpan interval)
	{
		public Action Action { get; } = action ?? throw new ArgumentNullException(nameof(action));
		public TimeSpan Interval { get; } = interval;
		public DateTime NextRun = DateTime.UtcNow + interval;
	}

	private readonly Lock _lock = new();
	private readonly List<Entry> _entries = [];
	private TimeSpan? _minInterval;

	/// <summary>
	/// Gets the minimal interval among all registered actions.
	/// </summary>
	/// <value>
	/// Minimal interval or <c>null</c> when there are no registered actions.
	/// </value>
	public TimeSpan? MinInterval
	{
		get
		{
			using (_lock.EnterScope())
				return _minInterval;
		}
	}

	/// <summary>
	/// Registers a periodic action.
	/// </summary>
	/// <param name="action">The action to be returned by <see cref="GetDueActions"/> when it becomes due.</param>
	/// <param name="interval">The interval between consecutive executions.</param>
	/// <returns>A subscription that removes this registration when disposed.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="interval"/> is not positive.</exception>
	public IDisposable Register(Action action, TimeSpan interval)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be positive.");

		var entry = new Entry(action, interval);

		using (_lock.EnterScope())
		{
			_entries.Add(entry);
			UpdateMinInterval();
		}

		return new Subscription(this, entry);
	}

	/// <summary>
	/// Returns actions that are due at the specified moment.
	/// </summary>
	/// <param name="utcNow">
	/// Current moment in UTC. The caller should pass a UTC value (for example, <see cref="DateTime.UtcNow"/>).
	/// </param>
	/// <returns>
	/// An array of actions that should be executed now. Can be empty.
	/// </returns>
	/// <remarks>
	/// For each returned action this method advances the next run time by its configured interval.
	/// </remarks>
	public Action[] GetDueActions(DateTime utcNow)
	{
		using var _ = _lock.EnterScope();

		if (_entries.Count == 0)
			return [];

		var due = new List<Action>();

		foreach (var entry in _entries)
		{
			if (utcNow < entry.NextRun)
				continue;

			entry.NextRun = utcNow + entry.Interval;
			due.Add(entry.Action);
		}

		return [.. due];
	}

	private void Unregister(Entry entry)
	{
		using var _ = _lock.EnterScope();

		if (!_entries.Remove(entry))
			return;

		UpdateMinInterval();
	}

	private void UpdateMinInterval()
		=> _minInterval = _entries.Count == 0 ? null : _entries.Min(e => e.Interval);

	private class Subscription(PeriodicActionPlanner owner, Entry entry) : Disposable
	{
		private readonly PeriodicActionPlanner _owner = owner ?? throw new ArgumentNullException(nameof(owner));
		private Entry _entry = entry ?? throw new ArgumentNullException(nameof(entry));

		protected override void DisposeManaged()
		{
			var entry = Interlocked.Exchange(ref _entry, null);
			if (entry != null)
				_owner.Unregister(entry);

			base.DisposeManaged();
		}
	}
}
