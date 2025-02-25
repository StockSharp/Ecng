namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Extends the <see cref="Stack{T}"/> class to explicitly implement the <see cref="ICollection{T}"/> interface.
	/// </summary>
	/// <typeparam name="T">The type of elements in the stack.</typeparam>
	public class StackEx<T> : Stack<T>, ICollection<T>
	{
		/// <summary>
		/// Adds an item to the top of the stack.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <remarks>This method delegates to <see cref="Stack{T}.Push(T)"/>.</remarks>
		void ICollection<T>.Add(T item)
		{
			Push(item);
		}

		/// <summary>
		/// Attempts to remove a specific item from the stack. This operation is not supported.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>Always throws <see cref="NotSupportedException"/>.</returns>
		/// <exception cref="NotSupportedException">Thrown because removing a specific item is not supported by a stack.</exception>
		bool ICollection<T>.Remove(T item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets a value indicating whether the stack is read-only.
		/// </summary>
		/// <remarks>Always returns false, as stacks are inherently mutable.</remarks>
		bool ICollection<T>.IsReadOnly => false;
	}
}