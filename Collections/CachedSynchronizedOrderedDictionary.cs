namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class CachedSynchronizedOrderedDictionary<TKey, TValue> : SynchronizedOrderedDictionary<TKey, TValue>
	{
		public CachedSynchronizedOrderedDictionary()
		{
		}

		public CachedSynchronizedOrderedDictionary(IComparer<TKey> comparer)
			: base(comparer)
		{
		}

		public CachedSynchronizedOrderedDictionary(Func<TKey, TKey, int> comparer)
			: base(comparer)
		{
		}

		private TKey[] _cachedKeys;

		public TKey[] CachedKeys
		{
			get
			{
				lock (SyncRoot)
					return _cachedKeys ??= Keys.ToArray();
			}
		}

		private TValue[] _cachedValues;

		public TValue[] CachedValues
		{
			get
			{
				lock (SyncRoot)
					return _cachedValues ??= Values.ToArray();
			}
		}

		private KeyValuePair<TKey, TValue>[] _cachedPairs;

		public KeyValuePair<TKey, TValue>[] CachedPairs
		{
			get
			{
				lock (SyncRoot)
					return _cachedPairs ??= this.ToArray();
			}
		}

		public override TValue this[TKey key]
		{
			set
			{
				lock (SyncRoot)
				{
					_cachedValues = null;
					_cachedPairs = null;

					if (!ContainsKey(key))
						_cachedKeys = null;

					base[key] = value;
				}
			}
		}

		public override void Add(TKey key, TValue value)
		{
			lock (SyncRoot)
			{
				base.Add(key, value);

				_cachedKeys = null;
				_cachedValues = null;
				_cachedPairs = null;
			}
		}

		public override bool Remove(TKey key)
		{
			lock (SyncRoot)
			{
				if (base.Remove(key))
				{
					_cachedKeys = null;
					_cachedValues = null;
					_cachedPairs = null;

					return true;
				}

				return false;
			}
		}

		public override void Clear()
		{
			lock (SyncRoot)
			{
				base.Clear();

				_cachedKeys = null;
				_cachedValues = null;
				_cachedPairs = null;
			}
		}
	}
}