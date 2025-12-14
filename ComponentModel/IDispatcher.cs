namespace Ecng.ComponentModel;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
	private readonly PeriodicActionPlanner _periodic = new(ex => Trace.WriteLine(ex));
	private readonly Lock _timerLock = new();
	private ControllablePeriodicTimer _timer;
	private TimeSpan _timerInterval = TimeSpan.Zero;

	/// <inheritdoc />
	bool IDispatcher.CheckAccess() => true;

	/// <inheritdoc />
	public void Invoke(Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		action();
	}

	/// <inheritdoc />
	void IDispatcher.InvokeAsync(Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		Task.Run(action);
	}

	/// <inheritdoc />
	IDisposable IDispatcher.InvokePeriodically(Action action, TimeSpan interval)
	{
		var sub = _periodic.Register(action, interval);
		EnsureTimerUpToDate();
		return new PeriodicSubscription(this, sub);
	}

	private void EnsureTimerUpToDate()
	{
		using (_timerLock.EnterScope())
		{
			var minInterval = _periodic.MinInterval;

			if (minInterval == null)
			{
				_timer?.Dispose();
				_timer = null;
				_timerInterval = TimeSpan.Zero;
				return;
			}

			if (_timer == null)
			{
				_timerInterval = minInterval.Value;
				_timer = new(() =>
				{
					var actions = _periodic.GetDueActions(DateTime.UtcNow);
					foreach (var action in actions)
					{
						Invoke(action);
					}

					return Task.CompletedTask;
				});

				_timer.Start(_timerInterval);
				return;
			}

			if (minInterval.Value != _timerInterval)
			{
				_timerInterval = minInterval.Value;
				_timer.ChangeInterval(_timerInterval);
			}
		}
	}

	private class PeriodicSubscription(DummyDispatcher owner, IDisposable inner) : Disposable
	{
		private readonly DummyDispatcher _owner = owner ?? throw new ArgumentNullException(nameof(owner));
		private IDisposable _inner = inner ?? throw new ArgumentNullException(nameof(inner));

		protected override void DisposeManaged()
		{
			var inner = Interlocked.Exchange(ref _inner, null);

			if (inner is not null)
			{
				inner.Dispose();
				_owner.EnsureTimerUpToDate();
			}

			base.DisposeManaged();
		}
	}
}
