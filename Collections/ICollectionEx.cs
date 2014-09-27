namespace Ecng.Collections
{
	using System.Collections.Generic;

	public interface ICollectionEx<T> : ICollection<T>
	{
		void AddRange(IEnumerable<T> items);
		IEnumerable<T> RemoveRange(IEnumerable<T> items);
		void RemoveRange(int index, int count);
	}
}