namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a base class for a strongly-typed list of items.
/// </summary>
/// <typeparam name="TItem">The type of elements in the list.</typeparam>
[Serializable]
public abstract class BaseList<TItem>(IList<TItem> innerList) : BaseCollection<TItem, IList<TItem>>(innerList)
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseList{TItem}"/> class with an empty inner list.
	/// </summary>
	protected BaseList()
		: this([])
	{
	}

	/// <summary>
	/// Retrieves the item at the specified index from the inner list.
	/// </summary>
	/// <param name="index">The zero-based index of the item to retrieve.</param>
	/// <returns>The item at the specified index.</returns>
	protected override TItem OnGetItem(int index)
	{
		return InnerCollection[index];
	}

	/// <summary>
	/// Inserts an item into the inner list at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index at which the item should be inserted.</param>
	/// <param name="item">The item to insert into the list.</param>
	protected override void OnInsert(int index, TItem item)
	{
		InnerCollection.Insert(index, item);
	}

	/// <summary>
	/// Removes the item at the specified index from the inner list.
	/// </summary>
	/// <param name="index">The zero-based index of the item to remove.</param>
	protected override void OnRemoveAt(int index)
	{
		InnerCollection.RemoveAt(index);
	}

	/// <summary>
	/// Determines the index of a specific item in the inner list.
	/// </summary>
	/// <param name="item">The item to locate in the list.</param>
	/// <returns>The index of the item if found in the list; otherwise, -1.</returns>
	public override int IndexOf(TItem item)
	{
		return InnerCollection.IndexOf(item);
	}
}