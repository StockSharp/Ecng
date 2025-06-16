namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a collection that notifies about its changes.
/// </summary>
/// <typeparam name="TItem">The type of elements in the collection.</typeparam>
public interface INotifyCollection<TItem> : ICollection<TItem>
{
	/// <summary>
	/// Occurs before adding an item, allowing cancellation.
	/// </summary>
	event Func<TItem, bool> Adding;

	/// <summary>
	/// Occurs after an item has been added.
	/// </summary>
	event Action<TItem> Added;

	/// <summary>
	/// Occurs before removing an item, allowing cancellation.
	/// </summary>
	event Func<TItem, bool> Removing;

	/// <summary>
	/// Occurs before removing an item at a specific index, allowing cancellation.
	/// </summary>
	event Func<int, bool> RemovingAt;

	/// <summary>
	/// Occurs after an item has been removed.
	/// </summary>
	event Action<TItem> Removed;

	/// <summary>
	/// Occurs before clearing the collection, allowing cancellation.
	/// </summary>
	event Func<bool> Clearing;

	/// <summary>
	/// Occurs after the collection has been cleared.
	/// </summary>
	event Action Cleared;

	/// <summary>
	/// Occurs before inserting an item at a specified index, allowing cancellation.
	/// </summary>
	event Func<int, TItem, bool> Inserting;

	/// <summary>
	/// Occurs after an item has been inserted at a specific index.
	/// </summary>
	event Action<int, TItem> Inserted;

	/// <summary>
	/// Occurs when a change happens in the collection.
	/// </summary>
	event Action Changed;
}

/// <summary>
/// Represents a list that notifies about its changes.
/// </summary>
/// <typeparam name="TItem">The type of elements in the list.</typeparam>
public interface INotifyList<TItem> : INotifyCollection<TItem>, IList<TItem>
{
}

/// <summary>
/// Represents an extended list that notifies about its changes.
/// </summary>
/// <typeparam name="TItem">The type of elements in the extended list.</typeparam>
public interface INotifyListEx<TItem> : INotifyList<TItem>, IListEx<TItem>
{
}