namespace Ecng.Linq;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

using Ecng.Common;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> to support asynchronous operations and dynamic ordering.
/// </summary>
/// <remarks>
/// This class includes methods to perform operations such as SkipLong, AnyAsync, FirstOrDefaultAsync, CountAsync, and ToArrayAsync.
/// Furthermore, it provides dynamic ordering methods to order the results by property names.
/// </remarks>
public static class QueryableExtensions
{
	/// <summary>
	/// Skips the specified number of elements in the source sequence.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">The source queryable sequence.</param>
	/// <param name="count">The number of elements to skip.</param>
	/// <returns>An <see cref="IQueryable{T}"/> that contains the elements that occur after the specified number of elements.</returns>
	public static IQueryable<T> SkipLong<T>(this IQueryable<T> source, long count)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return source.Provider.CreateQuery<T>(
			Expression.Call(
				null,
				GetMethodInfo(SkipLong, source, count),
				[source.Expression, Expression.Constant(count)]
				));
	}

	/// <summary>
	/// Asynchronously determines whether a sequence contains any elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">The source queryable sequence.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask{Boolean}"/> representing the asynchronous operation. The task result contains true if the sequence contains any elements; otherwise, false.</returns>
	public static async ValueTask<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		=> !EqualityComparer<T>.Default.Equals(
			await source.FirstOrDefaultAsync(cancellationToken),
			default
		);

	/// <summary>
	/// Asynchronously returns the first element of a sequence, or a default value if the sequence contains no elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">The source queryable sequence.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation. The task result contains the first element in the sequence or a default value if no element is found.</returns>
	public static ValueTask<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
		=> source.Provider.Execute<ValueTask<T>>(
				Expression.Call(
					null,
					GetMethodInfo(FirstOrDefaultAsync, source, cancellationToken),
					[
						source.Expression,
						Expression.Constant(cancellationToken),
					]
			)
		);

	/// <summary>
	/// Dynamically orders the elements of a sequence in ascending order based on a property name.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">The source queryable sequence.</param>
	/// <param name="propertyName">The property name to order by.</param>
	/// <param name="ignoreCase">If set to true, ignores case during property name comparison.</param>
	/// <returns>An <see cref="IOrderedQueryable{T}"/> whose elements are sorted according to the specified property.</returns>
	public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName, bool ignoreCase)
		=> ApplyOrder(source, propertyName, ignoreCase, nameof(OrderBy));

	/// <summary>
	/// Dynamically orders the elements of a sequence in descending order based on a property name.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">The source queryable sequence.</param>
	/// <param name="propertyName">The property name to order by.</param>
	/// <param name="ignoreCase">If set to true, ignores case during property name comparison.</param>
	/// <returns>An <see cref="IOrderedQueryable{T}"/> whose elements are sorted in descending order according to the specified property.</returns>
	public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName, bool ignoreCase)
		=> ApplyOrder(source, propertyName, ignoreCase, nameof(OrderByDescending));

	/// <summary>
	/// Dynamically performs a subsequent ordering of the elements of a sequence in ascending order based on a property name.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">An ordered queryable sequence.</param>
	/// <param name="propertyName">The property name to order by.</param>
	/// <param name="ignoreCase">If set to true, ignores case during property name comparison.</param>
	/// <returns>An <see cref="IOrderedQueryable{T}"/> whose elements are further sorted according to the specified property.</returns>
	public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string propertyName, bool ignoreCase)
		=> ApplyOrder(source, propertyName, ignoreCase, nameof(ThenBy));

	/// <summary>
	/// Dynamically performs a subsequent ordering of the elements of a sequence in descending order based on a property name.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">An ordered queryable sequence.</param>
	/// <param name="propertyName">The property name to order by.</param>
	/// <param name="ignoreCase">If set to true, ignores case during property name comparison.</param>
	/// <returns>An <see cref="IOrderedQueryable{T}"/> whose elements are further sorted in descending order according to the specified property.</returns>
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
			var pi = type.GetProperty(prop, flags) ??
				throw new InvalidOperationException($"Type '{type}' doesn't contain property '{prop}'.");

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
				.Invoke(null, [source, lambda]);

		return (IOrderedQueryable<T>)result;
	}

	/// <summary>
	/// Asynchronously counts the number of elements in a sequence.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">The source queryable sequence.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask{Long}"/> representing the asynchronous operation. The task result contains the count of elements.</returns>
	public static ValueTask<long> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return source.Provider.Execute<ValueTask<long>>(
			Expression.Call(
				null,
				GetMethodInfo(CountAsync, source, cancellationToken),
				[source.Expression, Expression.Constant(cancellationToken)]
				));
	}

	/// <summary>
	/// Asynchronously creates an array from a sequence.
	/// </summary>
	/// <typeparam name="T">The type of the elements.</typeparam>
	/// <param name="source">The source queryable sequence.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation. The task result contains an array that holds the elements from the sequence.</returns>
	public static ValueTask<T[]> ToArrayAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return source.Provider.Execute<ValueTask<T[]>>(Expression.Call(null, GetMethodInfo(ToArrayAsync, source, cancellationToken),
		[
			source.Expression,
			Expression.Constant(cancellationToken)
		]));
	}

	#region Helper methods to obtain MethodInfo in a safe way

