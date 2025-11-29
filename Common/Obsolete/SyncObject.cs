namespace Ecng.Common;

using System;
using System.Threading;

/// <summary>
/// Provides a synchronization object that encapsulates thread synchronization mechanisms using Monitor.
/// </summary>
public class SyncObject
{
#if !NET9_0_OR_GREATER
	/// <summary>
	/// A disposable scope that exits the synchronization block when disposed.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="Scope"/> struct.
	/// </remarks>
	/// <param name="owner">The <see cref="SyncObject"/> instance that owns this scope.</param>
	public readonly struct Scope(SyncObject owner) : IDisposable
	{
		void IDisposable.Dispose() => Monitor.Exit(owner);
	}

	/// <summary>
	/// Enters the synchronization block and returns a disposable scope to exit it.
	/// </summary>
	public Scope EnterScope()
	{
		Monitor.Enter(this);
		return new Scope(this);
	}

	/// <summary>
	/// Attempts to enter the synchronization block, with an optional timeout.
	/// </summary>
	/// <param name="timeOut">The timeout duration. If null, the method does not specify a timeout.</param>
	/// <returns>true if the synchronization lock was successfully acquired; otherwise, false.</returns>
	public bool TryEnter(TimeSpan? timeOut = null)
	{
		return timeOut is null ? Monitor.TryEnter(this) : Monitor.TryEnter(this, timeOut.Value);
	}

	/// <summary>
	/// Enters the synchronization block, waiting indefinitely until the lock is acquired.
	/// </summary>
	public void Enter()
	{
		Monitor.Enter(this);
	}

	/// <summary>
	/// Exits the synchronization block.
	/// </summary>
	public void Exit()
	{
		Monitor.Exit(this);
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