namespace Ecng.Linq;

using System;
using System.Collections.Generic;
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
}