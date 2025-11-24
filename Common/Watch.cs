namespace Ecng.Common;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// Provides utility methods for timing the execution of code.
/// </summary>
public static class Watch
{
	/// <summary>
	/// Executes the specified action, measures its execution time, and returns the elapsed time.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	/// <returns>
	/// A <see cref="TimeSpan"/> representing the elapsed time during the execution of the action.
	/// </returns>
	public static TimeSpan Do(Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		var watch = Stopwatch.StartNew();
		action();
		watch.Stop();
		return watch.Elapsed;
	}

	/// <summary>
	/// Executes the specified asynchronous function, measures its execution time, and returns the elapsed time.
	/// </summary>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <returns>
	/// A <see cref="Task{TimeSpan}"/> representing the asynchronous operation that returns the elapsed time during the execution of the function.
	/// </returns>
	public static async Task<TimeSpan> DoAsync(Func<Task> func)
	{
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		var watch = Stopwatch.StartNew();
		await func().NoWait();
		watch.Stop();
		return watch.Elapsed;
	}

	/// <summary>
	/// Executes the specified asynchronous function, measures its execution time, and returns both the result and the elapsed time.
	/// </summary>
	/// <typeparam name="T">The type of the result returned by the asynchronous function.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <returns>
	/// A <see cref="Task"/> representing the asynchronous operation that returns a tuple containing the result and the elapsed time during the execution of the function.
	/// </returns>
	public static async Task<(T Result, TimeSpan Elapsed)> DoAsync<T>(Func<Task<T>> func)
	{
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		var watch = Stopwatch.StartNew();
		var result = await func().NoWait();
		watch.Stop();
		return (result, watch.Elapsed);
	}

	/// <summary>
	/// Executes the specified asynchronous function that returns a <see cref="ValueTask"/>, measures its execution time, and returns the elapsed time.
	/// </summary>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <returns>
	/// A <see cref="ValueTask{TimeSpan}"/> representing the asynchronous operation that returns the elapsed time during the execution of the function.
	/// </returns>
	public static async ValueTask<TimeSpan> DoAsync(Func<ValueTask> func)
	{
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		var watch = Stopwatch.StartNew();
		await func().NoWait();
		watch.Stop();
		return watch.Elapsed;
	}

	/// <summary>
	/// Executes the specified asynchronous function that returns a <see cref="ValueTask{T}"/>, measures its execution time, and returns both the result and the elapsed time.
	/// </summary>
	/// <typeparam name="T">The type of the result returned by the asynchronous function.</typeparam>
	/// <param name="func">The asynchronous function to execute.</param>
	/// <returns>
	/// A <see cref="ValueTask"/> representing the asynchronous operation that returns a tuple containing the result and the elapsed time during the execution of the function.
	/// </returns>
	public static async ValueTask<(T Result, TimeSpan Elapsed)> DoAsync<T>(Func<ValueTask<T>> func)
	{
		if (func is null)
			throw new ArgumentNullException(nameof(func));

		var watch = Stopwatch.StartNew();
		var result = await func().NoWait();
		watch.Stop();
		return (result, watch.Elapsed);
	}
}