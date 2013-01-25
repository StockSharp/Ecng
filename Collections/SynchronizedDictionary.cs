namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Wintellect.PowerCollections;

	[Serializable]
	public class SynchronizedDictionary<TKey, TValue> : ISynchronizedCollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
	{
		private readonly IDictionary<TKey, TValue> _inner;

		public SynchronizedDictionary()
			: this(new Dictionary<TKey, TValue>())
		{
		}

		public SynchronizedDictionary(int capacity)
			: this(new Dictionary<TKey, TValue>(capacity))
		{
		}

		public SynchronizedDictionary(IEqualityComparer<TKey> comparer)
			: this(new Dictionary<TKey, TValue>(comparer))
		{
		}

		public SynchronizedDictionary(int capacity, IEqualityComparer<TKey> comparer)
			: this(new Dictionary<TKey, TValue>(capacity, comparer))
		{
		}

		private readonly object _syncRoot = new object();

		public object SyncRoot
		{
			get { return _syncRoot; }
		}

		protected SynchronizedDictionary(IDictionary<TKey, TValue> inner)
		{
			if (inner == null)
				throw new ArgumentNullException("inner");

			_inner = inner;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			lock (SyncRoot)
				return _inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public virtual void Clear()
		{
			lock (SyncRoot)
				_inner.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return ContainsKey(item.Key);
		}

		public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			lock (SyncRoot)
				_inner.CopyTo(array, arrayIndex);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}

		public int Count
		{
			get
			{
				lock (SyncRoot)
					return _inner.Count;
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public virtual bool ContainsKey(TKey key)
		{
			lock (SyncRoot)
				return _inner.ContainsKey(key);
		}

		public virtual void Add(TKey key, TValue value)
		{
			lock (SyncRoot)
				_inner.Add(key, value);
		}

		public virtual bool Remove(TKey key)
		{
			lock (SyncRoot)
				return _inner.Remove(key);
		}

		public virtual bool TryGetValue(TKey key, out TValue value)
		{
			lock (SyncRoot)
				return _inner.TryGetValue(key, out value);
		}

		public IDictionary<TKey, TValue> Range(TKey from, TKey to)
		{
			var ordered = _inner as OrderedDictionary<TKey, TValue>;

			if (ordered == null)
				throw new NotSupportedException();

			lock (SyncRoot)
			{
				var retVal = new Dictionary<TKey, TValue>();
				retVal.AddRange(ordered.Range(from, true, to, true));
				return retVal;
			}
		}

		public virtual TValue this[TKey key]
		{
			get
			{
				lock (SyncRoot)
					return _inner[key];
			}
			set
			{
				lock (SyncRoot)
					_inner[key] = value;
			}
		}

		public ICollection<TKey> Keys
		{
			get
			{
				lock (SyncRoot)
					return _inner.Keys;
			}
		}

		public ICollection<TValue> Values
		{
			get
			{
				lock (SyncRoot)
					return _inner.Values;
			}
		}
	}
}