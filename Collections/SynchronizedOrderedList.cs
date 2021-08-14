namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Common;

	using Wintellect.PowerCollections;

	[Serializable]
	public class SynchronizedOrderedList<T> : ISynchronizedCollection<T>, IList<T>
	{
		private readonly HashSet<T> _nonOrderedList = new HashSet<T>();
		private readonly OrderedBag<T> _inner;

		public SynchronizedOrderedList()
		{
			_inner = new OrderedBag<T>();
		}

		public SynchronizedOrderedList(Comparison<T> comparison)
		{
			_inner = new OrderedBag<T>(comparison);
		}

		public SynchronizedOrderedList(IComparer<T> comparison)
		{
			_inner = new OrderedBag<T>(comparison);
		}

		private readonly SyncObject _syncRoot = new SyncObject();

		public SyncObject SyncRoot => _syncRoot;

		public int Count
		{
			get
			{
				lock (SyncRoot)
					return _inner.Count;
			}
		}

		public bool IsReadOnly => false;

		public T this[int index]
		{
			get
			{
				lock (SyncRoot)
					return _inner[index];
			}
			set => throw new NotSupportedException();
		}

		public IEnumerator<T> GetEnumerator()
		{
			lock (SyncRoot)
				return _inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			if (item.IsNull())
				throw new ArgumentNullException(nameof(item));

			lock (SyncRoot)
			{
				if (_nonOrderedList.Add(item))
					_inner.Add(item);
			}
		}

		public void AddRange(IEnumerable<T> items)
		{
			((ICollection<T>)this).AddRange(items);
		}

		public void Clear()
		{
			lock (SyncRoot)
			{
				_nonOrderedList.Clear();
				_inner.Clear();
			}
		}

		public bool Contains(T item)
		{
			if (item.IsNull())
				throw new ArgumentNullException(nameof(item));

			lock (SyncRoot)
				return _nonOrderedList.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			lock (SyncRoot)
				Array.Copy(_inner.ToArray(), 0, array, arrayIndex, _inner.Count);
		}

		public bool Remove(T item)
		{
			if (item.IsNull())
				throw new ArgumentNullException(nameof(item));

			lock (SyncRoot)
			{
				return _nonOrderedList.Remove(item) && _inner.Remove(item);
			}
		}

		public int IndexOf(T item)
		{
			if (item.IsNull())
				throw new ArgumentNullException(nameof(item));

			lock (SyncRoot)
				return _inner.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			if (item.IsNull())
				throw new ArgumentNullException(nameof(item));

			Add(item);
		}

		public void RemoveAt(int index)
		{
			Remove(this[index]);
		}
	}
}