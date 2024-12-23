﻿namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading;

	using Ecng.Common;

	// http://stackoverflow.com/questions/530211/creating-a-blocking-queuet-in-net
	public abstract class BaseBlockingQueue<T, TF>(TF innerCollection) : ISynchronizedCollection<T>, IBlockingQueue<T>
		where TF : ICollection<T>
	{
		protected TF InnerCollection { get; } = innerCollection;

		// -1 is unlimited
		private int _maxSize = -1;

		public int MaxSize
		{
			get => _maxSize;
			set
			{
				if (value == 0 || value < -1)
					throw new ArgumentOutOfRangeException(nameof(value));

				_maxSize = value;
			}
		}

		public SyncObject SyncRoot { get; } = new();

		public int Count => InnerCollection.Count;

		private bool _isClosed;

		public bool IsClosed => _isClosed;

		public void Close()
		{
			lock (SyncRoot)
			{
				_isClosed = true;
				Monitor.PulseAll(SyncRoot);
			}
		}

		public void Open()
		{
			lock (SyncRoot)
			{
				_isClosed = false;
			}
		}

		private void WaitWhileFull()
		{
			while (InnerCollection.Count >= _maxSize && !_isClosed)
			{
				Monitor.Wait(SyncRoot);
			}
		}

		public void WaitUntilEmpty()
		{
			lock (SyncRoot)
			{
				while (InnerCollection.Count > 0 && !_isClosed)
					Monitor.Wait(SyncRoot);
			}
		}

		public void Enqueue(T item, bool force = false)
		{
			lock (SyncRoot)
			{
				if (_isClosed)
					return;

				if (!force && _maxSize != -1)
				{
					if (InnerCollection.Count >= _maxSize)
						WaitWhileFull();
				}

				OnEnqueue(item, force);

				if (InnerCollection.Count == 1)
				{
					// wake up any blocked dequeue
					Monitor.PulseAll(SyncRoot);
				}
			}
		}

		protected abstract void OnEnqueue(T item, bool force);
		protected abstract T OnDequeue();
		protected abstract T OnPeek();

		public T Dequeue()
		{
			TryDequeue(out T retVal, false);
			return retVal;
		}

		private bool WaitWhileEmpty(bool exitOnClose, bool block)
		{
			while (InnerCollection.Count == 0)
			{
				if (exitOnClose && _isClosed)
					return false;

				if (!block)
					return false;

				Monitor.Wait(SyncRoot);
			}

			return true;
		}

		public bool TryDequeue(out T value, bool exitOnClose = true, bool block = true)
		{
			lock (SyncRoot)
			{
				if (!WaitWhileEmpty(exitOnClose, block))
				{
					value = default;
					return false;
				}

				value = OnDequeue();

				if (InnerCollection.Count == (_maxSize - 1) || InnerCollection.Count == 0)
				{
					// wake up any blocked enqueue
					Monitor.PulseAll(SyncRoot);
				}

				return true;
			}
		}

		public T Peek()
		{
			TryPeek(out T retVal, false);
			return retVal;
		}

		public bool TryPeek(out T value, bool exitOnClose = true, bool block = true)
		{
			lock (SyncRoot)
			{
				if (!WaitWhileEmpty(exitOnClose, block))
				{
					value = default;
					return false;
				}

				value = OnPeek();

				return true;
			}
		}

		public void Clear()
		{
			lock (SyncRoot)
			{
				InnerCollection.Clear();
				Monitor.PulseAll(SyncRoot);
			}
		}

		bool ICollection<T>.Remove(T item)
		{
			throw new NotSupportedException();
		}

		bool ICollection<T>.IsReadOnly => false;

		void ICollection<T>.Add(T item)
		{
			Enqueue(item);
		}

		bool ICollection<T>.Contains(T item)
		{
			lock (SyncRoot)
				return InnerCollection.Contains(item);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			lock (SyncRoot)
				InnerCollection.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return InnerCollection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}