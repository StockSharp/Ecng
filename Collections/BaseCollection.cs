namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Common;

	/// <summary>
	/// Represents a base class for a generic collection with event notifications and inner collection management.
	/// </summary>
	/// <typeparam name="TItem">The type of elements in the collection.</typeparam>
	/// <typeparam name="TCollection">The type of the inner collection, which must implement <see cref="ICollection{TItem}"/>.</typeparam>
	[Serializable]
	public abstract class BaseCollection<TItem, TCollection> : ICollection<TItem>, ICollection, INotifyList<TItem>, IList
		where TCollection : ICollection<TItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseCollection{TItem, TCollection}"/> class with the specified inner collection.
		/// </summary>
		/// <param name="innerCollection">The inner collection to manage.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="innerCollection"/> is null.</exception>
		protected BaseCollection(TCollection innerCollection)
		{
			if (innerCollection.IsNull())
				throw new ArgumentNullException(nameof(innerCollection));

			InnerCollection = innerCollection;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to check for null items in the collection.
		/// </summary>
		public bool CheckNullableItems { get; set; }

		/// <summary>
		/// Gets the inner collection that stores the items.
		/// </summary>
		protected TCollection InnerCollection { get; }

		/// <summary>
		/// Retrieves an item at the specified index from the inner collection.
		/// </summary>
		/// <param name="index">The zero-based index of the item to retrieve.</param>
		/// <returns>The item at the specified index.</returns>
		protected abstract TItem OnGetItem(int index);

		/// <summary>
		/// Inserts an item into the inner collection at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which to insert the item.</param>
		/// <param name="item">The item to insert.</param>
		protected abstract void OnInsert(int index, TItem item);

		/// <summary>
		/// Removes an item from the inner collection at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		protected abstract void OnRemoveAt(int index);

		/// <summary>
		/// Adds an item to the inner collection.
		/// </summary>
		/// <param name="item">The item to add.</param>
		protected virtual void OnAdd(TItem item)
		{
			InnerCollection.Add(item);
		}

		/// <summary>
		/// Removes an item from the inner collection.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if the item was removed; otherwise, false.</returns>
		protected virtual bool OnRemove(TItem item)
		{
			return InnerCollection.Remove(item);
		}

		/// <summary>
		/// Removes all items from the inner collection.
		/// </summary>
		protected virtual void OnClear()
		{
			InnerCollection.Clear();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection (non-generic version).
		/// </summary>
		/// <returns>An enumerator for the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable<TItem>)this).GetEnumerator();

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator for the collection.</returns>
		public virtual IEnumerator<TItem> GetEnumerator()
			=> InnerCollection.GetEnumerator();

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public virtual int Count => InnerCollection.Count;

		/// <summary>
		/// Determines whether the collection contains a specific item.
		/// </summary>
		/// <param name="item">The item to locate.</param>
		/// <returns>True if the item is found; otherwise, false.</returns>
		public virtual bool Contains(TItem item) => InnerCollection.Contains(item);

		/// <summary>
		/// Validates the specified index to ensure it is within the valid range.
		/// </summary>
		/// <param name="index">The index to check.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the index is less than 0 or greater than <see cref="Count"/>.</exception>
		private void CheckIndex(int index)
		{
			if (index < 0 || index > Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index has incorrect value.");
		}

		/// <summary>
		/// Gets or sets the item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item.</param>
		/// <returns>The item at the specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the index is invalid.</exception>
		public virtual TItem this[int index]
		{
			get => OnGetItem(index);
			set
			{
				CheckIndex(index);

				if (index < Count)
				{
					RemoveAt(index);
					Insert(index, value);
				}
				else
					Add(value);
			}
		}

		/// <summary>
		/// Returns the index of the specified item in the collection.
		/// </summary>
		/// <param name="item">The item to locate.</param>
		/// <returns>The zero-based index of the item, or -1 if not found.</returns>
		public abstract int IndexOf(TItem item);

		/// <summary>
		/// Removes the item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public virtual void RemoveAt(int index)
		{
			if (OnRemovingAt(index))
			{
				OnRemoveAt(index);
				OnRemovedAt(index);
			}
		}

		/// <summary>
		/// Inserts an item into the collection at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which to insert the item.</param>
		/// <param name="item">The item to insert.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the index is invalid.</exception>
		public virtual void Insert(int index, TItem item)
		{
			CheckIndex(index);

			if (index == Count)
				Add(item);
			else
			{
				OnInserting(index, item);
				OnInsert(index, item);
				OnInserted(index, item);
			}
		}

		/// <summary>
		/// Gets a value indicating whether the collection is read-only.
		/// </summary>
		public virtual bool IsReadOnly => false;

		/// <summary>
		/// Adds an item to the collection.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <see cref="CheckNullableItems"/> is true.</exception>
		public virtual void Add(TItem item)
		{
			if (CheckNullableItems && item.IsNull())
				throw new ArgumentNullException(nameof(item));

			if (OnAdding(item))
			{
				OnAdd(item);
				OnAdded(item);
			}
		}

		/// <summary>
		/// Removes all items from the collection.
		/// </summary>
		public virtual void Clear()
		{
			if (OnClearing())
			{
				OnClear();
				OnCleared();
			}
		}

		/// <summary>
		/// Removes the specified item from the collection.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if the item was removed; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null and <see cref="CheckNullableItems"/> is true.</exception>
		public virtual bool Remove(TItem item)
		{
			if (CheckNullableItems && item.IsNull())
				throw new ArgumentNullException(nameof(item));

			if (OnRemoving(item))
			{
				if (OnRemove(item))
				{
					OnRemoved(item);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Occurs before an item is added to the collection.
		/// </summary>
		public event Func<TItem, bool> Adding;

		/// <summary>
		/// Occurs after an item has been added to the collection.
		/// </summary>
		public event Action<TItem> Added;

		/// <summary>
		/// Occurs before an item is removed from the collection.
		/// </summary>
		public event Func<TItem, bool> Removing;

		/// <summary>
		/// Occurs after an item has been removed from the collection.
		/// </summary>
		public event Action<TItem> Removed;

		/// <summary>
		/// Occurs before an item is removed at the specified index.
		/// </summary>
		public event Func<int, bool> RemovingAt;

		/// <summary>
		/// Occurs after an item has been removed at the specified index.
		/// </summary>
		public event Action<int> RemovedAt;

		/// <summary>
		/// Occurs before the collection is cleared.
		/// </summary>
		public event Func<bool> Clearing;

		/// <summary>
		/// Occurs after the collection has been cleared.
		/// </summary>
		public event Action Cleared;

		/// <summary>
		/// Occurs before an item is inserted at the specified index.
		/// </summary>
		public event Func<int, TItem, bool> Inserting;

		/// <summary>
		/// Occurs after an item has been inserted at the specified index.
		/// </summary>
		public event Action<int, TItem> Inserted;

		/// <summary>
		/// Occurs when the collection has been modified.
		/// </summary>
		public event Action Changed;

		/// <summary>
		/// Invokes the <see cref="Inserting"/> event before inserting an item.
		/// </summary>
		/// <param name="index">The zero-based index at which to insert the item.</param>
		/// <param name="item">The item to insert.</param>
		/// <returns>True if the insertion should proceed; otherwise, false.</returns>
		protected virtual bool OnInserting(int index, TItem item)
		{
			return Inserting?.Invoke(index, item) ?? true;
		}

		/// <summary>
		/// Invokes the <see cref="Inserted"/> event and notifies of a change after inserting an item.
		/// </summary>
		/// <param name="index">The zero-based index at which the item was inserted.</param>
		/// <param name="item">The inserted item.</param>
		protected virtual void OnInserted(int index, TItem item)
		{
			Inserted?.Invoke(index, item);
			OnChanged();
		}

		/// <summary>
		/// Invokes the <see cref="Adding"/> event before adding an item.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <returns>True if the addition should proceed; otherwise, false.</returns>
		protected virtual bool OnAdding(TItem item)
		{
			return Adding?.Invoke(item) ?? true;
		}

		/// <summary>
		/// Invokes the <see cref="Added"/> event and notifies of a change after adding an item.
		/// </summary>
		/// <param name="item">The added item.</param>
		protected virtual void OnAdded(TItem item)
		{
			Added?.Invoke(item);
			OnChanged();
		}

		/// <summary>
		/// Invokes the <see cref="Clearing"/> event before clearing the collection.
		/// </summary>
		/// <returns>True if the clearing should proceed; otherwise, false.</returns>
		protected virtual bool OnClearing()
		{
			return Clearing?.Invoke() ?? true;
		}

		/// <summary>
		/// Invokes the <see cref="Cleared"/> event and notifies of a change after clearing the collection.
		/// </summary>
		protected virtual void OnCleared()
		{
			Cleared?.Invoke();
			OnChanged();
		}

		/// <summary>
		/// Invokes the <see cref="Removing"/> event before removing an item.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if the removal should proceed; otherwise, false.</returns>
		protected virtual bool OnRemoving(TItem item)
		{
			return Removing?.Invoke(item) ?? true;
		}

		/// <summary>
		/// Invokes the <see cref="Removed"/> event and notifies of a change after removing an item.
		/// </summary>
		/// <param name="item">The removed item.</param>
		protected virtual void OnRemoved(TItem item)
		{
			Removed?.Invoke(item);
			OnChanged();
		}

		/// <summary>
		/// Invokes the <see cref="RemovingAt"/> event before removing an item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <returns>True if the removal should proceed; otherwise, false.</returns>
		protected virtual bool OnRemovingAt(int index)
		{
			return RemovingAt?.Invoke(index) ?? true;
		}

		/// <summary>
		/// Invokes the <see cref="RemovedAt"/> event and notifies of a change after removing an item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the removed item.</param>
		protected virtual void OnRemovedAt(int index)
		{
			RemovedAt?.Invoke(index);
			OnChanged();
		}

		/// <summary>
		/// Invokes the <see cref="Changed"/> event to notify of a modification in the collection.
		/// </summary>
		protected virtual void OnChanged()
		{
			Changed?.Invoke();
		}

		#region IList Members

		/// <summary>
		/// Determines whether the collection contains a specific value (non-generic).
		/// </summary>
		/// <param name="value">The value to locate.</param>
		/// <returns>True if the value is found and compatible; otherwise, false.</returns>
		bool IList.Contains(object value)
		{
			if (!IsCompatible(value))
				return false;

			return Contains((TItem)value);
		}

		/// <summary>
		/// Adds a value to the collection (non-generic).
		/// </summary>
		/// <param name="value">The value to add.</param>
		/// <returns>The new count of items in the collection.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null and incompatible with <typeparamref name="TItem"/>.</exception>
		int IList.Add(object value)
		{
			if (!IsCompatible(value))
				throw new ArgumentNullException(nameof(value));

			Add((TItem)value);
			return Count;
		}

		/// <summary>
		/// Gets a value indicating whether the collection is read-only (non-generic).
		/// </summary>
		bool IList.IsReadOnly => false;

		/// <summary>
		/// Gets or sets the item at the specified index (non-generic).
		/// </summary>
		/// <param name="index">The zero-based index of the item.</param>
		/// <returns>The item at the specified index.</returns>
		/// <exception cref="ArgumentNullException">Thrown when setting a value that is null and incompatible with <typeparamref name="TItem"/>.</exception>
		object IList.this[int index]
		{
			get => this[index];
			set
			{
				if (!IsCompatible(value))
					throw new ArgumentNullException(nameof(value));

				this[index] = (TItem)value;
			}
		}

		/// <summary>
		/// Removes all items from the collection (non-generic).
		/// </summary>
		void IList.Clear()
		{
			((ICollection<TItem>)this).Clear();
		}

		/// <summary>
		/// Returns the index of a specific value in the collection (non-generic).
		/// </summary>
		/// <param name="value">The value to locate.</param>
		/// <returns>The zero-based index of the value, or -1 if not found or incompatible.</returns>
		int IList.IndexOf(object value)
		{
			if (!IsCompatible(value))
				return -1;

			return IndexOf((TItem)value);
		}

		/// <summary>
		/// Inserts a value into the collection at the specified index (non-generic).
		/// </summary>
		/// <param name="index">The zero-based index at which to insert the value.</param>
		/// <param name="value">The value to insert.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null and incompatible with <typeparamref name="TItem"/>.</exception>
		void IList.Insert(int index, object value)
		{
			if (!IsCompatible(value))
				throw new ArgumentNullException(nameof(value));

			Insert(index, (TItem)value);
		}

		/// <summary>
		/// Removes a specific value from the collection (non-generic).
		/// </summary>
		/// <param name="value">The value to remove.</param>
		void IList.Remove(object value)
		{
			if (!IsCompatible(value))
				return;

			Remove((TItem)value);
		}

		/// <summary>
		/// Removes the item at the specified index (non-generic).
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		void IList.RemoveAt(int index)
		{
			RemoveAt(index);
		}

		/// <summary>
		/// Gets a value indicating whether the collection has a fixed size (non-generic). Always returns false.
		/// </summary>
		bool IList.IsFixedSize => false;

		#endregion

		private static readonly bool _isValueType = typeof(TItem).IsValueType;

		/// <summary>
		/// Determines whether a value is compatible with the collection's item type.
		/// </summary>
		/// <param name="value">The value to check.</param>
		/// <returns>True if the value is compatible; otherwise, false.</returns>
		private static bool IsCompatible(object value) => !_isValueType || value != null;

		/// <summary>
		/// Gets a value indicating whether the collection is synchronized (non-generic). Always returns false.
		/// </summary>
		bool ICollection.IsSynchronized => false;

		/// <summary>
		/// Gets the synchronization root object (non-generic). Returns the current instance.
		/// </summary>
		object ICollection.SyncRoot => this;

		/// <summary>
		/// Copies the elements of the collection to an array (non-generic).
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="index">The zero-based index in the array at which copying begins.</param>
		void ICollection.CopyTo(Array array, int index)
			=> CopyTo((TItem[])array, index);

		/// <summary>
		/// Copies the elements of the collection to an array.
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="index">The zero-based index in the array at which copying begins.</param>
		public void CopyTo(TItem[] array, int index)
		{
			int count = Count;

			if (count == 0)
				return;

			var i = 0;
			foreach (var item in (ICollection<TItem>)this)
			{
				if (i >= count)
					break;

				array[index] = item;

				index++;
				i++;
			}
		}
	}
}