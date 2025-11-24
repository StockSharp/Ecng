#if NETSTANDARD2_0
namespace System.Threading.Tasks;
/// <summary>
/// Provides extension methods for <see cref="Task"/>.
/// </summary>
public static class TaskExtensions
{
	/// <summary>
	/// Gets a <see cref="Task"/> that will complete when this <see cref="Task"/> completes, when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
	/// </summary>
	/// <param name="task">The task to wait on for completion.</param>
	/// <param name="timeout">The timeout after which the <see cref="Task"/> should be faulted with a <see cref="TimeoutException"/> if it hasn't otherwise completed.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for a cancellation request.</param>
	/// <returns>The <see cref="Task"/> representing the asynchronous wait. It may or may not be the same instance as the current instance.</returns>
	public static async Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
	{
		if (task is null)
			throw new ArgumentNullException(nameof(task));

		using var timeoutCts = new CancellationTokenSource(timeout);
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

		var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, linkedCts.Token)).ConfigureAwait(false);

		if (completedTask == task)
		{
			await task.ConfigureAwait(false);
		}
		else if (cancellationToken.IsCancellationRequested)
		{
			throw new OperationCanceledException(cancellationToken);
		}
		else
		{
			throw new TimeoutException();
		}
	}
}
#endif
