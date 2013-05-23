namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class SynchronizedSet<T> : SynchronizedCollection<T, HashSet<T>>, ISet<T>
	{
		private readonly PairSet<int, T> _indecies;
		private int _maxIndex = -1;

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
				throw new InvalidOperationException("Элемент уже добавлен.");
		}

		private void CheckIndexingEnabled()
		{
			if (_indecies == null)
				throw new InvalidOperationException("Индексация выключена.");
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
			if (InnerCollection.Add(item))
			{
				if (_indecies != null)
				{
					if (_maxIndex == -1)
						throw new InvalidOperationException();

					for (var i = _maxIndex; i >= index; i--)
						_indecies.SetKey(_indecies[i], i + 1);

					_indecies[index] = item;
					_maxIndex++;
				}
			}
		}

		protected override void OnRemoveAt(int index)
		{
			CheckIndexingEnabled();

			if (_indecies.ContainsKey(index))
				Remove(_indecies.GetValue(index));
		}

		protected override void OnAdd(T item)
		{
			if (InnerCollection.Add(item))
			{
				if (_indecies != null)
				{
					_maxIndex = Count - 1;
					_indecies.Add(_maxIndex, item);
				}
			}
		}

		protected override bool OnRemove(T item)
		{
			if (base.OnRemove(item))
			{
				if (_indecies != null)
				{
					if (_maxIndex == -1)
						throw new InvalidOperationException();

					var index = _indecies.GetKey(item);
					_indecies.RemoveByValue(item);

					for (var i = index + 1; i <= _maxIndex; i++)
						_indecies.SetKey(_indecies[i], i - 1);

					_maxIndex--;
				}

				return true;
			}

			return false;
		}

		protected override void OnClear()
		{
			base.OnClear();

			if (_indecies != null)
				_indecies.Clear();
		}

		protected override int OnIndexOf(T item)
		{
			CheckIndexingEnabled();

			return _indecies.GetKey(item);
		}

		#region Implementation of ISet<T>

		public void UnionWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public void IntersectWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public void ExceptWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			throw new NotImplementedException();
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			throw new NotImplementedException();
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
	}
}