namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	[Serializable]
	public abstract class KeyedCollection<TKey, TValue> : IDictionary<TKey, TValue>
	{
		protected KeyedCollection()
			: this(new Dictionary<TKey, TValue>())
		{
		}

		protected KeyedCollection(IEqualityComparer<TKey> comparer)
			: this(new Dictionary<TKey, TValue>(comparer))
		{
		}

		protected KeyedCollection(IDictionary<TKey, TValue> innerDictionary)
		{
			InnerDictionary = innerDictionary ?? throw new ArgumentNullException(nameof(innerDictionary));
		}

		protected IDictionary<TKey, TValue> InnerDictionary { get; }

		public virtual void Add(TKey key, TValue value)
		{
			if (!CanAdd(key, value))
				throw new ArgumentException();

			OnAdding(key, value);
			InnerDictionary.Add(key, value);
			OnAdded(key, value);
		}

		#region DictionaryBase<TKey, TValue> Members

		public virtual TValue this[TKey key]
		{
			get => InnerDictionary[key];
			set
			{
				OnSetting(key, value);
				InnerDictionary[key] = value;
				OnSetted(key, value);
			}
		}

		public virtual void Clear()
		{
			OnClearing();
			InnerDictionary.Clear();
			OnCleared();
		}

		public virtual bool Remove(TKey key)
		{
			if (InnerDictionary.TryGetValue(key, out var value))
			{
				OnRemoving(key, value);
				var retVal = InnerDictionary.Remove(key);
				OnRemoved(key, value);
				return retVal;
			}
			else
				return false;
		}

		public virtual bool TryGetValue(TKey key, out TValue value)
		{
			return InnerDictionary.TryGetValue(key, out value);
		}

		public virtual int Count => InnerDictionary.Count;

		public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return InnerDictionary.GetEnumerator();
		}

		#endregion

		protected virtual void OnAdding(TKey key, TValue value) { }
		protected virtual void OnAdded(TKey key, TValue value) { }

		protected virtual void OnSetting(TKey key, TValue value) { }
		protected virtual void OnSetted(TKey key, TValue value) { }

		protected virtual void OnClearing() {}
		protected virtual void OnCleared() {}

		protected virtual void OnRemoving(TKey key, TValue value) { }
		protected virtual void OnRemoved(TKey key, TValue value) { }

		protected virtual bool CanAdd(TKey key, TValue value) => true;

		public ICollection<TKey> Keys => InnerDictionary.Keys;
		public ICollection<TValue> Values => InnerDictionary.Values;

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => InnerDictionary.IsReadOnly;

		public bool ContainsKey(TKey key) => InnerDictionary.ContainsKey(key);
		public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
		public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => InnerDictionary.CopyTo(array, arrayIndex);
		public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}