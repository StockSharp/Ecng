namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public class StackEx<T> : Stack<T>, ICollection<T>
	{
		void ICollection<T>.Add(T item)
		{
			Push(item);
		}

		bool ICollection<T>.Remove(T item)
		{
			throw new NotSupportedException();
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}
	}
}