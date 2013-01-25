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

		protected KeyedCollection(IDictionary<TKey, TValue> innerDictionary)
		{
			if (innerDictionary == null)
				throw new ArgumentNullException("innerDictionary");

			InnerDictionary = innerDictionary;
		}

		protected IDictionary<TKey, TValue> InnerDictionary { get; private set; }

		#region DictionaryBase<TKey, TValue> Members

		public override TValue this[TKey key]
		{
			set
			{
				var pair = new Tuple<TKey, TValue>(key, value);
				OnSetting(pair);
				InnerDictionary[key] = value;
				OnSetted(pair);
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
			TValue value;

			if (InnerDictionary.TryGetValue(key, out value))
			{
				var pair = new Tuple<TKey, TValue>(key, value);
				OnRemoving(pair);
				var retVal = InnerDictionary.Remove(key);
				OnRemoved(pair);
				return retVal;
			}
			else
				return false;
		}

		public override bool TryGetValue(TKey key, out TValue value)
		{
			return InnerDictionary.TryGetValue(key, out value);
		}

		public override int Count
		{
			get { return InnerDictionary.Count; }
		}

		public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return InnerDictionary.GetEnumerator();
		}

		#endregion

		protected virtual void OnSetting(Tuple<TKey, TValue> pair) { }
		protected virtual void OnSetted(Tuple<TKey, TValue> pair) { }

		protected virtual void OnClearing() {}
		protected virtual void OnCleared() {}

		protected virtual void OnRemoving(Tuple<TKey, TValue> pair) { }
		protected virtual void OnRemoved(Tuple<TKey, TValue> pair) { }
	}
}