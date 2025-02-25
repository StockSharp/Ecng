namespace Ecng.Collections
{
	using System;

	/// <summary>
	/// Provides a thread-safe queue based on the <see cref="QueueEx{T}"/> collection.
	/// </summary>
	[Serializable]
	public class SynchronizedQueue<T> : SynchronizedCollection<T, QueueEx<T>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedQueue{T}"/> class.
		/// </summary>
		public SynchronizedQueue()
			: base(new QueueEx<T>())
		{
		}

		/// <summary>
		/// Adds an item to the queue in a thread-safe manner.
		/// </summary>
		/// <param name="item">The item to enqueue.</param>
		public void Enqueue(T item)
		{
			lock (SyncRoot)
				InnerCollection.Enqueue(item);
		}

		/// <summary>
		/// Removes and returns the item at the beginning of the queue in a thread-safe manner.
		/// </summary>
		/// <returns>The item that was removed from the queue.</returns>
		public T Dequeue()
		{
			lock (SyncRoot)
				return InnerCollection.Dequeue();
		}

		/// <summary>
		/// Returns the item at the beginning of the queue without removing it, in a thread-safe manner.
		/// </summary>
		/// <returns>The item at the beginning of the queue.</returns>
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