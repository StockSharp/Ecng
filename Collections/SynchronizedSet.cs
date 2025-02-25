namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	/// <summary>
	/// Represents a thread-safe set that supports optional indexing and range-based operations.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	[Serializable]
	public class SynchronizedSet<T> : SynchronizedCollection<T, ISet<T>>, ISet<T>, ICollectionEx<T>
	{
		private readonly PairSet<int, T> _indecies;
		private int _maxIndex = -1;
		private bool _raiseRangeEvents = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class.
		/// </summary>
		public SynchronizedSet()
			: this(false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class with an option to enable indexing.
		/// </summary>
		/// <param name="allowIndexing">True to enable indexing; otherwise, false.</param>
		public SynchronizedSet(bool allowIndexing)
			: this(allowIndexing, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class with a specified comparer.
		/// </summary>
		/// <param name="comparer">The comparer to use for comparing elements.</param>
		public SynchronizedSet(IEqualityComparer<T> comparer)
			: this(false, comparer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class with indexing and a custom comparer.
		/// </summary>
		/// <param name="allowIndexing">True to enable indexing; otherwise, false.</param>
		/// <param name="comparer">The comparer to use for comparing elements.</param>
		public SynchronizedSet(bool allowIndexing, IEqualityComparer<T> comparer)
			: this(allowIndexing, new HashSet<T>(comparer))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class from an existing collection.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new set.</param>
		public SynchronizedSet(IEnumerable<T> collection)
			: this(collection, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class from an existing collection and comparer.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new set.</param>
		/// <param name="comparer">The comparer to use for comparing elements.</param>
		public SynchronizedSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
			: this(false, collection, comparer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class with control over indexing, an existing collection, and comparer.
		/// </summary>
		/// <param name="allowIndexing">True to enable indexing; otherwise, false.</param>
		/// <param name="collection">The collection whose elements are copied to the new set.</param>
		/// <param name="comparer">The comparer to use for comparing elements.</param>
		public SynchronizedSet(bool allowIndexing, IEnumerable<T> collection, IEqualityComparer<T> comparer)
			: this(allowIndexing, new HashSet<T>(collection, comparer))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedSet{T}"/> class using the specified set as the inner collection.
		/// </summary>
		/// <param name="allowIndexing">True to enable indexing; otherwise, false.</param>
		/// <param name="innerCollection">The inner set for the synchronized collection.</param>
		protected SynchronizedSet(bool allowIndexing, ISet<T> innerCollection)
			: base(innerCollection)
		{
			if (allowIndexing)
				_indecies = [];
		}

		/// <summary>
		/// Gets or sets a value indicating whether an exception is thrown when adding a duplicate element.
		/// </summary>
		public bool ThrowIfDuplicate { get; set; }

		private void Duplicate()
		{
			if (ThrowIfDuplicate)
				throw new InvalidOperationException("Duplicate element.");
		}

		private void CheckIndexingEnabled()
		{
			if (_indecies is null)
				throw new InvalidOperationException("Indexing not switched on.");
		}

		private void AddIndicies(T item)
		{
			if (_indecies is null)
				return;

			_maxIndex = Count - 1;
			_indecies.Add(_maxIndex, item);
		}

		private bool RemoveIndicies(T item)
		{
			if (_indecies is null)
				return true;

			if (_maxIndex == -1)
				throw new InvalidOperationException();

			var index = _indecies.GetKey(item);
			_indecies.RemoveByValue(item);

			for (var i = index + 1; i <= _maxIndex; i++)
			{
				_indecies.SetKey(_indecies[i], i - 1);
			}

			_maxIndex--;

			return true;
		}

		/// <inheritdoc/>
		protected override bool OnAdding(T item)
		{
			if (InnerCollection.Contains(item))
			{
				Duplicate();
				return false;
			}

			return base.OnAdding(item);
		}

		/// <inheritdoc/>
		protected override T OnGetItem(int index)
		{
			CheckIndexingEnabled();

			return _indecies[index];
		}

		/// <inheritdoc/>
		protected override void OnInsert(int index, T item)
		{
			if (!InnerCollection.Add(item))
				return;

			if (_indecies is null)
				return;

			if (_maxIndex == -1)
				throw new InvalidOperationException();

			for (var i = _maxIndex; i >= index; i--)
				_indecies.SetKey(_indecies[i], i + 1);

			_indecies[index] = item;
			_maxIndex++;
		}

		/// <inheritdoc/>
		protected override void OnRemoveAt(int index)
		{
			CheckIndexingEnabled();

			if (_indecies.ContainsKey(index))
				Remove(_indecies.GetValue(index));
		}

		/// <inheritdoc/>
		protected override void OnAdd(T item)
		{
			if (!InnerCollection.Add(item))
				return;

			AddIndicies(item);
		}

		/// <inheritdoc/>
		protected override bool OnRemove(T item)
		{
			if (!base.OnRemove(item))
				return false;

			return RemoveIndicies(item);
		}

		/// <inheritdoc/>
		protected override void OnClear()
		{
			base.OnClear();

			_indecies?.Clear();
			_maxIndex = -1;
		}

		/// <inheritdoc/>
		protected override int OnIndexOf(T item)
		{
			CheckIndexingEnabled();

			return _indecies.GetKey(item);
		}

		#region Implementation of ISet<T>

		/// <summary>
		/// Modifies the current set so that it contains all elements that are present in both the current set and in the specified collection.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		public void UnionWith(IEnumerable<T> other)
		{
			AddRange(other);
		}

		/// <summary>
		/// Modifies the current set so that it contains only elements that are also in a specified collection.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		public void IntersectWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes all elements in the specified collection from the current set.
		/// </summary>
		/// <param name="other">The collection of items to remove from the set.</param>
		public void ExceptWith(IEnumerable<T> other)
		{
			RemoveRange(other);
		}

		/// <summary>
		/// Modifies the current set so that it contains only elements that are present either in the current set or in the specified collection, but not both.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Determines whether the current set is a subset of a specified collection.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		/// <returns>True if the set is a subset of other; otherwise, false.</returns>
		public bool IsSubsetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.IsSubsetOf(other);
		}

		/// <summary>
		/// Determines whether the current set is a superset of a specified collection.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		/// <returns>True if the set is a superset of other; otherwise, false.</returns>
		public bool IsSupersetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.IsSupersetOf(other);
		}

		/// <summary>
		/// Determines whether the current set is a proper superset of a specified collection.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		/// <returns>True if the set is a proper superset of other; otherwise, false.</returns>
		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.Overlaps(other);
		}

		/// <summary>
		/// Determines whether the current set is a proper subset of a specified collection.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		/// <returns>True if the set is a proper subset of other; otherwise, false.</returns>
		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.IsProperSubsetOf(other);
		}

		/// <summary>
		/// Determines whether the current set overlaps with the specified collection.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		/// <returns>True if the sets overlap; otherwise, false.</returns>
		public bool Overlaps(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.Overlaps(other);
		}

		/// <summary>
		/// Determines whether the current set and a specified collection contain the same elements.
		/// </summary>
		/// <param name="other">The collection to compare to the current set.</param>
		/// <returns>True if the sets contain the same elements; otherwise, false.</returns>
		public bool SetEquals(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Adds an item to the current set and returns a value to indicate if the item was successfully added.
		/// </summary>
		/// <param name="item">The element to add to the set.</param>
		/// <returns>True if the item is added to the set; otherwise, false.</returns>
		bool ISet<T>.Add(T item)
		{
			return TryAdd(item);
		}

		#endregion

		/// <summary>
		/// Attempts to add the specified item to the set without throwing an exception for duplicates.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <returns>True if the item was added; otherwise, false.</returns>
		public bool TryAdd(T item)
		{
			lock (SyncRoot)
			{
				if (InnerCollection.Contains(item))
				{
					Duplicate();
					return false;
				}

				Add(item);
				return true;
			}
		}

		/// <summary>
		/// Occurs when multiple items have been added to the set.
		/// </summary>
		public event Action<IEnumerable<T>> AddedRange;

		/// <summary>
		/// Occurs when multiple items have been removed from the set.
		/// </summary>
		public event Action<IEnumerable<T>> RemovedRange;

		/// <inheritdoc/>
		protected override void OnAdded(T item)
		{
			base.OnAdded(item);

			if (_raiseRangeEvents)
				AddedRange?.Invoke([item]);
		}

		/// <inheritdoc/>
		protected override void OnRemoved(T item)
		{
			base.OnRemoved(item);

			if (_raiseRangeEvents)
				RemovedRange?.Invoke([item]);
		}

		/// <summary>
		/// Adds a range of items to the set.
		/// </summary>
		/// <param name="items">The items to add.</param>
		public void AddRange(IEnumerable<T> items)
		{
			lock (SyncRoot)
			{
				var filteredItems = items.Where(t =>
				{
					if (CheckNullableItems && t.IsNull())
						throw new ArgumentNullException(nameof(t));

					return OnAdding(t);
				}).ToArray();
				InnerCollection.AddRange(filteredItems);

				ProcessRange(filteredItems, item =>
				{
					//AddIndicies(item);

					if (_indecies != null)
					{
						_indecies.Add(_maxIndex + 1, item);
						_maxIndex++;
					}

					OnAdded(item);
				});

				AddedRange?.Invoke(filteredItems);
			}
		}

		/// <summary>
		/// Removes a range of items from the set.
		/// </summary>
		/// <param name="items">The items to remove.</param>
		public void RemoveRange(IEnumerable<T> items)
		{
			lock (SyncRoot)
			{
				var filteredItems = items.Where(OnRemoving).ToArray();
				InnerCollection.RemoveRange(filteredItems);
				ProcessRange(filteredItems, item =>
				{
					RemoveIndicies(item);
					OnRemoved(item);
				});

				RemovedRange?.Invoke(filteredItems);
			}
		}

		private void ProcessRange(IEnumerable<T> items, Action<T> action)
		{
			_raiseRangeEvents = false;

			items.ForEach(action);

			_raiseRangeEvents = true;
		}

		/// <summary>
		/// Removes a specified number of items starting at the given index. Not yet implemented.
		/// </summary>
		/// <param name="index">The starting index.</param>
		/// <param name="count">The number of items to remove.</param>
		/// <returns>The number of items removed.</returns>
		/// <exception cref="NotImplementedException">Always thrown.</exception>
		public int RemoveRange(int index, int count)
		{
			throw new NotImplementedException();
		}
	}
}