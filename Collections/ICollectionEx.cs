namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public interface ICollectionEx<T> : ICollection<T>
	{
		event Action<IEnumerable<T>> AddedRange;
		event Action<IEnumerable<T>> RemovedRange;

		void AddRange(IEnumerable<T> items);
		void RemoveRange(IEnumerable<T> items);
		int RemoveRange(int index, int count);
	}
}