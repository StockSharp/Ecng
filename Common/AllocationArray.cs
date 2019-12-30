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
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
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

		//private readonly int _capacity;
		private T[] _buffer;
		private readonly AllocationArrayEnumerator _enumerator;

		public AllocationArray(int capacity = 1)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException(nameof(capacity));

			//_capacity = capacity;
			_buffer = new T[capacity];
			_enumerator = new AllocationArrayEnumerator(this);
		}

		public int MaxCount { get; set; } = int.MaxValue / 4;

		private int _count;

		public int Count
		{
			get => _count;
			set
			{
				if (_buffer.Length < value)
				{
					if (value > MaxCount)
						throw new ArgumentOutOfRangeException();

					Resize(value);
				}

				_count = value;
			}
		}

		public T this[int index]
		{
			get
			{
				if (index >= Count)
					throw new ArgumentOutOfRangeException();

				return _buffer[index];
			}
			set
			{
				if (index >= Count)
					Count = index + 1;

				_buffer[index] = value;
			}
		}

		public T[] Buffer => _buffer;

		public void Reset(int capacity = 1)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException(nameof(capacity));

			_count = 0;

			if (_buffer.Length < capacity)
				Resize(capacity);
		}

		private void EnsureCapacity(int newSize)
		{
			if (_buffer.Length > newSize)
				return;

			if (newSize > MaxCount)
				throw new ArgumentOutOfRangeException();

			Resize(newSize * 2);
		}

		public void Add(T item)
		{
			EnsureCapacity(_count + 1);

			_buffer[_count] = item;
			_count++;
		}

		public void Add(T[] items, int offset, int count)
		{
			EnsureCapacity(_count + count);

			Array.Copy(items, offset, _buffer, _count, count);
			_count += count;
		}

		public void RemoveAt(int index)
		{
			RemoveRange(index, 1);
		}

		public void RemoveRange(int startIndex, int count)
		{
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			var begin = startIndex + count;
			var countOfMove = _count - begin;

			if (countOfMove < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (countOfMove > 0)
				Array.Copy(_buffer, begin, _buffer, startIndex, countOfMove);

			_count -= count;
		}

		public IEnumerator<T> GetEnumerator() => _enumerator;

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private void Resize(int capacity)
		{
			Array.Resize(ref _buffer, capacity);
		}
	}
}