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

	private PeriodicTimer CreateAndStoreTimer(CancellationTokenSource cts)
	{
		PeriodicTimer timer;
		TimeSpan interval;

		using (_lock.EnterScope())
		{
			if (!ReferenceEquals(_cts, cts))
				return null;

			interval = _interval;
		}

		timer = new(interval);

		using (_lock.EnterScope())
		{
			if (!ReferenceEquals(_cts, cts))
			{
				timer.Dispose();
				return null;
			}

			_timer = timer;
		}

		return timer;
	}

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
			var cts = _cts;
			var token = cts.Token;

			_runningTask = Task.Run(async () =>
			{
				PeriodicTimer timer = null;

				async Task<bool> TryInvokeHandler()
				{
					if (token.IsCancellationRequested)
						return false;

					try
					{
						await _handler().NoWait();
						return true;
					}
					catch when (token.IsCancellationRequested)
					{
						return false;
					}
					catch (Exception ex)
					{
						Trace.WriteLine(ex);
						// Ignore handler exceptions - don't stop timer
						return true;
					}
				}

				try
				{
					if (start.HasValue && start.Value > TimeSpan.Zero)
						await start.Value.Delay(token).NoWait();

					timer = CreateAndStoreTimer(cts);
					if (timer is null)
						return;

					if (start.HasValue && start.Value > TimeSpan.Zero)
					{
						if (!await TryInvokeHandler())
							return;
					}

					while (!token.IsCancellationRequested)
					{
						try
						{
							if (!await timer.WaitForNextTickAsync(token).NoWait())
							{
								if (token.IsCancellationRequested)
									break;

								timer = CreateAndStoreTimer(cts);
								if (timer is null)
									break;

								continue;
							}
						}
						catch (OperationCanceledException)
						{
							break;
						}
						catch (ObjectDisposedException)
						{
							if (token.IsCancellationRequested)
								break;

							timer = CreateAndStoreTimer(cts);
							if (timer is null)
								break;

							continue;
						}

						if (!await TryInvokeHandler())
							break;
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
	/// Changes the interval of the timer.
	/// </summary>
	/// <param name="interval">The new interval between timer executions.</param>
	/// <returns>The ControllablePeriodicTimer instance for method chaining.</returns>
	public ControllablePeriodicTimer ChangeInterval(TimeSpan interval)
	{
		using (_lock.EnterScope())
		{
			_interval = interval;

			if (IsRunning)
				_timer?.Dispose();
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
