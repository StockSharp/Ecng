namespace Ecng.Common
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public class AllocationArray<T> : IEnumerable<T>
	{
		private class AllocationArrayEnumerator : IEnumerator<T>
		{
			private readonly AllocationArray<T> _parent;
			private T _current;
			private int _pos;

			public AllocationArrayEnumerator(AllocationArray<T> parent)
			{
				_parent = parent;
			}

			void IDisposable.Dispose() => _pos = 0;

			bool IEnumerator.MoveNext()
			{
				if (_pos < _parent._count)
				{
					_current = _parent._buffer[_pos++];
					return true;
				}

				return false;
			}

			void IEnumerator.Reset() => _pos = 0;

			T IEnumerator<T>.Current => _current;

			object IEnumerator.Current => _current;
		}

		private readonly int _capacity;
		private T[] _buffer;
		private readonly AllocationArrayEnumerator _enumerator;

		public AllocationArray(int capacity = 1)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException(nameof(capacity));

			_capacity = capacity;
			_buffer = new T[capacity];
			_enumerator = new AllocationArrayEnumerator(this);
		}

		private int _count;

		public int Count
		{
			get { return _count; }
			set
			{
				if (_buffer.Length < value)
					Array.Resize(ref _buffer, value);

				_count = value;
			}
		}

		public T[] Buffer => _buffer;

		public void Reset(int capacity = 1)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException(nameof(capacity));

			_count = 0;

			if (_buffer.Length < capacity)
				Array.Resize(ref _buffer, capacity);
		}

		public void Add(T item)
		{
			_buffer[_count++] = item;

			if (_buffer.Length <= _count)
				Array.Resize(ref _buffer, _buffer.Length + _capacity);
		}

		public IEnumerator<T> GetEnumerator() => _enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}