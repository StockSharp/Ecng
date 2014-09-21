namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

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

		// mika может и нужен для перфоманса этот метод, но текущая реализация не вызывает нотификацию Adding Added и т.д.
		//
		//public virtual void AddRange(IEnumerable<T> items)
		//{
		//    lock (SyncRoot)
		//        InnerCollection.AddRange(items);
		//}

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

		// NOTE не вызывается нотификация Adding Added и т.д.

		public void AddRange(IEnumerable<T> items)
		{
			lock (SyncRoot)
				InnerCollection.AddRange(items);
		}

		public void RemoveRange(IEnumerable<T> items)
		{
			lock (SyncRoot)
				InnerCollection.RemoveRange(items);
		}

		public IEnumerable<T> GetRange(int index, int count)
		{
			lock (SyncRoot)
				return InnerCollection.GetRange(index, count);
		}
	}
}