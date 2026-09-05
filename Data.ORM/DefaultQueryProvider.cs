namespace Ecng.Serialization;

interface IDefaultQueryProvider
{
	ValueTask<IQueryable> TryInitBulkLoad(CancellationToken cancellationToken);

	/// <summary>Reads the whole source into memory without blocking the calling thread.</summary>
	ValueTask<IQueryable> ReadAllAsync(IQueryable source, CancellationToken cancellationToken);
}

/// <summary>
/// Default LINQ query provider for database entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="context">The query execution context.</param>
public class DefaultQueryProvider<TEntity>(IQueryContext context) : IQueryProvider, IDefaultQueryProvider
{
	private readonly IQueryContext _context = context ?? throw new ArgumentNullException(nameof(context));
	private readonly IRelationManyList<TEntity> _list;

	private readonly MethodInfo _execEnumAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnumAsync));
	private readonly MethodInfo _execAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteAsync));
	private readonly MethodInfo _execResultAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteResultAsync));

	// Per-result-type cache for the closed generic IQueryContext.* methods we
	// dispatch into. Without this every Execute<T>() pays for MakeGenericMethod
	// on the hot path.
	private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, MethodInfo> _executeCache = new();

	/// <summary>
	/// Initializes a new instance using a relation-many list as the data source.
	/// </summary>
	/// <param name="list">The relation-many list.</param>
	internal DefaultQueryProvider(IRelationManyList<TEntity> list)
		: this(list.CheckOnNull(nameof(list)).Storage)
	{
		_list = list;
	}

	IQueryable IQueryProvider.CreateQuery(Expression expression)
	{
		try
		{
			return typeof(DefaultQueryable<>)
				.Make(expression.Type)
				.CreateInstance<IQueryable>(this, expression);
		}
		catch (TargetInvocationException e)
		{
			throw e.InnerException;
		}
	}

	IQueryable<T> IQueryProvider.CreateQuery<T>(Expression expression)
		=> new DefaultQueryable<T>(this, expression);

	object IQueryProvider.Execute(Expression expression)
		=> throw new NotSupportedException();

	T IQueryProvider.Execute<T>(Expression expression)
	{
		var closed = _executeCache.GetOrAdd(typeof(T), ResolveExecuteMethod);
		return (T)closed.Invoke(_context, [expression]);
	}

	private MethodInfo ResolveExecuteMethod(Type resultType)
	{
		// A thread waiting for a round-trip is a thread the pool cannot use, and every read has an awaitable
		// form. Asking for a plain sequence or a plain value is asking for the read to happen on this thread.
		if (IsConstructedGeneric(resultType, typeof(IEnumerable<>)))
			throw new NotSupportedException($"A query over {typeof(TEntity).Name} cannot be read on the calling thread. Read it with ToArrayAsyncEx, or walk it with ToAsync.");

		if (IsConstructedGeneric(resultType, typeof(IAsyncEnumerable<>)))
			return _execEnumAsync.Make(typeof(TEntity), resultType.GetGenericArguments()[0]);

		if (resultType == typeof(ValueTask))
			return _execAsync.Make(typeof(TEntity));

		if (IsConstructedGeneric(resultType, typeof(ValueTask<>)))
			return _execResultAsync.Make(typeof(TEntity), resultType.GetGenericArguments()[0]);

		throw new NotSupportedException($"A query over {typeof(TEntity).Name} cannot be answered on the calling thread. Use CountAsyncEx, AnyAsyncEx or FirstOrDefaultAsyncEx.");
	}

	private static bool IsConstructedGeneric(Type type, Type definition)
		=> type.IsGenericType && type.GetGenericTypeDefinition() == definition;

	async ValueTask<IQueryable> IDefaultQueryProvider.TryInitBulkLoad(CancellationToken cancellationToken)
		=> _list is null ? default : await _list.TryInitBulkLoad(cancellationToken).NoWait();

	async ValueTask<IQueryable> IDefaultQueryProvider.ReadAllAsync(IQueryable source, CancellationToken cancellationToken)
		=> (await ((IAsyncEnumerable<TEntity>)source).ToArrayAsync(cancellationToken).NoWait()).AsQueryable();
}