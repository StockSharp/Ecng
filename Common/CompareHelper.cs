namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Net;

/// <summary>
/// Provides helper methods for comparing various types including IPAddress, Type, object, and Version.
/// </summary>
public static class CompareHelper
{
	/// <summary>
	/// Compares two IPAddress instances by converting them to a numeric value.
	/// </summary>
	/// <param name="first">The first IPAddress to compare.</param>
	/// <param name="second">The second IPAddress to compare.</param>
	/// <returns>
	/// A signed integer that indicates the relative values of the two IP addresses.
	/// Less than zero if first is less than second, zero if equal, and greater than zero if first is greater than second.
	/// </returns>
	public static int Compare(this IPAddress first, IPAddress second)
	{
		return first.To<long>().CompareTo(second.To<long>());
	}

	/// <summary>
	/// Compares two Type instances for equality with an option to consider inheritance.
	/// </summary>
	/// <param name="first">The first Type to compare.</param>
	/// <param name="second">The second Type to compare.</param>
	/// <param name="useInheritance">If set to true, uses inheritance to determine equality.</param>
	/// <returns>
	/// True if the types are considered equal; otherwise, false.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown if either <paramref name="first"/> or <paramref name="second"/> is null.</exception>
	public static bool Compare(this Type first, Type second, bool useInheritance)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));

		if (second is null)
			throw new ArgumentNullException(nameof(second));

		if (useInheritance)
			return second.Is(first);
		else
			return first == second;
	}

	/// <summary>
	/// Compares two Type instances with inheritance ordering.
	/// </summary>
	/// <param name="first">The first Type to compare.</param>
	/// <param name="second">The second Type to compare.</param>
	/// <returns>
	/// 0 if the types are identical; 1 if <paramref name="second"/> is assignable from <paramref name="first"/>; otherwise, -1.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown if either <paramref name="first"/> or <paramref name="second"/> is null.</exception>
	public static int Compare(this Type first, Type second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));

		if (second is null)
			throw new ArgumentNullException(nameof(second));

		if (first == second)
			return 0;
		else if (second.Is(first))
			return 1;
		else
			return -1;
	}

	/// <summary>
	/// Compares two objects of the same type that implement <see cref="IComparable"/>.
	/// </summary>
	/// <param name="value1">The first object to compare.</param>
	/// <param name="value2">The second object to compare.</param>
	/// <returns>
	/// A signed integer that indicates the relative values of the two objects.
	/// Less than zero if <paramref name="value1"/> is less than <paramref name="value2"/>, zero if equal, and greater than zero if greater.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// Thrown if the objects are not of the same type or if they do not implement <see cref="IComparable"/>.
	/// </exception>
	public static int Compare(this object value1, object value2)
	{
		if (value1 is null && value2 is null)
			return 0;

		if (value1 is null)
			return -1;

		if (value2 is null)
			return 1;

		if (value1.GetType() != value2.GetType())
			throw new ArgumentException("The values must be a same types.", nameof(value2));


		if (value1 is IComparable compare1)
			return compare1.CompareTo(value2);

		throw new ArgumentException("The values must be IComparable.");
	}

	/// <summary>
	/// Determines whether a value is equal to its default value.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// True if the value equals the default value of its type; otherwise, false.
	/// </returns>
	public static bool IsDefault<T>(this T value)
	{
		return EqualityComparer<T>.Default.Equals(value, default);
	}

	/// <summary>
	/// Determines whether a value is equal to the default value as computed at runtime.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// True if the value equals the runtime default value; otherwise, false.
	/// </returns>
	public static bool IsRuntimeDefault<T>(this T value)
	{
		return EqualityComparer<T>.Default.Equals(value, (T)value.GetType().GetDefaultValue());
	}

	/// <summary>
	/// Compares two Version instances.
	/// </summary>
	/// <param name="first">The first Version to compare.</param>
	/// <param name="second">The second Version to compare.</param>
	/// <returns>
	/// A signed integer that indicates the relative values of the two versions.
	/// Less than zero if <paramref name="first"/> is less than <paramref name="second"/>, zero if equal, and greater than zero if greater.
	/// </returns>
	public static int Compare(this Version first, Version second)
	{
		if (first is null)
		{
			if (second is null)
				return 0;

			return -1;
		}

		if (second is null)
			return 1;

		var firstBuild = first.Build != -1 ? first.Build : 0;
		var firstRevision = first.Revision != -1 ? first.Revision : 0;

		var secondBuild = second.Build != -1 ? second.Build : 0;
		var secondRevision = second.Revision != -1 ? second.Revision : 0;

		if (first.Major != second.Major)
			return first.Major > second.Major ? 1 : -1;
		
		if (first.Minor != second.Minor)
			return first.Minor > second.Minor ? 1 : -1;

		if (firstBuild != secondBuild)
			return firstBuild > secondBuild ? 1 : -1;

		if (firstRevision == secondRevision)
			return 0;

		return firstRevision > secondRevision ? 1 : -1;
	}
}