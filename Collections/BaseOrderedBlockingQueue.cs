namespace Ecng.Collections
{
	using System.Collections.Generic;

	/// <summary>
	/// Message queue.
	/// </summary>
	public abstract class BaseOrderedBlockingQueue<TSort, TValue> :
		BaseBlockingQueue<KeyValuePair<TSort, TValue>, OrderedPriorityQueue<TSort, TValue>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseOrderedBlockingQueue{TSort, TValue}"/>.
		/// </summary>
		protected BaseOrderedBlockingQueue()
			: base(new OrderedPriorityQueue<TSort, TValue>())
		{
		}

		/// <inheritdoc />
		public bool TryDequeue(out TValue value, bool exitOnClose = true, bool block = true)
		{
			if (TryDequeue(out KeyValuePair<TSort, TValue> pair, exitOnClose, block))
			{
				value = pair.Value;
				return true;
			}

			value = default;
			return false;
		}

		/// <inheritdoc />
		public abstract void Enqueue(TValue message);

		/// <summary>
		/// Add new message.
		/// </summary>
		/// <param name="sort">Sort order.</param>
		/// <param name="message">Message.</param>
		protected void Enqueue(TSort sort, TValue value)
		{
			Enqueue(new KeyValuePair<TSort, TValue>(sort, value));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="force"></param>
		protected override void OnEnqueue(KeyValuePair<TSort, TValue> item, bool force)
		{
			InnerCollection.Enqueue(item.Key, item.Value);
		}

		/// <summary>
		/// Dequeue the next element.
		/// </summary>
		/// <returns>The next element.</returns>
		protected override KeyValuePair<TSort, TValue> OnDequeue()
		{
			return InnerCollection.Dequeue();
		}

		/// <summary>
		/// To get from top the current element.
		/// </summary>
		/// <returns>The current element.</returns>
		protected override KeyValuePair<TSort, TValue> OnPeek()
		{
			return InnerCollection.Peek();
		}
	}
}