namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class SynchronizedPairSet<TKey, TValue> : SynchronizedKeyedCollection<TKey, TValue>
	{
		#region Private Fields

		private readonly Dictionary<TValue, TKey> _values = new Dictionary<TValue, TKey>();

		#endregion

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
				if (Remove(key))
				{
					Add(key, value);
					OnSetting(new Tuple<TKey, TValue>(key, value));
				}
			}
		}

		#region SetValue

		public void SetValue(TKey key, TValue value)
		{
			lock (SyncRoot)
				this[key] = value;
		}

		#endregion

		#region SynchronizedKeyedCollection<TKey, TValue> Members

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
			lock (SyncRoot)
				return _values.TryGetValue(value);
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