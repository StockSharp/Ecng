namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

	[Serializable]
	public class SynchronizedSet<T> : SynchronizedCollection<T, HashSet<T>>, ISet<T>, ICollectionEx<T>
	{
		private readonly PairSet<int, T> _indecies;
		private int _maxIndex = -1;
		private bool _raiseRangeEvents = true;

		public SynchronizedSet()
			: this(false)
		{
		}

		public SynchronizedSet(bool allowIndexing)
			: this(allowIndexing, EqualityComparer<T>.Default)
		{
		}

		public SynchronizedSet(IEqualityComparer<T> comparer)
			: this(false, comparer)
		{
		}

		public SynchronizedSet(bool allowIndexing, IEqualityComparer<T> comparer)
			: base(new HashSet<T>(comparer))
		{
			if (allowIndexing)
				_indecies = new PairSet<int, T>();
		}

		public bool ThrowIfDuplicate { get; set; }

		private void Duplicate()
		{
			if (ThrowIfDuplicate)
				throw new InvalidOperationException("Duplicate element.");
		}

		private void CheckIndexingEnabled()
		{
			if (_indecies == null)
				throw new InvalidOperationException("Indexing not switched on.");
		}

		private void AddIndicies(T item)
		{
			if (_indecies == null)
				return;

			_maxIndex = Count - 1;
			_indecies.Add(_maxIndex, item);
		}

		private bool RemoveIndicies(T item)
		{
			if (_indecies == null)
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

		protected override bool OnAdding(T item)
		{
			if (InnerCollection.Contains(item))
			{
				Duplicate();
				return false;
			}

			return base.OnAdding(item);
		}

		protected override T OnGetItem(int index)
		{
			CheckIndexingEnabled();

			return _indecies[index];
		}

		protected override void OnInsert(int index, T item)
		{
			if (!InnerCollection.Add(item))
				return;

			if (_indecies == null)
				return;

			if (_maxIndex == -1)
				throw new InvalidOperationException();

			for (var i = _maxIndex; i >= index; i--)
				_indecies.SetKey(_indecies[i], i + 1);

			_indecies[index] = item;
			_maxIndex++;
		}

		protected override void OnRemoveAt(int index)
		{
			CheckIndexingEnabled();

			if (_indecies.ContainsKey(index))
				Remove(_indecies.GetValue(index));
		}

		protected override void OnAdd(T item)
		{
			if (!InnerCollection.Add(item))
				return;

			AddIndicies(item);
		}

		protected override bool OnRemove(T item)
		{
			if (!base.OnRemove(item))
				return false;

			return RemoveIndicies(item);
		}

		protected override void OnClear()
		{
			base.OnClear();

			_indecies?.Clear();
		}

		protected override int OnIndexOf(T item)
		{
			CheckIndexingEnabled();

			return _indecies.GetKey(item);
		}

		#region Implementation of ISet<T>

		public void UnionWith(IEnumerable<T> other)
		{
			AddRange(other);
		}

		public void IntersectWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public void ExceptWith(IEnumerable<T> other)
		{
			RemoveRange(other);
		}

		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.IsSubsetOf(other);
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.IsSupersetOf(other);
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.Overlaps(other);
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.IsProperSubsetOf(other);
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			lock (SyncRoot)
				return InnerCollection.Overlaps(other);
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		bool ISet<T>.Add(T item)
		{
			return TryAdd(item);
		}

		#endregion

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

		public event Action<IEnumerable<T>> AddedRange;
		public event Action<IEnumerable<T>> RemovedRange;

		protected override void OnAdded(T item)
		{
			base.OnAdded(item);

			if (_raiseRangeEvents)
				AddedRange?.Invoke(new[] { item });
		}

		protected override void OnRemoved(T item)
		{
			base.OnRemoved(item);

			if (_raiseRangeEvents)
				RemovedRange?.Invoke(new[] { item });
		}

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

		public int RemoveRange(int index, int count)
		{
			throw new NotImplementedException();
		}
	}
}