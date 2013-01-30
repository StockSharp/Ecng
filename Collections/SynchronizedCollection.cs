namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	[Serializable]
	public abstract class SynchronizedCollection<TItem, TCollection> : BaseCollection<TItem, TCollection>, ISynchronizedCollection<TItem>
		where TCollection : ICollection<TItem>
	{
		protected SynchronizedCollection(TCollection innerCollection)
			: base(innerCollection)
		{
		}

		private readonly SyncObject _syncRoot = new SyncObject();

		public SyncObject SyncRoot
		{
			get { return _syncRoot; }
		}

		public override int Count
		{
			get
			{
				lock (SyncRoot)
					return base.Count;
			}
		}

		public override TItem this[int index]
		{
			get
			{
				lock (SyncRoot)
					return base[index];
			}
			set
			{
				lock (SyncRoot)
					base[index] = value;
			}
		}

		public override void Add(TItem item)
		{
			lock (SyncRoot)
				base.Add(item);
		}

		public override void Clear()
		{
			lock (SyncRoot)
				base.Clear();
		}

		public override bool Remove(TItem item)
		{
			lock (SyncRoot)
				return base.Remove(item);
		}

		public override void RemoveAt(int index)
		{
			lock (SyncRoot)
				base.RemoveAt(index);
		}

		public override void Insert(int index, TItem item)
		{
			lock (SyncRoot)
				base.Insert(index, item);
		}

		public override int IndexOf(TItem item)
		{
			lock (SyncRoot)
				return OnIndexOf(item);
		}

		public override void CopyTo(TItem[] array, int arrayIndex)
		{
			lock (SyncRoot)
				base.CopyTo(array, arrayIndex);
		}

		public override void ForEach(Action<TItem> action)
		{
			lock (SyncRoot)
				base.ForEach(action);
		}

		public override bool Contains(TItem item)
		{
			lock (SyncRoot)
				return base.Contains(item);
		}

		protected abstract int OnIndexOf(TItem item);

		public override IEnumerator<TItem> GetEnumerator()
		{
			lock (SyncRoot)
				return InnerCollection.GetEnumerator();
		}
	}
}