namespace Ecng.Common
{
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Reflection;

	public static class QueryableExtensions
	{
		public static IQueryable<T> SkipLong<T>(this IQueryable<T> source, long count)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			return source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					GetMethodInfo(SkipLong, source, count),
					new Expression[] { source.Expression, Expression.Constant(count) }
					));
		}

		public static IOrderedQueryable<TSource> OrderByAscending<TSource>(this IQueryable<TSource> source, string propertyName)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (propertyName.IsEmpty())
				throw new ArgumentNullException(nameof(propertyName));

			return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					GetMethodInfo(OrderByAscending, source, propertyName),
					new Expression[] { source.Expression, Expression.Constant(propertyName) }
					));
		}

		public static IOrderedQueryable<TSource> OrderByDescending<TSource>(this IQueryable<TSource> source, string propertyName)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (propertyName.IsEmpty())
				throw new ArgumentNullException(nameof(propertyName));

			return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					GetMethodInfo(OrderByDescending, source, propertyName),
					new Expression[] { source.Expression, Expression.Constant(propertyName) }
					));
		}

		public static Task<long> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			return source.Provider.Execute<Task<long>>(
				Expression.Call(
					null,
					GetMethodInfo(CountAsync, source, cancellationToken),
					new Expression[] { source.Expression, Expression.Constant(cancellationToken) }
					));
		}

		#region Helper methods to obtain MethodInfo in a safe way

#pragma warning disable IDE0051 // Remove unused private members
		public static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused1)
		{
			return f.Method;
		}

		public static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
		{
			return f.Method;
		}

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3)
		{
			return f.Method;
		}

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
		{
			return f.Method;
		}

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
		{
			return f.Method;
		}

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
		{
			return f.Method;
		}
#pragma warning restore IDE0051 // Remove unused private members

		#endregion
	}
}