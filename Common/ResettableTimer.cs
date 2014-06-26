namespace Ecng.Common
{
	using System;
	using System.Threading;

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
				_timer = ThreadingHelper
					.Timer(() =>
					{
						if (!_changed)
						{
							_timer.Dispose();
							_timer = null;

							Elapsed.SafeInvoke();
						}
						else
							_changed = false;
					})
					.Interval(_period);
			}
			else
				_changed = true;
		}

		public void Flush()
		{
			if (_timer == null)
				return;

			_changed = false;

			_timer.Change(TimeSpan.Zero, _period);
		}
	}
}
