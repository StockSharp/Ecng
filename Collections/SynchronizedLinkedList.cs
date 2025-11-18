namespace Ecng.Collections;

using System.Collections;
using System.Collections.Generic;

using Ecng.Common;

/// <summary>
/// Represents a thread-safe linked list that provides synchronization for its operations.
/// </summary>
/// <typeparam name="T">The type of elements in the linked list.</typeparam>
public class SynchronizedLinkedList<T> : ISynchronizedCollection<T>
{
	private readonly LinkedList<T> _inner = new();
	private readonly SyncObject _syncRoot = new();

	/// <summary>
	/// Gets the synchronization root object used to synchronize access to the linked list.
	/// </summary>
	public SyncObject SyncRoot => _syncRoot;

	/// <summary>
	/// Gets the first node of the linked list.
	/// </summary>
	public virtual LinkedListNode<T> First
	{
		get
		{
			lock (SyncRoot)
				return _inner.First;
		}
	}

	/// <summary>
	/// Gets the last node of the linked list.
	/// </summary>
	public virtual LinkedListNode<T> Last
	{
		get
		{
			lock (SyncRoot)
				return _inner.Last;
		}
	}

	/// <summary>
	/// Adds a new node containing the specified value before the specified existing node.
	/// </summary>
	/// <param name="node">The node before which the new node should be added.</param>
	/// <param name="value">The value to add to the linked list.</param>
	public virtual void AddBefore(LinkedListNode<T> node, T value)
	{
		lock (SyncRoot)
			_inner.AddBefore(node, value);
	}

	/// <summary>
	/// Adds the specified new node before the specified existing node.
	/// </summary>
	/// <param name="node">The node before which the new node should be added.</param>
	/// <param name="newNode">The new node to add.</param>
	public virtual void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
	{
		lock (SyncRoot)
			_inner.AddBefore(node, newNode);
	}

	/// <summary>
	/// Adds a new node containing the specified value at the start of the linked list.
	/// </summary>
	/// <param name="value">The value to add to the linked list.</param>
	public virtual void AddFirst(T value)
	{
		lock (SyncRoot)
			_inner.AddFirst(value);
	}

	/// <summary>
	/// Adds the specified new node at the start of the linked list.
	/// </summary>
	/// <param name="node">The new node to add.</param>
	public virtual void AddFirst(LinkedListNode<T> node)
	{
		lock (SyncRoot)
			_inner.AddFirst(node);
	}

	/// <summary>
	/// Adds a new node containing the specified value at the end of the linked list.
	/// </summary>
	/// <param name="value">The value to add to the linked list.</param>
	public virtual void AddLast(T value)
	{
		lock (SyncRoot)
			_inner.AddLast(value);
	}

	/// <summary>
	/// Adds the specified new node at the end of the linked list.
	/// </summary>
	/// <param name="node">The new node to add.</param>
	public virtual void AddLast(LinkedListNode<T> node)
	{
		lock (SyncRoot)
			_inner.AddLast(node);
	}

	/// <summary>
	/// Removes the specified node from the linked list.
	/// </summary>
	/// <param name="node">The node to remove.</param>
	public virtual void Remove(LinkedListNode<T> node)
	{
		lock (SyncRoot)
			_inner.Remove(node);
	}

	/// <summary>
	/// Removes the node at the start of the linked list.
	/// </summary>
	public virtual void RemoveFirst()
	{
		lock (SyncRoot)
			_inner.RemoveFirst();
	}

	/// <summary>
	/// Removes the node at the end of the linked list.
	/// </summary>
	public virtual void RemoveLast()
	{
		lock (SyncRoot)
			_inner.RemoveLast();
	}

	/// <summary>
	/// Finds the first node that contains the specified value.
	/// </summary>
	/// <param name="value">The value to locate in the linked list.</param>
	/// <returns>The first node that contains the specified value, if found; otherwise, <c>null</c>.</returns>
	public virtual LinkedListNode<T> Find(T value)
	{
		lock (SyncRoot)
			return _inner.Find(value);
	}

	/// <summary>
	/// Finds the last node that contains the specified value.
	/// </summary>
	/// <param name="value">The value to locate in the linked list.</param>
	/// <returns>The last node that contains the specified value, if found; otherwise, <c>null</c>.</returns>
	public virtual LinkedListNode<T> FindLast(T value)
	{
		lock (SyncRoot)
			return _inner.FindLast(value);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the linked list.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the linked list.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		lock (SyncRoot)
			return _inner.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the linked list.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the linked list.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Adds an item to the linked list.
	/// </summary>
	/// <param name="item">The item to add to the linked list.</param>
	void ICollection<T>.Add(T item)
	{
		lock (SyncRoot)
			((ICollection<T>)_inner).Add(item);
	}

	/// <summary>
	/// Removes all nodes from the linked list.
	/// </summary>
	public void Clear()
	{
		lock (SyncRoot)
			_inner.Clear();
	}

	/// <summary>
	/// Determines whether the linked list contains the specified value.
	/// </summary>
	/// <param name="item">The value to locate in the linked list.</param>
	/// <returns><c>true</c> if the value is found in the linked list; otherwise, <c>false</c>.</returns>
	public bool Contains(T item)
	{
		lock (SyncRoot)
			return _inner.Contains(item);
	}

	/// <summary>
	/// Copies the elements of the linked list to an array, starting at a particular array index.
	/// </summary>
	/// <param name="array">The one-dimensional array that is the destination of the elements copied from the linked list.</param>
	/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
	public void CopyTo(T[] array, int arrayIndex)
	{
		lock (SyncRoot)
			_inner.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Removes the first occurrence of the specified value from the linked list.
	/// </summary>
	/// <param name="item">The value to remove from the linked list.</param>
	/// <returns><c>true</c> if the value was successfully removed; otherwise, <c>false</c>.</returns>
	public bool Remove(T item)
	{
		lock (SyncRoot)
			return _inner.Remove(item);
	}

	/// <summary>
	/// Gets the number of nodes contained in the linked list.
	/// </summary>
	public int Count
	{
		get
		{
			lock (SyncRoot)
				return _inner.Count;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the linked list is read-only.
	/// </summary>
	bool ICollection<T>.IsReadOnly => false;
}