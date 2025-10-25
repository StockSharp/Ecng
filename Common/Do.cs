namespace Ecng.Common;

using System;
using System.Globalization;
using System.Threading.Tasks;

/// <summary>
/// Provides utility methods to execute functions and actions under the invariant culture.
/// </summary>
public static class Do
{
	/// <summary>
	/// Executes the specified function under the invariant culture and returns its result.
	/// </summary>
	/// <typeparam name="T">The type of the result produced by the function.</typeparam>
	/// <param name="func">The function to execute.</param>
	/// <returns>The result of executing the function.</returns>
	public static T Invariant<T>(Func<T> func)
		=> CultureInfo.InvariantCulture.DoInCulture(func);

	/// <summary>
	/// Executes the specified action under the invariant culture.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	public static void Invariant(Action action)
		=> CultureInfo.InvariantCulture.DoInCulture(action);

	/// <summary>
	/// Executes the specified asynchronous function under the invariant culture and returns its result.
	/// </summary>
	/// <typeparam name="T">The type of the result produced by the asynchronous function.</typeparam>
	/// <param name="funcAsync">The asynchronous function to execute.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	public static async Task<T> InvariantAsync<T>(Func<Task<T>> funcAsync)
	{
		if (funcAsync is null)
			throw new ArgumentNullException(nameof(funcAsync));

		using (ThreadingHelper.WithInvariantCulture())
			return await funcAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Executes the specified asynchronous action under the invariant culture.
	/// </summary>
	/// <param name="actionAsync">The asynchronous action to execute.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task InvariantAsync(Func<Task> actionAsync)
	{
		if (actionAsync is null)
			throw new ArgumentNullException(nameof(actionAsync));

		using (ThreadingHelper.WithInvariantCulture())
			await actionAsync().ConfigureAwait(false);
	}
}