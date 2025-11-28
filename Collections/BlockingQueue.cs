#if !NET9_0_OR_GREATER
namespace Ecng.Collections;

using System;

/// <summary>
/// Represents a thread-safe blocking queue implementation for storing and retrieving items of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
public sealed class BlockingQueue<T> : BaseBlockingQueue<T, QueueEx<T>>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BlockingQueue{T}"/> class with an empty underlying queue.
	/// </summary>
	public BlockingQueue()
		: base(new QueueEx<T>())
	{
	}

	/// <summary>
	/// Adds an item to the underlying queue.
	/// </summary>
	/// <param name="item">The item to enqueue.</param>
	/// <param name="force">If true, forces the item to be enqueued even if the queue is full; otherwise, respects the maximum size limit.</param>
	protected override void OnEnqueue(T item, bool force)
	{
		InnerCollection.Enqueue(item);
	}

	/// <summary>
	/// Removes and returns the item at the head of the underlying queue.
	/// </summary>
	/// <returns>The dequeued item.</returns>
	protected override T OnDequeue()
	{
		return InnerCollection.Dequeue();
	}

	/// <summary>
	/// Retrieves, but does not remove, the item at the head of the underlying queue.
	/// </summary>
	/// <returns>The item at the head of the queue.</returns>
	protected override T OnPeek()
	{
		return InnerCollection.Peek();
	}
}
#endif