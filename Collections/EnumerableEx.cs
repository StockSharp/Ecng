#if NETSTANDARD2_0
namespace System.Linq;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides additional extension methods for working with enumerables.
/// </summary>
public static class EnumerableEx
{
	// https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/DistinctBy.cs

	/// <summary>
	/// Returns distinct elements from a sequence based on a key selector, using the default equality comparer.
	/// </summary>
	/// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
	/// <typeparam name="TKey">The type of the key returned by the key selector.</typeparam>
	/// <param name="source">The sequence to remove duplicates from.</param>
	/// <param name="keySelector">A function to extract the key for each element.</param>
	/// <returns>An <see cref="IEnumerable{TSource}"/> containing distinct elements based on the key.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector)
		=> source.DistinctBy(keySelector, null);

	/// <summary>
	/// Returns distinct elements from a sequence based on a key selector, using a specified equality comparer.
	/// </summary>
	/// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
	/// <typeparam name="TKey">The type of the key returned by the key selector.</typeparam>
	/// <param name="source">The sequence to remove duplicates from.</param>
	/// <param name="keySelector">A function to extract the key for each element.</param>
	/// <param name="comparer">The equality comparer to compare keys, or null to use the default comparer.</param>
	/// <returns>An <see cref="IEnumerable{TSource}"/> containing distinct elements based on the key.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

		return _(); IEnumerable<TSource> _()
		{
			var knownKeys = new HashSet<TKey>(comparer);
			foreach (var element in source)
			{
				if (knownKeys.Add(keySelector(element)))
					yield return element;
			}
		}
	}

	/// <summary>
	/// Returns the minimum element in a sequence according to a specified comparer.
	/// </summary>
	/// <typeparam name="TItem">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to determine the minimum element from.</param>
	/// <param name="comparer">The comparer to compare elements.</param>
	/// <returns>The minimum element in the sequence.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="comparer"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the sequence is empty.</exception>
	public static TItem Min<TItem>(this IEnumerable<TItem> source, IComparer<TItem> comparer)
	{
		if (source == null)		throw new ArgumentNullException(nameof(source));
		if (comparer == null)	throw new ArgumentNullException(nameof(comparer));

		using var enumerator = source.GetEnumerator();

		if (!enumerator.MoveNext())
			throw new InvalidOperationException("Sequence contains no elements.");

		TItem min = enumerator.Current;

		while (enumerator.MoveNext())
		{
			TItem current = enumerator.Current;

			if (comparer.Compare(current, min) < 0)
			{
				min = current;
			}
		}

		return min;
	}

	/// <summary>
	/// Returns the maximum element in a sequence according to a specified comparer.
	/// </summary>
	/// <typeparam name="TItem">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to determine the maximum element from.</param>
	/// <param name="comparer">The comparer to compare elements.</param>
	/// <returns>The maximum element in the sequence.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="comparer"/> is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the sequence is empty.</exception>
	public static TItem Max<TItem>(this IEnumerable<TItem> source, IComparer<TItem> comparer)
	{
		if (source == null)		throw new ArgumentNullException(nameof(source));
		if (comparer == null)	throw new ArgumentNullException(nameof(comparer));

		using var enumerator = source.GetEnumerator();

		if (!enumerator.MoveNext())
			throw new InvalidOperationException("Sequence contains no elements.");

		TItem max = enumerator.Current;

		while (enumerator.MoveNext())
		{
			TItem current = enumerator.Current;
			if (comparer.Compare(current, max) > 0)
			{
				max = current;
			}
		}

		return max;
	}
}
#endif