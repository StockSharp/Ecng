namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;

	public static class TupleHelper
	{
		private static readonly HashSet<Type> _tupleTypes = new()
		{
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
		};

		public static bool IsTuple(this Type tupleType)
		{
			if (tupleType is null)
				throw new ArgumentNullException(nameof(tupleType));

			return tupleType.IsGenericType && _tupleTypes.Contains(tupleType.GetGenericTypeDefinition());
		}

		public static IEnumerable<object> ToValues<T>(this Tuple<T> tuple)
		{
			yield return tuple.Item1;
		}

		public static IEnumerable<object> ToValues<T1, T2>(this Tuple<T1, T2> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3>(this Tuple<T1, T2, T3> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3, T4>(this Tuple<T1, T2, T3, T4> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5>(this Tuple<T1, T2, T3, T4, T5> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5, T6>(this Tuple<T1, T2, T3, T4, T5, T6> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
			yield return tuple.Item6;
		}

		public static IEnumerable<object> ToValues<T>(this ValueTuple<T> tuple)
		{
			yield return tuple.Item1;
		}

		public static IEnumerable<object> ToValues<T1, T2>(this ValueTuple<T1, T2> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3>(this ValueTuple<T1, T2, T3> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3, T4>(this ValueTuple<T1, T2, T3, T4> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5>(this ValueTuple<T1, T2, T3, T4, T5> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
		}

		public static IEnumerable<object> ToValues<T1, T2, T3, T4, T5, T6>(this ValueTuple<T1, T2, T3, T4, T5, T6> tuple)
		{
			yield return tuple.Item1;
			yield return tuple.Item2;
			yield return tuple.Item3;
			yield return tuple.Item4;
			yield return tuple.Item5;
			yield return tuple.Item6;
		}

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

			return specificType.CreateInstance(args.ToArray());
		}

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
		/// </summary>
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
