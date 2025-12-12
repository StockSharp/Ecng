namespace Ecng.Collections;

// Source: https://github.com/joaoportela/CircularBuffer-CSharp

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Circular buffer.
/// 
/// When writing to a full buffer:
/// PushBack -> removes this[0] / Front()
/// PushFront -> removes this[Size-1] / Back()
/// 
/// this implementation is inspired by
/// http://www.boost.org/doc/libs/1_53_0/libs/circular_buffer/doc/circular_buffer.html
/// because I liked their interface.
/// </summary>
public class CircularBuffer<T> : ICircularBuffer<T>
{
	private T[] _buffer;

	/// <summary>
	/// The _start. Index of the first element in buffer.
	/// </summary>
	private int _start;

	/// <summary>
	/// The _end. Index after the last element in the buffer.
	/// </summary>
	private int _end;

	/// <summary>
	/// The _size. Buffer size.
	/// </summary>
	private int _count;

	/// <summary>
	/// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
	/// 
	/// </summary>
	/// <param name='capacity'>
	/// Buffer capacity. Must be positive.
	/// </param>
	public CircularBuffer(int capacity)
		: this(capacity, [])
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
	/// 
	/// </summary>
	/// <param name='capacity'>
	/// Buffer capacity. Must be positive.
	/// </param>
	/// <param name='items'>
	/// Items to fill buffer with. Items length must be less than capacity.
	/// Suggestion: use Skip(x).Take(y).ToArray() to build this argument from
	/// any enumerable.
	/// </param>
	public CircularBuffer(int capacity, T[] items)
	{
		if (capacity < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Circular buffer cannot have negative or zero capacity.");
		}
		if (items == null)
		{
			throw new ArgumentNullException(nameof(items));
		}
		if (items.Length > capacity)
		{
			throw new ArgumentException(
				"Too many items to fit circular buffer", nameof(items));
		}

		_buffer = new T[capacity];

		Array.Copy(items, _buffer, items.Length);
		_count = items.Length;

		_start = 0;
		_end = _count == capacity ? 0 : _count;
	}

