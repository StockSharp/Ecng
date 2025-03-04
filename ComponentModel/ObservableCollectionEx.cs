namespace Ecng.ComponentModel;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

using Ecng.Collections;

/// <summary>
/// Represents an observable collection with extended methods, including range operations.
/// </summary>
/// <typeparam name="TItem">The type of items contained in the collection.</typeparam>
public class ObservableCollectionEx<TItem> : IListEx<TItem>, IList, INotifyCollectionChanged, INotifyPropertyChanged
{
	private readonly List<TItem> _items = [];

	/// <summary>
	/// Occurs when the collection changes.
	/// </summary>
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	/// <summary>
	/// Occurs when a property value changes.
	/// </summary>
	public event PropertyChangedEventHandler PropertyChanged;

	private Action<IEnumerable<TItem>> _addedRange;

	/// <summary>
	/// Occurs after a range of items has been added to the collection.
	/// </summary>
	public event Action<IEnumerable<TItem>> AddedRange
	{
		add => _addedRange += value;
		remove => _addedRange -= value;
	}

	private Action<IEnumerable<TItem>> _removedRange;

	/// <summary>
	/// Occurs after a range of items has been removed from the collection.
	/// </summary>
	public event Action<IEnumerable<TItem>> RemovedRange
	{
		add => _removedRange += value;
		remove => _removedRange -= value;
	}

	/// <summary>
	/// Adds a range of items to the collection.
	/// </summary>
	/// <param name="items">The items to add.</param>
	public virtual void AddRange(IEnumerable<TItem> items)
	{
		var arr = items.ToArray();

		if (arr.Length == 0)
			return;

		var index = _items.Count;

		_items.AddRange(arr);

		OnCountPropertyChanged();
		OnIndexerPropertyChanged();

		OnCollectionChanged(NotifyCollectionChangedAction.Add, arr, index);
	}

	/// <summary>
	/// Removes a range of items from the collection.
	/// </summary>
	/// <param name="items">The items to remove.</param>
	public virtual void RemoveRange(IEnumerable<TItem> items)
	{
		var arr = items.ToArray();

		if (arr.Length > 10000 || arr.Length > Count * 0.1)
		{
			var temp = new HashSet<TItem>(_items);
			temp.RemoveRange(arr);

			Clear();
			AddRange(temp);
		}
		else
			arr.ForEach(i => Remove(i));
	}

