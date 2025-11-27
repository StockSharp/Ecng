#if !NET9_0_OR_GREATER
namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using Ecng.Common;

// http://stackoverflow.com/questions/530211/creating-a-blocking-queuet-in-net
/// <summary>
/// Abstract base class for a blocking queue implementation with a generic type and an inner collection.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
/// <typeparam name="TF">The type of the inner collection, which must implement <see cref="ICollection{T}"/>.</typeparam>
public abstract class BaseBlockingQueue<T, TF>(TF innerCollection) : ISynchronizedCollection<T>, IBlockingQueue<T>
	where TF : ICollection<T>
{
	/// <summary>
	/// Gets the inner collection used to store the queue elements.
	/// </summary>
	protected TF InnerCollection { get; } = innerCollection;

	// -1 is unlimited
	private int _maxSize = -1;

	/// <summary>
	/// Gets or sets the maximum size of the queue. A value of -1 indicates no size limit.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the value is 0 or less than -1.</exception>
	public int MaxSize
	{
		get => _maxSize;
		set
		{
			if (value == 0 || value < -1)
				throw new ArgumentOutOfRangeException(nameof(value));

			_maxSize = value;
		}
	}

	/// <summary>
	/// Gets the synchronization object used to coordinate access to the queue.
	/// </summary>
	public SyncObject SyncRoot { get; } = new();

	/// <summary>
	/// Gets the current number of items in the queue.
	/// </summary>
	public int Count => InnerCollection.Count;

	private bool _isClosed;

	/// <summary>
	/// Gets a value indicating whether the queue is closed.
	/// </summary>
	public bool IsClosed => _isClosed;

	/// <summary>
	/// Closes the queue, preventing further enqueues and waking up any blocked threads.
	/// </summary>
	public void Close()
	{
		lock (SyncRoot)
		{
			_isClosed = true;
			Monitor.PulseAll(SyncRoot);
		}
	}

	/// <summary>
	/// Reopens the queue, allowing enqueue operations to proceed.
	/// </summary>
	public void Open()
	{
		lock (SyncRoot)
		{
			_isClosed = false;
		}
	}

	/// <summary>
	/// Blocks the current thread while the queue is full, unless it is closed.
	/// </summary>
	private void WaitWhileFull()
	{
		while (InnerCollection.Count >= _maxSize && !_isClosed)
		{
			Monitor.Wait(SyncRoot);
		}
	}

	/// <summary>
	/// Blocks the current thread until the queue is empty or closed.
	/// </summary>
	public void WaitUntilEmpty()
	{
		lock (SyncRoot)
		{
			while (InnerCollection.Count > 0 && !_isClosed)
				Monitor.Wait(SyncRoot);
		}
	}

	/// <summary>
	/// Adds an item to the queue, optionally forcing the enqueue even if the queue is full.
	/// </summary>
	/// <param name="item">The item to add to the queue.</param>
	/// <param name="force">If true, adds the item regardless of the maximum size; otherwise, waits if the queue is full.</param>
	public void Enqueue(T item, bool force = false)
		=> TryEnqueue(item, force);

	/// <summary>
	/// Attempts to add an item to the queue, optionally forcing the enqueue operation.
	/// </summary>
	/// <param name="item">The item to add to the queue.</param>
	/// <param name="force">If true, adds the item regardless of the maximum size; otherwise, waits if the queue is full.</param>
	/// <returns><see langword="true"/> if the item was successfully added to the queue; otherwise, <see langword="false"/> if the queue is closed.</returns>
	public bool TryEnqueue(T item, bool force = false)
	{
		lock (SyncRoot)
		{
			if (_isClosed)
				return false;

			if (!force && _maxSize != -1)
			{
				if (InnerCollection.Count >= _maxSize)
					WaitWhileFull();
			}

			OnEnqueue(item, force);

			if (InnerCollection.Count == 1)
			{
				// wake up any blocked dequeue
				Monitor.PulseAll(SyncRoot);
			}

			return true;
		}
	}

	/// <summary>
	/// Performs the actual enqueue operation on the inner collection.
	/// </summary>
	/// <param name="item">The item to enqueue.</param>
	/// <param name="force">Indicates whether the enqueue is forced.</param>
	protected abstract void OnEnqueue(T item, bool force);

	/// <summary>
	/// Performs the actual dequeue operation on the inner collection.
	/// </summary>
	/// <returns>The dequeued item.</returns>
	protected abstract T OnDequeue();

	/// <summary>
	/// Retrieves, but does not remove, the head of the queue.
	/// </summary>
	/// <returns>The item at the head of the queue.</returns>
	protected abstract T OnPeek();

	/// <summary>
	/// Removes and returns the item at the head of the queue.
	/// </summary>
	/// <returns>The dequeued item.</returns>
	public T Dequeue()
	{
		TryDequeue(out T retVal, false);
		return retVal;
	}

	/// <summary>
	/// Blocks the current thread while the queue is empty, based on the specified conditions.
	/// </summary>
	/// <param name="exitOnClose">If true, exits if the queue is closed.</param>
	/// <param name="block">If true, blocks until an item is available; otherwise, returns immediately.</param>
	/// <returns>True if an item is available; otherwise, false.</returns>
	private bool WaitWhileEmpty(bool exitOnClose, bool block)
	{
		while (InnerCollection.Count == 0)
		{
			if (exitOnClose && _isClosed)
				return false;

			if (!block)
				return false;

			Monitor.Wait(SyncRoot);
		}

		return true;
	}

	/// <summary>
	/// Attempts to remove and return the item at the head of the queue.
	/// </summary>
	/// <param name="value">When this method returns, contains the dequeued item if successful; otherwise, the default value.</param>
	/// <param name="exitOnClose">If true, exits if the queue is closed.</param>
	/// <param name="block">If true, blocks until an item is available; otherwise, returns immediately.</param>
	/// <returns>True if an item was dequeued; otherwise, false.</returns>
	public bool TryDequeue(out T value, bool exitOnClose = true, bool block = true)
	{
		lock (SyncRoot)
		{
			if (!WaitWhileEmpty(exitOnClose, block))
			{
				value = default;
				return false;
			}

			value = OnDequeue();

			if (InnerCollection.Count == (_maxSize - 1) || InnerCollection.Count == 0)
			{
				// wake up any blocked enqueue
				Monitor.PulseAll(SyncRoot);
			}

			return true;
		}
	}

	/// <summary>
	/// Retrieves, but does not remove, the item at the head of the queue.
	/// </summary>
	/// <returns>The item at the head of the queue.</returns>
	public T Peek()
	{
		TryPeek(out T retVal, false);
		return retVal;
	}

	/// <summary>
	/// Attempts to retrieve, but not remove, the item at the head of the queue.
	/// </summary>
	/// <param name="value">When this method returns, contains the peeked item if successful; otherwise, the default value.</param>
	/// <param name="exitOnClose">If true, exits if the queue is closed.</param>
	/// <param name="block">If true, blocks until an item is available; otherwise, returns immediately.</param>
	/// <returns>True if an item was peeked; otherwise, false.</returns>
	public bool TryPeek(out T value, bool exitOnClose = true, bool block = true)
	{
		lock (SyncRoot)
		{
			if (!WaitWhileEmpty(exitOnClose, block))
			{
				value = default;
				return false;
			}

			value = OnPeek();

			return true;
		}
	}

	/// <summary>
	/// Removes all items from the queue and notifies blocked threads.
	/// </summary>
	public void Clear()
	{
		lock (SyncRoot)
		{
			InnerCollection.Clear();
			Monitor.PulseAll(SyncRoot);
		}
	}

	/// <summary>
	/// Removes the specified item from the queue. This operation is not supported.
	/// </summary>
	/// <param name="item">The item to remove.</param>
	/// <returns>Always throws <see cref="NotSupportedException"/>.</returns>
	/// <exception cref="NotSupportedException">Thrown because removal of specific items is not supported.</exception>
	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Gets a value indicating whether the collection is read-only. Always returns false.
	/// </summary>
	bool ICollection<T>.IsReadOnly => false;

	/// <summary>
	/// Adds an item to the queue using the <see cref="Enqueue(T, bool)"/> method.
	/// </summary>
	/// <param name="item">The item to add.</param>
	void ICollection<T>.Add(T item)
	{
		Enqueue(item);
	}

	/// <summary>
	/// Determines whether the queue contains a specific item.
	/// </summary>
	/// <param name="item">The item to locate.</param>
	/// <returns>True if the item is found; otherwise, false.</returns>
	bool ICollection<T>.Contains(T item)
	{
		lock (SyncRoot)
			return InnerCollection.Contains(item);
	}

	/// <summary>
	/// Copies the elements of the queue to an array, starting at the specified index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		lock (SyncRoot)
			InnerCollection.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the queue.
	/// </summary>
	/// <returns>An enumerator for the queue.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		return InnerCollection.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the queue (non-generic version).
	/// </summary>
	/// <returns>An enumerator for the queue.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
#endif