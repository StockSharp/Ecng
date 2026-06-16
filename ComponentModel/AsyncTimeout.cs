namespace Ecng.ComponentModel;

/// <summary>
/// Represents an asynchronous timeout for an operation.
/// </summary>
public class AsyncTimeout
{
	/// <summary>
	/// Gets the configured timeout value.
	/// </summary>
	public TimeSpan Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AsyncTimeout"/> class with the specified timeout value.
	/// </summary>
	/// <param name="value">The timeout duration. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
	public AsyncTimeout(TimeSpan value)
	{
		if (value <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

		Value = value;
	}

	/// <summary>
	/// Registers an action to be invoked when the timeout elapses.
	/// </summary>
	/// <param name="action">The action to execute on timeout.</param>
	/// <returns>
	/// A handle that cancels the pending timeout (so a completed operation can stop it from firing)
	/// and releases the underlying <see cref="CancellationTokenSource"/> when disposed.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
	public IDisposable Register(Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		var cts = new CancellationTokenSource(Value);

		var registration = cts.Token.Register(() =>
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				// The callback fires on a thread-pool timer with no awaiter; without this an
				// exception from it would go unobserved and crash the process.
				Trace.WriteLine(ex);
			}
		}, useSynchronizationContext: false);

		return cts.MakeDisposable(c =>
		{
			// Unregister first so a timeout that fires concurrently doesn't run the action, then
			// release the source instead of leaking it once the guarded operation has finished.
			registration.Dispose();
			c.Dispose();
		});
	}
}