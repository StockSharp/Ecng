namespace Ecng.Common
{
	using System;
	using System.Threading;

	public class SyncObject
	{
		// TODO В .NET 4.5 поле стандартно в классе Timeout
		public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1);

		public bool TryEnter(TimeSpan? timeOut = null)
		{
			return timeOut == null ? Monitor.TryEnter(this) : Monitor.TryEnter(this, timeOut.Value);
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
			lock (this)
				Monitor.Pulse(this);
		}

		public void PulseAll()
		{
			lock (this)
				Monitor.PulseAll(this);
		}

		public void Wait(TimeSpan? timeOut = null)
		{
			lock (this)
			{
				if (timeOut == null)
					Monitor.Wait(this);
				else
					Monitor.Wait(this, timeOut.Value);
			}
		}
	}
}
