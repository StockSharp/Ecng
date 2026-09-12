namespace Ecng.Common;

/// <summary>
/// A wake-up for work that has to happen when something changes, rather than on a clock.
/// </summary>
/// <remarks>
/// Whatever makes the change calls <see cref="Raise"/>; a reader loops on <see cref="WaitAsync"/>,
/// which completes one window after the raise that woke it, so a run of changes inside that window
/// is one wake-up. That covers a change made while the window is being held: the round the reader is
/// about to do reads the state that change left behind, so it does not also earn a round of its own.
/// A raise made while nobody is waiting is kept for the next wait.
/// <para>
/// Use it where work is driven by change and repeating it costs more than it is worth - writing a
/// file, publishing a snapshot, recomputing a view. Idle costs nothing at all: with nothing raised
/// the reader simply sleeps, where a periodic timer would keep waking to find nothing to do.
/// </para>
/// <para>
/// One signal serves one reader: a wake-up is handed to a single waiter, so two readers sharing an
/// instance would take turns and each would miss the changes the other took. A second concurrent
/// waiter is refused rather than quietly served.
/// </para>
/// </remarks>
public sealed class CoalescingSignal : IDisposable
{
	// At most one wake-up is pending at a time: the reader takes the whole of whatever changed.
	private readonly SemaphoreSlim _raised = new(0, 1);

	// 1 while a reader is inside WaitAsync, which is what makes a second one an error rather than a
	// silent halving of the wake-ups.
	private int _waiting;

	private volatile bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="CoalescingSignal"/>.
	/// </summary>
	/// <param name="window">
	/// How long a raise is held before the wait completes, so everything changed within it is taken
	/// in one round. <see cref="TimeSpan.Zero"/> wakes the reader as soon as it is raised.
	/// </param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="window"/> is negative.</exception>
	public CoalescingSignal(TimeSpan window)
	{
		if (window < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(window), window, "The window cannot be negative.");

		Window = window;
	}

	/// <summary>
	/// How long a raise is held before the wait completes.
	/// </summary>
	public TimeSpan Window { get; }

	/// <summary>
	/// Notes that something changed and wakes whoever is waiting. Never blocks and never throws,
	/// including after <see cref="Dispose"/>: this runs on paths that see every message, and a
	/// change noted during shutdown is not a fault.
	/// </summary>
	public void Raise()
	{
		if (_disposed)
			return;

		// CurrentCount is read rather than letting Release throw: this runs on paths that see every
		// message. Two raisers can still both find it empty, which is what the catch is for.
		try
		{
			if (_raised.CurrentCount > 0)
				return;

			_raised.Release();
		}
		catch (SemaphoreFullException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}

	/// <summary>
	/// Completes one window after the next raise, or one window from now if a raise is already
	/// pending.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that completes once there is something to do.</returns>
	/// <exception cref="InvalidOperationException">Another reader is already waiting on this signal.</exception>
	/// <exception cref="ObjectDisposedException">The signal has been disposed.</exception>
	public async Task WaitAsync(CancellationToken cancellationToken)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(CoalescingSignal));

		if (Interlocked.Exchange(ref _waiting, 1) == 1)
			throw new InvalidOperationException("The signal is already being waited on: one signal serves one reader.");

		try
		{
			await _raised.WaitAsync(cancellationToken).ConfigureAwait(false);

			if (Window <= TimeSpan.Zero)
				return;

			await Task.Delay(Window, cancellationToken).ConfigureAwait(false);

			// The reader reads after this returns, so whatever was raised while the window was held is
			// already in what it is about to take. Leaving that raise standing would wake it once more
			// for a round with nothing left in it - the very cost the window exists to spare.
			_raised.Wait(0);
		}
		finally
		{
			Interlocked.Exchange(ref _waiting, 0);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_raised.Dispose();
	}
}
