namespace Ecng.Collections
{
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
}