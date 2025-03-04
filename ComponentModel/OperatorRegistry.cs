namespace Ecng.ComponentModel;

using System;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Localization;

/// <summary>
/// Provides a registry for operators to perform mathematical and comparison operations.
/// </summary>
public static class OperatorRegistry
{
	/// <summary>
	/// Represents an operator implementation for types implementing <see cref="IOperable{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type that implements <see cref="IOperable{T}"/>.</typeparam>
	private sealed class OperableOperator<T> : BaseOperator<T>
		where T : IOperable<T>
	{
		/// <inheritdoc />
		public override int Compare(T x, T y)
		{
			return x.CompareTo(y);
		}

		/// <inheritdoc />
		public override T Add(T first, T second)
		{
			return first.Add(second);
		}

		/// <inheritdoc />
		public override T Subtract(T first, T second)
		{
			return first.Subtract(second);
		}

		/// <inheritdoc />
		public override T Multiply(T first, T second)
		{
			return first.Multiply(second);
		}

		/// <inheritdoc />
		public override T Divide(T first, T second)
		{
			return first.Divide(second);
		}
	}

	private static readonly SynchronizedDictionary<Type, IOperator> _operators = [];

	/// <summary>
	/// Initializes static members of the <see cref="OperatorRegistry"/> class.
	/// Registers the default set of operators.
	/// </summary>
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

	/// <summary>
	/// Adds an operator for the specified type.
	/// </summary>
	/// <typeparam name="T">The type that the operator will handle.</typeparam>
	/// <param name="operator">The operator to add.</param>
	/// <exception cref="ArgumentNullException">Thrown when the <paramref name="operator"/> is null.</exception>
	public static void AddOperator<T>(IOperator<T> @operator)
	{
		if (@operator is null)
			throw new ArgumentNullException(nameof(@operator));

		_operators.Add(typeof(T), @operator);
	}

	/// <summary>
	/// Retrieves the operator associated with the specified type.
	/// </summary>
	/// <param name="type">The type for which to retrieve the operator.</param>
	/// <returns>The operator associated with the given type.</returns>
	/// <exception cref="InvalidOperationException">Thrown when no operator exists for the given type.</exception>
	public static IOperator GetOperator(this Type type)
	{
		if (!_operators.TryGetValue(type, out var @operator))
		{
			if (type.Is(typeof(IOperable<>).Make(type)))
			{
				@operator = typeof(OperableOperator<>).Make(type).CreateInstance<IOperator>();
				_operators.Add(type, @operator);
			}
			else
				throw new InvalidOperationException($"Operator for type {type} doesn't exist.");
		}

		return @operator;
	}

	/// <summary>
	/// Retrieves the operator associated with the specified type parameter.
	/// </summary>
	/// <typeparam name="T">The type for which to retrieve the operator.</typeparam>
	/// <returns>The operator associated with the given type.</returns>
	public static IOperator<T> GetOperator<T>()
	{
		return (IOperator<T>)GetOperator(typeof(T));
	}

	/// <summary>
	/// Checks if an operator is registered for the specified type parameter.
	/// </summary>
	/// <typeparam name="T">The type to check for operator registration.</typeparam>
	/// <returns><c>true</c> if an operator is registered; otherwise, <c>false</c>.</returns>
	public static bool IsRegistered<T>()
	{
		return IsRegistered(typeof(T));
	}

	/// <summary>
	/// Checks if an operator is registered for the specified type.
	/// </summary>
	/// <param name="type">The type to check for operator registration.</param>
	/// <returns><c>true</c> if an operator is registered; otherwise, <c>false</c>.</returns>
	public static bool IsRegistered(Type type)
	{
		return _operators.ContainsKey(type);
	}

	/// <summary>
	/// Removes the operator for the specified type.
	/// </summary>
	/// <typeparam name="T">The type whose operator is to be removed.</typeparam>
	/// <param name="operator">The operator instance to remove.</param>
	/// <exception cref="ArgumentNullException">Thrown when the <paramref name="operator"/> is null.</exception>
	public static void RemoveOperator<T>(IOperator<T> @operator)
	{
		if (@operator is null)
			throw new ArgumentNullException(nameof(@operator));

		_operators.Remove(typeof(T));
	}

	/// <summary>
	/// Throws an exception if the specified nullable long value is negative.
	/// </summary>
	/// <param name="value">The nullable long value to check.</param>
	/// <param name="name">The name of the variable to include in the exception.</param>
	/// <returns>The original value if it is not negative; otherwise, null.</returns>
	public static long? ThrowIfNegative(this long? value, string name)
	{
		if (value is null)
			return null;

		return value.Value.ThrowIfNegative(name);
	}

	/// <summary>
	/// Throws an exception if the specified long value is negative.
	/// </summary>
	/// <param name="value">The long value to check.</param>
	/// <param name="name">The name of the variable to include in the exception.</param>
	/// <returns>The original value if it is not negative.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
	public static long ThrowIfNegative(this long value, string name)
	{
		if (value < 0)
			throw new ArgumentOutOfRangeException(name, value, "Invalid value.".Localize());

		return value;
	}
}