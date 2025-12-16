namespace Ecng.UnitTesting;

using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Base test class.
/// </summary>
public abstract class BaseTestClass
{
	/// <summary>
	/// <see cref="TestContext"/>
	/// </summary>
	public TestContext TestContext { get; set; }

	/// <summary>
	/// Cancellation token for the test.
	/// </summary>
	protected CancellationToken CancellationToken => TestContext.CancellationToken;

	/// <summary>
	/// Skip all tests in this class when running in GitHub Actions.
	/// </summary>
	protected virtual bool SkipInGitHubActions => false;

	/// <summary>
	/// Reason for skipping tests in GitHub Actions.
	/// </summary>
	protected virtual string SkipInGitHubActionsReason => null;

	[TestInitialize]
	public void BaseTestInitialize()
	{
		if (!SkipInGitHubActions)
			return;

		var isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
		if (!isGitHubActions.IsEmpty() && isGitHubActions.EqualsIgnoreCase("true"))
			Inconclusive(SkipInGitHubActionsReason ?? "Skipped in GitHub Actions.");
	}

	/// <summary>
	/// Fails the test without checking any conditions.
	/// </summary>
	/// <param name="message">The message to include in the exception.</param>
	protected static void Fail(string message = default)
		=> Assert.Fail(message);

	/// <summary>
	/// Marks the test as inconclusive.
	/// </summary>
	/// <param name="message">The message to include in the exception.</param>
	protected static void Inconclusive(string message = default)
		=> Assert.Inconclusive(message);

	/// <summary>
	/// Tests whether the specified condition is true.
	/// </summary>
	/// <param name="condition">Condition.</param>
	/// <param name="message">Error message.</param>
	protected static void IsTrue(bool condition, string message = "")
		=> Assert.IsTrue(condition, message);

	/// <summary>
	/// Tests whether the specified condition is false.
	/// </summary>
	/// <param name="condition">Condition.</param>
	/// <param name="message">Error message.</param>
	protected static void IsFalse(bool condition, string message = "")
		=> Assert.IsFalse(condition, message);

	/// <summary>
	/// Tests whether the specified value is null.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNull(object value, string message = "")
		=> Assert.IsNull(value, message);

	/// <summary>
	/// Tests whether the specified value is not null.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotNull(object value, string message = "")
		=> Assert.IsNotNull(value, message);

	/// <summary>
	/// Tests whether two values are equal.
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="expected">Expected value.</param>
	/// <param name="actual">Actual value.</param>
	/// <param name="message">Error message.</param>
	protected static void AreEqual<T>(T expected, T actual, string message = "")
		=> Assert.AreEqual(expected, actual, message);

	/// <summary>
	/// Tests whether two values are not equal.
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="notExpected">Not expected value.</param>
	/// <param name="actual">Actual value.</param>
	/// <param name="message">Error message.</param>
	protected static void AreNotEqual<T>(T notExpected, T actual, string message = "")
		=> Assert.AreNotEqual(notExpected, actual, message);

	/// <summary>
	/// Tests whether two objects refer to the same instance.
	/// </summary>
	/// <param name="expected">Expected instance.</param>
	/// <param name="actual">Actual instance.</param>
	/// <param name="message">Error message.</param>
	protected static void AreSame(object expected, object actual, string message = "")
		=> Assert.AreSame(expected, actual, message);

	/// <summary>
	/// Tests whether two objects do not refer to the same instance.
	/// </summary>
	/// <param name="notExpected">Not expected instance.</param>
	/// <param name="actual">Actual instance.</param>
	/// <param name="message">Error message.</param>
	protected static void AreNotSame(object notExpected, object actual, string message = "")
		=> Assert.AreNotSame(notExpected, actual, message);

	/// <summary>
	/// Tests whether the specified object is an instance of the expected type.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="expectedType">Expected type.</param>
	/// <param name="message">Error message.</param>
	protected static void IsInstanceOfType(object value, Type expectedType, string message = "")
		=> Assert.IsInstanceOfType(value, expectedType, message);

	/// <summary>
	/// Tests whether the specified object is not an instance of the expected type.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="wrongType">Wrong type.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotInstanceOfType(object value, Type wrongType, string message = "")
		=> Assert.IsNotInstanceOfType(value, wrongType, message);

	/// <summary>
	/// Tests whether two strings are equal, ignoring case.
	/// </summary>
	/// <param name="expected">Expected string.</param>
	/// <param name="actual">Actual string.</param>
	/// <param name="ignoreCase">Ignore case.</param>
	/// <param name="message">Error message.</param>
	protected static void AreEqual(string expected, string actual, bool ignoreCase, string message = "")
		=> Assert.AreEqual(expected, actual, ignoreCase, message);

