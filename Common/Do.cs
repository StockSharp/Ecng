namespace Ecng.Common;

using System;
using System.Globalization;

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
}