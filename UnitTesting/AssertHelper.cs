namespace Ecng.UnitTesting;

using System;
using System.Security;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Assert helper.
/// </summary>
public static class AssertHelper
{
	/// <summary>
	/// Asserts that the value is true.
	/// </summary>
	/// <param name="value">Value.</param>
	public static void AssertTrue(this bool value)
		=> Assert.IsTrue(value);

	/// <summary>
	/// Asserts that the value is false.
	/// </summary>
	/// <param name="value">Value.</param>
	public static void AssertFalse(this bool value)
		=> Assert.IsFalse(value);

	/// <summary>
	/// Asserts that the value is null.
	/// </summary>
	/// <param name="value">Value.</param>
	public static void AssertNull(this object value)
		=> Assert.IsNull(value);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <param name="value">Value.</param>
	public static void AssertNull(this Exception value)
	{
		if (value != null)
			throw value;
	}

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <param name="value">Value.</param>
	public static void AssertNotNull(this object value)
		=> Assert.IsNotNull(value);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="value">Value.</param>
	public static void AssertOfType<T>(this object value)
		=> Assert.IsInstanceOfType(value, typeof(T));

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="value">Value.</param>
	public static void AssertNotOfType<T>(this object value)
		=> Assert.IsNotInstanceOfType(value, typeof(T));

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	public static void AreEqual<T>(this T value, T expected)
		=> value.AssertEqual(expected);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	public static void AssertEqual<T>(this T value, T expected)
	{
		if (value is SecureString str)
			str.IsEqualTo(expected.To<SecureString>()).AssertTrue();
		else
			Assert.AreEqual(expected, value);
	}

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	/// <param name="delta">Delta for comparing floating point numbers.</param>
	public static void AssertEqual(this double value, double expected, double delta)
		=> Assert.AreEqual(expected, value, delta);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	/// <param name="delta">Delta for comparing floating point numbers.</param>
	public static void AssertEqual(this float value, float expected, float delta)
		=> Assert.AreEqual(expected, value, delta);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	public static void AssertNotEqual<T>(this T value, T expected)
		=> Assert.AreNotEqual(expected, value);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	public static void AssertSame<T>(this T value, T expected)
		=> Assert.AreSame(expected, value);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	public static void AssertNotSame<T>(this T value, T expected)
		=> Assert.AreNotSame(expected, value);

	/// <summary>
	/// Asserts that the value is not null.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="expected">Expected value.</param>
	/// <param name="nullAsEmpty">If <see langword="true"/>, then null and empty strings are considered equal.</param>
	public static void AssertEqual(this string value, string expected, bool nullAsEmpty = false)
	{
		if (nullAsEmpty && value.IsEmpty() && expected.IsEmpty())
			return;

		value.AssertEqual(expected);
	}


	/// <summary>
	/// Asserts that two arrays are equal by comparing their lengths and elements.
	/// </summary>
	/// <typeparam name="T">The type of the array elements.</typeparam>
	/// <param name="value">The actual array to be tested. Can be null.</param>
	/// <param name="expected">The expected array. If <paramref name="value"/> is null, then <paramref name="expected"/> must also be null.</param>
	public static void AssertEqual<T>(this T[] value, T[] expected)
	{
		if (value is null)
		{
			expected.AssertNull();
			return;
		}

		expected.AssertNotNull();

		value.Length.AssertEqual(expected.Length);

		for (var i = 0; i < value.Length; i++)
			value[i].AssertEqual(expected[i]);
	}

	/// <summary>
	/// Asserts that <paramref name="value"/> is greater than <paramref name="expected"/>.
	/// </summary>
	/// <typeparam name="T">Type implementing <see cref="IComparable{T}"/>.</typeparam>
	/// <param name="value">Actual value.</param>
	/// <param name="expected">Value to compare against.</param>
	public static void AssertGreater<T>(this T value, T expected)
		where T : IComparable<T>
	{
		(value.CompareTo(expected) > 0).AssertTrue();
	}

	/// <summary>
	/// Asserts that <paramref name="value"/> is less than <paramref name="expected"/>.
	/// </summary>
	/// <typeparam name="T">Type implementing <see cref="IComparable{T}"/>.</typeparam>
	/// <param name="value">Actual value.</param>
	/// <param name="expected">Value to compare against.</param>
	public static void AssertLess<T>(this T value, T expected)
		where T : IComparable<T>
	{
		(value.CompareTo(expected) < 0).AssertTrue();
	}

	/// <summary>
	/// Asserts that <paramref name="value"/> is greater than <paramref name="min"/> and less than <paramref name="max"/>.
	/// </summary>
	/// <typeparam name="T">Type implementing <see cref="IComparable{T}"/>.</typeparam>
	/// <param name="value">Actual value.</param>
	/// <param name="min">Minimum value (exclusive).</param>
	/// <param name="max">Maximum value (exclusive).</param>
	public static void AssertInRange<T>(this T value, T min, T max)
		where T : IComparable<T>
	{
		value.AssertGreater(min);
		value.AssertLess(max);
	}
}