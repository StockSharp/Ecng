#if !NET7_0_OR_GREATER
namespace System.Linq;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Provides ToBlockingEnumerable extension method for <see cref="IAsyncEnumerable{T}"/>.
/// Compatibility implementation for target frameworks below .NET 7.
/// </summary>
public static class AsyncEnumerableCompat
{
	/// <summary>
	/// Converts an <see cref="IAsyncEnumerable{T}"/> to a blocking <see cref="IEnumerable{T}"/>.
	/// </summary>
	/// <typeparam name="TSource">The type of the elements.</typeparam>
	/// <param name="source">The source async enumerable.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>An <see cref="IEnumerable{TSource}"/> that yields items from the async source sequence.</returns>
	/// <remarks>
	/// This method blocks the calling thread for each element. Use with caution in UI or async contexts.
	/// Unlike ToArrayAsync/ToListAsync, this does not buffer all items - elements are fetched lazily.
	/// </remarks>
	public static IEnumerable<TSource> ToBlockingEnumerable<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return Iterator(source, cancellationToken);

		static IEnumerable<TSource> Iterator(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
		{
			var enumerator = source.GetAsyncEnumerator(cancellationToken);
			try
			{
				while (AsyncHelper.Run(() => enumerator.MoveNextAsync()))
				{
					yield return enumerator.Current;
				}
			}
			finally
			{
				AsyncHelper.Run(() => enumerator.DisposeAsync());
			}
		}
	}
}
#endif
