namespace Ecng.Serialization;

interface IDefaultQueryProvider
{
	ValueTask<IQueryable> TryInitBulkLoad(CancellationToken cancellationToken);
}

public class DefaultQueryProvider<TEntity>(IQueryContext context) : IQueryProvider, IDefaultQueryProvider
{
	private readonly IQueryContext _context = context ?? throw new ArgumentNullException(nameof(context));
	private readonly IRelationManyList<TEntity> _list;

	private readonly MethodInfo _execEnum = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnum));
	private readonly MethodInfo _execEnumAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnumAsync));
	private readonly MethodInfo _execAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteAsync));
	private readonly MethodInfo _execResultAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteResultAsync));
	private readonly MethodInfo _execResult = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteResult));

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
		if (typeof(T).GetGenericType(typeof(IEnumerable<>)) is not null)
		{
			return (T)_execEnum.Make(typeof(TEntity), typeof(T).GetGenericArguments().First()).Invoke(_context, [expression]);
		}
		else if (typeof(T).GetGenericType(typeof(IAsyncEnumerable<>)) is not null)
		{
			return (T)_execEnumAsync.Make(typeof(TEntity), typeof(T).GetGenericArguments().First()).Invoke(_context, [expression]);
		}
		else if (typeof(T) == typeof(ValueTask))
		{
			return (T)_execAsync.Make(typeof(TEntity)).Invoke(_context, [expression]);
		}
		else if (typeof(T).GetGenericType(typeof(ValueTask<>)) is not null)
		{
			return (T)_execResultAsync.Make(typeof(TEntity), typeof(T).GetGenericArguments().First()).Invoke(_context, [expression]);
		}
		else
		{
			return (T)_execResult.Make(typeof(TEntity), typeof(T)).Invoke(_context, [expression]);
		}
	}

	async ValueTask<IQueryable> IDefaultQueryProvider.TryInitBulkLoad(CancellationToken cancellationToken)
		=> _list is null ? default : await _list.TryInitBulkLoad(cancellationToken);
}