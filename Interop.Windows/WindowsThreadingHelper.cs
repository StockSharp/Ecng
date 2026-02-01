namespace Ecng.Interop;

using System;
using System.Threading;

using Ecng.Common;

/// <summary>
/// Provides helper methods to run code on threads with specific apartment states.
/// </summary>
public static class WindowsThreadingHelper
{
	/// <summary>
	/// Sets the apartment state of the specified thread to single-threaded apartment (STA).
	/// </summary>
	/// <param name="thread">The thread to set the apartment state for.</param>
	/// <returns>The same thread with the updated apartment state.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the thread is null.</exception>
	public static Thread STA(this Thread thread)
	{
		if (thread is null)
			throw new ArgumentNullException(nameof(thread));

		thread.SetApartmentState(ApartmentState.STA);
		return thread;
	}

	/// <summary>
	/// Sets the apartment state of the specified thread to multi-threaded apartment (MTA).
	/// </summary>
	/// <param name="thread">The thread to set the apartment state for.</param>
	/// <returns>The same thread with the updated apartment state.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the thread is null.</exception>
	public static Thread MTA(this Thread thread)
	{
		if (thread is null)
			throw new ArgumentNullException(nameof(thread));

		thread.SetApartmentState(ApartmentState.MTA);
		return thread;
	}

	/// <summary>
	/// Invokes the specified action on a new STA (single-threaded apartment) thread.
	/// </summary>
	/// <param name="action">The action to invoke.</param>
	/// <exception cref="ArgumentNullException">Thrown when the action is null.</exception>
	public static void InvokeAsSTA(this Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		InvokeAsSTA<object>(() =>
		{
			action();
			return null;
		});
	}

	// http://stackoverflow.com/questions/518701/clipboard-gettext-returns-null-empty-string

	/// <summary>
	/// Invokes the specified function on a new STA (single-threaded apartment) thread and returns a result.
	/// </summary>
	/// <typeparam name="T">The type of the return value.</typeparam>
	/// <param name="func">The function to invoke.</param>
	/// <returns>The result returned by the function.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the function is null.</exception>
	/// <exception cref="Exception">Throws any exception that occurs during the function execution.</exception>
	public static T InvokeAsSTA<T>(this Func<T> func)
	{
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		T retVal = default;
		Exception threadEx = null;

		var staThread = new Thread(() =>
		{
			try
			{
				retVal = func();
			}
			catch (Exception ex)
			{
				threadEx = ex;
			}
		}) { IsBackground = true };
		staThread.STA();
		staThread.Start();

		staThread.Join();

		if (threadEx != null)
			throw threadEx;

		return retVal;
	}
}