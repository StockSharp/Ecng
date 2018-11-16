namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public sealed class PairSet<TKey, TValue> : KeyedCollection<TKey, TValue>
	{
		#region Private Fields

		private readonly Dictionary<TValue, TKey> _values = new Dictionary<TValue, TKey>();

		#endregion

		public PairSet()
		{
		}

		public PairSet(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
		}

		#region Item

		public TKey this[TValue value] => _values[value];

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

		public void SetKey(TValue value, TKey key)
		{
			RemoveByValue(value);
			Add(key, value);
		}

		#region SetValue

		public void SetValue(TKey key, TValue value)
		{
			base[key] = value;
		}

		#endregion

        #region KeyedCollection<TKey, TValue> Members

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

		public bool TryGetKey(TValue value, out TKey key)
		{
			return _values.TryGetValue(value, out key);
		}

		public bool TryAdd(TKey key, TValue value)
		{
			if (ContainsKey(key) || _values.ContainsKey(value))
				return false;

			Add(key, value);
			return true;
		}

		public bool RemoveByValue(TValue value)
		{
			return _values.ContainsKey(value) && Remove(_values[value]);
		}

		public bool ContainsValue(TValue value)
		{
			return _values.ContainsKey(value);
		}
	}
}