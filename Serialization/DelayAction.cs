namespace Ecng.Serialization
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
		public class Group
		{
			private class Dummy : IDisposable
			{
				void IDisposable.Dispose()
				{
				}
			}

			private readonly DelayAction _parent;
			private readonly Func<IDisposable> _init;
			private readonly SynchronizedList<Item> _actions = new SynchronizedList<Item>();
			private readonly Dummy _dummy = new Dummy();

			internal Group(DelayAction parent, Func<IDisposable> init)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
				_init = init;
			}

			public IDisposable Init()
			{
				if (_init == null)
					return _dummy;

				var state = _init.Invoke();

				if (state == null)
					throw new InvalidOperationException();

				return state;
			}

			public void Add(Action action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				Add(s => action(), postAction, canBatch, breakBatchOnError);
			}

			public void Add(Action<IDisposable> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
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
					_actions.Add(item);

				_parent.TryCreateTimer();
			}

			public void WaitFlush(bool dispose)
			{
				var item = new FlushItem(_parent, dispose);
				Add(item);
				item.Wait();
			}

			internal Item[] GetItemsAndClear()
			{
				lock (_actions.SyncRoot)
					return _actions.CopyAndClear();
			}
		}

		internal class Item
		{
			public Action<IDisposable> Action { get; protected set; }
			public Action<Exception> PostAction { get; protected set; }
			public bool CanBatch { get; protected set; }
			public bool BreakBatchOnError { get; protected set; }

			protected Item()
			{
			}

			public Item(Action<IDisposable> action, Action<Exception> postAction, bool canBatch, bool breakBatchOnError)
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

				Action = s => {};
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

		private readonly CachedSynchronizedList<Group> _groups = new CachedSynchronizedList<Group>();

		public DelayAction(Action<Exception> errorHandler)
		{
			if (errorHandler == null)
				throw new ArgumentNullException(nameof(errorHandler));

			_errorHandler = errorHandler;
			DefaultGroup = CreateGroup();
		}

		public Group DefaultGroup { get; }

		public Group CreateGroup(Func<IDisposable> init = null)
		{
			var group = new Group(this, init);
			_groups.Add(group);
			return group;
		}

		public void DeleteGroup(Group group)
		{
			if (group == null)
				throw new ArgumentNullException(nameof(group));

			if (group == DefaultGroup)
				throw new ArgumentException();

			_groups.Remove(group);
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

		private void TryCreateTimer()
		{
			lock (_groups.SyncRoot)
			{
				if (!_isFlushing && _flushTimer == null)
				{
					_flushTimer = ThreadingHelper
						.Timer(() => CultureInfo.InvariantCulture.DoInCulture(OnFlush))
						.Interval(_flushInterval);
				}
			}
		}

		public void OnFlush()
		{
			try
			{
				Group[] groups;

				lock (_groups.SyncRoot)
				{
					if (_isFlushing)
						return;

					_isFlushing = true;

					groups = _groups.Cache;
				}

				var hasItems = false;

				try
				{
					foreach (var group in groups)
					{
						if (IsDisposed)
							break;

						var items = group.GetItemsAndClear();

						if (items.Length == 0)
							continue;

						hasItems = true;

						var list = new List<Item>();

						foreach (var item in items)
						{
							if (!item.CanBatch)
							{
								BatchFlushAndClear(group, list);
								list.Clear();

								if (IsDisposed)
									break;

								Flush(item);
							}
							else
								list.Add(item);
						}

						if (!IsDisposed)
							BatchFlushAndClear(group, list);
					}
				}
				finally
				{
					lock (_groups.SyncRoot)
					{
						_isFlushing = false;

						if (!hasItems)
						{
							if (_flushTimer != null)
							{
								_flushTimer.Dispose();
								_flushTimer = null;
							}
						}
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
				item.Action(null);
				error = null;
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
				error = ex;
			}

			item.PostAction?.Invoke(error);
		}

		private void BatchFlushAndClear(Group group, ICollection<Item> actions)
		{
			if (actions.IsEmpty())
				return;

			Exception error = null;

			try
			{
				using (var state = group.Init())
				{
					foreach (var packet in actions.Batch(MaxBatchSize))
					{
						using (var batch = BeginBatch(group))
						{
							foreach (var action in packet)
							{
								if (action.BreakBatchOnError)
									action.Action(state);
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

		protected virtual IBatchContext BeginBatch(Group group)
		{
			return _batchContext;
		}
	}
}