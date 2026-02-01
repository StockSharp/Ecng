namespace Ecng.Common;

#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

#endregion

/// <summary>
/// Provides extension methods and utilities for enum types.
/// </summary>
public static class Enumerator
{
	/// <summary>
	/// Gets the underlying base type of the enum type T.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <returns>The underlying numeric type for the enum.</returns>
	public static Type GetEnumBaseType<T>()
	{
		return typeof(T).GetEnumBaseType();
	}

	/// <summary>
	/// Gets the underlying type of the specified enum type.
	/// </summary>
	/// <param name="enumType">The enum type.</param>
	/// <returns>The underlying numeric type for the enum.</returns>
	public static Type GetEnumBaseType(this Type enumType)
	{
		return Enum.GetUnderlyingType(enumType);
	}

	/// <summary>
	/// Gets the name of the specified enum value.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="value">The enum value.</param>
	/// <returns>The name of the enum value.</returns>
	public static string GetName<T>(T value)
	{
		return value.To<Enum>().GetName();
	}

	/// <summary>
	/// Gets the name of the enum value.
	/// </summary>
	/// <param name="value">The enum value.</param>
	/// <returns>The name of the enum value.</returns>
	public static string GetName(this Enum value)
	{
		return Enum.GetName(value.GetType(), value);
	}

	/// <summary>
	/// Gets all enum values of type T.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <returns>An enumerable of all enum values.</returns>
	public static IEnumerable<T> GetValues<T>()
	{
		return typeof(T).GetValues().Cast<T>();
	}

	/// <summary>
	/// Excludes obsolete enum values from the provided collection.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="values">The collection of enum values.</param>
	/// <returns>An enumerable without obsolete enum values.</returns>
	public static IEnumerable<T> ExcludeObsolete<T>(this IEnumerable<T> values)
	{
		return values.Where(v => v.GetAttributeOfType<ObsoleteAttribute>() is null);
	}

	/// <summary>
	/// Gets all enum values for the specified enum type as objects.
	/// </summary>
	/// <param name="enumType">The enum type.</param>
	/// <returns>An enumerable of enum values as objects.</returns>
	public static IEnumerable<object> GetValues(this Type enumType)
	{
		return Enum.GetValues(enumType).Cast<object>();
	}

	/// <summary>
	/// Gets all names of the enum type T as a collection of strings.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <returns>An enumerable of enum names.</returns>
	public static IEnumerable<string> GetNames<T>()
	{
		return typeof(T).GetNames();
	}

	/// <summary>
	/// Gets all names of the specified enum type.
	/// </summary>
	/// <param name="enumType">The enum type.</param>
	/// <returns>An enumerable of enum names.</returns>
	public static IEnumerable<string> GetNames(this Type enumType)
	{
		return Enum.GetNames(enumType);
	}

	/// <summary>
	/// Determines whether the specified enum value is defined in its type.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="enumValue">The enum value.</param>
	/// <returns><c>true</c> if the value is defined; otherwise, <c>false</c>.</returns>
	public static bool IsDefined<T>(this T enumValue)
	{
		return Enum.IsDefined(typeof(T), enumValue);
	}

	/// <summary>
	/// Determines whether the specified enum type has the Flags attribute.
	/// </summary>
	/// <param name="enumType">The enum type.</param>
	/// <returns><c>true</c> if the Flags attribute is present; otherwise, <c>false</c>.</returns>
	public static bool IsFlags(this Type enumType)
		=> enumType.GetAttribute<FlagsAttribute>() is not null;

	/// <summary>
	/// Splits the masked enum value into its constituent flag values and returns them as objects.
	/// </summary>
	/// <param name="maskedValue">The masked enum value.</param>
	/// <returns>An enumerable of individual flag values as objects.</returns>
	/// <exception cref="ArgumentNullException">Thrown when maskedValue is null.</exception>
	public static IEnumerable<object> SplitMask2(this object maskedValue)
	{
		if (maskedValue is null)
			throw new ArgumentNullException(nameof(maskedValue));

		return maskedValue.GetType().GetValues().Where(v => HasFlags(maskedValue, v));
	}

	/// <summary>
	/// Splits the masked enum value into its constituent flag values of type T.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="maskedValue">The masked enum value.</param>
	/// <returns>An enumerable of individual flag values of type T.</returns>
	public static IEnumerable<T> SplitMask<T>(this T maskedValue)
	{
		return GetValues<T>().Where(v => HasFlags(maskedValue, v));
	}

