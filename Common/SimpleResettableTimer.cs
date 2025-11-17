namespace Ecng.Common;

using System;
using System.Threading;

#if NET10_0
using SyncObject = System.Threading.Lock;
#endif

/// <summary>
/// Represents a simple timer that can be reset.
/// When the timer period elapses without a reset, the <see cref="Elapsed"/> event is invoked.
/// </summary>
[Obsolete("Use Tasks instead.")]
public class SimpleResettableTimer(TimeSpan period) : IDisposable
{
	private readonly SyncObject _sync = new();
	private readonly TimeSpan _period = period;

	private Timer _timer;
	private bool _changed;

	/// <summary>
	/// Occurs when the timer elapses without being reset.
	/// </summary>
	public event Action Elapsed;

	/// <summary>
	/// Resets the timer. If the timer is not already running, it starts the timer with the specified period.
	/// If it is running, it marks that the timer should restart the count.
	/// </summary>
	public void Reset()
	{
		lock (_sync)
		{
			if (_timer is null)
			{
				_timer = ThreadingHelper
					.Timer(OnTimer)
					.Interval(_period);
			}
			else
				_changed = true;
		}
	}

	private void OnTimer()
	{
		var elapsed = false;

		lock (_sync)
		{
			if (!_changed)
			{
				if (_timer != null)
				{
					_timer.Dispose();
					_timer = null;
				}

				elapsed = true;
			}
			else
				_changed = false;
		}

		if (elapsed)
			Elapsed?.Invoke();
	}

	/// <summary>
	/// Forces the timer to immediately run its elapsed logic if it is running,
	/// effectively flushing the timer cycle.
	/// </summary>
	public void Flush()
	{
		lock (_sync)
		{
			if (_timer is null)
				return;

			_changed = false;
			_timer.Change(TimeSpan.Zero, _period);
		}
	}

	/// <summary>
	/// Disposes the timer and stops any further executions.
	/// </summary>
	public void Dispose()
	{
		lock (_sync)
		{
			if (_timer is null)
				return;

			_changed = true;
			
			_timer.Dispose();
			_timer = null;
		}
	}
}