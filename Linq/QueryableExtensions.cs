namespace Ecng.Linq
{
	using System;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Reflection;

	using Ecng.Common;

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

		public static async ValueTask<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
			=> await source.FirstOrDefaultAsync(cancellationToken) is not null;

		public static ValueTask<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
			=> source.Provider.Execute<ValueTask<T>>(
					Expression.Call(
						null,
						GetMethodInfo(FirstOrDefaultAsync, source, cancellationToken),
						new Expression[]
						{
							source.Expression,
							Expression.Constant(cancellationToken),
						}
				)
			);

		public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName, bool ignoreCase)
			=> ApplyOrder(source, propertyName, ignoreCase, nameof(OrderBy));

		public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName, bool ignoreCase)
			=> ApplyOrder(source, propertyName, ignoreCase, nameof(OrderByDescending));

		public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string propertyName, bool ignoreCase)
			=> ApplyOrder(source, propertyName, ignoreCase, nameof(ThenBy));

		public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string propertyName, bool ignoreCase)
			=> ApplyOrder(source, propertyName, ignoreCase, nameof(ThenByDescending));

		// https://stackoverflow.com/a/233505
		private static IOrderedQueryable<T> ApplyOrder<T>(this IQueryable<T> source, string propertyName, bool ignoreCase, string methodName)
		{
			var type = typeof(T);
			var arg = Expression.Parameter(type, "x");

			var flags = BindingFlags.Public | BindingFlags.Instance;

			if (ignoreCase)
				flags |= BindingFlags.IgnoreCase;

			Expression expr = arg;

			foreach (var prop in propertyName.Split('.'))
			{
				var pi = type.GetProperty(prop, flags);

				if (pi is null)
					throw new InvalidOperationException($"Type '{type}' doesn't contains {prop} property.");

				expr = Expression.Property(expr, pi);
				type = pi.PropertyType;
			}

			var delegateType = typeof(Func<,>).Make(typeof(T), type);
			var lambda = Expression.Lambda(delegateType, expr, arg);

			object result = typeof(Queryable).GetMethods().Single(
					method => method.Name == methodName
							&& method.IsGenericMethodDefinition
							&& method.GetGenericArguments().Length == 2
							&& method.GetParameters().Length == 2)
					.MakeGenericMethod(typeof(T), type)
					.Invoke(null, new object[] { source, lambda });

			return (IOrderedQueryable<T>)result;
		}

		public static ValueTask<long> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			return source.Provider.Execute<ValueTask<long>>(
				Expression.Call(
					null,
					GetMethodInfo(CountAsync, source, cancellationToken),
					new Expression[] { source.Expression, Expression.Constant(cancellationToken) }
					));
		}

		public static ValueTask<T[]> ToArrayAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			return source.Provider.Execute<ValueTask<T[]>>(Expression.Call(null, GetMethodInfo(ToArrayAsync, source, cancellationToken), new Expression[]
			{
				source.Expression,
				Expression.Constant(cancellationToken)
			}));
		}

		#region Helper methods to obtain MethodInfo in a safe way

#pragma warning disable IDE0051 // Remove unused private members
		public static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused1)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1>(Action<T1> f)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2>(Action<T1, T2> f, T1 unused1, T2 unused2)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3>(Action<T1, T2, T3> f, T1 unused1, T2 unused2, T3 unused3)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Action<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
			=> f.Method;

		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
			=> f.Method;
#pragma warning restore IDE0051 // Remove unused private members

		#endregion
	}
}