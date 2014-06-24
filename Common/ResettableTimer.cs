namespace Ecng.Common
{
	using System;
	using System.Timers;

	public class ResettableTimer
	{
		private readonly TimeSpan _period;

		private Timer _timer;
		private bool _changed;

		public event Action Elapsed;

		public ResettableTimer(TimeSpan period)
		{
			_period = period;
		}

		public void Reset()
		{
			if (_timer == null)
			{
				_timer = new Timer(_period.TotalMilliseconds);
				_timer.Elapsed += OnTimerElapsed;
				_timer.Start();
			}
			else
				_changed = true;
		}

		public void Flush()
		{
			if (_timer == null)
				return;

			_timer.Stop();
			_timer.Elapsed -= OnTimerElapsed;
			_timer = null;

			Elapsed.SafeInvoke();
		}

		private void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			if (!_changed)
			{
				Flush();
			}
			else
				_changed = false;
		}
	}
}