	/// <summary>
	/// Tests whether two doubles are equal within the specified delta.
	/// </summary>
	/// <param name="expected">Expected value.</param>
	/// <param name="actual">Actual value.</param>
	/// <param name="delta">Delta.</param>
	/// <param name="message">Error message.</param>
	protected static void AreEqual(double expected, double actual, double delta, string message = "")
		=> Assert.AreEqual(expected, actual, delta, message);

	/// <summary>
	/// Tests whether two floats are equal within the specified delta.
	/// </summary>
	/// <param name="expected">Expected value.</param>
	/// <param name="actual">Actual value.</param>
	/// <param name="delta">Delta.</param>
	/// <param name="message">Error message.</param>
	protected static void AreEqual(float expected, float actual, float delta, string message = "")
		=> Assert.AreEqual(expected, actual, delta, message);

	/// <summary>
	/// Tests whether two doubles are not equal within the specified delta.
	/// </summary>
	/// <param name="notExpected">Not expected value.</param>
	/// <param name="actual">Actual value.</param>
	/// <param name="delta">Delta.</param>
	/// <param name="message">Error message.</param>
	protected static void AreNotEqual(double notExpected, double actual, double delta, string message = "")
		=> Assert.AreNotEqual(notExpected, actual, delta, message);

	/// <summary>
	/// Tests whether two floats are not equal within the specified delta.
	/// </summary>
	/// <param name="notExpected">Not expected value.</param>
	/// <param name="actual">Actual value.</param>
	/// <param name="delta">Delta.</param>
	/// <param name="message">Error message.</param>
	protected static void AreNotEqual(float notExpected, float actual, float delta, string message = "")
		=> Assert.AreNotEqual(notExpected, actual, delta, message);

	/// <summary>
	/// Tests whether the code specified by delegate action throws exception of type <typeparamref name="T"/> (or derived).
	/// </summary>
	/// <typeparam name="T">The type of exception expected to be thrown.</typeparam>
	/// <param name="action">Delegate to code to be tested and which is expected to throw exception.</param>
	/// <param name="message">The message to include in the exception when action does not throw.</param>
	/// <returns>The exception that was thrown.</returns>
	protected static T Throws<T>(Action action, string message = "")
		where T : Exception
		=> Assert.Throws<T>(action, message);

	/// <summary>
	/// Tests whether the code specified by delegate action throws exact given exception of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of exception expected to be thrown.</typeparam>
	/// <param name="action">Delegate to code to be tested and which is expected to throw exception.</param>
	/// <param name="message">The message to include in the exception when action does not throw.</param>
	/// <returns>The exception that was thrown.</returns>
	protected static T ThrowsExactly<T>(Action action, string message = "")
		where T : Exception
		=> Assert.ThrowsExactly<T>(action, message);

	/// <summary>
	/// Tests whether the code specified by delegate action throws exception of type <typeparamref name="T"/> (or derived).
	/// </summary>
	/// <typeparam name="T">The type of exception expected to be thrown.</typeparam>
	/// <param name="action">Delegate to async code to be tested and which is expected to throw exception.</param>
	/// <param name="message">The message to include in the exception when action does not throw.</param>
	/// <returns>The exception that was thrown.</returns>
	protected static Task<T> ThrowsAsync<T>(Func<Task> action, string message = "")
		where T : Exception
		=> Assert.ThrowsAsync<T>(action, message);

	/// <summary>
	/// Tests whether the code specified by delegate action throws exact given exception of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of exception expected to be thrown.</typeparam>
	/// <param name="action">Delegate to async code to be tested and which is expected to throw exception.</param>
	/// <param name="message">The message to include in the exception when action does not throw.</param>
	/// <returns>The exception that was thrown.</returns>
	protected static Task<T> ThrowsExactlyAsync<T>(Func<Task> action, string message = "")
		where T : Exception
		=> Assert.ThrowsExactlyAsync<T>(action, message);

	/// <summary>
	/// Tests whether a string contains a specified substring.
	/// </summary>
	/// <param name="substring">Substring expected to be present.</param>
	/// <param name="value">The string to search.</param>
	/// <param name="message">Error message.</param>
	protected static void Contains(string substring, string value, string message = "")
		=> Assert.Contains(substring, value, message);

	/// <summary>
	/// Tests whether a string starts with the specified substring.
	/// </summary>
	/// <param name="substring">Substring expected at the beginning.</param>
	/// <param name="value">The string to search.</param>
	/// <param name="message">Error message.</param>
	protected static void StartsWith(string substring, string value, string message = "")
		=> Assert.StartsWith(substring, value, message);

