namespace Ecng.Serialization;

public static class QueryableAsyncExtensions
{
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

	public static async ValueTask<bool> AnyAsyncEx<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		=> await source.FirstOrDefaultAsyncEx(cancellationToken) is not null;

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

	public static IAsyncEnumerable<T> ToAsync<T>(this IQueryable<T> q)
	{
		if (q is IAsyncEnumerable<T> ae)
			return ae;
		else
			return q.ToAsyncEnumerable();
	}

	public static ValueTask<T[]> ToArrayAsyncEx<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		=> source.ToAsync().ToArrayAsync(cancellationToken);
}