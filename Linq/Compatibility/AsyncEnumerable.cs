#if NET10_0_OR_GREATER == false
namespace System.Linq;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// The extensions for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerable
{
	/// <summary>
	/// Converts the <see cref="IAsyncEnumerable{T}"/> to the array.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="enu">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The array of the elements of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken)
	{
		if (enu is null)
			throw new ArgumentNullException(nameof(enu));

		var list = new List<T>();

		await foreach (var item in enu.WithEnforcedCancellation(cancellationToken))
			list.Add(item);

		return [.. list];
	}

	/// <summary>
	/// Converts the <see cref="IAsyncEnumerable{T}"/> to the <see cref="List{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="enu">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The <see cref="List{T}"/> of the elements of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken = default)
	{
		if (enu is null)
			throw new ArgumentNullException(nameof(enu));

		var list = new List<T>();

		await foreach (var item in enu.WithEnforcedCancellation(cancellationToken))
			list.Add(item);

		return list;
	}

	/// <summary>
	/// Converts the <see cref="IAsyncEnumerable{T}"/> to the <see cref="Dictionary{TKey, TValue}"/>.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <param name="source">The source enumeration.</param>
	/// <param name="keySelector">A function to extract the key for each element.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The <see cref="Dictionary{TKey, TValue}"/> of the elements.</returns>
	public static async ValueTask<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default)
		where TKey : notnull
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		var dict = new Dictionary<TKey, TSource>();

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			dict.Add(keySelector(item), item);

		return dict;
	}

	/// <summary>
	/// Converts the <see cref="IAsyncEnumerable{T}"/> to the <see cref="Dictionary{TKey, TValue}"/>.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TElement">The type of the value.</typeparam>
	/// <param name="source">The source enumeration.</param>
	/// <param name="keySelector">A function to extract the key for each element.</param>
	/// <param name="elementSelector">A function to extract the value for each element.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The <see cref="Dictionary{TKey, TValue}"/> of the elements.</returns>
	public static async ValueTask<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken = default)
		where TKey : notnull
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));
		if (elementSelector is null)
			throw new ArgumentNullException(nameof(elementSelector));

		var dict = new Dictionary<TKey, TElement>();

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			dict.Add(keySelector(item), elementSelector(item));

		return dict;
	}

	/// <summary>
	/// Converts the <see cref="IAsyncEnumerable{T}"/> to the <see cref="HashSet{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The source enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The <see cref="HashSet{T}"/> of the elements.</returns>
	public static async ValueTask<HashSet<T>> ToHashSetAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var set = new HashSet<T>();

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			set.Add(item);

		return set;
	}

	/// <summary>
	/// Gets the first element of the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="enu">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The first element of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<T> FirstAsync<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken = default)
	{
		if (enu is null)
			throw new ArgumentNullException(nameof(enu));

		await foreach (var item in enu.WithEnforcedCancellation(cancellationToken))
			return item;

		throw new InvalidOperationException();
	}

	/// <summary>
	/// Gets the first element of the <see cref="IAsyncEnumerable{T}"/> or the default value if the enumeration is empty.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="enu">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The first element of the <see cref="IAsyncEnumerable{T}"/> or the default value if the enumeration is empty.</returns>
	public static async ValueTask<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken = default)
	{
		if (enu is null)
			throw new ArgumentNullException(nameof(enu));

		await foreach (var item in enu.WithEnforcedCancellation(cancellationToken))
			return item;

		return default;
	}

	/// <summary>
	/// Gets the last element of the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The last element of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<T> LastAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		T last = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			last = item;
			found = true;
		}

		if (!found)
			throw new InvalidOperationException();

		return last;
	}

	/// <summary>
	/// Gets the last element of the <see cref="IAsyncEnumerable{T}"/> or the default value if the enumeration is empty.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The last element of the <see cref="IAsyncEnumerable{T}"/> or the default value if the enumeration is empty.</returns>
	public static async ValueTask<T> LastOrDefaultAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		T last = default;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			last = item;

		return last;
	}

	/// <summary>
	/// Gets the only element of the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The single element of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<T> SingleAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		T result = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (found)
				throw new InvalidOperationException("Sequence contains more than one element");

			result = item;
			found = true;
		}

		if (!found)
			throw new InvalidOperationException("Sequence contains no elements");

		return result;
	}

	/// <summary>
	/// Gets the only element of the <see cref="IAsyncEnumerable{T}"/> or the default value if the enumeration is empty.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The single element of the <see cref="IAsyncEnumerable{T}"/> or the default value.</returns>
	public static async ValueTask<T> SingleOrDefaultAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		T result = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (found)
				throw new InvalidOperationException("Sequence contains more than one element");

			result = item;
			found = true;
		}

		return result;
	}

	/// <summary>
	/// Gets the element at the specified index in the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="index">The zero-based index of the element to retrieve.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The element at the specified position in the source sequence.</returns>
	public static async ValueTask<T> ElementAtAsync<T>(this IAsyncEnumerable<T> source, int index, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (index < 0)
			throw new ArgumentOutOfRangeException(nameof(index));

		var currentIndex = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (currentIndex == index)
				return item;

			currentIndex++;
		}

		throw new ArgumentOutOfRangeException(nameof(index));
	}

	/// <summary>
	/// Gets the element at the specified index in the <see cref="IAsyncEnumerable{T}"/> or the default value if the index is out of range.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="index">The zero-based index of the element to retrieve.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The element at the specified position or default value.</returns>
	public static async ValueTask<T> ElementAtOrDefaultAsync<T>(this IAsyncEnumerable<T> source, int index, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (index < 0)
			return default;

		var currentIndex = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (currentIndex == index)
				return item;

			currentIndex++;
		}

		return default;
	}

	/// <summary>
	/// Determines whether any element of the <see cref="IAsyncEnumerable{T}"/> exists.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to check for emptiness.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>true if the source sequence contains any elements; otherwise, false.</returns>
	public static async ValueTask<bool> AnyAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		await foreach (var _ in source.WithEnforcedCancellation(cancellationToken))
			return true;

		return false;
	}

	/// <summary>
	/// Determines whether any element of the <see cref="IAsyncEnumerable{T}"/> satisfies a condition.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> whose elements to apply the predicate to.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>true if any elements in the source sequence pass the test in the specified predicate; otherwise, false.</returns>
	public static async ValueTask<bool> AnyAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Determines whether all elements of the <see cref="IAsyncEnumerable{T}"/> satisfy a condition.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> that contains the elements to apply the predicate to.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>true if every element of the source sequence passes the test in the specified predicate, or if the sequence is empty; otherwise, false.</returns>
	public static async ValueTask<bool> AllAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (!predicate(item))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Determines whether the <see cref="IAsyncEnumerable{T}"/> contains a specified element.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> in which to locate a value.</param>
	/// <param name="value">The value to locate in the sequence.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>true if the source sequence contains an element that has the specified value; otherwise, false.</returns>
	public static async ValueTask<bool> ContainsAsync<T>(this IAsyncEnumerable<T> source, T value, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (EqualityComparer<T>.Default.Equals(item, value))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Returns the number of elements in the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> that contains the elements to be counted.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The number of elements in the input sequence.</returns>
	public static async ValueTask<int> CountAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var count = 0;

		await foreach (var _ in source.WithEnforcedCancellation(cancellationToken))
			count++;

		return count;
	}

	/// <summary>
	/// Returns an <see cref="long"/> that represents the total number of elements in the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> that contains the elements to be counted.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The number of elements in the source sequence.</returns>
	public static async ValueTask<long> LongCountAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		long count = 0;

		await foreach (var _ in source.WithEnforcedCancellation(cancellationToken))
			count++;

		return count;
	}

	/// <summary>
	/// Computes the sum of the sequence of <see cref="int"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the sum of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The sum of the values in the sequence.</returns>
	public static async ValueTask<int> SumAsync(this IAsyncEnumerable<int> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var sum = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			sum += item;

		return sum;
	}

	/// <summary>
	/// Computes the sum of the sequence of <see cref="long"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the sum of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The sum of the values in the sequence.</returns>
	public static async ValueTask<long> SumAsync(this IAsyncEnumerable<long> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		long sum = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			sum += item;

		return sum;
	}

	/// <summary>
	/// Computes the sum of the sequence of <see cref="decimal"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the sum of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The sum of the values in the sequence.</returns>
	public static async ValueTask<decimal> SumAsync(this IAsyncEnumerable<decimal> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		decimal sum = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			sum += item;

		return sum;
	}

	/// <summary>
	/// Computes the sum of the sequence of <see cref="double"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the sum of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The sum of the values in the sequence.</returns>
	public static async ValueTask<double> SumAsync(this IAsyncEnumerable<double> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		double sum = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			sum += item;

		return sum;
	}

	/// <summary>
	/// Computes the average of the sequence of <see cref="int"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the average of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The average of the values in the sequence.</returns>
	public static async ValueTask<double> AverageAsync(this IAsyncEnumerable<int> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		long sum = 0;
		long count = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			sum += item;
			count++;
		}

		if (count == 0)
			throw new InvalidOperationException("Sequence contains no elements");

		return (double)sum / count;
	}

	/// <summary>
	/// Computes the average of the sequence of <see cref="long"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the average of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The average of the values in the sequence.</returns>
	public static async ValueTask<double> AverageAsync(this IAsyncEnumerable<long> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		long sum = 0;
		long count = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			sum += item;
			count++;
		}

		if (count == 0)
			throw new InvalidOperationException("Sequence contains no elements");

		return (double)sum / count;
	}

	/// <summary>
	/// Computes the average of the sequence of <see cref="decimal"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the average of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The average of the values in the sequence.</returns>
	public static async ValueTask<decimal> AverageAsync(this IAsyncEnumerable<decimal> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		decimal sum = 0;
		long count = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			sum += item;
			count++;
		}

		if (count == 0)
			throw new InvalidOperationException("Sequence contains no elements");

		return sum / count;
	}

	/// <summary>
	/// Computes the average of the sequence of <see cref="double"/> values.
	/// </summary>
	/// <param name="source">The sequence of values to calculate the average of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The average of the values in the sequence.</returns>
	public static async ValueTask<double> AverageAsync(this IAsyncEnumerable<double> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		double sum = 0;
		long count = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			sum += item;
			count++;
		}

		if (count == 0)
			throw new InvalidOperationException("Sequence contains no elements");

		return sum / count;
	}

	/// <summary>
	/// Returns the minimum value in the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence of values to determine the minimum value of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The minimum value in the sequence.</returns>
	public static async ValueTask<T> MinAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var comparer = Comparer<T>.Default;
		T min = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (!found || comparer.Compare(item, min) < 0)
			{
				min = item;
				found = true;
			}
		}

		if (!found)
			throw new InvalidOperationException("Sequence contains no elements");

		return min;
	}

	/// <summary>
	/// Returns the maximum value in the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence of values to determine the maximum value of.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The maximum value in the sequence.</returns>
	public static async ValueTask<T> MaxAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var comparer = Comparer<T>.Default;
		T max = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (!found || comparer.Compare(item, max) > 0)
			{
				max = item;
				found = true;
			}
		}

		if (!found)
			throw new InvalidOperationException("Sequence contains no elements");

		return max;
	}

	private class Grouping<TKey, TSource>(TKey key, IEnumerable<TSource> source) : IGrouping<TKey, TSource>
	{
		public TKey Key => key;
		public IEnumerator<TSource> GetEnumerator() => source.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Groups the elements of an <see cref="IAsyncEnumerable{T}"/> according to a specified key selector function.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of the source sequence.</typeparam>
	/// <typeparam name="TKey">The type of the key returned by the key selector function.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to group.</param>
	/// <param name="keySelector">A function to extract the key for each element.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements of type <see cref="IGrouping{TKey, TSource}"/></returns>
	[Obsolete("This method assumes that the source is ordered by the key.")]
	public static async IAsyncEnumerable<IGrouping<TKey, TSource>> GroupByAsync<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, [EnumeratorCancellation]CancellationToken cancellationToken)
		where TKey : IEquatable<TKey>
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		List<TSource> group = null;
		TKey currentKey = default;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			var key = keySelector(item);

			if (group == null)
			{
				group = [item];
				currentKey = key;
			}
			else if (currentKey.Equals(key))
			{
				group.Add(item);
			}
			else
			{
				yield return new Grouping<TKey, TSource>(currentKey, group);

				group = [item];
				currentKey = key;
			}
		}

		if (group != null)
			yield return new Grouping<TKey, TSource>(currentKey, group);
	}

	/// <summary>
	/// Filters the elements of the <see cref="IAsyncEnumerable{T}"/> based on a predicate.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to filter.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
	public static async IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
				yield return item;
		}
	}

	/// <summary>
	/// Projects each element of the <see cref="IAsyncEnumerable{T}"/> into a new form.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TResult">The type of the value returned by selector.</typeparam>
	/// <param name="source">A sequence of values to invoke a transform function on.</param>
	/// <param name="selector">A transform function to apply to each element.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of invoking the transform function on each element of source.</returns>
	public static async IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			yield return selector(item);
	}

	/// <summary>
	/// Bypasses a specified number of elements in the <see cref="IAsyncEnumerable{T}"/> and then returns the remaining elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
	/// <param name="count">The number of elements to skip before returning the remaining elements.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements that occur after the specified index in the input sequence.</returns>
	public static async IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, int count, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var index = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (index >= count)
				yield return item;

			index++;
		}
	}

	/// <summary>
	/// Returns a specified number of contiguous elements from the start of the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence to return elements from.</param>
	/// <param name="count">The number of elements to return.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the specified number of elements from the start of the input sequence.</returns>
	public static async IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (count <= 0)
			yield break;

		var taken = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			yield return item;

			taken++;
			if (taken >= count)
				break;
		}
	}

	/// <summary>
	/// Returns distinct elements from the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence to remove duplicate elements from.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
	public static async IAsyncEnumerable<T> Distinct<T>(this IAsyncEnumerable<T> source, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var seen = new HashSet<T>();

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (seen.Add(item))
				yield return item;
		}
	}

	/// <summary>
	/// Concatenates two async sequences.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
	/// <param name="first">The first sequence to concatenate.</param>
	/// <param name="second">The sequence to concatenate to the first sequence.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the concatenated elements of the two input sequences.</returns>
	public static async IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));

		await foreach (var item in first.WithEnforcedCancellation(cancellationToken))
			yield return item;

		await foreach (var item in second.WithEnforcedCancellation(cancellationToken))
			yield return item;
	}

	/// <summary>
	/// Appends a value to the end of the sequence.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">A sequence of values.</param>
	/// <param name="element">The value to append to source.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A new sequence that ends with element.</returns>
	public static async IAsyncEnumerable<T> Append<T>(this IAsyncEnumerable<T> source, T element, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			yield return item;

		yield return element;
	}

	/// <summary>
	/// Adds a value to the beginning of the sequence.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">A sequence of values.</param>
	/// <param name="element">The value to prepend to source.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A new sequence that begins with element.</returns>
	public static async IAsyncEnumerable<T> Prepend<T>(this IAsyncEnumerable<T> source, T element, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		yield return element;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			yield return item;
	}

	/// <summary>
	/// Inverts the order of the elements in the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">A sequence of values to reverse.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A sequence whose elements correspond to those of the input sequence in reverse order.</returns>
	public static async IAsyncEnumerable<T> Reverse<T>(this IAsyncEnumerable<T> source, [EnumeratorCancellation]CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var list = new List<T>();

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			list.Add(item);

		for (var i = list.Count - 1; i >= 0; i--)
			yield return list[i];
	}

	/// <summary>
	/// Returns an empty <see cref="IAsyncEnumerable{T}"/> that has the specified type argument.
	/// </summary>
	/// <typeparam name="TResult">The type of the elements of the sequence.</typeparam>
	/// <returns>An empty <see cref="IAsyncEnumerable{T}"/> whose type argument is <typeparamref name="TResult"/>.</returns>
	public static IAsyncEnumerable<TResult> Empty<TResult>() => EmptyAsyncEnumerable<TResult>.Instance;

	private class EmptyAsyncEnumerable<TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
	{
		public static readonly EmptyAsyncEnumerable<TResult> Instance = new();

		IAsyncEnumerator<TResult> IAsyncEnumerable<TResult>.GetAsyncEnumerator(CancellationToken cancellationToken) => this;

		ValueTask<bool> IAsyncEnumerator<TResult>.MoveNextAsync() => default;
		TResult IAsyncEnumerator<TResult>.Current => default;

		ValueTask IAsyncDisposable.DisposeAsync() => default;
	}

	/// <summary>
	/// Converts a synchronous <see cref="IEnumerable{T}"/> to an asynchronous <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements.</typeparam>
	/// <param name="source">The source enumerable.</param>
	/// <returns>An <see cref="IAsyncEnumerable{TSource}"/> that yields items from the source sequence.</returns>
	public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IEnumerable<TSource> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return source switch
		{
			TSource[] array => array.Length == 0 ? Empty<TSource>() : FromArray(array),
			List<TSource> list => FromList(list),
			IList<TSource> list => FromIList(list),
			_ when source == Enumerable.Empty<TSource>() => Empty<TSource>(),
			_ => FromIterator(source),
		};

		static async IAsyncEnumerable<TSource> FromArray(TSource[] source)
		{
			for (var i = 0; ; i++)
			{
				var localI = i;
				var localSource = source;
				if ((uint)localI >= (uint)localSource.Length)
				{
					break;
				}
				yield return localSource[localI];
			}
		}

		static async IAsyncEnumerable<TSource> FromList(List<TSource> source)
		{
			for (var i = 0; i < source.Count; i++)
			{
				yield return source[i];
			}
		}

		static async IAsyncEnumerable<TSource> FromIList(IList<TSource> source)
		{
			var count = source.Count;
			for (var i = 0; i < count; i++)
			{
				yield return source[i];
			}
		}

		static async IAsyncEnumerable<TSource> FromIterator(IEnumerable<TSource> source)
		{
			foreach (var element in source)
			{
				yield return element;
			}
		}
	}
}
#endif