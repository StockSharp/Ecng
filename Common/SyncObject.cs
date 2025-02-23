namespace Ecng.Common
{
	using System;
	using System.Threading;

	/// <summary>
	/// Provides a synchronization object that encapsulates thread synchronization mechanisms using Monitor.
	/// </summary>
	public class SyncObject
	{
		private bool _processed;
		private object _state;

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

		/// <summary>
		/// Sends a pulse to a single waiting thread.
		/// </summary>
		public void Pulse()
		{
			Pulse(null);
		}

		/// <summary>
		/// Sends a pulse to a single waiting thread and sets the optional state.
		/// </summary>
		/// <param name="state">An optional state object to pass along with the pulse.</param>
		public void Pulse(object state)
		{
			lock (this)
			{
				_state = state;
				Monitor.Pulse(this);
			}
		}

		/// <summary>
		/// Sends a pulse to all waiting threads.
		/// </summary>
		public void PulseAll()
		{
			PulseAll(null);
		}

		/// <summary>
		/// Sends a pulse to all waiting threads and sets the optional state.
		/// </summary>
		/// <param name="state">An optional state object to pass along with the pulse.</param>
		public void PulseAll(object state)
		{
			lock (this)
			{
				_state = state;
				Monitor.PulseAll(this);
			}
		}

		/// <summary>
		/// Signals a waiting thread by sending a pulse along with setting the processed flag.
		/// </summary>
		/// <param name="state">An optional state object to pass along with the pulse.</param>
		public void PulseSignal(object state = null)
		{
			lock (this)
			{
				_processed = true;
				_state = state;
				Monitor.Pulse(this);
			}
		}

		/// <summary>
		/// Waits for a pulse with an optional timeout.
		/// </summary>
		/// <param name="timeOut">The timeout duration. If null, waits indefinitely.</param>
		/// <returns>true if the pulse was received; otherwise, false (in case of timeout).</returns>
		public bool Wait(TimeSpan? timeOut = null)
		{
			lock (this)
				return WaitInternal(timeOut);
		}

		/// <summary>
		/// Waits for a signal with an optional timeout.
		/// </summary>
		/// <param name="timeOut">The timeout duration. If null, waits indefinitely.</param>
		/// <returns>true if the signal was received; otherwise, false (in case of timeout).</returns>
		public bool WaitSignal(TimeSpan? timeOut = null)
		{
			return WaitSignal(timeOut, out _);
		}

		/// <summary>
		/// Waits for a signal with an optional timeout and outputs the state associated with the signal.
		/// </summary>
		/// <param name="timeOut">The timeout duration. If null, waits indefinitely.</param>
		/// <param name="state">The state object passed by the signaling call.</param>
		/// <returns>true if a signal was received; otherwise, false (in case of timeout).</returns>
		public bool WaitSignal(TimeSpan? timeOut, out object state)
		{
			lock (this)
			{
				var result = _processed || WaitInternal(timeOut);
				_processed = false;
				state = _state;
				return result;
			}
		}

		/// <summary>
		/// Waits internally for a pulse with an optional timeout.
		/// </summary>
		/// <param name="timeOut">The timeout duration. If null, waits indefinitely.</param>
		/// <returns>true if the pulse was received; otherwise, false (in case of timeout).</returns>
		private bool WaitInternal(TimeSpan? timeOut)
		{
			return timeOut is null
				? Monitor.Wait(this)
				: Monitor.Wait(this, timeOut.Value);
		}
	}
}
