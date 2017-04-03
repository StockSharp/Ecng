﻿namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	public class DelayAction : Disposable
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

			public FlushItem(DelayAction parent, bool dispose)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				Action = () => {};
				PostAction = err =>
				{
					_err = err;

					if (dispose)
						parent.Dispose();

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

		private readonly Action<Exception> _errorHandler;

		private Timer _flushTimer;
		private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
		private bool _isFlushing;
		private readonly SynchronizedList<Item> _actions = new SynchronizedList<Item>();

		public DelayAction(Action<Exception> errorHandler)
		{
			if (errorHandler == null)
				throw new ArgumentNullException(nameof(errorHandler));

			_errorHandler = errorHandler;
		}

		private int _maxBatchSize = 1000;

		public int MaxBatchSize
		{
			get { return _maxBatchSize; }
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				_maxBatchSize = value;
			}
		}

		public void Add(Action action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			Add(new Item(action, postAction, canBatch, breakBatchOnError));
		}

		private void Add(Item item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (_actions.SyncRoot)
			{
				_actions.Add(item);
				TryCreateTimer();
			}
		}

		private void TryCreateTimer()
		{
			if (!_isFlushing && _flushTimer == null)
			{
				_flushTimer = ThreadingHelper
					.Timer(() => CultureInfo.InvariantCulture.DoInCulture(OnFlush))
					.Interval(_flushInterval);
			}
		}

		public void WaitFlush(bool dispose)
		{
			var item = new FlushItem(this, dispose);
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

								if (IsDisposed)
									break;

								Flush(actions[index]);
							}
							else
								list.Add(actions[index]);

							index++;
						}

						if (!IsDisposed)
							BatchFlushAndClear(list);
					}
					else
					{
						if (_flushTimer == null)
							return;

						_flushTimer.Dispose();
						_flushTimer = null;
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

			item.PostAction?.Invoke(error);
		}

		private void BatchFlushAndClear(ICollection<Item> actions)
		{
			if (actions.IsEmpty())
				return;

			Exception error = null;

			try
			{
				foreach (var packet in actions.Batch(MaxBatchSize))
				{
					using (var batch = BeginBatch())
					{
						foreach (var action in packet)
						{
							if (action.BreakBatchOnError)
								action.Action();
							else
								Flush(action);

							if (IsDisposed)
								break;
						}

						batch.Commit();
					}

					if (IsDisposed)
						break;
				}
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

		private class DummyBatchContext : IBatchContext
		{
			void IDisposable.Dispose()
			{
			}

			void IBatchContext.Commit()
			{
			}
		}

		private readonly DummyBatchContext _batchContext = new DummyBatchContext();

		protected virtual IBatchContext BeginBatch()
		{
			return _batchContext;
		}
	}
}