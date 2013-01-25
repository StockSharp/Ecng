namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	public static class OperatorRegistry
	{
		private sealed class OperableOperator<T> : IOperator<T>
			where T : IOperable<T>
		{
			int IComparer<T>.Compare(T x, T y)
			{
				return x.CompareTo(y);
			}

			T IOperator<T>.Add(T first, T second)
			{
				return first.Add(second);
			}

			T IOperator<T>.Subtract(T first, T second)
			{
				return first.Subtract(second);
			}

			T IOperator<T>.Multiply(T first, T second)
			{
				return first.Multiply(second);
			}

			T IOperator<T>.Divide(T first, T second)
			{
				return first.Divide(second);
			}
		}

		private static readonly SynchronizedDictionary<Type, object> _operators = new SynchronizedDictionary<Type, object>();

		static OperatorRegistry()
		{
			AddOperator(new ByteOperator());
			AddOperator(new ShortOperator());
			AddOperator(new IntOperator());
			AddOperator(new LongOperator());
			AddOperator(new FloatOperator());
			AddOperator(new DoubleOperator());
			AddOperator(new DecimalOperator());
			AddOperator(new TimeSpanOperator());
			AddOperator(new DateTimeOperator());
			AddOperator(new SByteOperator());
			AddOperator(new UShortOperator());
			AddOperator(new UIntOperator());
			AddOperator(new ULongOperator());
		}

		public static void AddOperator<T>(IOperator<T> @operator)
		{
			if (@operator == null)
				throw new ArgumentNullException("operator");

			_operators.Add(typeof(T), @operator);
		}

		public static IOperator<T> GetOperator<T>()
		{
			var type = typeof(T);
			var @operator = (IOperator<T>)_operators.TryGetValue(type);

			if (@operator == null)
			{
				if (typeof(IOperable<T>).IsAssignableFrom(type))
				{
					@operator = typeof(OperableOperator<>).Make(type).CreateInstance<IOperator<T>>();
					_operators.Add(type, @operator);
				}
				else
					throw new InvalidOperationException("Operator for type {0} doesn't exist.".Put(type));
			}

			return @operator;
		}

		public static bool IsRegistered<T>()
		{
			return IsRegistered(typeof(T));
		}

		public static bool IsRegistered(Type type)
		{
			return _operators.ContainsKey(type);
		}

		public static void RemoveOperator<T>(IOperator<T> @operator)
		{
			if (@operator == null)
				throw new ArgumentNullException("operator");

			_operators.Remove(typeof(T));
		}
	}
}