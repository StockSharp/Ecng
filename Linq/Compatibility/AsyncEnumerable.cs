#if NET10_0_OR_GREATER == false
namespace System.Linq;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Linq;

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
	public static async ValueTask<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken = default)
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
	/// <param name="comparer">An equality comparer to compare values.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>true if the source sequence contains an element that has the specified value; otherwise, false.</returns>
	public static async ValueTask<bool> ContainsAsync<T>(this IAsyncEnumerable<T> source, T value, IEqualityComparer<T>? comparer = default, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		comparer ??= EqualityComparer<T>.Default;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (comparer.Equals(item, value))
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
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements of type <see cref="IGrouping{TKey, TSource}"/></returns>
	[Obsolete("This method assumes that the source is ordered by the key.")]
	public static IAsyncEnumerable<IGrouping<TKey, TSource>> GroupByAsync<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		where TKey : IEquatable<TKey>
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		return Impl(source, keySelector);

		static async IAsyncEnumerable<IGrouping<TKey, TSource>> Impl(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			List<TSource> group = null;
			TKey currentKey = default;

			await foreach (var item in source.WithCancellation(cancellationToken))
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
	}

	/// <summary>
	/// Filters the elements of the <see cref="IAsyncEnumerable{T}"/> based on a predicate.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to filter.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
	public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		return Impl(source, predicate);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, Func<T, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (predicate(item))
					yield return item;
			}
		}
	}

	/// <summary>
	/// Projects each element of the <see cref="IAsyncEnumerable{T}"/> into a new form.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TResult">The type of the value returned by selector.</typeparam>
	/// <param name="source">A sequence of values to invoke a transform function on.</param>
	/// <param name="selector">A transform function to apply to each element.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of invoking the transform function on each element of source.</returns>
	public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		return
			source == EmptyAsyncEnumerable<TResult>.Instance ? Empty<TResult>() :
			Impl(source, selector, default);

		static async IAsyncEnumerable<TResult> Impl(
				IAsyncEnumerable<TSource> source,
				Func<TSource, TResult> selector,
				[EnumeratorCancellation] CancellationToken cancellationToken)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				yield return selector(item);
			}
		}
	}

	/// <summary>
	/// Bypasses a specified number of elements in the <see cref="IAsyncEnumerable{T}"/> and then returns the remaining elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
	/// <param name="count">The number of elements to skip before returning the remaining elements.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements that occur after the specified index in the input sequence.</returns>
	public static IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, int count)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source, count);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var index = 0;

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (index >= count)
					yield return item;

				index++;
			}
		}
	}

	/// <summary>
	/// Returns a specified number of contiguous elements from the start of the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence to return elements from.</param>
	/// <param name="count">The number of elements to return.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the specified number of elements from the start of the input sequence.</returns>
	public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return count <= 0 ? Empty<T>() : Impl(source, count);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var taken = 0;

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				yield return item;

				taken++;
				if (taken >= count)
					break;
			}
		}
	}

	/// <summary>
	/// Returns distinct elements from the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence to remove duplicate elements from.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
	public static IAsyncEnumerable<T> Distinct<T>(this IAsyncEnumerable<T> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var seen = new HashSet<T>();

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (seen.Add(item))
					yield return item;
			}
		}
	}

	/// <summary>
	/// Concatenates two async sequences.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
	/// <param name="first">The first sequence to concatenate.</param>
	/// <param name="second">The sequence to concatenate to the first sequence.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the concatenated elements of the two input sequences.</returns>
	public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));

		return Impl(first, second);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in first.WithCancellation(cancellationToken))
				yield return item;

			await foreach (var item in second.WithCancellation(cancellationToken))
				yield return item;
		}
	}

	/// <summary>
	/// Appends a value to the end of the sequence.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">A sequence of values.</param>
	/// <param name="element">The value to append to source.</param>
	/// <returns>A new sequence that ends with element.</returns>
	public static IAsyncEnumerable<T> Append<T>(this IAsyncEnumerable<T> source, T element)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source, element);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, T element, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
				yield return item;

			yield return element;
		}
	}

	/// <summary>
	/// Adds a value to the beginning of the sequence.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">A sequence of values.</param>
	/// <param name="element">The value to prepend to source.</param>
	/// <returns>A new sequence that begins with element.</returns>
	public static IAsyncEnumerable<T> Prepend<T>(this IAsyncEnumerable<T> source, T element)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source, element);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, T element, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			yield return element;

			await foreach (var item in source.WithCancellation(cancellationToken))
				yield return item;
		}
	}

	/// <summary>
	/// Inverts the order of the elements in the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">A sequence of values to reverse.</param>
	/// <returns>A sequence whose elements correspond to those of the input sequence in reverse order.</returns>
	public static IAsyncEnumerable<T> Reverse<T>(this IAsyncEnumerable<T> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var list = new List<T>();

			await foreach (var item in source.WithCancellation(cancellationToken))
				list.Add(item);

			for (var i = list.Count - 1; i >= 0; i--)
				yield return list[i];
		}
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
			TSource[] array => array.Length == 0 ? Empty<TSource>() : new SyncAsyncEnumerable<TSource>(array),
			List<TSource> list => new SyncAsyncEnumerable<TSource>(list),
			IList<TSource> list => new SyncAsyncEnumerable<TSource>(list),
			_ when source == Enumerable.Empty<TSource>() => Empty<TSource>(),
			_ => new SyncAsyncEnumerable<TSource>(source),
		};
	}

	/// <summary>
	/// Gets the first element of the <see cref="IAsyncEnumerable{T}"/> that satisfies a condition.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The first element that satisfies the condition.</returns>
	public static async ValueTask<T> FirstAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
				return item;
		}

		throw new InvalidOperationException("Sequence contains no matching element");
	}

	/// <summary>
	/// Gets the first element of the <see cref="IAsyncEnumerable{T}"/> that satisfies a condition or default.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The first element that satisfies the condition or default.</returns>
	public static async ValueTask<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
				return item;
		}

		return default;
	}

	/// <summary>
	/// Gets the last element of the <see cref="IAsyncEnumerable{T}"/> that satisfies a condition.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The last element that satisfies the condition.</returns>
	public static async ValueTask<T> LastAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		T last = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
			{
				last = item;
				found = true;
			}
		}

		if (!found)
			throw new InvalidOperationException("Sequence contains no matching element");

		return last;
	}

	/// <summary>
	/// Gets the last element of the <see cref="IAsyncEnumerable{T}"/> that satisfies a condition or default.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The last element that satisfies the condition or default.</returns>
	public static async ValueTask<T> LastOrDefaultAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		T last = default;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
				last = item;
		}

		return last;
	}

	/// <summary>
	/// Gets the only element of the <see cref="IAsyncEnumerable{T}"/> that satisfies a condition.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The single element that satisfies the condition.</returns>
	public static async ValueTask<T> SingleAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		T result = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
			{
				if (found)
					throw new InvalidOperationException("Sequence contains more than one matching element");

				result = item;
				found = true;
			}
		}

		if (!found)
			throw new InvalidOperationException("Sequence contains no matching element");

		return result;
	}

	/// <summary>
	/// Gets the only element of the <see cref="IAsyncEnumerable{T}"/> that satisfies a condition or default.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="source">The enumeration.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The single element that satisfies the condition or default.</returns>
	public static async ValueTask<T> SingleOrDefaultAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		T result = default;
		var found = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
			{
				if (found)
					throw new InvalidOperationException("Sequence contains more than one matching element");

				result = item;
				found = true;
			}
		}

		return result;
	}

	/// <summary>
	/// Returns the number of elements in the <see cref="IAsyncEnumerable{T}"/> that satisfy a condition.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> that contains the elements to be counted.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The number of elements that satisfy the condition.</returns>
	public static async ValueTask<int> CountAsync<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		var count = 0;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (predicate(item))
				count++;
		}

		return count;
	}

	/// <summary>
	/// Projects each element of an async sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one async sequence.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
	/// <param name="source">A sequence of values to project.</param>
	/// <param name="selector">A transform function to apply to each element.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
	public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		return Impl(source, selector);

		static async IAsyncEnumerable<TResult> Impl(IAsyncEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				foreach (var result in selector(item))
					yield return result;
			}
		}
	}

	/// <summary>
	/// Projects each element of an async sequence to an <see cref="IAsyncEnumerable{T}"/> and flattens the resulting sequences into one async sequence.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
	/// <param name="source">A sequence of values to project.</param>
	/// <param name="selector">A transform function to apply to each element.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
	public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (selector is null)
			throw new ArgumentNullException(nameof(selector));

		return Impl(source, selector);

		static async IAsyncEnumerable<TResult> Impl(IAsyncEnumerable<TSource> source, Func<TSource, IAsyncEnumerable<TResult>> selector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				await foreach (var result in selector(item).WithCancellation(cancellationToken))
					yield return result;
			}
		}
	}

	/// <summary>
	/// Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by predicate.</returns>
	public static IAsyncEnumerable<T> SkipWhile<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		return Impl(source, predicate);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, Func<T, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var yielding = false;

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (!yielding && !predicate(item))
					yielding = true;

				if (yielding)
					yield return item;
			}
		}
	}

	/// <summary>
	/// Returns elements from a sequence as long as a specified condition is true.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">A sequence to return elements from.</param>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
	public static IAsyncEnumerable<T> TakeWhile<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		return Impl(source, predicate);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, Func<T, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (!predicate(item))
					yield break;

				yield return item;
			}
		}
	}

	/// <summary>
	/// Returns distinct elements from a sequence according to a specified key selector function.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
	/// <param name="source">The sequence to remove duplicate elements from.</param>
	/// <param name="keySelector">A function to extract the key for each element.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
	public static IAsyncEnumerable<TSource> DistinctBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		return Impl(source, keySelector);

		static async IAsyncEnumerable<TSource> Impl(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var seen = new HashSet<TKey>();

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (seen.Add(keySelector(item)))
					yield return item;
			}
		}
	}

	/// <summary>
	/// Sorts the elements of a sequence in ascending order according to a key.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
	/// <param name="source">A sequence of values to order.</param>
	/// <param name="keySelector">A function to extract a key from an element.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> whose elements are sorted according to a key.</returns>
	public static IAsyncEnumerable<TSource> OrderBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		return Impl(source, keySelector);

		static async IAsyncEnumerable<TSource> Impl(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var list = await source.ToListAsync(cancellationToken);
			list.Sort((x, y) => Comparer<TKey>.Default.Compare(keySelector(x), keySelector(y)));

			foreach (var item in list)
				yield return item;
		}
	}

	/// <summary>
	/// Sorts the elements of a sequence in descending order according to a key.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
	/// <param name="source">A sequence of values to order.</param>
	/// <param name="keySelector">A function to extract a key from an element.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> whose elements are sorted in descending order according to a key.</returns>
	public static IAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (keySelector is null)
			throw new ArgumentNullException(nameof(keySelector));

		return Impl(source, keySelector);

		static async IAsyncEnumerable<TSource> Impl(IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var list = await source.ToListAsync(cancellationToken);
			list.Sort((x, y) => Comparer<TKey>.Default.Compare(keySelector(y), keySelector(x)));

			foreach (var item in list)
				yield return item;
		}
	}

	/// <summary>
	/// Filters the elements of an <see cref="IAsyncEnumerable{T}"/> based on a specified type.
	/// </summary>
	/// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> whose elements to filter.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence of type <typeparamref name="TResult"/>.</returns>
	public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source);

		static async IAsyncEnumerable<TResult> Impl(IAsyncEnumerable<object> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				if (item is TResult result)
					yield return result;
			}
		}
	}

	/// <summary>
	/// Casts the elements of an <see cref="IAsyncEnumerable{T}"/> to the specified type.
	/// </summary>
	/// <typeparam name="TResult">The type to cast the elements of source to.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> that contains the elements to be cast.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains each element of the source sequence cast to the specified type.</returns>
	public static IAsyncEnumerable<TResult> Cast<TResult>(this IAsyncEnumerable<object> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source);

		static async IAsyncEnumerable<TResult> Impl(IAsyncEnumerable<object> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
				yield return (TResult)item;
		}
	}

	/// <summary>
	/// Returns the elements of the specified sequence or the specified value in a singleton collection if the sequence is empty.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence to return the specified value for if it is empty.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains defaultValue if source is empty; otherwise, source.</returns>
	public static IAsyncEnumerable<T> DefaultIfEmpty<T>(this IAsyncEnumerable<T> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var hasElements = false;

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				hasElements = true;
				yield return item;
			}

			if (!hasElements)
				yield return default;
		}
	}

	/// <summary>
	/// Returns the elements of the specified sequence or the specified value if the sequence is empty.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">The sequence to return the specified value for if it is empty.</param>
	/// <param name="defaultValue">The value to return if the sequence is empty.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains defaultValue if source is empty; otherwise, source.</returns>
	public static IAsyncEnumerable<T> DefaultIfEmpty<T>(this IAsyncEnumerable<T> source, T defaultValue)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source, defaultValue);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> source, T defaultValue, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var hasElements = false;

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				hasElements = true;
				yield return item;
			}

			if (!hasElements)
				yield return defaultValue;
		}
	}

	/// <summary>
	/// Splits the elements of a sequence into chunks of size at most <paramref name="size"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="source">An <see cref="IAsyncEnumerable{T}"/> whose elements to chunk.</param>
	/// <param name="size">Maximum size of each chunk.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.</returns>
	public static IAsyncEnumerable<T[]> Chunk<T>(this IAsyncEnumerable<T> source, int size)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (size <= 0)
			throw new ArgumentOutOfRangeException(nameof(size));

		return Impl(source, size);

		static async IAsyncEnumerable<T[]> Impl(IAsyncEnumerable<T> source, int size, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var chunk = new List<T>(size);

			await foreach (var item in source.WithCancellation(cancellationToken))
			{
				chunk.Add(item);

				if (chunk.Count == size)
				{
					yield return [.. chunk];
					chunk.Clear();
				}
			}

			if (chunk.Count > 0)
				yield return [.. chunk];
		}
	}

	/// <summary>
	/// Applies an accumulator function over a sequence.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
	/// <param name="func">An accumulator function to be invoked on each element.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The final accumulator value.</returns>
	public static async ValueTask<TSource> AggregateAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, TSource> func, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		TSource result = default;
		var hasValue = false;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
		{
			if (!hasValue)
			{
				result = item;
				hasValue = true;
			}
			else
			{
				result = func(result, item);
			}
		}

		if (!hasValue)
			throw new InvalidOperationException("Sequence contains no elements");

		return result;
	}

	/// <summary>
	/// Applies an accumulator function over a sequence with a seed value.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
	/// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
	/// <param name="seed">The initial accumulator value.</param>
	/// <param name="func">An accumulator function to be invoked on each element.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The final accumulator value.</returns>
	public static async ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(this IAsyncEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		var result = seed;

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken))
			result = func(result, item);

		return result;
	}

	/// <summary>
	/// Determines whether two sequences are equal by comparing their elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
	/// <param name="first">An <see cref="IAsyncEnumerable{T}"/> to compare to second.</param>
	/// <param name="second">An <see cref="IAsyncEnumerable{T}"/> to compare to the first sequence.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>true if the two source sequences are of equal length and their corresponding elements compare equal; otherwise, false.</returns>
	public static async ValueTask<bool> SequenceEqualAsync<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, CancellationToken cancellationToken = default)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));

		var comparer = EqualityComparer<T>.Default;

		await using var e1 = first.GetAsyncEnumerator(cancellationToken);
		await using var e2 = second.GetAsyncEnumerator(cancellationToken);

		while (await e1.MoveNextAsync())
		{
			if (!await e2.MoveNextAsync() || !comparer.Equals(e1.Current, e2.Current))
				return false;
		}

		return !await e2.MoveNextAsync();
	}

	/// <summary>
	/// Produces the set union of two sequences.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
	/// <param name="first">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements form the first set for the union.</param>
	/// <param name="second">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements form the second set for the union.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements from both input sequences, excluding duplicates.</returns>
	public static IAsyncEnumerable<T> Union<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));

		return Impl(first, second);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var seen = new HashSet<T>();

			await foreach (var item in first.WithCancellation(cancellationToken))
			{
				if (seen.Add(item))
					yield return item;
			}

			await foreach (var item in second.WithCancellation(cancellationToken))
			{
				if (seen.Add(item))
					yield return item;
			}
		}
	}

	/// <summary>
	/// Produces the set intersection of two sequences.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
	/// <param name="first">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements that also appear in second will be returned.</param>
	/// <param name="second">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements that also appear in the first sequence will be returned.</param>
	/// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
	public static IAsyncEnumerable<T> Intersect<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));

		return Impl(first, second);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var secondSet = await second.ToHashSetAsync(cancellationToken);
			var seen = new HashSet<T>();

			await foreach (var item in first.WithCancellation(cancellationToken))
			{
				if (secondSet.Contains(item) && seen.Add(item))
					yield return item;
			}
		}
	}

	/// <summary>
	/// Produces the set difference of two sequences.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
	/// <param name="first">An <see cref="IAsyncEnumerable{T}"/> whose elements that are not also in second will be returned.</param>
	/// <param name="second">An <see cref="IAsyncEnumerable{T}"/> whose elements that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
	/// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
	public static IAsyncEnumerable<T> Except<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));

		return Impl(first, second);

		static async IAsyncEnumerable<T> Impl(IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var secondSet = await second.ToHashSetAsync(cancellationToken);
			var seen = new HashSet<T>();

			await foreach (var item in first.WithCancellation(cancellationToken))
			{
				if (!secondSet.Contains(item) && seen.Add(item))
					yield return item;
			}
		}
	}

	/// <summary>
	/// Merges two sequences by using the specified predicate function.
	/// </summary>
	/// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
	/// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
	/// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
	/// <param name="first">The first sequence to merge.</param>
	/// <param name="second">The second sequence to merge.</param>
	/// <param name="resultSelector">A function that specifies how to merge the elements from the two sequences.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains merged elements of two input sequences.</returns>
	public static IAsyncEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IAsyncEnumerable<TFirst> first, IAsyncEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));
		if (resultSelector is null)
			throw new ArgumentNullException(nameof(resultSelector));

		return Impl(first, second, resultSelector);

		static async IAsyncEnumerable<TResult> Impl(IAsyncEnumerable<TFirst> first, IAsyncEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await using var e1 = first.GetAsyncEnumerator(cancellationToken);
			await using var e2 = second.GetAsyncEnumerator(cancellationToken);

			while (await e1.MoveNextAsync() && await e2.MoveNextAsync())
				yield return resultSelector(e1.Current, e2.Current);
		}
	}

	/// <summary>
	/// Produces a sequence of tuples with elements from the two specified sequences.
	/// </summary>
	/// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
	/// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
	/// <param name="first">The first sequence to merge.</param>
	/// <param name="second">The second sequence to merge.</param>
	/// <returns>A sequence of tuples with elements taken from the first and second sequences, in that order.</returns>
	public static IAsyncEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this IAsyncEnumerable<TFirst> first, IAsyncEnumerable<TSecond> second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));
		if (second is null)
			throw new ArgumentNullException(nameof(second));

		return Impl(first, second);

		static async IAsyncEnumerable<(TFirst First, TSecond Second)> Impl(IAsyncEnumerable<TFirst> first, IAsyncEnumerable<TSecond> second, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await using var e1 = first.GetAsyncEnumerator(cancellationToken);
			await using var e2 = second.GetAsyncEnumerator(cancellationToken);

			while (await e1.MoveNextAsync() && await e2.MoveNextAsync())
				yield return (e1.Current, e2.Current);
		}
	}

	/// <summary>
	/// Generates a sequence of integral numbers within a specified range.
	/// </summary>
	/// <param name="start">The value of the first integer in the sequence.</param>
	/// <param name="count">The number of sequential integers to generate.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains a range of sequential integral numbers.</returns>
	public static async IAsyncEnumerable<int> Range(int start, int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		for (var i = 0; i < count; i++)
			yield return start + i;

		await Task.CompletedTask;
	}

	/// <summary>
	/// Generates a sequence that contains one repeated value.
	/// </summary>
	/// <typeparam name="T">The type of the value to be repeated in the result sequence.</typeparam>
	/// <param name="element">The value to be repeated.</param>
	/// <param name="count">The number of times to repeat the value in the generated sequence.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains a repeated value.</returns>
	public static async IAsyncEnumerable<T> Repeat<T>(T element, int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));

		for (var i = 0; i < count; i++)
			yield return element;

		await Task.CompletedTask;
	}
}
#endif