	/// <summary>
	/// Joins all flags of type T into a single masked enum value.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <returns>The joined masked enum value.</returns>
	public static T JoinMask<T>()
	{
		return GetValues<T>().JoinMask();
	}

	/// <summary>
	/// Joins the provided collection of enum values into a single masked enum value.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="values">An enumerable of enum values.</param>
	/// <returns>The resulting joined masked enum value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
	public static T JoinMask<T>(this IEnumerable<T> values)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		return values.Aggregate(default(T), (current, t) => (current.To<long>() | t.To<long>()).To<T>());
	}

	/// <summary>
	/// Removes the specified flag(s) from the source enum value and returns the result.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="enumSource">The source enum value.</param>
	/// <param name="enumPart">The flag(s) to remove.</param>
	/// <returns>The enum value after removal of the specified flag(s).</returns>
	public static T Remove<T>(T enumSource, T enumPart)
	{
		return enumSource.To<Enum>().Remove(enumPart);
	}

	/// <summary>
	/// Removes the specified flag from the enum value.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="enumSource">The source enum value.</param>
	/// <param name="enumPart">The flag to remove.</param>
	/// <returns>The enum value after removal of the flag.</returns>
	/// <exception cref="ArgumentException">Thrown when the type of enumPart does not match enumSource.</exception>
	public static T Remove<T>(this Enum enumSource, T enumPart)
	{
		if (enumSource.GetType() != typeof(T))
			throw new ArgumentException(nameof(enumPart));

		return (enumSource.To<long>() & ~enumPart.To<long>()).To<T>();
	}

	/// <summary>
	/// Determines whether the specified enum value has the given flag(s).
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="enumSource">The source enum value.</param>
	/// <param name="enumPart">The flag(s) to check for.</param>
	/// <returns><c>true</c> if the flag(s) are present; otherwise, <c>false</c>.</returns>
	public static bool HasFlags<T>(T enumSource, T enumPart)
	{
		return enumSource.To<Enum>().HasFlag(enumPart.To<Enum>());
	}

	/// <summary>
	/// Attempts to parse the string to an enum value of type T.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="str">The string representation of the enum value.</param>
	/// <param name="value">When this method returns, contains the enum value equivalent to the string, if the parse succeeded.</param>
	/// <param name="ignoreCase">if set to <c>true</c> ignores case during parsing.</param>
	/// <returns><c>true</c> if the parse was successful; otherwise, <c>false</c>.</returns>
	public static bool TryParse<T>(this string str, out T value, bool ignoreCase = true)
		where T : struct
	{
		return Enum.TryParse(str, ignoreCase, out value);
	}

	//
	// https://stackoverflow.com/a/9276348
	//

	/// <summary>
	/// Retrieves an attribute of type TAttribute from the enum field value.
	/// </summary>
	/// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
	/// <param name="enumVal">The enum value.</param>
	/// <returns>The attribute instance if found; otherwise, <c>null</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when enumVal is null.</exception>
	public static TAttribute GetAttributeOfType<TAttribute>(this object enumVal)
		where TAttribute : Attribute
	{
		if (enumVal is null)
			throw new ArgumentNullException(nameof(enumVal));

		var memInfo = enumVal.GetType().GetMember(enumVal.ToString());
		return memInfo.Length == 0 ? null : memInfo[0].GetAttribute<TAttribute>(false);
	}

	/// <summary>
	/// Determines whether the enum value is marked as browsable.
	/// </summary>
	/// <param name="enumVal">The enum value.</param>
	/// <returns><c>true</c> if the enum value is browsable; otherwise, <c>false</c>.</returns>
	public static bool IsEnumBrowsable(this object enumVal)
		=> enumVal.GetAttributeOfType<BrowsableAttribute>()?.Browsable ?? true;

	/// <summary>
	/// Excludes non-browsable enum values from the collection.
	/// </summary>
	/// <typeparam name="T">The enum type.</typeparam>
	/// <param name="values">The collection of enum values.</param>
	/// <returns>An enumerable of browsable enum values.</returns>
	public static IEnumerable<T> ExcludeNonBrowsable<T>(this IEnumerable<T> values)
		=> values.Where(v => v.IsEnumBrowsable());
}