namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;

	/// <summary>
	/// Provides helper methods for working with tuple types.
	/// </summary>
	public static class TupleHelper
	{
		private static readonly HashSet<Type> _tupleTypes =
		[
			typeof(Tuple<>),
			typeof(Tuple<,>),
			typeof(Tuple<,,>),
			typeof(Tuple<,,,>),
			typeof(Tuple<,,,,>),
			typeof(Tuple<,,,,,>),
			typeof(Tuple<,,,,,,>),
			typeof(Tuple<,,,,,,,>),

			typeof(ValueTuple<>),
			typeof(ValueTuple<,>),
			typeof(ValueTuple<,,>),
			typeof(ValueTuple<,,,>),
			typeof(ValueTuple<,,,,>),
			typeof(ValueTuple<,,,,,>),
			typeof(ValueTuple<,,,,,,>),
			typeof(ValueTuple<,,,,,,,>),
		];

		/// <summary>
		/// Determines whether the specified type is a tuple type.
		/// </summary>
		/// <param name="tupleType">The type to check.</param>
		/// <returns>true if the specified type is a tuple; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="tupleType"/> is null.</exception>
		public static bool IsTuple(this Type tupleType)
		{
			if (tupleType is null)
				throw new ArgumentNullException(nameof(tupleType));

			return tupleType.IsGenericType && _tupleTypes.Contains(tupleType.GetGenericTypeDefinition());
		}

		/// <summary>
		/// Returns the values of a <see cref="Tuple{T}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T">The type of the tuple element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T>(this Tuple<T> tuple)
		{
			yield return tuple.Item1;
		}

		/// <summary>
		/// Returns the values of a <see cref="Tuple{T1, T2}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2>(this Tuple<T1, T2> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
		}

		/// <summary>
		/// Returns the values of a <see cref="Tuple{T1, T2, T3}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3>(this Tuple<T1, T2, T3> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
		}

		/// <summary>
		/// Returns the values of a <see cref="Tuple{T1, T2, T3, T4}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <typeparam name="T4">The type of the fourth element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3, T4>(this Tuple<T1, T2, T3, T4> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
		}

		/// <summary>
		/// Returns the values of a <see cref="Tuple{T1, T2, T3, T4, T5}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <typeparam name="T4">The type of the fourth element.</typeparam>
		/// <typeparam name="T5">The type of the fifth element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5>(this Tuple<T1, T2, T3, T4, T5> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
		}

		/// <summary>
		/// Returns the values of a <see cref="Tuple{T1, T2, T3, T4, T5, T6}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <typeparam name="T4">The type of the fourth element.</typeparam>
		/// <typeparam name="T5">The type of the fifth element.</typeparam>
		/// <typeparam name="T6">The type of the sixth element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5, T6>(this Tuple<T1, T2, T3, T4, T5, T6> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
			yield return tuple.Item6;
		}

		/// <summary>
		/// Returns the values of a <see cref="ValueTuple{T}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T">The type of the tuple element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T>(this ValueTuple<T> tuple)
		{
			yield return tuple.Item1;
		}

		/// <summary>
		/// Returns the values of a <see cref="ValueTuple{T1, T2}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2>(this ValueTuple<T1, T2> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
		}

		/// <summary>
		/// Returns the values of a <see cref="ValueTuple{T1, T2, T3}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3>(this ValueTuple<T1, T2, T3> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
		}

		/// <summary>
		/// Returns the values of a <see cref="ValueTuple{T1, T2, T3, T4}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <typeparam name="T4">The type of the fourth element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3, T4>(this ValueTuple<T1, T2, T3, T4> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
		}

		/// <summary>
		/// Returns the values of a <see cref="ValueTuple{T1, T2, T3, T4, T5}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <typeparam name="T4">The type of the fourth element.</typeparam>
		/// <typeparam name="T5">The type of the fifth element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5>(this ValueTuple<T1, T2, T3, T4, T5> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
		}

		/// <summary>
		/// Returns the values of a <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/> as an enumerable collection.
		/// </summary>
		/// <typeparam name="T1">The type of the first element.</typeparam>
		/// <typeparam name="T2">The type of the second element.</typeparam>
		/// <typeparam name="T3">The type of the third element.</typeparam>
		/// <typeparam name="T4">The type of the fourth element.</typeparam>
		/// <typeparam name="T5">The type of the fifth element.</typeparam>
		/// <typeparam name="T6">The type of the sixth element.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5, T6>(this ValueTuple<T1, T2, T3, T4, T5, T6> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
			yield return tuple.Item6;
		}

		/// <summary>
		/// Creates a tuple from the provided collection of values.
		/// </summary>
		/// <param name="values">An enumerable collection of values.</param>
		/// <param name="isValue">If set to true, a <see cref="ValueTuple"/> is created; otherwise, a <see cref="Tuple"/> is created.</param>
		/// <returns>The created tuple.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
		public static object ToTuple(this IEnumerable<object> values, bool isValue)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			var types = new List<Type>();
			var args = new List<object>();

			foreach (var value in values)
			{
				types.Add(value?.GetType() ?? typeof(object));
				args.Add(value);
			}

			var prefix = isValue ? "Value" : string.Empty;

			var genericType = $"System.{prefix}Tuple`{types.Count}".To<Type>();
			var specificType = genericType.Make(types);

			return specificType.CreateInstance([.. args]);
		}

		/// <summary>
		/// Extracts the values of a tuple instance into an enumerable collection.
		/// </summary>
		/// <typeparam name="T">The type of the tuple.</typeparam>
		/// <param name="tuple">The tuple instance.</param>
		/// <returns>An enumerable collection of tuple element values.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the provided object is not a tuple.</exception>
		public static IEnumerable<object> ToValues<T>(this T tuple)
		{
#if NETSTANDARD2_0
			if (!tuple.GetType().IsTuple())
				throw new InvalidOperationException($"Type {typeof(T)} is not tuple.");
#else
			if (tuple is not ITuple)
				throw new InvalidOperationException($"{tuple} is not tuple.");
#endif

			var type = tuple.GetType();

			if (type.IsClass)
			{
				return type
					.GetProperties()
					.Where(m => m.Name.StartsWith("Item"))
					.OrderBy(m => m.Name)
					.Select(m => m.GetValue(tuple));
			}
			else
			{
				return type
					.GetFields()
					.Where(m => m.Name.StartsWith("Item"))
					.OrderBy(m => m.Name)
					.Select(m => m.GetValue(tuple));
			}
		}

		/// <summary>
		/// Recursively unwraps all inner exceptions from the provided exception.
		/// </summary>
		/// <param name="exception">The exception to unwrap.</param>
		/// <returns>An enumerable collection of exceptions, including the original exception and all nested inner exceptions.</returns>
		public static IEnumerable<Exception> UnwrapExceptions(this Exception exception)
		{
			if (exception == null)
				yield break;

			yield return exception;

			if (exception is AggregateException ae)
				foreach (var innerException in ae.InnerExceptions.SelectMany(ie => ie.UnwrapExceptions()))
					yield return innerException;

			if (exception.InnerException != null)
				foreach (var innerException in exception.InnerException.UnwrapExceptions())
					yield return innerException;
		}
	}
}
