namespace Ecng.Common;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Represents a dynamically resizing array allocation.
/// </summary>
/// <typeparam name="T">The type of elements stored in the array.</typeparam>
public class AllocationArray<T> : IEnumerable<T>
{
	/// <summary>
	/// Enumerator for the AllocationArray.
	/// </summary>
	private class AllocationArrayEnumerator(AllocationArray<T> parent) : IEnumerator<T>
	{
		private readonly AllocationArray<T> _parent = parent ?? throw new ArgumentNullException(nameof(parent));
		private T _current;
		private int _pos;

		/// <summary>
		/// Releases resources used by the enumerator.
		/// </summary>
		void IDisposable.Dispose() => _pos = 0;

		/// <summary>
		/// Advances the enumerator to the next element.
		/// </summary>
		/// <returns>true if the enumerator was successfully advanced; otherwise, false.</returns>
		bool IEnumerator.MoveNext()
		{
			if (_pos < _parent._count)
			{
				_current = _parent._buffer[_pos++];
				return true;
			}

			return false;
		}

		/// <summary>
		/// Resets the enumerator to its initial position.
		/// </summary>
		void IEnumerator.Reset() => _pos = 0;

		/// <summary>
		/// Gets the current element in the array.
		/// </summary>
		T IEnumerator<T>.Current => _current;

		/// <summary>
		/// Gets the current element in the array.
		/// </summary>
		object IEnumerator.Current => _current;
	}

	//private readonly int _capacity;
	private T[] _buffer;

	/// <summary>
	/// Initializes a new instance of the <see cref="AllocationArray{T}"/> class with the specified capacity.
	/// </summary>
	/// <param name="capacity">The initial capacity of the array. Must be at least 1.</param>
	public AllocationArray(int capacity = 1)
	{
		if (capacity < 1)
			throw new ArgumentOutOfRangeException(nameof(capacity));

		//_capacity = capacity;
		_buffer = new T[capacity];
	}

	/// <summary>
	/// Gets or sets the maximum allowed count of elements.
	/// </summary>
	public int MaxCount { get; set; } = int.MaxValue / 4;

	private int _count;

	/// <summary>
	/// Gets or sets the current number of elements in the array.
	/// </summary>
	public int Count
	{
		get => _count;
		set
		{
			if (_buffer.Length < value)
			{
				if (value > MaxCount)
					throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

				Resize(value);
			}

			_count = value;
		}
	}

	/// <summary>
	/// Gets or sets the element at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the element to get or set.</param>
	/// <returns>The element at the specified index.</returns>
	public T this[int index]
	{
		get
		{
			if (index >= Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, "Invalid value.");

			return _buffer[index];
		}
		set
		{
			if (index >= Count)
				Count = index + 1;

			_buffer[index] = value;
		}
	}

	/// <summary>
	/// Gets the underlying buffer array.
	/// </summary>
	public T[] Buffer => _buffer;

	/// <summary>
	/// Resets the array, clearing all elements and optionally resizing the buffer.
	/// </summary>
	/// <param name="capacity">The desired capacity. Must be at least 1.</param>
	public void Reset(int capacity = 1)
	{
		if (capacity < 1)
			throw new ArgumentOutOfRangeException(nameof(capacity));

		_count = 0;

		if (_buffer.Length < capacity)
			Resize(capacity);
	}

	/// <summary>
	/// Ensures that the array has the specified new size capacity.
	/// </summary>
	/// <param name="newSize">The required size.</param>
	private void EnsureCapacity(int newSize)
	{
		if (_buffer.Length > newSize)
			return;

		if (newSize > MaxCount)
			throw new ArgumentOutOfRangeException(nameof(newSize), newSize, "Invalid value.");

		Resize(newSize * 2);
	}

	/// <summary>
	/// Adds an item to the end of the array.
	/// </summary>
	/// <param name="item">The item to add.</param>
	public void Add(T item)
	{
		EnsureCapacity(_count + 1);

		_buffer[_count] = item;
		_count++;
	}

	/// <summary>
	/// Adds a range of items from an array to the end of the allocation array.
	/// </summary>
	/// <param name="items">The array of items to add.</param>
	/// <param name="offset">The zero-based index at which to start copying from the source array.</param>
	/// <param name="count">The number of items to copy.</param>
	public void Add(T[] items, int offset, int count)
	{
		EnsureCapacity(_count + count);

		Array.Copy(items, offset, _buffer, _count, count);
		_count += count;
	}

	/// <summary>
	/// Removes the item at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the item to remove.</param>
	public void RemoveAt(int index)
	{
		RemoveRange(index, 1);
	}

	/// <summary>
	/// Removes a range of items from the array.
	/// </summary>
	/// <param name="startIndex">The zero-based starting index of the range to remove.</param>
	/// <param name="count">The number of items to remove.</param>
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

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the allocation array.</returns>
	public IEnumerator<T> GetEnumerator() => new AllocationArrayEnumerator(this);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator for the allocation array.</returns>
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <summary>
	/// Resizes the buffer array to the specified capacity.
	/// </summary>
	/// <param name="capacity">The new capacity of the buffer.</param>
	private void Resize(int capacity)
	{
		Array.Resize(ref _buffer, capacity);
	}
}