namespace Ecng.Collections;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Represents a thread-safe list that supports range operations and notification events.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
[Serializable]
public class SynchronizedList<T>(int capacity) : SynchronizedCollection<T, List<T>>(new List<T>(capacity)), INotifyListEx<T>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SynchronizedList{T}"/> class with default capacity.
	/// </summary>
	public SynchronizedList()
		: this(0)
	{
	}

	/// <summary>
	/// Retrieves an item at the specified index from the inner list.
	/// </summary>
	/// <param name="index">The zero-based index of the item to retrieve.</param>
	/// <returns>The item at the specified index.</returns>
	protected override T OnGetItem(int index)
	{
		return InnerCollection[index];
	}

	/// <summary>
	/// Inserts an item into the inner list at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index at which to insert the item.</param>
	/// <param name="item">The item to insert.</param>
	protected override void OnInsert(int index, T item)
	{
		InnerCollection.Insert(index, item);
	}

	/// <summary>
	/// Removes an item from the inner list at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the item to remove.</param>
	protected override void OnRemoveAt(int index)
	{
		InnerCollection.RemoveAt(index);
	}

	/// <summary>
	/// Returns the index of the specified item in the inner list.
	/// </summary>
	/// <param name="item">The item to locate.</param>
	/// <returns>The zero-based index of the item, or -1 if not found.</returns>
	protected override int OnIndexOf(T item)
	{
		return InnerCollection.IndexOf(item);
	}

	/// <summary>
	/// Occurs when a range of items is added to the list.
	/// </summary>
	public event Action<IEnumerable<T>> AddedRange;

	/// <summary>
	/// Occurs when a range of items is removed from the list.
	/// </summary>
	public event Action<IEnumerable<T>> RemovedRange;

	/// <summary>
	/// Called after an item is added to the list, raising the <see cref="AddedRange"/> event.
	/// </summary>
	/// <param name="item">The item that was added.</param>
	protected override void OnAdded(T item)
	{
		base.OnAdded(item);

		var evt = AddedRange;
		evt?.Invoke([item]);
	}

	/// <summary>
	/// Called after an item is removed from the list, raising the <see cref="RemovedRange"/> event.
	/// </summary>
	/// <param name="item">The item that was removed.</param>
	protected override void OnRemoved(T item)
	{
		base.OnRemoved(item);

		var evt = RemovedRange;
		evt?.Invoke([item]);
	}

	/// <summary>
	/// Adds a range of items to the list in a thread-safe manner.
	/// </summary>
	/// <param name="items">The items to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when an item is null and <see cref="BaseCollection{TItem,TCollection}.CheckNullableItems"/> is true.</exception>
	public void AddRange(IEnumerable<T> items)
	{
		using (SyncRoot.EnterScope())
		{
			var filteredItems = items.Where(t =>
			{
				if (CheckNullableItems && t.IsNull())
					throw new ArgumentNullException(nameof(t));

				return OnAdding(t);
			}).ToArray();
			InnerCollection.AddRange(filteredItems);
			filteredItems.ForEach(base.OnAdded);

			AddedRange?.Invoke(filteredItems);
		}
	}

	/// <summary>
	/// Removes a range of items from the list in a thread-safe manner.
	/// </summary>
	/// <param name="items">The items to remove.</param>
	public void RemoveRange(IEnumerable<T> items)
	{
		using (SyncRoot.EnterScope())
		{
			var filteredItems = items.Where(OnRemoving).ToArray();
			InnerCollection.RemoveRange(filteredItems);
			filteredItems.ForEach(base.OnRemoved);

			RemovedRange?.Invoke(filteredItems);
		}
	}

	/// <summary>
	/// Removes a range of items from the list starting at the specified index, in a thread-safe manner.
	/// </summary>
	/// <param name="index">The zero-based starting index of the range to remove.</param>
	/// <param name="count">The number of items to remove.</param>
	/// <returns>The number of items actually removed.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than -1 or <paramref name="count"/> is less than or equal to zero.</exception>
	public int RemoveRange(int index, int count)
	{
		if (index < 0)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (count <= 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		using (SyncRoot.EnterScope())
		{
			var realCount = Count;
			realCount -= index;
			InnerCollection.RemoveRange(index, count);
			return (realCount.Min(count)).Max(0);
		}
	}

	/// <summary>
	/// Retrieves a range of items from the list starting at the specified index, in a thread-safe manner.
	/// </summary>
	/// <param name="index">The zero-based starting index of the range to retrieve.</param>
	/// <param name="count">The number of items to retrieve.</param>
	/// <returns>An enumerable containing the specified range of items.</returns>
	public IEnumerable<T> GetRange(int index, int count)
	{
		using (SyncRoot.EnterScope())
			return InnerCollection.GetRange(index, count);
	}
}