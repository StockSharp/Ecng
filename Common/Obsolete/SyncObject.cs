namespace Ecng.Common;

using System;
using System.Threading;

/// <summary>
/// Provides a synchronization object that encapsulates thread synchronization mechanisms using Monitor.
/// </summary>
public class SyncObject
#if !NET9_0_OR_GREATER
	: Lock
#endif
{
#if !NET9_0_OR_GREATER
	/// <summary>
	/// Attempts to enter the synchronization block, with an optional timeout.
	/// </summary>
	/// <param name="timeOut">The timeout duration. If null, the method does not specify a timeout.</param>
	/// <returns>true if the synchronization lock was successfully acquired; otherwise, false.</returns>
	public bool TryEnter(TimeSpan? timeOut = null)
	{
		return timeOut is null ? Monitor.TryEnter(this) : Monitor.TryEnter(this, timeOut.Value);
	}
#endif

	/// <summary>
	/// Sends a pulse to a single waiting thread.
	/// </summary>
	public void Pulse()
	{
		lock (this)
			Monitor.Pulse(this);
	}

	/// <summary>
	/// Sends a pulse to all waiting threads.
	/// </summary>
	public void PulseAll()
	{
		lock (this)
			Monitor.PulseAll(this);
	}

	/// <summary>
	/// Waits for a pulse with an optional timeout.
	/// </summary>
	/// <param name="timeOut">The timeout duration. If null, waits indefinitely.</param>
	/// <returns>true if the pulse was received; otherwise, false (in case of timeout).</returns>
	public bool Wait(TimeSpan? timeOut = null)
	{
		lock (this)
		{
			return timeOut is null
				? Monitor.Wait(this)
				: Monitor.Wait(this, timeOut.Value);
		}
	}
}