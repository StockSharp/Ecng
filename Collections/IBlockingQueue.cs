namespace Ecng.Collections
{
	public interface IBlockingQueue<T>
	{
		int Count { get; }
		int MaxSize { get; set; }
		bool IsClosed { get; }

		void Open();
		void Close();
		void WaitUntilEmpty();

		void Enqueue(T item, bool force = false);

		T Dequeue();
		bool TryDequeue(out T value, bool exitOnClose = true, bool block = true);

		T Peek();
		bool TryPeek(out T value, bool exitOnClose = true, bool block = true);

		void Clear();
	}
}