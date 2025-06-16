namespace Ecng.Common;

using System;

/// <summary>
/// Provides helper extension methods for working with nullable types.
/// </summary>
public static class NullableHelper
{
	/// <summary>
	/// Gets the underlying type argument of the specified nullable type.
	/// </summary>
	/// <param name="nullableType">The nullable type to get the underlying type from.</param>
	/// <returns>The underlying type if the provided type is nullable; otherwise, null.</returns>
	public static Type GetUnderlyingType(this Type nullableType)
	{
		return Nullable.GetUnderlyingType(nullableType);
	}

	/// <summary>
	/// Determines whether the specified type is a nullable type.
	/// </summary>
	/// <param name="type">The type to inspect.</param>
	/// <returns>True if the type is nullable; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
	public static bool IsNullable(this Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return type.GetUnderlyingType() != null;
	}

	/// <summary>
	/// Determines whether the specified value is null.
	/// For value types, this method returns false.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to check for null.</param>
	/// <returns>True if the value is null; otherwise, false.</returns>
	public static bool IsNull<T>(this T value)
	{
		return value.IsNull(false);
	}

	/// <summary>
	/// Determines whether the specified value is null or its default for value types.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to check.</param>
	/// <param name="checkValueTypeOnDefault">
	/// If true and the type is a value type, the value is compared to its default value.
	/// </param>
	/// <returns>
	/// True if the reference type value is null or if the value type is equal to its default value when <paramref name="checkValueTypeOnDefault"/> is true; otherwise, false.
	/// </returns>
	public static bool IsNull<T>(this T value, bool checkValueTypeOnDefault)
	{
		if (value is not ValueType)
			return value is null;

		if (!checkValueTypeOnDefault)
			return false;

		var defValue = default(T);

		// typeof(T) == typeof(object)
		defValue ??= (T)Activator.CreateInstance(value.GetType());

		return value.Equals(defValue);
	}

	/// <summary>
	/// Converts the value to a result type using the appropriate conversion function based on whether the value is null.
	/// </summary>
	/// <typeparam name="T">The type of the input value. Must be a reference type.</typeparam>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	/// <param name="value">The input value to convert.</param>
	/// <param name="notNullFunc">A function to convert the value when it is not null.</param>
	/// <param name="nullFunc">A function to produce a result when the value is null.</param>
	/// <returns>
	/// The result of applying either <paramref name="notNullFunc"/> or <paramref name="nullFunc"/> to the value.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="notNullFunc"/> or <paramref name="nullFunc"/> is null.
	/// </exception>
	public static TResult Convert<T, TResult>(this T value, Func<T, TResult> notNullFunc, Func<TResult> nullFunc)
		where T : class
	{
		if (notNullFunc is null)
			throw new ArgumentNullException(nameof(notNullFunc));

		if (nullFunc is null)
			throw new ArgumentNullException(nameof(nullFunc));

		return value is null ? nullFunc() : notNullFunc(value);
	}

	/// <summary>
	/// Returns the default value as null if the value is equal to its default; otherwise, returns the value as a nullable type.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// Null if the value is the default of its type; otherwise, the value wrapped as a nullable type.
	/// </returns>
	public static T? DefaultAsNull<T>(this T value)
		where T : struct
	{
		return value.IsDefault() ? null : value;
	}

	/// <summary>
	/// Creates a nullable type from the given type.
	/// </summary>
	/// <param name="type">The type to make nullable.</param>
	/// <returns>A nullable type of the given type.</returns>
	public static Type MakeNullable(this Type type)
		=> typeof(Nullable<>).Make(type);

	/// <summary>
	/// Converts the given type to a nullable type if it is a non-nullable value type.
	/// </summary>
	/// <param name="type">The type to convert.</param>
	/// <returns>
	/// The nullable version of the type if it is a value type and not already nullable; otherwise, the original type.
	/// </returns>
	public static Type TryMakeNullable(this Type type)
	{
		if (!type.IsNullable() && type.IsValueType)
			type = typeof(Nullable<>).Make(type);

		return type;
	}
}