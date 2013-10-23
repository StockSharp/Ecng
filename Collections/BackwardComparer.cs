namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public class BackwardComparer<T> : IComparer<T>
		where T : IComparable<T>
	{
		int IComparer<T>.Compare(T x, T y)
		{
			return -x.CompareTo(y);
		}
	}
}