	/// <summary>
	/// Tests whether a string ends with the specified substring.
	/// </summary>
	/// <param name="substring">Substring expected at the end.</param>
	/// <param name="value">The string to search.</param>
	/// <param name="message">Error message.</param>
	protected static void EndsWith(string substring, string value, string message = "")
		=> Assert.EndsWith(substring, value, message);

	/// <summary>
	/// Tests whether a string matches the specified regular expression.
	/// </summary>
	/// <param name="pattern">Regex pattern.</param>
	/// <param name="value">The string to match.</param>
	/// <param name="message">Error message.</param>
	protected static void MatchesRegex(Regex pattern, string value, string message = "")
		=> Assert.MatchesRegex(pattern, value, message);

	/// <summary>
	/// Tests whether a string does not match the specified regular expression.
	/// </summary>
	/// <param name="pattern">Regex pattern.</param>
	/// <param name="value">The string to test.</param>
	/// <param name="message">Error message.</param>
	protected static void DoesNotMatch(Regex pattern, string value, string message = "")
		=> Assert.DoesNotMatchRegex(pattern, value, message);

	/// <summary>
	/// Asserts that two collections are equal.
	/// </summary>
	/// <param name="expected">Expected collection.</param>
	/// <param name="actual">Actual collection.</param>
	/// <param name="message">Error message.</param>
	protected static void AreEqual(ICollection expected, ICollection actual, string message = "")
		=> CollectionAssert.AreEqual(expected, actual, message);

	/// <summary>
	/// Asserts that two collections are not equal.
	/// </summary>
	/// <param name="notExpected">Not expected collection.</param>
	/// <param name="actual">Actual collection.</param>
	/// <param name="message">Error message.</param>
	protected static void AreNotEqual(ICollection notExpected, ICollection actual, string message = "")
		=> CollectionAssert.AreNotEqual(notExpected, actual, message);

	/// <summary>
	/// Asserts that two collections contain the same elements.
	/// </summary>
	/// <param name="expected">Expected collection.</param>
	/// <param name="actual">Actual collection.</param>
	/// <param name="message">Error message.</param>
	protected static void AreEquivalent(ICollection expected, ICollection actual, string message = "")
		=> CollectionAssert.AreEquivalent(expected, actual, message);

	/// <summary>
	/// Asserts that two collections do not contain the same elements.
	/// </summary>
	/// <param name="notExpected">Not expected collection.</param>
	/// <param name="actual">Actual collection.</param>
	/// <param name="message">Error message.</param>
	protected static void AreNotEquivalent(ICollection notExpected, ICollection actual, string message = "")
		=> CollectionAssert.AreNotEquivalent(notExpected, actual, message);

	/// <summary>
	/// Asserts that the collection has the specified number of elements.
	/// </summary>
	/// <param name="count">Expected number of elements.</param>
	/// <param name="collection">Collection to check.</param>
	/// <param name="message">Error message.</param>
	protected static void HasCount(int count, ICollection collection, string message = "")
		=> Assert.HasCount(count, collection, message);

	/// <summary>
	/// Asserts that all elements are non-null.
	/// </summary>
	/// <param name="collection">Collection.</param>
	/// <param name="message">Error message.</param>
	protected static void AllItemsAreNotNull(ICollection collection, string message = "")
		=> CollectionAssert.AllItemsAreNotNull(collection, message);

	/// <summary>
	/// Asserts that all elements are unique.
	/// </summary>
	/// <param name="collection">Collection.</param>
	/// <param name="message">Error message.</param>
	protected static void AllItemsAreUnique(ICollection collection, string message = "")
		=> CollectionAssert.AllItemsAreUnique(collection, message);

	/// <summary>
	/// Asserts that all elements are instances of the specified type.
	/// </summary>
	/// <param name="collection">Collection.</param>
	/// <param name="expectedType">Expected type.</param>
	/// <param name="message">Error message.</param>
	protected static void AllItemsAreInstancesOfType(ICollection collection, Type expectedType, string message = "")
		=> CollectionAssert.AllItemsAreInstancesOfType(collection, expectedType, message);

	/// <summary>
	/// Asserts that the collection contains the specified element.
	/// </summary>
	/// <param name="collection">Collection.</param>
	/// <param name="element">Element expected to be present.</param>
	/// <param name="message">Error message.</param>
	protected static void Contains(ICollection collection, object element, string message = "")
		=> CollectionAssert.Contains(collection, element, message);

