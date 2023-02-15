namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.ComponentModel;

	[Obsolete]
	public interface IRangeCollection : ICollection
	{
		IEnumerable GetRange(long startIndex, long count, string sortExpression = null, ListSortDirection directions = ListSortDirection.Ascending);
	}
}