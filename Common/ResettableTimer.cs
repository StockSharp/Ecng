namespace Ecng.Common;

using System;
using System.Threading;

/// <summary>
/// Represents a timer that can be reset and activated repeatedly.
/// The timer executes the Elapsed event periodically based on the specified period.
/// </summary>
[Obsolete("Use Tasks instead.")]
public class ResettableTimer(TimeSpan period, string name) : Disposable
{
	private readonly object _sync = new();
	private readonly object _finish = new();
	private bool _isActivated;
	private bool _isFinished = true;
	private bool _isCancelled;

	private readonly TimeSpan _period = period;
	private readonly string _name = name;

	/// <summary>
	/// Occurs when the timer interval has elapsed.
	/// The event receives a function that determines if processing can be executed.
	/// </summary>
	public event Action<Func<bool>> Elapsed;

	/// <summary>
	/// Activates the timer. 
	/// If the timer is not already running, starts a new thread that periodically raises the Elapsed event.
	/// </summary>
	public void Activate()
	{
		lock (_sync)
		{
			_isActivated = true;

			if (!_isFinished)
				return;

			_isFinished = false;
			_isCancelled = false;
		}

		ThreadingHelper.Thread(() =>
		{
			try
			{
				while (!IsDisposed)
				{
					lock (_sync)
					{
						_isCancelled = false;

						if (_isActivated)
							_isActivated = false;
						else
						{
							_isFinished = true;
							break;
						}
					}

					Elapsed?.Invoke(CanProcess);
					_period.Sleep();
				}
			}
			finally
			{
				lock (_finish)
					Monitor.PulseAll(_finish);
			}
		}).Name(_name).Launch();
	}

	/// <summary>
	/// Cancels the current timer operation.
	/// If the timer is running, this method signals that the current cycle should not process further.
	/// </summary>
	public void Cancel()
	{
		lock (_sync)
		{
			if (_isFinished)
				return;

			_isActivated = false;
			_isCancelled = true;
		}
	}

	/// <summary>
	/// Flushes the timer by activating it and then waiting for the ongoing timer cycle to finish.
	/// </summary>
	public void Flush()
	{
		lock (_finish)
		{
			Activate();
			Monitor.Wait(_finish);
		}
	}

	/// <summary>
	/// Determines if the timer can process the Elapsed event based on its cancellation or disposal state.
	/// </summary>
	/// <returns>
	/// True if the timer is not cancelled and not disposed; otherwise, false.
	/// </returns>
	private bool CanProcess()
	{
		return !_isCancelled && !IsDisposed;
	}

	/// <summary>
	/// Releases the managed resources used by the timer.
	/// Cancels the timer operation before disposing.
	/// </summary>
	protected override void DisposeManaged()
	{
		Cancel();
		base.DisposeManaged();
	}
}