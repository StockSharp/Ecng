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
public class CircularBuffer<T> : IEnumerable<T>, IList<T>
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
			throw new ArgumentException(
				"Circular buffer cannot have negative or zero capacity.", nameof(capacity));
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

	/// <summary>
	/// Maximum capacity of the buffer. Elements pushed into the buffer after
	/// maximum capacity is reached (IsFull = true), will remove an element.
	/// </summary>
	public virtual int Capacity
	{
		get => _buffer.Length;
		set
		{
			Array.Resize(ref _buffer, value);
		}
	}

	/// <summary>
	/// Boolean indicating if Circular is at full capacity.
	/// Adding more elements when the buffer is full will
	/// cause elements to be removed from the other end
	/// of the buffer.
	/// </summary>
	public bool IsFull
	{
		get
		{
			return Count == Capacity;
		}
	}

	/// <summary>
	/// True if has no elements.
	/// </summary>
	public bool IsEmpty => Count == 0;

	/// <summary>
	/// Current buffer size (the number of elements that the buffer has).
	/// </summary>
	public int Count => _count;

	/// <summary>
	/// Element at the front of the buffer - this[0].
	/// </summary>
	/// <returns>The value of the element of type T at the front of the buffer.</returns>
	public T Front()
	{
		ThrowIfEmpty();
		return _buffer[_start];
	}

	/// <summary>
	/// Element at the back of the buffer - this[Size - 1].
	/// </summary>
	/// <returns>The value of the element of type T at the back of the buffer.</returns>
	public T Back()
	{
		ThrowIfEmpty();
		return _buffer[(_end != 0 ? _end : Capacity) - 1];
	}

	/// <summary>
	/// Index access to elements in buffer.
	/// Index does not loop around like when adding elements,
	/// valid interval is [0;Size[
	/// </summary>
	/// <param name="index">Index of element to access.</param>
	/// <exception cref="IndexOutOfRangeException">Thrown when index is outside of [; Size[ interval.</exception>
	public virtual T this[int index]
	{
		get
		{
			if (IsEmpty)
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
			}
			if (index >= _count)
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _count));
			}
			int actualIndex = InternalIndex(index);
			return _buffer[actualIndex];
		}
		set
		{
			if (IsEmpty)
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
			}
			if (index >= _count)
			{
				throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, _count));
			}
			int actualIndex = InternalIndex(index);
			_buffer[actualIndex] = value;
		}
	}

	/// <summary>
	/// Pushes a new element to the back of the buffer. Back()/this[Size-1]
	/// will now return this element.
	/// 
	/// When the buffer is full, the element at Front()/this[0] will be 
	/// popped to allow for this new element to fit.
	/// </summary>
	/// <param name="item">Item to push to the back of the buffer</param>
	public virtual void PushBack(T item)
	{
		if (IsFull)
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

	/// <summary>
	/// Pushes a new element to the front of the buffer. Front()/this[0]
	/// will now return this element.
	/// 
	/// When the buffer is full, the element at Back()/this[Size-1] will be 
	/// popped to allow for this new element to fit.
	/// </summary>
	/// <param name="item">Item to push to the front of the buffer</param>
	public virtual void PushFront(T item)
	{
		if (IsFull)
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

	/// <summary>
	/// Removes the element at the back of the buffer. Decreasing the 
	/// Buffer size by 1.
	/// </summary>
	public virtual void PopBack()
	{
		ThrowIfEmpty("Cannot take elements from an empty buffer.");
		Decrement(ref _end);
		_buffer[_end] = default;
		--_count;
	}

	/// <summary>
	/// Removes the element at the front of the buffer. Decreasing the 
	/// Buffer size by 1.
	/// </summary>
	public virtual void PopFront()
	{
		ThrowIfEmpty("Cannot take elements from an empty buffer.");
		_buffer[_start] = default;
		Increment(ref _start);
		--_count;
	}

	/// <summary>
	/// Clears the contents of the array. Size = 0, Capacity is unchanged.
	/// </summary>
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
		if (IsEmpty)
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
		if (IsEmpty)
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
		if (IsEmpty)
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

		if (IsFull)
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