namespace Ecng.Collections
{
	using System.Collections.Generic;

	public interface ICollectionEx<T> : ICollection<T>
	{
		void AddRange(IEnumerable<T> items);
		void RemoveRange(IEnumerable<T> items);
	}
}