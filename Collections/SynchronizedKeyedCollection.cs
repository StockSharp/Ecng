namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public abstract class SynchronizedKeyedCollection<TKey, TValue> : KeyedCollection<TKey, TValue>, ISynchronizedCollection<KeyValuePair<TKey, TValue>>
	{
		protected SynchronizedKeyedCollection()
			: base(new SynchronizedDictionary<TKey, TValue>())
		{
		}

		private SynchronizedDictionary<TKey, TValue> SyncDict
		{
			get { return (SynchronizedDictionary<TKey, TValue>)InnerDictionary; }
		}

		public object SyncRoot { get { return SyncDict.SyncRoot; } }

		public override TValue this[TKey key]
		{
			set
			{
				lock (SyncRoot)
					base[key] = value;
			}
		}

		public override void Clear()
		{
			lock (SyncRoot)
				base.Clear();
		}

		public override bool Remove(TKey key)
		{
			lock (SyncRoot)
				return base.Remove(key);
		}
	}
}