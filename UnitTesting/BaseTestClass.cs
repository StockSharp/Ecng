namespace Ecng.UnitTesting;

using System;
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
}