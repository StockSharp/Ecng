namespace Ecng.Linq;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerableExtensions
{
	/// <summary>
	/// Filters a sequence of values based on a predicate that compares each element with its previous element.
	/// The first element is always included in the result.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
	/// <param name="predicate">A function to test each element against its previous element for a condition.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence where the predicate returns true when compared to the previous element.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
	public static IAsyncEnumerable<TSource> WhereWithPrevious<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, TSource, bool> predicate)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (predicate is null)
			throw new ArgumentNullException(nameof(predicate));

		return Impl(source, predicate);

		static async IAsyncEnumerable<TSource> Impl(IAsyncEnumerable<TSource> source, Func<TSource, TSource, bool> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await using var iterator = source.GetAsyncEnumerator(cancellationToken);

			if (!await iterator.MoveNextAsync())
				yield break;

			var previous = iterator.Current;
			yield return previous;

			while (await iterator.MoveNextAsync())
			{
				var current = iterator.Current;

				if (!predicate(previous, current))
					continue;

				yield return current;
				previous = current;
			}
		}
	}

	/// <summary>
	/// Casts the elements of an <see cref="IAsyncEnumerable{T}"/> to the specified type.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TResult">The type to cast the elements of source to.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> that contains the elements to be cast.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains each element of the source sequence cast to the specified type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
	/// <exception cref="InvalidCastException">An element in the sequence cannot be cast to type <typeparamref name="TResult"/>.</exception>
	public static IAsyncEnumerable<TResult> Cast<TSource, TResult>(this IAsyncEnumerable<TSource> source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Impl(source);

		static async IAsyncEnumerable<TResult> Impl(IAsyncEnumerable<TSource> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
				yield return (TResult)(object)item;
		}
	}

	/// <summary>
	/// Converts the elements of an <see cref="IAsyncEnumerable{T}"/> to the specified type using a converter function.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements of source.</typeparam>
	/// <typeparam name="TResult">The type to convert the elements of source to.</typeparam>
	/// <param name="source">The <see cref="IAsyncEnumerable{T}"/> that contains the elements to be converted.</param>
	/// <param name="converter">A function to convert each element from <typeparamref name="TSource"/> to <typeparamref name="TResult"/>.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains each element of the source sequence converted to the specified type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="converter"/> is null.</exception>
	public static IAsyncEnumerable<TResult> Cast<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> converter)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (converter is null)
			throw new ArgumentNullException(nameof(converter));

		return Impl(source, converter);

		static async IAsyncEnumerable<TResult> Impl(IAsyncEnumerable<TSource> source, Func<TSource, TResult> converter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in source.WithCancellation(cancellationToken))
				yield return converter(item);
		}
	}
}
