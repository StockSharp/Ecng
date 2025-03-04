namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Extends the <see cref="Queue{T}"/> class to explicitly implement the <see cref="ICollection{T}"/> interface.
/// </summary>
/// <typeparam name="T">The type of elements in the queue.</typeparam>
public class QueueEx<T> : Queue<T>, ICollection<T>
{
	#region Implementation of ICollection<T>

	void ICollection<T>.Add(T item)
	{
		Enqueue(item);
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.IsReadOnly => false;

	#endregion
}