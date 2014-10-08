namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

	[Serializable]
	public class SynchronizedList<T> : SynchronizedCollection<T, List<T>>, ICollectionEx<T>
	{
		public SynchronizedList()
			: this(0)
		{
		}

		public SynchronizedList(int capacity)
			: base(new List<T>(capacity))
		{
		}

		protected override T OnGetItem(int index)
		{
			return InnerCollection[index];
		}

		protected override void OnInsert(int index, T item)
		{
			InnerCollection.Insert(index, item);
		}

		protected override void OnRemoveAt(int index)
		{
			InnerCollection.RemoveAt(index);
		}

		protected override int OnIndexOf(T item)
		{
			return InnerCollection.IndexOf(item);
		}

		public void AddRange(IEnumerable<T> items)
		{
			lock (SyncRoot)
			{
				var filteredItems = items.Where(OnAdding).ToArray();
				InnerCollection.AddRange(filteredItems);
				filteredItems.ForEach(OnAdded);
			}
		}

		public IEnumerable<T> RemoveRange(IEnumerable<T> items)
		{
			IEnumerable<T> removedItems;

			lock (SyncRoot)
			{
				var filteredItems = items.Where(OnRemoving).ToArray();
				removedItems = InnerCollection.RemoveRange(filteredItems);
				filteredItems.ForEach(OnRemoved);
			}

			return removedItems;
		}

		public int RemoveRange(int index, int count)
		{
			if (index < -1)
				throw new ArgumentOutOfRangeException("index");

			if (count <= 0)
				throw new ArgumentOutOfRangeException("count");

			lock (SyncRoot)
			{
				var realCount = Count;
				realCount -= index;
				InnerCollection.RemoveRange(index, count);
				return (realCount.Min(count)).Max(0);
			}
		}

		public IEnumerable<T> GetRange(int index, int count)
		{
			lock (SyncRoot)
				return InnerCollection.GetRange(index, count);
		}
	}
}