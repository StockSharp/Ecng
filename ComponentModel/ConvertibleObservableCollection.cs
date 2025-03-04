namespace Ecng.ComponentModel;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Ecng.Collections;

/// <summary>
/// Collection which maps items to another type to display.
/// </summary>
public interface IConvertibleObservableCollection<TItem> : IListEx<TItem>
{
	/// <summary>
	/// Remove items for which <paramref name="pred"/> returns true.
	/// </summary>
	/// <param name="pred">Filter.</param>
	void RemoveAll(Func<TItem, bool> pred);
}

/// <summary>
/// Collection which maps items to another type to display.
/// </summary>
/// <typeparam name="TItem">Original item type.</typeparam>
/// <typeparam name="TDisplay">Display item type.</typeparam>
/// <remarks>
/// </remarks>
public class ConvertibleObservableCollection<TItem, TDisplay>(ICollection<TDisplay> collection, Func<TItem, TDisplay> converter) : BaseObservableCollection, IConvertibleObservableCollection<TItem>
	where TDisplay : class
{
	private readonly ICollection<TDisplay> _collection = collection ?? throw new ArgumentNullException(nameof(collection));
	private readonly Func<TItem, TDisplay> _converter = converter ?? throw new ArgumentNullException(nameof(converter));
	private readonly OrderedDictionary _convertedValues = [];

	readonly struct KVPair(TItem item, TDisplay display)
	{
		public TItem Item { get; } = item;
		public TDisplay Display { get; } = display;
	}

	private object SyncRoot => ((ICollection)_collection).SyncRoot;

	/// <summary>
	/// </summary>
	public TItem[] Items
	{
		get
		{
			lock (SyncRoot)
				return [.. _convertedValues.Keys.Cast<TItem>()];
		}
	}

	/// <summary>
	/// Get display item by item.
	/// </summary>
	public TDisplay TryGet(TItem item)
	{
		lock (SyncRoot)
			return _convertedValues.Contains(item) ? ((KVPair)_convertedValues[item]!).Display : default;
	}

	/// <summary>
	/// </summary>
	public event Action<IEnumerable<TItem>> AddedRange;

	/// <summary>
	/// </summary>
	public event Action<IEnumerable<TItem>> RemovedRange;

	/// <summary>
	/// </summary>
	public void AddRange(IEnumerable<TItem> items)
	{
		var arr = items.ToArray();
		var added = new List<TItem>();

		lock (SyncRoot)
		{
			var converted = new List<TDisplay>();

			foreach (var item in arr)
			{
				var display = _converter(item);

				if(_convertedValues.Contains(item))
					continue;

				_convertedValues.Add(item, new KVPair(item, display));

				converted.Add(display);
				added.Add(item);
			}

			if (converted.Count == 0)
				return;

			_collection.AddRange(converted);
		}

		AddedRange?.Invoke(added);
		CheckCount();
	}

	/// <summary>
	/// </summary>
	public void RemoveRange(IEnumerable<TItem> items)
	{
		var arr = items.ToArray();

		lock (SyncRoot)
		{
			var converted = new List<TDisplay>();

			foreach (var item in arr)
			{
				var display = TryGet(item);

				if (display == null)
					continue;

				_convertedValues.Remove(item);
				converted.Add(display);
			}

			_collection.RemoveRange(converted);
		}

		RemovedRange?.Invoke(arr);
	}

	/// <summary>
	/// </summary>
	public override int RemoveRange(int index, int count)
	{
		lock (SyncRoot)
		{
			var items = _convertedValues.Keys.Cast<TItem>().Skip(index).Take(count).ToArray();
			RemoveRange(items);
			return items.Length;
		}
	}

	/// <summary>
	/// </summary>
	public void Clear()
	{
		TItem[] removedItems;

		lock (SyncRoot)
		{
			removedItems = [.. _convertedValues.Keys.Cast<TItem>()];

			_convertedValues.Clear();
			_collection.Clear();
		}

		RemovedRange?.Invoke(removedItems);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>
	/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
	/// </returns>
	public IEnumerator<TItem> GetEnumerator()
	{
		lock (SyncRoot)
			return _convertedValues.Keys.Cast<TItem>().GetEnumerator();
	}

	/// <summary>
	/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
	/// </summary>
	/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
	public void Add(TItem item)
	{
		lock (SyncRoot)
		{
			var display = _converter(item);
			_convertedValues.Add(item, new KVPair(item, display));
			_collection.Add(display);
		}

		CheckCount();
	}

	/// <summary>
	/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
	/// </summary>
	/// <returns>
	/// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
	/// </returns>
	/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
	public bool Remove(TItem item)
	{
		lock (SyncRoot)
		{
			var display = TryGet(item);

			if (display == null)
				return false;

			_convertedValues.Remove(item);
			_collection.Remove(display);
			return true;
		}
	}

	/// <summary>
	/// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
	/// </summary>
	/// <returns>
	/// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
	/// </returns>
	public override int Count
	{
		get
		{
			lock (SyncRoot)
				return _convertedValues.Count;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
	/// </summary>
	/// <returns>
	/// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
	/// </returns>
	bool ICollection<TItem>.IsReadOnly => false;

	/// <summary>
	/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
	/// </summary>
	/// <returns>
	/// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
	/// </returns>
	/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
	public bool Contains(TItem item)
	{
		lock (SyncRoot)
			return _convertedValues.Contains(item);
	}

	/// <summary>
	/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
	/// </summary>
	/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
	public void CopyTo(TItem[] array, int arrayIndex)
	{
		lock (SyncRoot)
			_convertedValues.Keys.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
	/// </summary>
	/// <returns>
	/// The index of <paramref name="item"/> if found in the list; otherwise, -1.
	/// </returns>
	/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
	public int IndexOf(TItem item)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param><param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
	public void Insert(int index, TItem item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		if (_collection is not IList<TDisplay> coll)
			throw new NotSupportedException($"base collection must implement IList<{typeof(TDisplay).Name}>");

		lock (SyncRoot)
		{
			var pair = new KVPair(item, _converter(item));
			_convertedValues.Insert(index, item, pair);
			coll.Insert(index, pair.Display);
		}
	}

	/// <summary>
	/// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the item to remove.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
	public void RemoveAt(int index)
	{
		if (_collection is not IList<TDisplay> coll)
			throw new NotSupportedException($"base collection must implement IList<{typeof(TDisplay).Name}>");

		lock (SyncRoot)
		{
			_convertedValues.RemoveAt(index);
			coll.RemoveAt(index);
		}
	}

	/// <summary>
	/// Remove items for which <paramref name="pred"/> returns true.
	/// </summary>
	public void RemoveAll(Func<TItem, bool> pred)
	{
		if (_collection is not IList<TDisplay> coll)
			throw new NotSupportedException($"base collection must implement IList<{typeof(TDisplay).Name}>");

		lock (SyncRoot)
		{
			for (var i = _convertedValues.Count - 1; i >= 0; --i)
			{
				var kvp = (KVPair)_convertedValues[i];
				if (!pred(kvp.Item))
					continue;

				_convertedValues.RemoveAt(i);
				coll.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Gets or sets the element at the specified index.
	/// </summary>
	/// <returns>
	/// The element at the specified index.
	/// </returns>
	/// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
	public TItem this[int index]
	{
		get
		{
			lock (SyncRoot)
				return ((KVPair)_convertedValues[index]!).Item;
		}
		set => throw new NotSupportedException();
	}

	/// <summary>
	/// Returns an enumerator that iterates through a collection.
	/// </summary>
	/// <returns>
	/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
	/// </returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