	/// <summary>
	/// Removes a specified range of items starting at a given index.
	/// </summary>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of items to remove.</param>
	/// <returns>The number of removed items.</returns>
	public virtual int RemoveRange(int index, int count)
	{
		var items = _items.GetRange(index, count).ToArray();

		if (items.Length == 0)
			return 0;

		_items.RemoveRange(index, count);

		OnRemove(items, index);

		return items.Length;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the collection.</returns>
	public IEnumerator<TItem> GetEnumerator()
	{
		return _items.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Adds an item to the collection.
	/// </summary>
	/// <param name="item">The item to add.</param>
	public virtual void Add(TItem item)
	{
		AddRange([item]);
	}

	/// <summary>
	/// Removes the first occurrence of a specific item from the collection.
	/// </summary>
	/// <param name="item">The item to remove.</param>
	/// <returns>true if the item was successfully removed; otherwise, false.</returns>
	public virtual bool Remove(TItem item)
	{
		var index = _items.IndexOf(item);

		if (index == -1)
			return false;

		_items.RemoveAt(index);

		OnRemove([item], index);
		return true;
	}

	int IList.Add(object value)
	{
		Add((TItem)value);
		return Count - 1;
	}

	bool IList.Contains(object value)
	{
		return Contains((TItem)value);
	}

	/// <summary>
	/// Removes all items from the collection.
	/// </summary>
	public virtual void Clear()
	{
		_items.Clear();

		OnCountPropertyChanged();
		OnIndexerPropertyChanged();
		OnCollectionReset();
	}

	int IList.IndexOf(object value)
	{
		return IndexOf((TItem)value);
	}

	void IList.Insert(int index, object value)
	{
		Insert(index, (TItem)value);
	}

	void IList.Remove(object value)
	{
		Remove((TItem)value);
	}

	/// <summary>
	/// Determines whether the collection contains a specific value.
	/// </summary>
	/// <param name="item">The item to locate.</param>
	/// <returns>true if the item is found; otherwise, false.</returns>
	public bool Contains(TItem item)
	{
		return _items.Contains(item);
	}

	/// <summary>
	/// Copies the elements of the collection to an array, starting at a particular index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index at which copying begins.</param>
	public void CopyTo(TItem[] array, int arrayIndex)
	{
		_items.CopyTo(array, arrayIndex);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array is not TItem[] items)
			items = [.. array.Cast<TItem>()];

		CopyTo(items, index);
	}

	/// <summary>
	/// Gets the number of elements contained in the collection.
	/// </summary>
	public int Count => _items.Count;

	object ICollection.SyncRoot => this;

	bool ICollection.IsSynchronized => true;

	/// <summary>
	/// Gets a value indicating whether the collection is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	bool IList.IsFixedSize => false;

	/// <summary>
	/// Determines the index of a specific item in the collection.
	/// </summary>
	/// <param name="item">The item to locate.</param>
	/// <returns>The index of the item if found; otherwise, -1.</returns>
	public int IndexOf(TItem item)
	{
		return _items.IndexOf(item);
	}

	/// <summary>
	/// Inserts an item into the collection at the specified index.
	/// </summary>
	/// <param name="index">The index at which the item should be inserted.</param>
	/// <param name="item">The item to insert.</param>
	public void Insert(int index, TItem item)
	{
		_items.Insert(index, item);

		OnCountPropertyChanged();
		OnIndexerPropertyChanged();

		OnCollectionChanged(NotifyCollectionChangedAction.Add, [item], index);
	}

	/// <summary>
	/// Removes the item at the specified index of the collection.
	/// </summary>
	/// <param name="index">The zero-based index of the item to remove.</param>
	public void RemoveAt(int index)
	{
		var item = _items[index];
		_items.RemoveAt(index);

		OnRemove([item], index);
	}

	object IList.this[int index]
	{
		get => this[index];
		set => this[index] = (TItem)value;
	}

	/// <summary>
	/// Gets or sets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to get or set.</param>
	/// <returns>The element at the specified index.</returns>
	public TItem this[int index]
	{
		get => _items[index];
		set
		{
			var originalItem = _items[index];
			_items[index] = value;

			OnIndexerPropertyChanged();
			OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, value, index);
		}
	}

	/// <summary>
	/// Raises the collection changed event for a removal.
	/// </summary>
	/// <param name="items">The items removed.</param>
	/// <param name="index">The index from which the items were removed.</param>
	private void OnRemove(IList<TItem> items, int index)
	{
		OnCountPropertyChanged();
		OnIndexerPropertyChanged();

		OnCollectionChanged(NotifyCollectionChangedAction.Remove, items, index);
	}

	/// <summary>
	/// Raises the PropertyChanged event.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	protected void OnPropertyChanged(string propertyName)
	{
		var evt = PropertyChanged;
		evt?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void OnCountPropertyChanged() => OnPropertyChanged("Count");

	// This must agree with Binding.IndexerName.  It is declared separately
	// here so as to avoid a dependency on PresentationFramework.dll.
	private void OnIndexerPropertyChanged() => OnPropertyChanged("Item[]");

	private void OnCollectionChanged(NotifyCollectionChangedAction action, IList<TItem> items, int index)              => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, (IList)items, index));
	private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)                     => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
	private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)       => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
	private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)  => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));

	/// <summary>
	/// Raises the CollectionChanged event.
	/// </summary>
	/// <param name="args">Details about the change.</param>
	private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
	{
		if (args.OldItems?.Count > 0)
			_removedRange?.Invoke(args.OldItems.Cast<TItem>());

		if (args.NewItems?.Count > 0)
			_addedRange?.Invoke(args.NewItems.Cast<TItem>());

		var evt = CollectionChanged;
		if (evt == null)
			return;

		ProcessCollectionChanged(evt.GetInvocationList().Cast<NotifyCollectionChangedEventHandler>(), args);
	}

	/// <summary>
	/// Processes the NotifyCollectionChangedEventHandler subscribers.
	/// </summary>
	/// <param name="subscribers">The collection of subscribers.</param>
	/// <param name="args">Details about the change.</param>
	protected virtual void ProcessCollectionChanged(IEnumerable<NotifyCollectionChangedEventHandler> subscribers, NotifyCollectionChangedEventArgs args)
	{
		foreach (var subscriber in subscribers)
			subscriber(this, args);
	}

	/// <summary>
	/// Raises the CollectionChanged event with a reset action.
	/// </summary>
	private void OnCollectionReset()
	{
		var evt = CollectionChanged;
		evt?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}
}
