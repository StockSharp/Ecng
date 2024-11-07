namespace Ecng.ComponentModel;

using System;
using System.Threading;

public class AsyncTimeout
{
    public TimeSpan Value { get; }

	public AsyncTimeout(TimeSpan value)
	{
		if (value <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

		Value = value;
	}

	public void Register(Action action)
	{
		var ct = new CancellationTokenSource(Value);
		ct.Token.Register(action, useSynchronizationContext: false);
	}
}