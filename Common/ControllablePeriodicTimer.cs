namespace Ecng.Common;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a controllable periodic timer that can be started, stopped, and have its interval changed.
/// </summary>
public sealed class ControllablePeriodicTimer : IDisposable
{
	private readonly Func<Task> _handler;
	private CancellationTokenSource _cts;
	private IDisposable _timer;
	private Task _runningTask;
	private TimeSpan _interval;
	private readonly SyncObject _lock = new();

	/// <summary>
	/// Initializes a new instance of the ControllablePeriodicTimer class.
	/// </summary>
	/// <param name="handler">The asynchronous function to be executed periodically.</param>
	internal ControllablePeriodicTimer(Func<Task> handler)
	{
		_handler = handler;
	}

	/// <summary>
	/// Gets the current interval between timer executions.
	/// </summary>
	public TimeSpan Interval
	{
		get
		{
			lock (_lock)
				return _interval;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the timer is currently running.
	/// </summary>
	public bool IsRunning
	{
		get
		{
			lock (_lock)
				return _cts != null && !_cts.IsCancellationRequested;
		}
	}

	/// <summary>
	/// Starts the timer with the specified interval.
	/// </summary>
	/// <param name="interval">The interval between timer executions.</param>
	/// <param name="start">Optional delay before the first execution.</param>
	/// <returns>The ControllablePeriodicTimer instance for method chaining.</returns>
	public ControllablePeriodicTimer Start(TimeSpan interval, TimeSpan? start = null)
	{
		lock (_lock)
		{
			Stop();

			_interval = interval;
			_cts = new CancellationTokenSource();

#if NETSTANDARD2_0
			var timer = new PeriodicTimer(interval);
#else
			var timer = new System.Threading.PeriodicTimer(interval);
#endif
			_timer = timer;

			_runningTask = Task.Run(async () =>
			{
				try
				{
					// Wait for initial delay if specified
					if (start.HasValue && start.Value > TimeSpan.Zero)
						await start.Value.Delay(_cts.Token).NoWait();

					while (!_cts.Token.IsCancellationRequested)
					{
						try
						{
							await _handler().NoWait();
						}
						catch when (_cts.Token.IsCancellationRequested)
						{
							break;
						}
						catch
						{
							throw;
						}

						// Wait for the next tick
						try
						{
							if (!await timer.WaitForNextTickAsync(_cts.Token).NoWait())
								break;
						}
						catch (OperationCanceledException)
						{
							break;
						}
					}
				}
				catch (OperationCanceledException)
				{
					// Expected when stopping
				}
			}, _cts.Token);
		}

		return this;
	}

	/// <summary>
	/// Stops the timer.
	/// </summary>
	public void Stop()
	{
		lock (_lock)
		{
			if (_cts != null)
			{
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}

			_timer?.Dispose();
			_timer = null;
			_runningTask = null;
		}
	}

	/// <summary>
	/// Changes the interval of the running timer. The timer must be restarted for the change to take effect.
	/// </summary>
	/// <param name="interval">The new interval between timer executions.</param>
	/// <returns>The ControllablePeriodicTimer instance for method chaining.</returns>
	public ControllablePeriodicTimer ChangeInterval(TimeSpan interval)
	{
		lock (_lock)
		{
			if (IsRunning)
			{
				Stop();
				Start(interval);
			}
			else
			{
				_interval = interval;
			}
		}

		return this;
	}

	/// <summary>
	/// Disposes the timer and releases associated resources.
	/// </summary>
	public void Dispose()
	{
		Stop();
	}
}
