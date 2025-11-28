namespace Ecng.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a priority queue that sorts elements based on a specified key.
/// </summary>
/// <typeparam name="TPriority">
/// The type used to determine the sort order of elements.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values stored in the queue.
/// </typeparam>
[Obsolete("Use PriorityQueue.")]
public class OrderedPriorityQueue<TPriority, TValue> : ICollection<KeyValuePair<TPriority, TValue>>, IQueue<KeyValuePair<TPriority, TValue>>
{
	private readonly SortedDictionary<TPriority, Queue<TValue>> _dictionary;

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderedPriorityQueue{TPriority, TValue}"/> class.
	/// </summary>
	public OrderedPriorityQueue()
	{
		_dictionary = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderedPriorityQueue{TPriority, TValue}"/> class using the specified comparer.
	/// </summary>
	/// <param name="comparer">The comparer used to sort the keys.</param>
	public OrderedPriorityQueue(IComparer<TPriority> comparer)
	{
		_dictionary = new SortedDictionary<TPriority, Queue<TValue>>(comparer);
	}

	#region Priority queue operations

	/// <summary>
	/// Enqueues element into priority queue
	/// </summary>
	/// <param name="priority">element priority</param>
	/// <param name="value">element value</param>
	public void Enqueue(TPriority priority, TValue value)
	{
		_dictionary.SafeAdd(priority).Enqueue(value);
		Count++;
	}

	/// <summary>
	/// Dequeues element with minimum priority and return its priority and value as <see cref="KeyValuePair{TPriority,TValue}"/> 
	/// </summary>
	/// <returns>priority and value of the dequeued element</returns>
	/// <remarks>
	/// Method throws <see cref="InvalidOperationException"/> if priority queue is empty
	/// </remarks>
	public KeyValuePair<TPriority, TValue> Dequeue()
	{
		if (IsEmpty)
			throw new InvalidOperationException("Priority queue is empty");

		var first = _dictionary.First();

		var item = first.Value.Dequeue();

		if (first.Value.Count == 0)
			_dictionary.Remove(first.Key);

		Count--;

		return new KeyValuePair<TPriority, TValue>(first.Key, item);
	}

	/// <summary>
	/// Dequeues element with minimum priority and return its priority and value as <see cref="KeyValuePair{TPriority,TValue}"/> 
	/// </summary>
	/// <returns>priority and value of the dequeued element</returns>
	/// <remarks>
	/// Method throws <see cref="InvalidOperationException"/> if priority queue is empty
	/// </remarks>
	public TValue DequeueValue()
	{
		return Dequeue().Value;
	}

	/// <summary>
	/// Returns priority and value of the element with minimun priority, without removing it from the queue
	/// </summary>
	/// <returns>priority and value of the element with minimum priority</returns>
	/// <remarks>
	/// Method throws <see cref="InvalidOperationException"/> if priority queue is empty
	/// </remarks>
	public KeyValuePair<TPriority, TValue> Peek()
	{
		if (IsEmpty)
			throw new InvalidOperationException("Priority queue is empty");

		var first = _dictionary.First();
		var item = first.Value.Peek();

		return new KeyValuePair<TPriority, TValue>(first.Key, item);
	}

	/// <summary>
	/// Returns priority and value of the element with minimun priority, without removing it from the queue
	/// </summary>
	/// <returns>priority and value of the element with minimum priority</returns>
	/// <remarks>
	/// Method throws <see cref="InvalidOperationException"/> if priority queue is empty
	/// </remarks>
	public TValue PeekValue()
	{
		return Peek().Value;
	}

	/// <summary>
	/// Gets whether priority queue is empty
	/// </summary>
	public bool IsEmpty => Count == 0;

	#endregion

	#region ICollection<KeyValuePair<TPriority, TValue>> implementation

	/// <summary>
	/// Enqueus element into priority queue
	/// </summary>
	/// <param name="item">element to add</param>
	public void Add(KeyValuePair<TPriority, TValue> item)
	{
		Enqueue(item.Key, item.Value);
	}

	/// <summary>
	/// Clears the collection
	/// </summary>
	public void Clear()
	{
		_dictionary.Clear();
		Count = 0;
	}

	/// <summary>
	/// Determines whether the priority queue contains a specific element
	/// </summary>
	/// <param name="item">The object to locate in the priority queue</param>
	/// <returns><c>true</c> if item is found in the priority queue; otherwise, <c>false.</c> </returns>
	public bool Contains(KeyValuePair<TPriority, TValue> item)
	{
		if (!_dictionary.TryGetValue(item.Key, out var dic))
			return false;

		return dic.Contains(item.Value);
	}

	/// <summary>
	/// Gets number of elements in the priority queue
	/// </summary>
	public int Count { get; private set; }

	/// <summary>
	/// Copies the elements of the priority queue to an Array, starting at a particular Array index. 
	/// </summary>
	/// <param name="array">The one-dimensional Array that is the destination of the elements copied from the priority queue. The Array must have zero-based indexing. </param>
	/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
	/// <remarks>
	/// It is not guaranteed that items will be copied in the sorted order.
	/// </remarks>
	public void CopyTo(KeyValuePair<TPriority, TValue>[] array, int arrayIndex)
	{
		AllItems.ToArray().CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Gets a value indicating whether the collection is read-only. 
	/// </summary>
	/// <remarks>
	/// For priority queue this property returns <c>false</c>.
	/// </remarks>
	public bool IsReadOnly => false;

	/// <summary>
	/// Removes the first occurrence of a specific object from the priority queue. 
	/// </summary>
	/// <param name="item">The object to remove from the <see cref="ICollection{T}"/>.</param>
	/// <returns><c>true</c> if item was successfully removed from the priority queue.
	/// This method returns false if item is not found in the collection. </returns>
	public bool Remove(KeyValuePair<TPriority, TValue> item)
	{
		return Remove(item.Key, [item.Value]);
	}

	/// <summary>
	/// Removes the range of elements from the priority queue.
	/// </summary>
	/// <param name="items">The range of elements to remove from the priority queue.</param>
	public void RemoveRange(IEnumerable<KeyValuePair<TPriority, TValue>> items)
	{
		var groups = items.GroupBy(i => i.Key, i => i.Value);

		foreach (var g in groups)
		{
			Remove(g.Key, [.. g]);
		}
	}

	private bool Remove(TPriority key, ICollection<TValue> items)
	{
		if (!_dictionary.TryGetValue(key, out var queue))
			return false;

		_dictionary.Remove(key);

		while (queue.Count > 0)
		{
			var queueItem = queue.Dequeue();

			if (items.Contains(queueItem))
			{
				items.Remove(queueItem);
				Count--;
				continue;
			}

			_dictionary.SafeAdd(key).Enqueue(queueItem);
		}

		return true;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>Enumerator</returns>
	/// <remarks>
	/// Returned enumerator does not iterate elements in sorted order.</remarks>
	public IEnumerator<KeyValuePair<TPriority, TValue>> GetEnumerator()
	{
		return AllItems.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>Enumerator</returns>
	/// <remarks>
	/// Returned enumerator does not iterate elements in sorted order.</remarks>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private IEnumerable<KeyValuePair<TPriority, TValue>> AllItems
	{
		get { return _dictionary.SelectMany(d => d.Value.Select(v => new KeyValuePair<TPriority, TValue>(d.Key, v))); }
	}

	#endregion

	void IQueue<KeyValuePair<TPriority, TValue>>.Enqueue(KeyValuePair<TPriority, TValue> item) =>
		Enqueue(item.Key, item.Value);
}