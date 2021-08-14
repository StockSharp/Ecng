namespace Ecng.Common
{
	using System;
	using System.Threading;

	public class SyncObject
	{
		// TODO В .NET 4.5 поле стандартно в классе Timeout
		public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1);

		private bool _processed;
		private object _state;

		public bool TryEnter(TimeSpan? timeOut = null)
		{
			return timeOut is null ? Monitor.TryEnter(this) : Monitor.TryEnter(this, timeOut.Value);
		}

		public void Enter()
		{
			Monitor.Enter(this);
		}

		public void Exit()
		{
			Monitor.Exit(this);
		}

		public void Pulse()
		{
			Pulse(null);
		}

		public void Pulse(object state)
		{
			lock (this)
			{
				_state = state;
				Monitor.Pulse(this);
			}
		}

		public void PulseAll()
		{
			PulseAll(null);
		}

		public void PulseAll(object state)
		{
			lock (this)
			{
				_state = state;
				Monitor.PulseAll(this);
			}
		}

		public void PulseSignal(object state = null)
		{
			lock (this)
			{
				_processed = true;
				_state = state;
				Monitor.Pulse(this);
			}
		}

		public bool Wait(TimeSpan? timeOut = null)
		{
			lock (this)
				return WaitInternal(timeOut);
		}

		public bool WaitSignal(TimeSpan? timeOut = null)
		{
			return WaitSignal(timeOut, out _);
		}

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

		private bool WaitInternal(TimeSpan? timeOut)
		{
			return timeOut is null
				? Monitor.Wait(this)
				: Monitor.Wait(this, timeOut.Value);
		}
	}
}