	/// <inheritdoc />
	public virtual int Capacity
	{
		get => _buffer.Length;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Circular buffer cannot have negative or zero capacity.");

			if (value == Capacity)
				return;

			if (_count == 0)
			{
				_buffer = new T[value];
				_start = _end = 0;
				return;
			}

			// materialize logical order once
			var src = ToArray();

			if (value >= _count)
			{
				// grow: keep all items in order
				var dst = new T[value];
				Array.Copy(src, 0, dst, 0, _count);

				_buffer = dst;
				_start = 0;
				_end = _count % value; // == _count when value > _count, иначе 0 если ровно
			}
			else
			{
				// shrink: keep the last 'value' items (drop oldest)
				var dst = new T[value];
				Array.Copy(src, src.Length - value, dst, 0, value);

				_buffer = dst;
				_count = value;
				_start = 0;
				_end = 0;
			}
		}
	}

	/// <inheritdoc />
	public int Count => _count;

	/// <inheritdoc />
	public T Front()
	{
		ThrowIfEmpty();
		return _buffer[_start];
	}

	/// <inheritdoc />
	public T Back()
	{
		ThrowIfEmpty();
		return _buffer[(_end != 0 ? _end : Capacity) - 1];
	}

	/// <inheritdoc />
	public virtual T this[int index]
	{
		get
		{
			if (this.IsEmpty())
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
			}
			if (index < 0 || index >= _count)
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _count));
			}
			int actualIndex = InternalIndex(index);
			return _buffer[actualIndex];
		}
		set
		{
			if (this.IsEmpty())
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
			}
			if (index < 0 || index >= _count)
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _count));
			}
			int actualIndex = InternalIndex(index);
			_buffer[actualIndex] = value;
		}
	}

	/// <inheritdoc />
	public virtual void PushBack(T item)
	{
		if (this.IsFull())
		{
			_buffer[_end] = item;
			Increment(ref _end);
			_start = _end;
		}
		else
		{
			_buffer[_end] = item;
			Increment(ref _end);
			++_count;
		}
	}

	/// <inheritdoc />
	public virtual void PushFront(T item)
	{
		if (this.IsFull())
		{
			Decrement(ref _start);
			_end = _start;
			_buffer[_start] = item;
		}
		else
		{
			Decrement(ref _start);
			_buffer[_start] = item;
			++_count;
		}
	}

	/// <inheritdoc />
	public virtual void PopBack()
	{
		ThrowIfEmpty("Cannot take elements from an empty buffer.");
		Decrement(ref _end);
		_buffer[_end] = default;
		--_count;
	}

	/// <inheritdoc />
	public virtual void PopFront()
	{
		ThrowIfEmpty("Cannot take elements from an empty buffer.");
		_buffer[_start] = default;
		Increment(ref _start);
		--_count;
	}

	/// <inheritdoc />
	public virtual void Clear()
	{
		// to clear we just reset everything.
		_start = 0;
		_end = 0;
		_count = 0;
		Array.Clear(_buffer, 0, _buffer.Length);
	}

	/// <summary>
	/// Copies the buffer contents to an array, according to the logical
	/// contents of the buffer (i.e. independent of the internal 
	/// order/contents)
	/// </summary>
	/// <returns>A new array with a copy of the buffer contents.</returns>
	public T[] ToArray()
	{
		T[] newArray = new T[Count];
		int newArrayOffset = 0;
		var segments = ToArraySegments();
		foreach (var segment in segments)
		{
			Array.Copy(segment.Array, segment.Offset, newArray, newArrayOffset, segment.Count);
			newArrayOffset += segment.Count;
		}
		return newArray;
	}

	/// <summary>
	/// Get the contents of the buffer as 2 ArraySegments.
	/// Respects the logical contents of the buffer, where
	/// each segment and items in each segment are ordered
	/// according to insertion.
	///
	/// Fast: does not copy the array elements.
	/// Useful for methods like <c>Send(IList&lt;ArraySegment&lt;Byte&gt;&gt;)</c>.
	/// 
	/// <remarks>Segments may be empty.</remarks>
	/// </summary>
	/// <returns>An IList with 2 segments corresponding to the buffer content.</returns>
	public IList<ArraySegment<T>> ToArraySegments()
	{
		return [ArrayOne(), ArrayTwo()];
	}

	#region IEnumerable<T> implementation
	/// <summary>
	/// Returns an enumerator that iterates through this buffer.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate this collection.</returns>
	public IEnumerator<T> GetEnumerator()
	{
		var segments = ToArraySegments();
		foreach (var segment in segments)
		{
			for (int i = 0; i < segment.Count; i++)
			{
				yield return segment.Array[segment.Offset + i];
			}
		}
	}
	#endregion
	#region IEnumerable implementation
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	#endregion

	private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
	{
		if (this.IsEmpty())
		{
			throw new InvalidOperationException(message);
		}
	}

	/// <summary>
	/// Increments the provided index variable by one, wrapping
	/// around if necessary.
	/// </summary>
	/// <param name="index"></param>
	private void Increment(ref int index)
	{
		if (++index == Capacity)
		{
			index = 0;
		}
	}

	/// <summary>
	/// Decrements the provided index variable by one, wrapping
	/// around if necessary.
	/// </summary>
	/// <param name="index"></param>
	private void Decrement(ref int index)
	{
		if (index == 0)
		{
			index = Capacity;
		}
		index--;
	}

	/// <summary>
	/// Converts the index in the argument to an index in <code>_buffer</code>
	/// </summary>
	/// <returns>
	/// The transformed index.
	/// </returns>
	/// <param name='index'>
	/// External index.
	/// </param>
	private int InternalIndex(int index)
	{
		return _start + (index < (Capacity - _start) ? index : index - Capacity);
	}

	// doing ArrayOne and ArrayTwo methods returning ArraySegment<T> as seen here: 
	// http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1957cccdcb0c4ef7d80a34a990065818d
	// http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1f5081a54afbc2dfc1a7fb20329df7d5b
	// should help a lot with the code.

	#region Array items easy access.
	// The array is composed by at most two non-contiguous segments, 
	// the next two methods allow easy access to those.

	private ArraySegment<T> ArrayOne()
	{
		if (this.IsEmpty())
		{
			return new([]);
		}
		else if (_start < _end)
		{
			return new(_buffer, _start, _end - _start);
		}
		else
		{
			return new(_buffer, _start, _buffer.Length - _start);
		}
	}

	private ArraySegment<T> ArrayTwo()
	{
		if (this.IsEmpty())
		{
			return new([]);
		}
		else if (_start < _end)
		{
			return new(_buffer, _end, 0);
		}
		else
		{
			return new(_buffer, 0, _end);
		}
	}

	#endregion

	int IList<T>.IndexOf(T item)
	{
		var comparer = EqualityComparer<T>.Default;

		for (int i = 0; i < _count; i++)
		{
			if (comparer.Equals(this[i], item))
				return i;
		}

		return -1;
	}

	void IList<T>.Insert(int index, T item)
	{
		if (index < 0 || index > _count)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (this.IsFull())
			throw new InvalidOperationException("Buffer is full.");

		if (index == 0)
		{
			PushFront(item);
			return;
		}

		if (index == _count)
		{
			PushBack(item);
			return;
		}

		// Shift elements to make space
		PushBack(this[_count - 1]);

		for (int i = _count - 2; i > index - 1; i--)
		{
			this[i] = this[i - 1];
		}
		this[index] = item;
	}

	void IList<T>.RemoveAt(int index)
	{
		if (index < 0 || index >= _count)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (index == 0)
		{
			PopFront();
			return;
		}

		if (index == _count - 1)
		{
			PopBack();
			return;
		}

		for (int i = index; i < _count - 1; i++)
		{
			this[i] = this[i + 1];
		}

		PopBack();
	}

	bool ICollection<T>.Contains(T item)
	{
		return ((IList<T>)this).IndexOf(item) != -1;
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
			throw new ArgumentNullException(nameof(array));

		if (arrayIndex < 0 || arrayIndex + _count > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		for (int i = 0; i < _count; i++)
			array[arrayIndex + i] = this[i];
	}

	bool ICollection<T>.Remove(T item)
	{
		int idx = ((IList<T>)this).IndexOf(item);

		if (idx == -1)
			return false;

		((IList<T>)this).RemoveAt(idx);
		return true;
	}

	bool ICollection<T>.IsReadOnly => false;
	void ICollection<T>.Add(T item) => PushBack(item);
}