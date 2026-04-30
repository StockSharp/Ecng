namespace Ecng.Serialization;

interface IDefaultQueryProvider
{
	ValueTask<IQueryable> TryInitBulkLoad(CancellationToken cancellationToken);
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

	private readonly MethodInfo _execEnum = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnum));
	private readonly MethodInfo _execEnumAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnumAsync));
	private readonly MethodInfo _execAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteAsync));
	private readonly MethodInfo _execResultAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteResultAsync));
	private readonly MethodInfo _execResult = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteResult));

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
		if (resultType.GetGenericType(typeof(IEnumerable<>)) is not null)
			return _execEnum.Make(typeof(TEntity), resultType.GetGenericArguments().First());

		if (resultType.GetGenericType(typeof(IAsyncEnumerable<>)) is not null)
			return _execEnumAsync.Make(typeof(TEntity), resultType.GetGenericArguments().First());

		if (resultType == typeof(ValueTask))
			return _execAsync.Make(typeof(TEntity));

		if (resultType.GetGenericType(typeof(ValueTask<>)) is not null)
			return _execResultAsync.Make(typeof(TEntity), resultType.GetGenericArguments().First());

		return _execResult.Make(typeof(TEntity), resultType);
	}

	async ValueTask<IQueryable> IDefaultQueryProvider.TryInitBulkLoad(CancellationToken cancellationToken)
		=> _list is null ? default : await _list.TryInitBulkLoad(cancellationToken).NoWait();}