#if !NET9_0_OR_GREATER
namespace Ecng.Collections;

/// <summary>
/// Represents a blocking queue interface for managing items in a thread-safe manner.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
public interface IBlockingQueue<T>
{
	/// <summary>
	/// Gets the number of elements contained in the queue.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Gets or sets the maximum number of elements that the queue can hold.
	/// </summary>
	int MaxSize { get; set; }

	/// <summary>
	/// Gets a value indicating whether the queue is closed.
	/// </summary>
	bool IsClosed { get; }

	/// <summary>
	/// Opens the queue, enabling enqueuing and dequeuing operations.
	/// </summary>
	void Open();

	/// <summary>
	/// Closes the queue to prevent further enqueuing operations.
	/// </summary>
	void Close();

	/// <summary>
	/// Blocks the current thread until the queue becomes empty.
	/// </summary>
	void WaitUntilEmpty();

	/// <summary>
	/// Adds an item to the end of the queue.
	/// </summary>
	/// <param name="item">The item to add.</param>
	/// <param name="force">If set to <c>true</c>, the item is enqueued even if the queue is at its maximum capacity.</param>
	void Enqueue(T item, bool force = false);

	/// <summary>
	/// Removes and returns the object at the beginning of the queue.
	/// </summary>
	/// <returns>The object that is removed from the beginning of the queue.</returns>
	T Dequeue();

	/// <summary>
	/// Attempts to remove and return the object at the beginning of the queue.
	/// </summary>
	/// <param name="value">When this method returns, contains the object removed from the beginning of the queue, if the operation succeeded.</param>
	/// <param name="exitOnClose">If set to <c>true</c>, the operation exits if the queue is closed.</param>
	/// <param name="block">If set to <c>true</c>, blocks until an item is available.</param>
	/// <returns><c>true</c> if an object was successfully removed; otherwise, <c>false</c>.</returns>
	bool TryDequeue(out T value, bool exitOnClose = true, bool block = true);

	/// <summary>
	/// Returns the object at the beginning of the queue without removing it.
	/// </summary>
	/// <returns>The object at the beginning of the queue.</returns>
	T Peek();

	/// <summary>
	/// Attempts to return the object at the beginning of the queue without removing it.
	/// </summary>
	/// <param name="value">When this method returns, contains the object at the beginning of the queue, if the operation succeeded.</param>
	/// <param name="exitOnClose">If set to <c>true</c>, the operation exits if the queue is closed.</param>
	/// <param name="block">If set to <c>true</c>, blocks until an item is available.</param>
	/// <returns><c>true</c> if an object was successfully retrieved; otherwise, <c>false</c>.</returns>
	bool TryPeek(out T value, bool exitOnClose = true, bool block = true);

	/// <summary>
	/// Removes all objects from the queue.
	/// </summary>
	void Clear();
}
#endif