	/// <summary>
	/// Asserts that the collection does not contain the specified element.
	/// </summary>
	/// <param name="collection">Collection.</param>
	/// <param name="element">Element expected to be absent.</param>
	/// <param name="message">Error message.</param>
	protected static void DoesNotContain(ICollection collection, object element, string message = "")
		=> CollectionAssert.DoesNotContain(collection, element, message);

	/// <summary>
	/// Asserts that one collection is a subset of another.
	/// </summary>
	/// <param name="subset">Subset.</param>
	/// <param name="superset">Superset.</param>
	/// <param name="message">Error message.</param>
	protected static void IsSubsetOf(ICollection subset, ICollection superset, string message = "")
		=> CollectionAssert.IsSubsetOf(subset, superset, message);

	/// <summary>
	/// Asserts that one collection is not a subset of another.
	/// </summary>
	/// <param name="subset">Subset.</param>
	/// <param name="superset">Superset.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotSubsetOf(ICollection subset, ICollection superset, string message = "")
		=> CollectionAssert.IsNotSubsetOf(subset, superset, message);

	/// <summary>
	/// Tests whether the specified object is an instance of the expected type.
	/// </summary>
	/// <typeparam name="T">Expected type.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsInstanceOfType<T>(object value, string message = "")
		=> Assert.IsInstanceOfType<T>(value, message);

	/// <summary>
	/// Tests whether the specified object is not an instance of the expected type.
	/// </summary>
	/// <typeparam name="T">Wrong type.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotInstanceOfType<T>(object value, string message = "")
		=> Assert.IsNotInstanceOfType<T>(value, message);

