namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;

using Ecng.Common;

/// <summary>
/// Calculates which periodic actions are due for execution at a given point in time.
/// This class does not run timers and does not marshal actions to any thread.
/// </summary>
/// <remarks>
/// Intended to be used from a host that owns a timer (for example, a background timer or a WPF <c>DispatcherTimer</c>).
/// On each timer tick, call <see cref="GetDueActions"/> and then execute/marshal returned actions as appropriate.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="PeriodicActionPlanner"/> class.
/// </remarks>
/// <param name="errorHandler">
/// An callback invoked when a periodic action throws an exception.
/// Handler exceptions are ignored.
/// </param>
public class PeriodicActionPlanner(Action<Exception> errorHandler)
{
	private class Entry(PeriodicActionPlanner owner, Action action, TimeSpan interval)
	{
		private readonly PeriodicActionPlanner _owner = owner ?? throw new ArgumentNullException(nameof(owner));
		private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));
		private int _consecutiveErrors;

		public TimeSpan Interval { get; } = interval;
		public DateTime NextRun = DateTime.UtcNow + interval;

		public void Invoke()
		{
			try
			{
				_action();
				_consecutiveErrors = 0;
			}
			catch (Exception ex)
			{
				try
				{
					_owner._errorHandler(ex);
				}
				catch (Exception ex2)
				{
					Trace.WriteLine(ex2);
				}

				_consecutiveErrors++;

				var maxErrors = _owner.MaxErrors;
				if (maxErrors <= 0)
					return;

				if (_consecutiveErrors < maxErrors)
					return;

				_owner.Unregister(this);
			}
		}
	}

	private readonly Lock _lock = new();
	private readonly List<Entry> _entries = [];
	private TimeSpan? _minInterval;

	private readonly Action<Exception> _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

	/// <summary>
	/// Gets the number of currently registered actions.
	/// </summary>
	public int Count
	{
		get
		{
			using (_lock.EnterScope())
				return _entries.Count;
		}
	}

	private int _maxErrors;

	/// <summary>
	/// Gets or sets the maximum number of consecutive errors allowed for a registered action.
	/// </summary>
	/// <value>
	/// If the value is greater than <c>0</c>, then an action is automatically removed when its consecutive error counter
	/// reaches this value. If the value is <c>0</c>, automatic removal is disabled.
	/// </value>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
	public int MaxErrors
	{
		get => _maxErrors;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "MaxErrors cannot be negative.");

			_maxErrors = value;
		}
	}

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

		var entry = new Entry(this, action, interval);

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
	/// The returned actions are wrapped by the planner to track consecutive errors for <see cref="MaxErrors"/>.
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
			due.Add(entry.Invoke);
		}

		return [.. due];
	}

	private void Unregister(Entry entry)
	{
		using var _ = _lock.EnterScope();

		if (_entries.Remove(entry))
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
