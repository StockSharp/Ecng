namespace Ecng.ComponentModel;

using System;
using System.Threading;

using Ecng.Localization;

/// <summary>
/// Represents an asynchronous timeout for an operation.
/// </summary>
public class AsyncTimeout
{
	/// <summary>
	/// Gets the configured timeout value.
	/// </summary>
	public TimeSpan Value { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AsyncTimeout"/> class with the specified timeout value.
	/// </summary>
	/// <param name="value">The timeout duration. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
	public AsyncTimeout(TimeSpan value)
	{
		if (value <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

		Value = value;
	}

	/// <summary>
	/// Registers an action to be invoked when the timeout elapses.
	/// </summary>
	/// <param name="action">The action to execute on timeout.</param>
	public void Register(Action action)
	{
		var ct = new CancellationTokenSource(Value);
		ct.Token.Register(action, useSynchronizationContext: false);
	}
}