namespace Ecng.Collections
{
	using System.Collections;
	using System.Collections.Generic;
    using System.Web.UI.WebControls;

	public interface IListEx : IList
	{
		//Array Array { get; }

		//void CopyTo(Array array);
		//void CopyTo(Array array, int index, int count);

		//IEnumerable GetRange();
		//IEnumerable GetRange(long startIndex, long count);
		IEnumerable GetRange(long startIndex, long count, string sortExpression = null, SortDirection directions = SortDirection.Ascending);
	}

	public interface IListEx<T> : IList<T>
	{
		//new T[] Array { get; }

		//int LastIndexOf(T item);

		//IEnumerable<T> GetRange();
		//IEnumerable<T> GetRange(long startIndex, long count);
		IEnumerable<T> GetRange(long startIndex, long count, string sortExpression = null, SortDirection directions = SortDirection.Ascending);
		//T[] ToArray();

		//void CopyTo(T[] array);
		//void CopyTo(T[] array, int index, int count);
	}
}