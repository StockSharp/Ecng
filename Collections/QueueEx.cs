namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

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

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		#endregion
	}
}