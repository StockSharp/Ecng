namespace Ecng.Collections;

using System;

/// <summary>
/// Provides a thread-safe queue based on the <see cref="QueueEx{T}"/> collection.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
[Serializable]
public class SynchronizedQueue<T> : SynchronizedCollection<T, QueueEx<T>>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedQueue{T}"/> class.
	/// </summary>
	public SynchronizedQueue()
		: base(new QueueEx<T>())
	{
	}

	/// <summary>
	/// Adds an item to the queue in a thread-safe manner.
	/// </summary>
	/// <param name="item">The item to enqueue.</param>
	public void Enqueue(T item)
	{
		using (SyncRoot.EnterScope())
			InnerCollection.Enqueue(item);
	}

	/// <summary>
	/// Removes and returns the item at the beginning of the queue in a thread-safe manner.
	/// </summary>
	/// <returns>The item that was removed from the queue.</returns>
	public T Dequeue()
	{
		using (SyncRoot.EnterScope())
			return InnerCollection.Dequeue();
	}

	/// <summary>
	/// Returns the item at the beginning of the queue without removing it, in a thread-safe manner.
	/// </summary>
	/// <returns>The item at the beginning of the queue.</returns>
	public T Peek()
	{
		using (SyncRoot.EnterScope())
			return InnerCollection.Peek();
	}

	/// <summary>
	/// Throws a <see cref="NotSupportedException"/> as this operation is not supported for a queue.
	/// </summary>
	/// <param name="index">The index of the item to retrieve.</param>
	/// <returns>This method always throws an exception.</returns>
	/// <exception cref="NotSupportedException">This operation is not supported for a queue.</exception>
	protected override T OnGetItem(int index)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Throws a <see cref="NotSupportedException"/> as this operation is not supported for a queue.
	/// </summary>
	/// <param name="index">The index at which the item should be inserted.</param>
	/// <param name="item">The item to insert.</param>
	/// <exception cref="NotSupportedException">This operation is not supported for a queue.</exception>
	protected override void OnInsert(int index, T item)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Throws a <see cref="NotSupportedException"/> as this operation is not supported for a queue.
	/// </summary>
	/// <param name="index">The index of the item to remove.</param>
	/// <exception cref="NotSupportedException">This operation is not supported for a queue.</exception>
	protected override void OnRemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Throws a <see cref="NotSupportedException"/> as this operation is not supported for a queue.
	/// </summary>
	/// <param name="item">The item to locate in the queue.</param>
	/// <returns>This method always throws an exception.</returns>
	/// <exception cref="NotSupportedException">This operation is not supported for a queue.</exception>
	protected override int OnIndexOf(T item)
	{
		throw new NotSupportedException();
	}
}