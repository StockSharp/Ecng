namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

	[Serializable]
	public class SynchronizedList<T> : SynchronizedCollection<T, List<T>>, INotifyListEx<T>
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

		public event Action<IEnumerable<T>> AddedRange;
		public event Action<IEnumerable<T>> RemovedRange;

		protected override void OnAdded(T item)
		{
			base.OnAdded(item);

			var evt = AddedRange;
			evt?.Invoke(new[] { item });
		}

		protected override void OnRemoved(T item)
		{
			base.OnRemoved(item);

			var evt = RemovedRange;
			evt?.Invoke(new[] { item });
		}

		public void AddRange(IEnumerable<T> items)
		{
			lock (SyncRoot)
			{
				var filteredItems = items.Where(t =>
				{
					if (CheckNullableItems && t.IsNull())
						throw new ArgumentNullException(nameof(t));

					return OnAdding(t);
				}).ToArray();
				InnerCollection.AddRange(filteredItems);
				filteredItems.ForEach(base.OnAdded);

				AddedRange?.Invoke(filteredItems);
			}
		}

		public void RemoveRange(IEnumerable<T> items)
		{
			lock (SyncRoot)
			{
				var filteredItems = items.Where(OnRemoving).ToArray();
				InnerCollection.RemoveRange(filteredItems);
				filteredItems.ForEach(base.OnRemoved);

				RemovedRange?.Invoke(filteredItems);
			}
		}

		public int RemoveRange(int index, int count)
		{
			if (index < -1)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));

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