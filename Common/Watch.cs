namespace Ecng.Common;

using System;
using System.Diagnostics;

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
		var watch = Stopwatch.StartNew();
		action();
		watch.Stop();
		return watch.Elapsed;
	}
}