#if NETSTANDARD2_0
namespace System.Threading;

using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Provides a periodic timer for .NET Standard 2.0 that mimics System.Threading.PeriodicTimer behavior.
/// </summary>
public sealed class PeriodicTimer : IDisposable
{
	private readonly TimeSpan _period;
	private readonly CancellationTokenSource _disposeCts = new();
	private DateTimeOffset? _nextTick;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="PeriodicTimer"/> class.
	/// </summary>
	/// <param name="period">The time interval between invocations of the timer.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when period is less than or equal to zero.</exception>
	public PeriodicTimer(TimeSpan period)
	{
		if (period <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(period));

		_period = period;
	}

	/// <summary>
	/// Waits for the next tick of the timer.
	/// </summary>
	/// <param name="cancellationToken">A token that may be used to cancel the wait operation.</param>
	/// <returns>A task that will be completed when the next tick occurs, or false if the timer has been disposed.</returns>
	public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed)
			return false;

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);

		try
		{
			var now = DateTimeOffset.UtcNow;

			// Initialize next tick on first call
			if (_nextTick == null)
				_nextTick = now + _period;

			var delay = _nextTick.Value - now;

			if (delay > TimeSpan.Zero)
				await delay.Delay(linkedCts.Token).NoWait();

			// Schedule next tick
			_nextTick = _nextTick.Value + _period;
			return true;
		}
		catch (OperationCanceledException)
		{
			return !_disposed && !cancellationToken.IsCancellationRequested;
		}
	}

	/// <summary>
	/// Releases the resources used by the timer.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_disposeCts.Cancel();
		_disposeCts.Dispose();
	}
}
#endif
