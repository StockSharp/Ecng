namespace Ecng.Common;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

/// <summary>
/// Represents a controllable periodic timer that can be started, stopped, and have its interval changed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ControllablePeriodicTimer class.
/// </remarks>
/// <param name="handler">The asynchronous function to be executed periodically.</param>
public sealed class ControllablePeriodicTimer(Func<Task> handler) : IDisposable
{
	private readonly Func<Task> _handler = handler;
	private CancellationTokenSource _cts;
	private PeriodicTimer _timer;
	private Task _runningTask;
	private TimeSpan _interval;
	private readonly Lock _lock = new();

	/// <summary>
	/// Gets the current interval between timer executions.
	/// </summary>
	public TimeSpan Interval
	{
		get
		{
			using (_lock.EnterScope())
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
			using (_lock.EnterScope())
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
		using (_lock.EnterScope())
		{
			Stop();

			_interval = interval;
			_cts = new();

			var token = _cts.Token;

			_timer = new(interval);

			_runningTask = Task.Run(async () =>
			{
				try
				{
					// Wait for initial delay if specified
					if (start.HasValue && start.Value > TimeSpan.Zero)
						await start.Value.Delay(token).NoWait();

					while (!token.IsCancellationRequested)
					{
						// Wait for the next tick first
						try
						{
							if (!await _timer.WaitForNextTickAsync(token).NoWait())
								break;
						}
						catch (OperationCanceledException)
						{
							break;
						}

						// Then execute handler
						try
						{
							await _handler().NoWait();
						}
						catch when (token.IsCancellationRequested)
						{
							break;
						}
						catch (Exception ex)
						{
							Trace.WriteLine(ex);
							// Ignore handler exceptions - don't stop timer
						}
					}
				}
				catch (OperationCanceledException)
				{
					// Expected when stopping
				}
			}, token);
		}

		return this;
	}

	/// <summary>
	/// Stops the timer.
	/// </summary>
	public void Stop()
	{
		using (_lock.EnterScope())
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
		using (_lock.EnterScope())
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
