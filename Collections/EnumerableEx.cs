namespace System.Linq;

using System;
using System.Collections.Generic;

public static class EnumerableEx
{
	// https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/DistinctBy.cs

	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector)
		=> source.DistinctBy(keySelector, null);

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