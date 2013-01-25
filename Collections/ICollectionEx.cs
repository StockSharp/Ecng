namespace Ecng.Collections
{
	using System.Collections.Generic;

	public interface ICollectionEx<T>
	{
		void AddRange(IEnumerable<T> items);
	}
}