namespace Ecng.Collections
{
	using System;

	[Serializable]
	public class SynchronizedStack<T> : SynchronizedCollection<T, StackEx<T>>
	{
		public SynchronizedStack()
			: base(new StackEx<T>())
		{
		}

		public void Push(T item)
		{
			lock (SyncRoot)
				InnerCollection.Push(item);
		}

		public T Pop()
		{
			lock (SyncRoot)
				return InnerCollection.Pop();
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