namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Reflection;

	public class DefaultQueryProvider<TEntity> : IQueryProvider
	{
		private readonly IQueryContext _context;

		private readonly MethodInfo _execEnum;
		private readonly MethodInfo _execEnumAsync;
		private readonly MethodInfo _execAsync;
		private readonly MethodInfo _execResultAsync;

		public DefaultQueryProvider(IQueryContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));

			_execEnum = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnum));
			_execEnumAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnumAsync));
			_execAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteAsync));
			_execResultAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteResultAsync));
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
				return (T)_execEnum.Make(typeof(TEntity)).Invoke(_context, new object[] { expression });
			}
			else if (typeof(T).GetGenericType(typeof(IAsyncEnumerable<>)) is not null)
			{
				return (T)_execEnumAsync.Make(typeof(TEntity)).Invoke(_context, new object[] { expression });
			}
			else if (typeof(T) == typeof(ValueTask))
			{
				return (T)_execAsync.Make(typeof(TEntity)).Invoke(_context, new object[] { expression });
			}
			else if (typeof(T).GetGenericType(typeof(ValueTask<>)) is not null)
			{
				return (T)_execResultAsync.Make(typeof(TEntity), typeof(T).GetGenericArguments().First()).Invoke(_context, new object[] { expression });
			}
			else
				throw new NotSupportedException();
		}
	}
}