namespace Ecng.Serialization;

/// <summary>
/// Extension methods for asynchronous queryable operations with bulk-load support.
/// </summary>
public static class QueryableAsyncExtensions
{
	/// <summary>
	/// Asynchronously counts the elements in a queryable sequence, using bulk-load when available.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="source">The queryable source.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The number of elements.</returns>
	public static async ValueTask<long> CountAsyncEx<T>(this IQueryable<T> source, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(source);

		if (source.Provider is IDefaultQueryProvider defProvider)
		{
			var bulkSource = await defProvider.TryInitBulkLoad(cancellationToken);

			if (bulkSource is null)
				return await source.CountAsync(cancellationToken);

			((DefaultQueryable<T>)source).ReplaceProvider(bulkSource.Provider);
		}

		return source.Count();
	}

	/// <summary>
	/// Asynchronously determines whether a queryable sequence contains any elements.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="source">The queryable source.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><see langword="true"/> if the sequence contains any elements; otherwise, <see langword="false"/>.</returns>
	public static async ValueTask<bool> AnyAsyncEx<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		=> await source.FirstOrDefaultAsyncEx(cancellationToken) is not null;

	/// <summary>
	/// Asynchronously returns the first element of a sequence, or a default value, using bulk-load when available.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="source">The queryable source.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The first element, or the default value if the sequence is empty.</returns>
	public static async ValueTask<T> FirstOrDefaultAsyncEx<T>(this IQueryable<T> source, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(source);

		if (source.Provider is IDefaultQueryProvider defProvider)
		{
			var bulkSource = await defProvider.TryInitBulkLoad(cancellationToken);

			if (bulkSource is null)
				return await source.FirstOrDefaultAsync(cancellationToken);

			((DefaultQueryable<T>)source).ReplaceProvider(bulkSource.Provider);
		}

		return source.FirstOrDefault();
	}

	/// <summary>
	/// Converts a queryable to an async enumerable, returning the existing one if already async.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="q">The queryable source.</param>
	/// <returns>An async enumerable over the elements.</returns>
	public static IAsyncEnumerable<T> ToAsync<T>(this IQueryable<T> q)
	{
		if (q is IAsyncEnumerable<T> ae)
			return ae;
		else
			return q.ToAsyncEnumerable();
	}

	/// <summary>
	/// Asynchronously converts a queryable sequence to an array.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="source">The queryable source.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>An array containing the elements.</returns>
	public static ValueTask<T[]> ToArrayAsyncEx<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		=> source.ToAsync().ToArrayAsync(cancellationToken);
}