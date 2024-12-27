namespace Ecng.Collections
{
	using System.Collections.Generic;

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
					return _cachedKeys ??= [.. Keys];
			}
		}

		private TValue[] _cachedValues;

		public TValue[] CachedValues
		{
			get
			{
				lock (SyncRoot)
					return _cachedValues ??= [.. Values];
			}
		}

		private KeyValuePair<TKey, TValue>[] _cachedPairs;

		public KeyValuePair<TKey, TValue>[] CachedPairs
		{
			get
			{
				lock (SyncRoot)
					return _cachedPairs ??= [.. this];
			}
		}

		public override TValue this[TKey key]
		{
			set
			{
				lock (SyncRoot)
				{
					var isKey = false;

					if (_cachedKeys != null && !ContainsKey(key))
						isKey = true;

					OnResetCache(isKey);

					base[key] = value;
				}
			}
		}

		public override void Add(TKey key, TValue value)
		{
			lock (SyncRoot)
			{
				base.Add(key, value);

				OnResetCache(true);
			}
		}

		public override bool Remove(TKey key)
		{
			lock (SyncRoot)
			{
				if (base.Remove(key))
				{
					OnResetCache(true);

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

				OnResetCache(true);
			}
		}

		protected virtual void OnResetCache(bool isKey)
		{
			_cachedKeys = null;
			_cachedValues = null;
			_cachedPairs = null;
		}
	}
}