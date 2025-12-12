namespace Ecng.Collections;

using System.Collections.Generic;

/// <summary>
/// Common interface for circular buffers.
/// </summary>
/// <typeparam name="T">The type of elements in the buffer.</typeparam>
public interface ICircularBuffer<T> : IList<T>
{
	/// <summary>
	/// Element at the front of the buffer - this[0].
	/// </summary>
	/// <returns>The value of the element of type T at the front of the buffer.</returns>
	T Front();

	/// <summary>
	/// Element at the back of the buffer - this[Size - 1].
	/// </summary>
	/// <returns>The value of the element of type T at the back of the buffer.</returns>
	T Back();

	/// <summary>
	/// Pushes a new element to the back of the buffer. Back()/this[Size-1]
	/// will now return this element.
	/// 
	/// When the buffer is full, the element at Front()/this[0] will be 
	/// popped to allow for this new element to fit.
	/// </summary>
	/// <param name="item">Item to push to the back of the buffer</param>
	void PushBack(T item);

	/// <summary>
	/// Pushes a new element to the front of the buffer. Front()/this[0]
	/// will now return this element.
	/// 
	/// When the buffer is full, the element at Back()/this[Size-1] will be 
	/// popped to allow for this new element to fit.
	/// </summary>
	/// <param name="item">Item to push to the front of the buffer</param>
	void PushFront(T item);

	/// <summary>
	/// Removes the element at the back of the buffer. Decreasing the 
	/// Buffer size by 1.
	/// </summary>
	void PopBack();

	/// <summary>
	/// Removes the element at the front of the buffer. Decreasing the 
	/// Buffer size by 1.
	/// </summary>
	void PopFront();

	/// <summary>
	/// Maximum capacity of the buffer.
	/// </summary>
	int Capacity { get; set; }
}