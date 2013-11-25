namespace Ecng.Collections
{
	using Wintellect.PowerCollections;

	public sealed class BlockingQueue<T> : BaseBlockingQueue<T, QueueEx<T>>
	{
		public BlockingQueue()
			: base(new QueueEx<T>())
		{
		}

		protected override void OnEnqueue(T item, bool force)
		{
			InnerCollection.Enqueue(item);
		}

		protected override T OnDequeue()
		{
			return InnerCollection.Dequeue();
		}

		protected override T OnPeek()
		{
			return InnerCollection.Peek();
		}
	}

	public sealed class BlockingDeque<T> : BaseBlockingQueue<T, Deque<T>>
	{
		public BlockingDeque()
			: base(new Deque<T>())
		{
		}

		protected override void OnEnqueue(T item, bool force)
		{
			if (force)
				InnerCollection.AddToFront(item);
			else
				InnerCollection.AddToBack(item);
		}

		protected override T OnDequeue()
		{
			return InnerCollection.RemoveFromFront();
		}

		protected override T OnPeek()
		{
			return InnerCollection.GetAtFront();
		}
	}
}