#pragma warning disable IDE0060
	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the delegate parameter.</typeparam>
	/// <typeparam name="T2">The return type of the delegate.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused1)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The return type of the delegate.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The type of the third delegate parameter.</typeparam>
	/// <typeparam name="T4">The return type of the delegate.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <param name="unused3">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The type of the third delegate parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth delegate parameter.</typeparam>
	/// <typeparam name="T5">The return type of the delegate.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <param name="unused3">A parameter used only for type inference.</param>
	/// <param name="unused4">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The type of the third delegate parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth delegate parameter.</typeparam>
	/// <typeparam name="T5">The type of the fifth delegate parameter.</typeparam>
	/// <typeparam name="T6">The return type of the delegate.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <param name="unused3">A parameter used only for type inference.</param>
	/// <param name="unused4">A parameter used only for type inference.</param>
	/// <param name="unused5">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the delegate parameter.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1>(Action<T1> f)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2>(Action<T1, T2> f, T1 unused1, T2 unused2)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The type of the third delegate parameter.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <param name="unused3">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3>(Action<T1, T2, T3> f, T1 unused1, T2 unused2, T3 unused3)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The type of the third delegate parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth delegate parameter.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <param name="unused3">A parameter used only for type inference.</param>
	/// <param name="unused4">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Action<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The type of the third delegate parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth delegate parameter.</typeparam>
	/// <typeparam name="T5">The type of the fifth delegate parameter.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <param name="unused3">A parameter used only for type inference.</param>
	/// <param name="unused4">A parameter used only for type inference.</param>
	/// <param name="unused5">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
		=> f.Method;

	/// <summary>
	/// Retrieves the <see cref="MethodInfo"/> for the specified delegate.
	/// </summary>
	/// <typeparam name="T1">The type of the first delegate parameter.</typeparam>
	/// <typeparam name="T2">The type of the second delegate parameter.</typeparam>
	/// <typeparam name="T3">The type of the third delegate parameter.</typeparam>
	/// <typeparam name="T4">The type of the fourth delegate parameter.</typeparam>
	/// <typeparam name="T5">The type of the fifth delegate parameter.</typeparam>
	/// <typeparam name="T6">The type of the sixth delegate parameter.</typeparam>
	/// <param name="f">The delegate to obtain the method information from.</param>
	/// <param name="unused1">A parameter used only for type inference.</param>
	/// <param name="unused2">A parameter used only for type inference.</param>
	/// <param name="unused3">A parameter used only for type inference.</param>
	/// <param name="unused4">A parameter used only for type inference.</param>
	/// <param name="unused5">A parameter used only for type inference.</param>
	/// <param name="unused6">A parameter used only for type inference.</param>
	/// <returns>The <see cref="MethodInfo"/> of the delegate.</returns>
	public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
		=> f.Method;
#pragma warning restore IDE0060

	#endregion
}