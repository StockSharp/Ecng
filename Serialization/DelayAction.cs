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
		private class Item
		{
			public Action Action { get; protected set; }
			public Action<Exception> PostAction { get; protected set; }
			public bool CanBatch { get; protected set; }
			public bool BreakBatchOnError { get; protected set; }

			protected Item()
			{
			}

			public Item(Action action, Action<Exception> postAction, bool canBatch, bool breakBatchOnError)
			{
				Action = action;
				PostAction = postAction;
				CanBatch = canBatch;
				BreakBatchOnError = breakBatchOnError;
			}
		}

		private class FlushItem : Item
		{
			private readonly SyncObject _syncObject = new SyncObject();
			private bool _isProcessed;
			private Exception _err;

			public FlushItem()
			{
				Action = () => {};
				PostAction = err =>
				{
					_err = err;

					lock (_syncObject)
					{
						_isProcessed = true;
						_syncObject.Pulse();
					}
				};
				CanBatch = true;
				BreakBatchOnError = true;
			}

			public void Wait()
			{
				lock (_syncObject)
				{
					if (!_isProcessed)
						_syncObject.Wait();
				}

				if (_err != null)
					throw _err;
			}
		}

		private readonly IStorage _storage;
		private readonly Action<Exception> _errorHandler;

		private Timer _flushTimer;
		private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
		private bool _isFlushing;
		private readonly SynchronizedList<Item> _actions = new SynchronizedList<Item>();

		public DelayAction(IStorage storage, Action<Exception> errorHandler)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			if (errorHandler == null)
				throw new ArgumentNullException("errorHandler");

			_storage = storage;
			_errorHandler = errorHandler;
		}

		public void Add(Action action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			lock (_actions.SyncRoot)
			{
				_actions.Add(new Item(action, postAction, canBatch, breakBatchOnError));

				if (!_isFlushing && _flushTimer == null)
				{
					_flushTimer = ThreadingHelper
						.Timer(OnFlush)
						.Interval(_flushInterval);
				}
			}
		}

		private void Add(Item item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			lock (_actions.SyncRoot)
			{
				_actions.Add(item);

				if (!_isFlushing && _flushTimer == null)
				{
					_flushTimer = ThreadingHelper
						.Timer(OnFlush)
						.Interval(_flushInterval);
				}
			}
		}

		public void WaitFlush()
		{
			var item = new FlushItem();
			Add(item);
			item.Wait();
		}

		public void OnFlush()
		{
			try
			{
				Item[] actions;

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
						var list = new List<Item>();

						var index = 0;
						while (index < actions.Length)
						{
							if (!actions[index].CanBatch)
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
					{
						_isFlushing = false;
						//_actions.SyncRoot.PulseAll();
					}
				}
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
			}
		}

		private void Flush(Item item)
		{
			Exception error;

			try
			{
				item.Action();
				error = null;
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
				error = ex;
			}

			if (item.PostAction != null)
				item.PostAction(error);
		}

		private void BatchFlushAndClear(ICollection<Item> actions)
		{
			if (actions.IsEmpty())
				return;

			Exception error;

			try
			{
				using (var batch = _storage.BeginBatch())
				{
					foreach (var action in actions)
					{
						if (action.BreakBatchOnError)
							action.Action();
						else
							Flush(action);
					}

					batch.Commit();
				}

				error = null;
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
				error = ex;
			}

			actions
				.Where(a => a.PostAction != null)
				.ForEach(a => a.PostAction(error));
		}
	}
}