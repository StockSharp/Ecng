namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	using Wintellect.PowerCollections;

	[Serializable]
	public abstract class KeyedCollection<TKey, TValue> : DictionaryBase<TKey, TValue>
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

		public override void Add(TKey key, TValue value)
		{
			OnAdding(key, value);
			InnerDictionary.Add(key, value);
			OnAdded(key, value);
		}

		#region DictionaryBase<TKey, TValue> Members

		public override TValue this[TKey key]
		{
			set
			{
				OnSetting(key, value);
				InnerDictionary[key] = value;
				OnSetted(key, value);
			}
		}

		public override void Clear()
		{
			OnClearing();
			InnerDictionary.Clear();
			OnCleared();
		}

		public override bool Remove(TKey key)
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

		public override bool TryGetValue(TKey key, out TValue value)
		{
			return InnerDictionary.TryGetValue(key, out value);
		}

		public override int Count => InnerDictionary.Count;

		public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
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
	}
}