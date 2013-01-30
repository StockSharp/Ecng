namespace Ecng.Collections
{
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Common;

	public class SynchronizedLinkedList<T> : ISynchronizedCollection<T>
	{
		private readonly LinkedList<T> _inner = new LinkedList<T>();

		private readonly SyncObject _syncRoot = new SyncObject();

		public SyncObject SyncRoot
		{
			get { return _syncRoot; }
		}

		public virtual LinkedListNode<T> First
		{
			get
			{
				lock (SyncRoot)
					return _inner.First;
			}
		}

		public virtual LinkedListNode<T> Last
		{
			get
			{
				lock (SyncRoot)
					return _inner.Last;
			}
		}

		public virtual void AddBefore(LinkedListNode<T> node, T value)
		{
			lock (SyncRoot)
				_inner.AddBefore(node, value);
		}

		public virtual void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
		{
			lock (SyncRoot)
				_inner.AddBefore(node, newNode);
		}

		public virtual void AddFirst(T value)
		{
			lock (SyncRoot)
				_inner.AddFirst(value);
		}

		public virtual void AddFirst(LinkedListNode<T> node)
		{
			lock (SyncRoot)
				_inner.AddFirst(node);
		}

		public virtual void AddLast(T value)
		{
			lock (SyncRoot)
				_inner.AddLast(value);
		}

		public virtual void AddLast(LinkedListNode<T> node)
		{
			lock (SyncRoot)
				_inner.AddLast(node);
		}

		public virtual void Remove(LinkedListNode<T> node)
		{
			lock (SyncRoot)
				_inner.Remove(node);
		}

		public virtual void RemoveFirst()
		{
			lock (SyncRoot)
				_inner.RemoveFirst();
		}

		public virtual void RemoveLast()
		{
			lock (SyncRoot)
				_inner.RemoveLast();
		}

		public virtual LinkedListNode<T> Find(T value)
		{
			lock (SyncRoot)
				return _inner.Find(value);
		}

		public virtual LinkedListNode<T> FindLast(T value)
		{
			lock (SyncRoot)
				return _inner.FindLast(value);
		}

		public IEnumerator<T> GetEnumerator()
		{
			lock (SyncRoot)
				return _inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		void ICollection<T>.Add(T item)
		{
			lock (SyncRoot)
				((ICollection<T>)_inner).Add(item);
		}

		public void Clear()
		{
			lock (SyncRoot)
				_inner.Clear();
		}

		public bool Contains(T item)
		{
			lock (SyncRoot)
				return _inner.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			lock (SyncRoot)
				_inner.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			lock (SyncRoot)
				return _inner.Remove(item);
		}

		public int Count
		{
			get
			{
				lock (SyncRoot)
					return _inner.Count;
			}
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}
	}
}