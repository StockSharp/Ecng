﻿namespace Ecng.Common
{
	using System;
	using System.Threading;

	public class SimpleResettableTimer(TimeSpan period) : IDisposable
	{
		private readonly SyncObject _sync = new();
		private readonly TimeSpan _period = period;

		private Timer _timer;
		private bool _changed;

		public event Action Elapsed;

		public void Reset()
		{
			lock (_sync)
			{
				if (_timer is null)
				{
					_timer = ThreadingHelper
						.Timer(OnTimer)
						.Interval(_period);
				}
				else
					_changed = true;
			}
		}

		private void OnTimer()
		{
			var elapsed = false;

			lock (_sync)
			{
				if (!_changed)
				{
					if (_timer != null)
					{
						_timer.Dispose();
						_timer = null;
					}

					elapsed = true;
				}
				else
					_changed = false;
			}

			if (elapsed)
				Elapsed?.Invoke();
		}

		public void Flush()
		{
			lock (_sync)
			{
				if (_timer is null)
					return;

				_changed = false;
				_timer.Change(TimeSpan.Zero, _period);
			}
		}

		public void Dispose()
		{
			lock (_sync)
			{
				if (_timer is null)
					return;

				_changed = true;
				
				_timer.Dispose();
				_timer = null;
			}
		}
	}
}