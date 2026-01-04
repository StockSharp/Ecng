#if NETSTANDARD2_0
namespace System.Linq;

using System.Collections.Generic;

/// <summary>
/// Provides additional extension methods for working with enumerables.
/// Contains compatibility helpers that are not available in .NET Standard 2.0.
/// </summary>
public static class EnumerableEx
{
	/// <summary>
	/// Returns all elements of a sequence except the last specified number of elements.
	/// </summary>
	/// <typeparam name="T">The type of elements in the sequence.</typeparam>
	/// <param name="source">The sequence to process.</param>
	/// <param name="count">The number of elements to skip from the end.</param>
	/// <returns>An enumerable with the last <paramref name="count"/> elements omitted.</returns>
	public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
		=> source.Take(source.Count() - count);

	/// <summary>
	/// Splits a sequence into chunks of the specified size.
	/// Compatible implementation of .NET Core's Enumerable.Chunk.
	/// </summary>
	/// <typeparam name="T">Element type.</typeparam>
	/// <param name="source">Source sequence.</param>
	/// <param name="size">Chunk size (must be &gt; 0).</param>
	/// <returns>Sequence of arrays where each array is up to <paramref name="size"/> elements.</returns>
	public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

		T[] bucket = null;
		var count = 0;

		foreach (var item in source)
		{
			bucket ??= new T[size];
			bucket[count++] = item;

			if (count != size)
				continue;

			yield return bucket;
			bucket = null;
			count = 0;
		}

		if (bucket != null && count > 0)
		{
			if (count != bucket.Length)
				Array.Resize(ref bucket, count);

			yield return bucket;
		}
	}

	/// <summary>
	/// Creates a <see cref="HashSet{TSource}"/> that contains elements from the input sequence.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
	/// <param name="source">The sequence whose elements are used to create the set.</param>
	/// <returns>A new <see cref="HashSet{TSource}"/> that contains elements from the input sequence.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		return new HashSet<TSource>(source);
	}

	/// <summary>
	/// Creates a <see cref="HashSet{TSource}"/> that contains elements from the input sequence and uses the specified equality comparer.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
	/// <param name="source">The sequence whose elements are used to create the set.</param>
	/// <param name="comparer">An <see cref="IEqualityComparer{TSource}"/> to compare values, or null to use the default comparer.</param>
	/// <returns>A new <see cref="HashSet{TSource}"/> that contains elements from the input sequence.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
	{
		if (source == null) throw new ArgumentNullException(nameof(source));
		return new(source, comparer);
	}

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
				min = current;
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
				max = current;
		}

		return max;
	}

	/// <summary>
	/// Converts a sequence of key-value pairs to a dictionary using the default equality comparer.
	/// </summary>
	/// <typeparam name="TKey">The type of keys.</typeparam>
	/// <typeparam name="TValue">The type of values.</typeparam>
	/// <param name="source">The sequence of key-value pairs.</param>
	/// <returns>A dictionary containing the key-value pairs.</returns>
	public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
	{
		return source.ToDictionary(pair => pair.Key, pair => pair.Value);
	}
}
#endif