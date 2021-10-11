namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;

	public static class OperatorRegistry
	{
		private sealed class OperableOperator<T> : BaseOperator<T>
			where T : IOperable<T>
		{
			public override int Compare(T x, T y)
			{
				return x.CompareTo(y);
			}

			public override T Add(T first, T second)
			{
				return first.Add(second);
			}

			public override T Subtract(T first, T second)
			{
				return first.Subtract(second);
			}

			public override T Multiply(T first, T second)
			{
				return first.Multiply(second);
			}

			public override T Divide(T first, T second)
			{
				return first.Divide(second);
			}
		}

		private static readonly SynchronizedDictionary<Type, IOperator> _operators = new();

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
			AddOperator(new DateTimeOffsetOperator());
			AddOperator(new SByteOperator());
			AddOperator(new UShortOperator());
			AddOperator(new UIntOperator());
			AddOperator(new ULongOperator());
		}

		public static void AddOperator<T>(IOperator<T> @operator)
		{
			if (@operator is null)
				throw new ArgumentNullException(nameof(@operator));

			_operators.Add(typeof(T), @operator);
		}

		public static IOperator GetOperator(Type type)
		{
			if (!_operators.TryGetValue(type, out var @operator))
			{
				if (typeof(IOperable<>).Make(type).IsAssignableFrom(type))
				{
					@operator = typeof(OperableOperator<>).Make(type).CreateInstance<IOperator>();
					_operators.Add(type, @operator);
				}
				else
					throw new InvalidOperationException("Operator for type {0} doesn't exist.".Translate().Put(type));
			}

			return @operator;
		}

		public static IOperator<T> GetOperator<T>()
		{
			return (IOperator<T>)GetOperator(typeof(T));
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
			if (@operator is null)
				throw new ArgumentNullException(nameof(@operator));

			_operators.Remove(typeof(T));
		}
	}
}