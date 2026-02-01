#if NETSTANDARD2_0
namespace System.Collections.Concurrent;

/// <summary>
/// Provides extension methods for <see cref="ConcurrentQueue{T}"/> that are not available in .NET Standard 2.0.
/// </summary>
public static class ConcurrentQueueExtensions
{
	/// <summary>
	/// Clears all elements from a concurrent queue.
	/// </summary>
	/// <typeparam name="T">The type of elements in the queue.</typeparam>
	/// <param name="queue">The concurrent queue to clear.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="queue"/> is null.</exception>
	public static void Clear<T>(this ConcurrentQueue<T> queue)
	{
		if (queue is null)
			throw new ArgumentNullException(nameof(queue));

		while (queue.TryDequeue(out _)) { }
	}
}
#endif
