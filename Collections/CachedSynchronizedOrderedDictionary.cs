namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

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

					if (!ContainsKey(key))
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