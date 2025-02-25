namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	/// <summary>
	/// Represents a thread-safe collection that provides synchronization for its operations.
	/// </summary>
	/// <typeparam name="TItem">The type of elements in the collection.</typeparam>
	/// <typeparam name="TCollection">The type of the inner collection.</typeparam>
	[Serializable]
	public abstract class SynchronizedCollection<TItem, TCollection>(TCollection innerCollection) : BaseCollection<TItem, TCollection>(innerCollection), ISynchronizedCollection<TItem>
		where TCollection : ICollection<TItem>
	{
		/// <summary>
		/// Gets the synchronization root object used to synchronize access to the collection.
		/// </summary>
		public SyncObject SyncRoot { get; } = new SyncObject();

		/// <summary>
		/// Gets the number of elements contained in the collection.
		/// </summary>
		public override int Count
		{
			get
			{
				lock (SyncRoot)
					return base.Count;
			}
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		public override TItem this[int index]
		{
			get
			{
				lock (SyncRoot)
					return base[index];
			}
			set
			{
				lock (SyncRoot)
					base[index] = value;
			}
		}

		/// <summary>
		/// Adds an item to the collection.
		/// </summary>
		/// <param name="item">The item to add to the collection.</param>
		public override void Add(TItem item)
		{
			lock (SyncRoot)
				base.Add(item);
		}

		/// <summary>
		/// Removes all items from the collection.
		/// </summary>
		public override void Clear()
		{
			lock (SyncRoot)
				base.Clear();
		}

		/// <summary>
		/// Removes the first occurrence of a specific item from the collection.
		/// </summary>
		/// <param name="item">The item to remove from the collection.</param>
		/// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
		public override bool Remove(TItem item)
		{
			lock (SyncRoot)
				return base.Remove(item);
		}

		/// <summary>
		/// Removes the item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public override void RemoveAt(int index)
		{
			lock (SyncRoot)
				base.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item into the collection at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which the item should be inserted.</param>
		/// <param name="item">The item to insert into the collection.</param>
		public override void Insert(int index, TItem item)
		{
			lock (SyncRoot)
				base.Insert(index, item);
		}

		/// <summary>
		/// Determines the index of a specific item in the collection.
		/// </summary>
		/// <param name="item">The item to locate in the collection.</param>
		/// <returns>The index of the item if found in the collection; otherwise, -1.</returns>
		public override int IndexOf(TItem item)
		{
			lock (SyncRoot)
				return OnIndexOf(item);
		}

		/// <summary>
		/// Determines whether the collection contains a specific item.
		/// </summary>
		/// <param name="item">The item to locate in the collection.</param>
		/// <returns><c>true</c> if the item is found in the collection; otherwise, <c>false</c>.</returns>
		public override bool Contains(TItem item)
		{
			lock (SyncRoot)
				return base.Contains(item);
		}

		/// <summary>
		/// When overridden in a derived class, determines the index of a specific item in the collection.
		/// </summary>
		/// <param name="item">The item to locate in the collection.</param>
		/// <returns>The index of the item if found in the collection; otherwise, -1.</returns>
		protected abstract int OnIndexOf(TItem item);

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public override IEnumerator<TItem> GetEnumerator()
		{
			lock (SyncRoot)
				return InnerCollection.GetEnumerator();
		}
	}
}