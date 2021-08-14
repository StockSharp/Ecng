namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using Wintellect.PowerCollections;

	[Serializable]
	public class SynchronizedMultiDictionary<TKey, TValue> : MultiDictionaryBase<TKey, TValue>, ISynchronizedCollection<KeyValuePair<TKey, ICollection<TValue>>>
	{
		private readonly MultiDictionaryBase<TKey, TValue> _inner;

		public SynchronizedMultiDictionary()
		{
			_inner = new MultiDictionary<TKey, TValue>(false);
		}

		public SynchronizedMultiDictionary(IComparer<TKey> comparer)
		{
			_inner = new OrderedMultiDictionary<TKey, TValue>(false, comparer);
		}

		public SynchronizedMultiDictionary(Func<TKey, TKey, int> comparer)
		{
			_inner = new OrderedMultiDictionary<TKey, TValue>(false, comparer.ToComparer());
		}

		private readonly SyncObject _syncRoot = new();

		public SyncObject SyncRoot => _syncRoot;

		public override void Clear()
		{
			lock (SyncRoot)
				_inner.Clear();
		}

		public override int Count
		{
			get
			{
				lock (SyncRoot)
					return _inner.Count;
			}
		}

		protected override IEnumerator<TKey> EnumerateKeys()
		{
			lock (SyncRoot)
				return _inner.Keys.GetEnumerator();
		}

		protected override bool TryEnumerateValuesForKey(TKey key, out IEnumerator<TValue> values)
		{
			lock (SyncRoot)
			{
				var value = _inner.TryGetValue(key);

				if (value != null)
				{
					values = value.GetEnumerator();
					return true;
				}
				else
				{
					values = null;
					return false;
				}
			}
		}

		public override void Add(TKey key, TValue value)
		{
			lock (SyncRoot)
				_inner.Add(key, value);
		}

		public override bool Remove(TKey key)
		{
			lock (SyncRoot)
				return _inner.Remove(key);
		}

		public override bool Remove(TKey key, TValue value)
		{
			lock (SyncRoot)
				return _inner.Remove(key, value);
		}

		public override bool Contains(TKey key, TValue value)
		{
			lock (SyncRoot)
				return _inner.Contains(key, value);
		}

		public MultiDictionary<TKey, TValue> Range(TKey from, TKey to)
		{
			if (_inner is not OrderedMultiDictionary<TKey, TValue> ordered)
				throw new NotSupportedException();

			lock (SyncRoot)
			{
				var retVal = new MultiDictionary<TKey, TValue>(false);
				retVal.AddRange(ordered.Range(from, true, to, true));
				return retVal;
			}
		}
	}
}