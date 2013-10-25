namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading;

	using Ecng.Common;

	// http://stackoverflow.com/questions/530211/creating-a-blocking-queuet-in-net
	public sealed class BlockingQueue<T> : ISynchronizedCollection<T>
	{
		private readonly Queue<T> _queue = new Queue<T>();

		// -1 is unlimited
		private int _maxSize = -1;

		public int MaxSize
		{
			get { return _maxSize; }
			set
			{
				if (value == 0 || value < -1)
					throw new ArgumentOutOfRangeException("value");

				_maxSize = value;
			}
		}

		private readonly SyncObject _syncRoot = new SyncObject();

		public SyncObject SyncRoot
		{
			get { return _syncRoot; }
		}

		public int Count
		{
			get { return _queue.Count; }
		}

		private bool _isClosed;

		public bool IsClosed { get { return _isClosed; } }

		public void Close()
		{
			lock (SyncRoot)
			{
				_isClosed = true;
				Monitor.PulseAll(_syncRoot);
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
			while (_queue.Count >= _maxSize && !_isClosed)
			{
				Monitor.Wait(_syncRoot);
			}
		}

		public void WaitUntilEmpty()
		{
			lock (_syncRoot)
			{
				while (_queue.Count > 0 && !_isClosed)
					Monitor.Wait(_syncRoot);
			}
		}

		public void Enqueue(T item, bool force = false)
		{
			lock (_syncRoot)
			{
				if (_isClosed)
					return;

				if (!force && _maxSize != -1)
				{
					if (_queue.Count >= _maxSize)
						WaitWhileFull();
				}

				_queue.Enqueue(item);

				if (_queue.Count == 1)
				{
					// wake up any blocked dequeue
					Monitor.PulseAll(_syncRoot);
				}
			}
		}

		public T Dequeue()
		{
			T retVal;
			TryDequeue(out retVal, false);
			return retVal;
		}

		public bool TryDequeue(out T value)
		{
			return TryDequeue(out value, true);
		}

		private bool WaitWhileEmpty(bool exitOnClose)
		{
			while (_queue.Count == 0)
			{
				if (exitOnClose && _isClosed)
					return false;

				Monitor.Wait(_syncRoot);
			}

			return true;
		}

		public bool TryDequeue(out T value, bool exitOnClose)
		{
			lock (_syncRoot)
			{
				if (!WaitWhileEmpty(exitOnClose))
				{
					value = default(T);
					return false;
				}

				value = _queue.Dequeue();

				if (_queue.Count == (_maxSize - 1) || _queue.Count == 0)
				{
					// wake up any blocked enqueue
					Monitor.PulseAll(_syncRoot);
				}

				return true;
			}
		}

		public T Peek()
		{
			T retVal;
			TryPeek(out retVal, false);
			return retVal;
		}

		public bool TryPeek(out T value)
		{
			return TryPeek(out value, true);
		}

		private bool TryPeek(out T value, bool exitOnClose)
		{
			lock (SyncRoot)
			{
				if (!WaitWhileEmpty(exitOnClose))
				{
					value = default(T);
					return false;
				}

				value = _queue.Peek();

				return true;
			}
		}

		public void Clear()
		{
			lock (SyncRoot)
			{
				_queue.Clear();
				Monitor.PulseAll(SyncRoot);
			}
		}

		bool ICollection<T>.Remove(T item)
		{
			throw new NotSupportedException();
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		void ICollection<T>.Add(T item)
		{
			Enqueue(item);
		}

		bool ICollection<T>.Contains(T item)
		{
			return _queue.Contains(item);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			lock (SyncRoot)
				_queue.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _queue.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}