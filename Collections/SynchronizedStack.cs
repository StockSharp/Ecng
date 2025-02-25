namespace Ecng.Collections
{
	using System;

	/// <summary>
	/// Represents a synchronized stack of items.
	/// </summary>
	[Serializable]
	public class SynchronizedStack<T> : SynchronizedCollection<T, StackEx<T>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SynchronizedStack{T}"/> class.
		/// </summary>
		public SynchronizedStack()
			: base(new StackEx<T>())
		{
		}

		/// <summary>
		/// Adds an item to the top of this stack.
		/// </summary>
		/// <param name="item">The item to add.</param>
		public void Push(T item)
		{
			lock (SyncRoot)
				InnerCollection.Push(item);
		}

		/// <summary>
		/// Removes and returns the object at the top of this stack.
		/// </summary>
		/// <returns>The item removed from the top.</returns>
		public T Pop()
		{
			lock (SyncRoot)
				return InnerCollection.Pop();
		}

		/// <summary>
		/// Returns the object at the top of this stack without removing it.
		/// </summary>
		/// <returns>The item at the top.</returns>
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