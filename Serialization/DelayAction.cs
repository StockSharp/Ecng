namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	public class DelayAction
	{
		private readonly IStorage _storage;
		private readonly Action<Exception> _errorHandler;

		private Timer _flushTimer;
		private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
		private bool _isFlushing;
		private readonly SynchronizedList<Tuple<Action, Action, bool, bool>> _actions = new SynchronizedList<Tuple<Action, Action, bool, bool>>();

		public DelayAction(IStorage storage, Action<Exception> errorHandler)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			if (errorHandler == null)
				throw new ArgumentNullException("errorHandler");

			_storage = storage;
			_errorHandler = errorHandler;
		}

		public void Add(Action action, bool canBatch = true, bool breakBatchOnError = true)
		{
			// breakBatchOnError
			// если мы сохраняем инструмент, то дать возможность сохраниться хоть каким-то инструментам, если среди них есть с ошибками

			Add(action, null, canBatch, breakBatchOnError);
		}

		public void Add(Action action, Action postAction, bool canBatch, bool breakBatchOnError)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			lock (_actions.SyncRoot)
			{
				_actions.Add(new Tuple<Action, Action, bool, bool>(action, postAction, canBatch, breakBatchOnError));

				if (!_isFlushing && _flushTimer == null)
				{
					_flushTimer = ThreadingHelper
						.Timer(OnFlush)
						.Interval(_flushInterval);
				}
			}
		}

		private void OnFlush()
		{
			try
			{
				Tuple<Action, Action, bool, bool>[] actions;

				lock (_actions.SyncRoot)
				{
					if (_isFlushing)
						return;

					_isFlushing = true;
					actions = _actions.CopyAndClear();
				}

				try
				{
					if (actions.Length > 0)
					{
						var list = new List<Tuple<Action, Action, bool, bool>>();

						var index = 0;
						while (index < actions.Length)
						{
							if (!actions[index].Item3)
							{
								BatchFlushAndClear(list);
								list.Clear();

								Flush(actions[index]);
							}
							else
								list.Add(actions[index]);

							index++;
						}

						BatchFlushAndClear(list);
					}
					else
					{
						if (_flushTimer != null)
						{
							_flushTimer.Dispose();
							_flushTimer = null;
						}
					}
				}
				finally
				{
					lock (_actions.SyncRoot)
						_isFlushing = false;
				}
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
			}
		}

		private void Flush(Tuple<Action, Action, bool, bool> action)
		{
			try
			{
				action.Item1();
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
			}

			if (action.Item2 != null)
				action.Item2();
		}

		private void BatchFlushAndClear(IEnumerable<Tuple<Action, Action, bool, bool>> actions)
		{
			if (actions.IsEmpty())
				return;

			try
			{
				using (var batch = _storage.BeginBatch())
				{
					foreach (var action in actions)
					{
						if (action.Item4)
							action.Item1();
						else
							Flush(action);
					}

					batch.Commit();
				}
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
			}

			actions.Where(a => a.Item2 != null).ForEach(a => a.Item2());
		}
	}
}