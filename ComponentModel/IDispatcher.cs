namespace Ecng.ComponentModel;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Threads dispatcher.
/// </summary>
public interface IDispatcher
{
	/// <summary>
	/// Call action in dispatcher thread.
	/// </summary>
	/// <param name="action">Action.</param>
	void Invoke(Action action);

	/// <summary>
	/// Call action in dispatcher thread.
	/// </summary>
	/// <param name="action">Action.</param>
	void InvokeAsync(Action action);

	/// <summary>
	/// Verify that current thread is dispatcher thread.
	/// </summary>
	/// <returns>Operation result.</returns>
	bool CheckAccess();

	/// <summary>
	/// Invoke action periodically in dispatcher thread.
	/// </summary>
	/// <param name="action">Action.</param>
	/// <param name="interval">Interval between invocations.</param>
	/// <returns>IDisposable to unsubscribe.</returns>
	IDisposable InvokePeriodically(Action action, TimeSpan interval);
}

/// <summary>
/// Dummy dispatcher.
/// </summary>
public class DummyDispatcher : IDispatcher
{
	bool IDispatcher.CheckAccess() => true;
	void IDispatcher.Invoke(Action action) => action();
	void IDispatcher.InvokeAsync(Action action) => Task.Run(action);

	// Single timer for all periodic actions on this dispatcher. Created on first use.
	private readonly Lock _periodicLock = new();
	private readonly List<PeriodicEntry> _periodicActions = [];
	private ControllablePeriodicTimer _periodicTimer;
	private TimeSpan _periodicInterval = TimeSpan.Zero;

	private class PeriodicEntry(Action action, TimeSpan interval)
	{
		public Action Action = action;
		public TimeSpan Interval = interval;
		public DateTime NextRun = DateTime.UtcNow + interval;
	}

	IDisposable IDispatcher.InvokePeriodically(Action action, TimeSpan interval)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		using (_periodicLock.EnterScope())
		{
			_periodicActions.Add(new(action, interval));

			// determine minimal interval among registered actions
			var minInterval = _periodicActions.Min(e => e.Interval);

			if (_periodicTimer == null)
			{
				_periodicInterval = minInterval;
				_periodicTimer = new ControllablePeriodicTimer(() =>
				{
					PeriodicEntry[] toRun;
					using (_periodicLock.EnterScope())
					{
						toRun = [.. _periodicActions];
					}

					var now = DateTime.UtcNow;
					foreach (var e in toRun)
					{
						if (now < e.NextRun)
							continue;

						try { e.Action(); } catch { }
						// schedule next run relative to now to avoid drift
						e.NextRun = now + e.Interval;
					}

					return Task.CompletedTask;
				});

				_periodicTimer.Start(_periodicInterval);
			}
			else
			{
				if (minInterval < _periodicInterval)
				{
					_periodicInterval = minInterval;
					_periodicTimer.ChangeInterval(minInterval);
				}
			}
		}

		return new Subscription(this, action);
	}

	private void UnregisterPeriodic(Action action)
	{
		using (_periodicLock.EnterScope())
		{
			var idx = _periodicActions.FindIndex(e => e.Action == action);
			if (idx >= 0)
				_periodicActions.RemoveAt(idx);

			if (_periodicActions.Count == 0 && _periodicTimer != null)
			{
				_periodicTimer.Dispose();
				_periodicTimer = null;
				_periodicInterval = TimeSpan.Zero;
			}
			else if (_periodicActions.Count > 0 && _periodicTimer != null)
			{
				// Recalculate interval - it may increase if a fast action was removed
				var minInterval = _periodicActions.Min(e => e.Interval);
				if (minInterval != _periodicInterval)
				{
					_periodicInterval = minInterval;
					_periodicTimer.ChangeInterval(minInterval);
				}
			}
		}
	}

	private class Subscription(DummyDispatcher owner, Action action) : Disposable
	{
		private readonly DummyDispatcher _owner = owner ?? throw new ArgumentNullException(nameof(owner));
		private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));

		protected override void DisposeManaged()
        {
			_owner.UnregisterPeriodic(_action);

			base.DisposeManaged();
        }
	}
}