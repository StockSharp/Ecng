namespace Ecng.Collections;

using System.Collections.Generic;

/// <summary>
/// Represents a base class for an ordered blocking queue that sorts elements based on a specified key.
/// </summary>
/// <typeparam name="TSort">The type used to determine the sort order of elements.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the queue.</typeparam>
/// <typeparam name="TCollection">The type of the inner collection, which must implement <see cref="ICollection{T}"/> and <see cref="IQueue{T}"/> for tuples of <typeparamref name="TSort"/> and <typeparamref name="TValue"/>.</typeparam>
public abstract class BaseOrderedBlockingQueue<TSort, TValue, TCollection>(TCollection collection) :
	BaseBlockingQueue<(TSort sort, TValue elem), TCollection>(collection)
	where TCollection : ICollection<(TSort, TValue)>, IQueue<(TSort, TValue)>
{
	/// <summary>
	/// Attempts to remove and return the next value from the queue.
	/// </summary>
	/// <param name="value">When this method returns, contains the dequeued value if successful; otherwise, the default value of <typeparamref name="TValue"/>.</param>
	/// <param name="exitOnClose">If true, exits immediately if the queue is closed; otherwise, waits for an item.</param>
	/// <param name="block">If true, blocks until an item is available; otherwise, returns immediately.</param>
	/// <returns>True if a value was successfully dequeued; otherwise, false.</returns>
	public bool TryDequeue(out TValue value, bool exitOnClose = true, bool block = true)
	{
		if (base.TryDequeue(out var pair, exitOnClose, block))
		{
			value = pair.elem;
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Adds a new value to the queue with the specified sort order.
	/// </summary>
	/// <param name="sort">The sort key determining the order of the value in the queue.</param>
	/// <param name="value">The value to add to the queue.</param>
	protected void Enqueue(TSort sort, TValue value)
		=> Enqueue(new(sort, value));

	/// <summary>
	/// Adds an item to the inner collection when enqueuing.
	/// </summary>
	/// <param name="item">The tuple containing the sort key and value to enqueue.</param>
	/// <param name="force">If true, forces the item to be enqueued even if the queue is full; otherwise, respects the maximum size limit.</param>
	protected override void OnEnqueue((TSort, TValue) item, bool force)
		=> InnerCollection.Enqueue(item);

	/// <summary>
	/// Removes and returns the next item from the inner collection.
	/// </summary>
	/// <returns>The tuple containing the sort key and value of the dequeued item.</returns>
	protected override (TSort, TValue) OnDequeue()
		=> InnerCollection.Dequeue();

	/// <summary>
	/// Retrieves, but does not remove, the next item from the inner collection.
	/// </summary>
	/// <returns>The tuple containing the sort key and value of the item at the head of the queue.</returns>
	protected override (TSort, TValue) OnPeek()
		=> InnerCollection.Peek();
}