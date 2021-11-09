namespace Ecng.Collections
{
	using System.Collections.Generic;
	using System.Linq;

	public class CachedSynchronizedPairSet<TKey, TValue> : SynchronizedPairSet<TKey, TValue>
	{
		public CachedSynchronizedPairSet()
		{
		}

		public CachedSynchronizedPairSet(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
		}

		public CachedSynchronizedPairSet(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
			: base(keyComparer, valueComparer)
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
					return _cachedPairs ??= ToArray();
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

					if (_cachedKeys != null && !ContainsKey(key))
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