namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class SynchronizedPairSet<TKey, TValue> : SynchronizedKeyedCollection<TKey, TValue>
	{
		#region Private Fields

		private readonly Dictionary<TValue, TKey> _values;

		#endregion

		public SynchronizedPairSet()
		{
			_values = new Dictionary<TValue, TKey>();
		}

		public SynchronizedPairSet(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
			_values = new Dictionary<TValue, TKey>();
		}

		public SynchronizedPairSet(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
			: base(keyComparer)
		{
			_values = new Dictionary<TValue, TKey>(valueComparer);
		}

		public override void Add(TKey key, TValue value)
		{
			lock (SyncRoot)
				base.Add(key, value);
		}

		#region Item

		public TKey this[TValue value]
		{
			get
			{
				lock (SyncRoot)
					return _values[value];
			}
		}

		#endregion

		#region GetKey

		public TKey GetKey(TValue value)
		{
			return this[value];
		}

		#endregion

		#region GetValue

		public TValue GetValue(TKey key)
		{
			return base[key];
		}

		#endregion

		public void SetKey(TKey key, TValue value)
		{
			lock (SyncRoot)
			{
				Remove(key);

				Add(key, value);
				OnSetting(key, value);
			}
		}

		#region SetValue

		public void SetValue(TKey key, TValue value)
		{
			lock (SyncRoot)
				this[key] = value;
		}

		#endregion

		protected override bool CanAdd(TKey key, TValue value)
		{
			return !_values.ContainsKey(value);
		}

		#region SynchronizedKeyedCollection<TKey, TValue> Members

		protected override void OnAdding(TKey key, TValue value)
		{
			_values.Add(value, key);
		}

		protected override void OnSetting(TKey key, TValue value)
		{
			_values[value] = key;
		}

		protected override void OnClearing()
		{
			_values.Clear();
		}

		protected override void OnRemoving(TKey key, TValue value)
		{
			_values.Remove(value);
		}

		#endregion

		public bool TryAdd(TKey key, TValue value)
		{
			lock (SyncRoot)
			{
				if (ContainsKey(key) || _values.ContainsKey(value))
					return false;

				Add(key, value);
				return true;
			}
		}

		public TKey TryGetKey(TValue value)
		{
			lock (SyncRoot)
				return _values.TryGetValue(value);
		}

		public bool TryGetKey(TValue value, out TKey key)
		{
			lock (SyncRoot)
				return _values.TryGetValue(value, out key);
		}

		public bool TryGetKeyAndRemove(TValue value, out TKey key)
		{
			lock (SyncRoot)
			{
				if (!_values.TryGetAndRemove(value, out key))
					return false;

				return base.Remove(key);
			}
		}

		public bool RemoveByValue(TValue value)
		{
			lock (SyncRoot)
				return _values.ContainsKey(value) && Remove(_values[value]);
		}

		public bool ContainsValue(TValue value)
		{
			lock (SyncRoot)
				return _values.ContainsKey(value);
		}
	}
}