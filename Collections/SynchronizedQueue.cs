namespace Ecng.Collections
{
	using System;

	[Serializable]
	public class SynchronizedQueue<T> : SynchronizedCollection<T, QueueEx<T>>
	{
		public SynchronizedQueue()
			: base(new QueueEx<T>())
		{
		}

		public void Enqueue(T item)
		{
			lock (SyncRoot)
				InnerCollection.Enqueue(item);
		}

		public T Dequeue()
		{
			lock (SyncRoot)
				return InnerCollection.Dequeue();
		}

		public T Peek()
		{
			lock (SyncRoot)
				return InnerCollection.Peek();
		}

		protected override T OnGetItem(int index)
		{
			throw new NotSupportedException();
		}

		protected override void OnInsert(int index, T item)
		{
			throw new NotSupportedException();
		}

		protected override void OnRemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		protected override int OnIndexOf(T item)
		{
			throw new NotSupportedException();
		}
	}
}