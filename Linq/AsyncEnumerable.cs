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
	/// Gets the first element of the <see cref="IAsyncEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the elements of the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
	/// <param name="enu">The enumeration.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The first element of the <see cref="IAsyncEnumerable{T}"/>.</returns>
	public static async ValueTask<T> FirstAsync<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken)
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
	public static async ValueTask<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken)
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