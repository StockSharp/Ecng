namespace Ecng.Collections;

/// <summary>
/// Defines a generic queue interface for enqueueing, dequeueing, and peeking at items.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
public interface IQueue<T>
{
	/// <summary>
	/// Removes and returns the item at the front of the queue.
	/// </summary>
	/// <returns>The item at the front of the queue.</returns>
	T Dequeue();

	/// <summary>
	/// Returns the item at the front of the queue without removing it.
	/// </summary>
	/// <returns>The item at the front of the queue.</returns>
	T Peek();

	/// <summary>
	/// Adds an item to the back of the queue.
	/// </summary>
	/// <param name="item">The item to add to the queue.</param>
	void Enqueue(T item);
}