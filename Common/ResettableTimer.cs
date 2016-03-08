namespace Ecng.Common
{
	using System;

	public class ResettableTimer : Disposable
	{
		private readonly SyncObject _sync = new SyncObject();
		private readonly SyncObject _finish = new SyncObject();
		private bool _isActivated;
		private bool _isFinished = true;
		private bool _isCancelled;

		private readonly TimeSpan _period;
		private readonly string _name;

		public ResettableTimer(TimeSpan period, string name)
		{
			_period = period;
			_name = name;
		}

		public event Action<Func<bool>> Elapsed;

		public void Activate()
		{
			lock (_sync)
			{
				_isActivated = true;

				if (!_isFinished)
					return;

				_isFinished = false;
				_isCancelled = false;
			}

			ThreadingHelper.Thread(() =>
			{
				try
				{
					while (!IsDisposed)
					{
						lock (_sync)
						{
							_isCancelled = false;

							if (_isActivated)
								_isActivated = false;
							else
							{
								_isFinished = true;
								break;
							}
						}

						Elapsed?.Invoke(CanProcess);
						_period.Sleep();
					}
				}
				finally
				{
					_finish.PulseAll();
				}
			}).Name(_name).Launch();
		}

		public void Cancel()
		{
			lock (_sync)
			{
				if (_isFinished)
					return;

				_isActivated = false;
				_isCancelled = true;
			}
		}

		public void Flush()
		{
			lock (_finish)
			{
				Activate();
				_finish.Wait();	
			}
		}

		private bool CanProcess()
		{
			return !_isCancelled && !IsDisposed;
		}

		protected override void DisposeManaged()
		{
			Cancel();
			base.DisposeManaged();
		}
	}
}