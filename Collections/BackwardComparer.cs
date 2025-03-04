namespace Ecng.Collections;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides a comparer that compares objects in reverse order.
/// </summary>
/// <typeparam name="T">The type of objects to compare. Must implement <see cref="IComparable{T}"/>.</typeparam>
public class BackwardComparer<T> : IComparer<T>
	where T : IComparable<T>
{
	int IComparer<T>.Compare(T x, T y)
	{
		return -x.CompareTo(y);
	}
}