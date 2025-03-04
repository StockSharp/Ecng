namespace Ecng.Collections;

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Extended version of <see cref="IEnumerable"/> that provides the total number of elements.
/// </summary>
public interface IEnumerableEx : IEnumerable
{
	/// <summary>
	/// Gets the total number of elements.
	/// </summary>
	int Count { get; }
}

/// <summary>
/// Extended version of <see cref="IEnumerable{T}"/> that provides the total number of elements.
/// </summary>
/// <typeparam name="T">The type of the element.</typeparam>
public interface IEnumerableEx<out T> : IEnumerable<T>, IEnumerableEx
{
}