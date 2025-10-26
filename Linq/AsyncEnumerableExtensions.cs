namespace Ecng.Linq;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// The extensions for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerableExtensions
{
	/// <summary>
	/// Converts the <see cref="IAsyncEnumerable{T}"/> to the array.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="enu">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The array of the elements of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<T[]> ToArrayAsync2<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken)
	{
		if (enu is null)
			throw new ArgumentNullException(nameof(enu));

		var list = new List<T>();

		await foreach (var item in enu.WithEnforcedCancellation(cancellationToken))
			list.Add(item);

		return [.. list];
	}

	/// <summary>
	/// Gets the first element of the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="enu">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The first element of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<T> FirstAsync2<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken)
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
	public static async ValueTask<T> FirstOrDefaultAsync2<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken)
	{
		if (enu is null)
			throw new ArgumentNullException(nameof(enu));

		await foreach (var item in enu.WithEnforcedCancellation(cancellationToken))
			return item;

		return default;
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
	public static async IAsyncEnumerable<IGrouping<TKey, TSource>> GroupByAsync2<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, [EnumeratorCancellation]CancellationToken cancellationToken)
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
}