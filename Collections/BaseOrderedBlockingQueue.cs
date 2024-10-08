﻿namespace Ecng.Collections
{
	using System.Collections.Generic;

	/// <summary>
	/// Message queue.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="BaseOrderedBlockingQueue{TSort, TValue}"/>.
	/// </remarks>
	public abstract class BaseOrderedBlockingQueue<TSort, TValue, TCollection>(TCollection collection) :
		BaseBlockingQueue<(TSort sort, TValue elem), TCollection>(collection)
		where TCollection : ICollection<(TSort, TValue)>, IQueue<(TSort, TValue)>
	{

		/// <inheritdoc />
		public bool TryDequeue(out TValue value, bool exitOnClose = true, bool block = true)
		{
			if (base.TryDequeue(out var pair, exitOnClose, block))
			{
				value = pair.elem;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Add new message.
		/// </summary>
		/// <param name="sort">Sort order.</param>
		/// <param name="value">value.</param>
		protected void Enqueue(TSort sort, TValue value)
			=> Enqueue(new(sort, value));

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="force"></param>
		protected override void OnEnqueue((TSort, TValue) item, bool force)
			=> InnerCollection.Enqueue(item);

		/// <summary>
		/// Dequeue the next element.
		/// </summary>
		/// <returns>The next element.</returns>
		protected override (TSort, TValue) OnDequeue()
			=> InnerCollection.Dequeue();

		/// <summary>
		/// To get from top the current element.
		/// </summary>
		/// <returns>The current element.</returns>
		protected override (TSort, TValue) OnPeek()
			=> InnerCollection.Peek();
	}
}