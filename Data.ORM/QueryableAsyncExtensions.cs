namespace Ecng.Serialization;

/// <summary>
/// Extension methods for asynchronous queryable operations with bulk-load support.
/// </summary>
public static class QueryableAsyncExtensions
{
	// Every source has to be swapped, not just the one the query is composed over: one left naming a table is
	// reached through synchronous enumeration. Null when held copies cannot answer it.
	private static async ValueTask<IQueryable<T>> TryOverBulk<T>(IQueryable<T> source, CancellationToken cancellationToken)
	{
		var pinned = source.Expression.PinSources(out var sources);

		if (sources.Length == 0)
			return null;

		var held = new Dictionary<object, IQueryable>(ReferenceEqualityComparer.Instance);
		IQueryable any = null;

		foreach (var part in sources)
		{
			if (part.Provider is not IDefaultQueryProvider provider)
				return null;

			var bulk = await provider.TryInitBulkLoad(cancellationToken).NoWait();

			if (bulk is null)
				return null;

			held.Add(part, bulk);
			any ??= bulk;
		}

		return (IQueryable<T>)any.Provider.CreateQuery(pinned.ReplaceSources(held));
	}
	// For a query composed by LINQ-to-Objects: the outer provider is not ours, but the tree still names tables,
	// and it was going to read them whole anyway. Reading them here only makes that read awaited.
	private static async ValueTask<IQueryable<T>> OverHeldCopies<T>(IQueryable<T> source, CancellationToken cancellationToken)
	{
		var pinned = source.Expression.PinSources(out var sources);
		var held = new Dictionary<object, IQueryable>(ReferenceEqualityComparer.Instance);

		foreach (var part in sources)
		{
			if (part.Provider is not IDefaultQueryProvider provider)
				continue;

			held.Add(part, await provider.TryInitBulkLoad(cancellationToken).NoWait()
				?? await provider.ReadAllAsync(part, cancellationToken).NoWait());
		}

		if (held.Count == 0)
			return source;

		return (IQueryable<T>)source.Provider.CreateQuery(pinned.ReplaceSources(held));
	}

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

		if (source.Provider is IDefaultQueryProvider)
		{
			var bulk = await TryOverBulk(source, cancellationToken).NoWait();

			if (bulk is null)
				return await source.CountAsync(cancellationToken).NoWait();

			source = bulk;
		}
		else
			source = await OverHeldCopies(source, cancellationToken).NoWait();

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
		// A FirstOrDefault-based check is wrong for value-type T: an empty sequence yields
		// default(T), which is never null, so the result would be true. Counting via the
		// existing bulk-load-aware CountAsyncEx is correct for both reference and value types.
		=> await source.CountAsyncEx(cancellationToken).NoWait() > 0;

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

		if (source.Provider is IDefaultQueryProvider)
		{
			var bulk = await TryOverBulk(source, cancellationToken).NoWait();

			if (bulk is null)
				return await source.FirstOrDefaultAsync(cancellationToken).NoWait();

			source = bulk;
		}
		else
			source = await OverHeldCopies(source, cancellationToken).NoWait();

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
	public static async ValueTask<T[]> ToArrayAsyncEx<T>(this IQueryable<T> source, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(source);

		if (source.Provider is IDefaultQueryProvider)
		{
			var bulk = await TryOverBulk(source, cancellationToken).NoWait();

			if (bulk is null)
				return await source.ToAsync().ToArrayAsync(cancellationToken).NoWait();

			source = bulk;
		}
		else
			source = await OverHeldCopies(source, cancellationToken).NoWait();

		return [.. source];
	}
}