namespace Ecng.UnitTesting;

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

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
	protected static void IsTrue(bool condition, string message = "")
		=> Assert.IsTrue(condition, message);

	/// <summary>
	/// Tests whether the specified condition is false.
	/// </summary>
	protected static void IsFalse(bool condition, string message = "")
		=> Assert.IsFalse(condition, message);

	/// <summary>
	/// Tests whether the specified value is null.
	/// </summary>
	protected static void IsNull(object value, string message = "")
		=> Assert.IsNull(value, message);

	/// <summary>
	/// Tests whether the specified value is not null.
	/// </summary>
	protected static void IsNotNull(object value, string message = "")
		=> Assert.IsNotNull(value, message);

	/// <summary>
	/// Tests whether two values are equal.
	/// </summary>
	protected static void AreEqual<T>(T expected, T actual, string message = "")
		=> Assert.AreEqual(expected, actual, message);

	/// <summary>
	/// Tests whether two values are not equal.
	/// </summary>
	protected static void AreNotEqual<T>(T notExpected, T actual, string message = "")
		=> Assert.AreNotEqual(notExpected, actual, message);

	/// <summary>
	/// Tests whether two objects refer to the same instance.
	/// </summary>
	protected static void AreSame(object expected, object actual, string message = "")
		=> Assert.AreSame(expected, actual, message);

	/// <summary>
	/// Tests whether two objects do not refer to the same instance.
	/// </summary>
	protected static void AreNotSame(object notExpected, object actual, string message = "")
		=> Assert.AreNotSame(notExpected, actual, message);

	/// <summary>
	/// Tests whether the specified object is an instance of the expected type.
	/// </summary>
	protected static void IsInstanceOfType(object value, Type expectedType, string message = "")
		=> Assert.IsInstanceOfType(value, expectedType, message);

	/// <summary>
	/// Tests whether the specified object is not an instance of the expected type.
	/// </summary>
	protected static void IsNotInstanceOfType(object value, Type wrongType, string message = "")
		=> Assert.IsNotInstanceOfType(value, wrongType, message);

	/// <summary>
	/// Tests whether two strings are equal, ignoring case.
	/// </summary>
	protected static void AreEqual(string expected, string actual, bool ignoreCase, string message = "")
		=> Assert.AreEqual(expected, actual, ignoreCase, message);

	/// <summary>
	/// Tests whether two doubles are equal within the specified delta.
	/// </summary>
	protected static void AreEqual(double expected, double actual, double delta, string message = "")
		=> Assert.AreEqual(expected, actual, delta, message);

	/// <summary>
	/// Tests whether two floats are equal within the specified delta.
	/// </summary>
	protected static void AreEqual(float expected, float actual, float delta, string message = "")
		=> Assert.AreEqual(expected, actual, delta, message);

	/// <summary>
	/// Tests whether two doubles are not equal within the specified delta.
	/// </summary>
	protected static void AreNotEqual(double notExpected, double actual, double delta, string message = "")
		=> Assert.AreNotEqual(notExpected, actual, delta, message);

	/// <summary>
	/// Tests whether two floats are not equal within the specified delta.
	/// </summary>
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
	protected static void Contains(string value, string substring, string message = "")
		=> Assert.Contains(value, substring, message);

	/// <summary>
	/// Tests whether a string starts with the specified substring.
	/// </summary>
	protected static void StartsWith(string value, string substring, string message = "")
		=> Assert.StartsWith(value, substring, message);

	/// <summary>
	/// Tests whether a string ends with the specified substring.
	/// </summary>
	protected static void EndsWith(string value, string substring, string message = "")
		=> Assert.EndsWith(value, substring, message);

	/// <summary>
	/// Asserts that two collections are equal.
	/// </summary>
	protected static void AreEqual(ICollection expected, ICollection actual, string message = "")
		=> CollectionAssert.AreEqual(expected, actual, message);

	/// <summary>
	/// Asserts that two collections are not equal.
	/// </summary>
	protected static void AreNotEqual(ICollection notExpected, ICollection actual, string message = "")
		=> CollectionAssert.AreNotEqual(notExpected, actual, message);

	/// <summary>
	/// Asserts that two collections contain the same elements.
	/// </summary>
	protected static void AreEquivalent(ICollection expected, ICollection actual, string message = "")
		=> CollectionAssert.AreEquivalent(expected, actual, message);

	/// <summary>
	/// Asserts that two collections do not contain the same elements.
	/// </summary>
	protected static void AreNotEquivalent(ICollection notExpected, ICollection actual, string message = "")
		=> CollectionAssert.AreNotEquivalent(notExpected, actual, message);

	/// <summary>
	/// Asserts that all elements are non-null.
	/// </summary>
	protected static void AllItemsAreNotNull(ICollection collection, string message = "")
		=> CollectionAssert.AllItemsAreNotNull(collection, message);

	/// <summary>
	/// Asserts that all elements are unique.
	/// </summary>
	protected static void AllItemsAreUnique(ICollection collection, string message = "")
		=> CollectionAssert.AllItemsAreUnique(collection, message);

	/// <summary>
	/// Asserts that all elements are instances of the specified type.
	/// </summary>
	protected static void AllItemsAreInstancesOfType(ICollection collection, Type expectedType, string message = "")
		=> CollectionAssert.AllItemsAreInstancesOfType(collection, expectedType, message);

	/// <summary>
	/// Asserts that the collection contains the specified element.
	/// </summary>
	protected static void Contains(ICollection collection, object element, string message = "")
		=> CollectionAssert.Contains(collection, element, message);

	/// <summary>
	/// Asserts that the collection does not contain the specified element.
	/// </summary>
	protected static void DoesNotContain(ICollection collection, object element, string message = "")
		=> CollectionAssert.DoesNotContain(collection, element, message);

	/// <summary>
	/// Asserts that one collection is a subset of another.
	/// </summary>
	protected static void IsSubsetOf(ICollection subset, ICollection superset, string message = "")
		=> CollectionAssert.IsSubsetOf(subset, superset, message);

	/// <summary>
	/// Asserts that one collection is not a subset of another.
	/// </summary>
	protected static void IsNotSubsetOf(ICollection subset, ICollection superset, string message = "")
		=> CollectionAssert.IsNotSubsetOf(subset, superset, message);
}