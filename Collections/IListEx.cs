namespace Ecng.Collections
{
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;

	public interface IListEx : IList
	{
		IEnumerable GetRange(long startIndex, long count, string sortExpression = null, ListSortDirection directions = ListSortDirection.Ascending);
	}

	public interface IListEx<T> : IList<T>
	{
		IEnumerable<T> GetRange(long startIndex, long count, string sortExpression = null, ListSortDirection directions = ListSortDirection.Ascending);
	}
}