	/// <summary>
	/// Tests whether the first value is greater than the second value.
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="actual">Actual value.</param>
	/// <param name="comparand">Value to compare against.</param>
	/// <param name="message">Error message.</param>
	protected static void IsGreater<T>(T actual, T comparand, string message = "")
		where T : IComparable<T>
	{
		if (actual.CompareTo(comparand) <= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected {actual} to be greater than {comparand}." : message);
	}

	/// <summary>
	/// Tests whether the first value is greater than or equal to the second value.
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="actual">Actual value.</param>
	/// <param name="comparand">Value to compare against.</param>
	/// <param name="message">Error message.</param>
	protected static void IsGreaterOrEqual<T>(T actual, T comparand, string message = "")
		where T : IComparable<T>
	{
		if (actual.CompareTo(comparand) < 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected {actual} to be greater than or equal to {comparand}." : message);
	}

	/// <summary>
	/// Tests whether the first value is less than the second value.
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="actual">Actual value.</param>
	/// <param name="comparand">Value to compare against.</param>
	/// <param name="message">Error message.</param>
	protected static void IsLess<T>(T actual, T comparand, string message = "")
		where T : IComparable<T>
	{
		if (actual.CompareTo(comparand) >= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected {actual} to be less than {comparand}." : message);
	}

	/// <summary>
	/// Tests whether the first value is less than or equal to the second value.
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="actual">Actual value.</param>
	/// <param name="comparand">Value to compare against.</param>
	/// <param name="message">Error message.</param>
	protected static void IsLessOrEqual<T>(T actual, T comparand, string message = "")
		where T : IComparable<T>
	{
		if (actual.CompareTo(comparand) > 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected {actual} to be less than or equal to {comparand}." : message);
	}

	/// <summary>
	/// Tests whether the value is within the specified range (inclusive).
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="actual">Actual value.</param>
	/// <param name="min">Minimum value (inclusive).</param>
	/// <param name="max">Maximum value (inclusive).</param>
	/// <param name="message">Error message.</param>
	protected static void IsInRange<T>(T actual, T min, T max, string message = "")
		where T : IComparable<T>
	{
		if (actual.CompareTo(min) < 0 || actual.CompareTo(max) > 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected {actual} to be in range [{min}, {max}]." : message);
	}

	/// <summary>
	/// Tests whether the value is outside the specified range.
	/// </summary>
	/// <typeparam name="T">Type of values.</typeparam>
	/// <param name="actual">Actual value.</param>
	/// <param name="min">Minimum value.</param>
	/// <param name="max">Maximum value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotInRange<T>(T actual, T min, T max, string message = "")
		where T : IComparable<T>
	{
		if (actual.CompareTo(min) >= 0 && actual.CompareTo(max) <= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected {actual} to be outside range [{min}, {max}]." : message);
	}

	/// <summary>
	/// Tests whether the string is empty.
	/// </summary>
	/// <param name="value">String value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsEmpty(string value, string message = "")
	{
		if (value is null)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected empty string but was null." : message);
		if (value.Length != 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected empty string but was \"{value}\"." : message);
	}

	/// <summary>
	/// Tests whether the string is not empty.
	/// </summary>
	/// <param name="value">String value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotEmpty(string value, string message = "")
	{
		if (value is null)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-empty string but was null." : message);
		if (value.Length == 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-empty string but was empty." : message);
	}

	/// <summary>
	/// Tests whether the collection is empty.
	/// </summary>
	/// <param name="collection">Collection.</param>
	/// <param name="message">Error message.</param>
	protected static void IsEmpty(ICollection collection, string message = "")
	{
		if (collection is null)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected empty collection but was null." : message);
		if (collection.Count != 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected empty collection but had {collection.Count} elements." : message);
	}

	/// <summary>
	/// Tests whether the collection is not empty.
	/// </summary>
	/// <param name="collection">Collection.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotEmpty(ICollection collection, string message = "")
	{
		if (collection is null)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-empty collection but was null." : message);
		if (collection.Count == 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-empty collection but was empty." : message);
	}

	/// <summary>
	/// Tests whether the string does not contain the specified substring.
	/// </summary>
	/// <param name="substring">Substring expected to be absent.</param>
	/// <param name="value">The string to search.</param>
	/// <param name="message">Error message.</param>
	protected static void DoesNotContain(string substring, string value, string message = "")
		=> Assert.DoesNotContain(substring, value, message);

	/// <summary>
	/// Tests whether the value is positive (greater than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsPositive(int value, string message = "")
	{
		if (value <= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected positive value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is positive (greater than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsPositive(long value, string message = "")
	{
		if (value <= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected positive value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is positive (greater than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsPositive(double value, string message = "")
	{
		if (value <= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected positive value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is positive (greater than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsPositive(decimal value, string message = "")
	{
		if (value <= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected positive value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is negative (less than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNegative(int value, string message = "")
	{
		if (value >= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected negative value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is negative (less than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNegative(long value, string message = "")
	{
		if (value >= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected negative value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is negative (less than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNegative(double value, string message = "")
	{
		if (value >= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected negative value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is negative (less than zero).
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNegative(decimal value, string message = "")
	{
		if (value >= 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected negative value but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsZero(int value, string message = "")
	{
		if (value != 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected zero but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsZero(long value, string message = "")
	{
		if (value != 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected zero but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsZero(double value, string message = "")
	{
		if (value != 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected zero but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsZero(decimal value, string message = "")
	{
		if (value != 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected zero but was {value}." : message);
	}

	/// <summary>
	/// Tests whether the value is not zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotZero(int value, string message = "")
	{
		if (value == 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-zero value but was 0." : message);
	}

	/// <summary>
	/// Tests whether the value is not zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotZero(long value, string message = "")
	{
		if (value == 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-zero value but was 0." : message);
	}

	/// <summary>
	/// Tests whether the value is not zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotZero(double value, string message = "")
	{
		if (value == 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-zero value but was 0." : message);
	}

	/// <summary>
	/// Tests whether the value is not zero.
	/// </summary>
	/// <param name="value">Value to check.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotZero(decimal value, string message = "")
	{
		if (value == 0)
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-zero value but was 0." : message);
	}

	/// <summary>
	/// Tests whether the string is null or empty.
	/// </summary>
	/// <param name="value">String value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNullOrEmpty(string value, string message = "")
	{
		if (!string.IsNullOrEmpty(value))
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected null or empty string but was \"{value}\"." : message);
	}

	/// <summary>
	/// Tests whether the string is not null or empty.
	/// </summary>
	/// <param name="value">String value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotNullOrEmpty(string value, string message = "")
	{
		if (string.IsNullOrEmpty(value))
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-null and non-empty string." : message);
	}

	/// <summary>
	/// Tests whether the string is null or whitespace.
	/// </summary>
	/// <param name="value">String value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNullOrWhiteSpace(string value, string message = "")
	{
		if (!string.IsNullOrWhiteSpace(value))
			Assert.Fail(string.IsNullOrEmpty(message) ? $"Expected null or whitespace string but was \"{value}\"." : message);
	}

	/// <summary>
	/// Tests whether the string is not null or whitespace.
	/// </summary>
	/// <param name="value">String value.</param>
	/// <param name="message">Error message.</param>
	protected static void IsNotNullOrWhiteSpace(string value, string message = "")
	{
		if (string.IsNullOrWhiteSpace(value))
			Assert.Fail(string.IsNullOrEmpty(message) ? "Expected non-null and non-whitespace string." : message);
	}
}
