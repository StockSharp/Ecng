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

		protected override void OnSetting(Tuple<TKey, TValue> pair)
		{
			_values.Add(pair.Item2, pair.Item1);
		}

		protected override void OnClearing()
		{
			_values.Clear();
		}

		protected override void OnRemoving(Tuple<TKey, TValue> pair)
		{
			_values.Remove(pair.Item2);
		}

		#endregion

		public TKey TryGetKey(TValue value)
		{
			return _values.TryGetValue(value);
		}

		public bool TryGetKey(TValue value, out TKey key)
		{
			return _values.TryGetValue(value, out key);
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