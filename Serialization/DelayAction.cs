namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	public class DelayAction : Disposable
	{
		public interface IGroup
		{
			void Add(Action action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true);
			void Add(Action<IDisposable> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true);
			void WaitFlush(bool dispose);
		}

		public interface IGroup<T> : IGroup
			where T : IDisposable
		{
			void Add(Action<T> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true);
			void Add<TState>(Action<T, TState> action, TState state, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true, Func<TState, TState, bool> compareStates = null);
		}

		private interface IInternalGroup
		{
			IDisposable Init();
			IGroupItem[] GetItemsAndClear();
		}

		private interface IGroupItem
		{
			void Do(IDisposable scope);
			Action<Exception> PostAction { get; }
			bool CanBatch { get; }
			bool BreakBatchOnError { get; }
		}

		private interface IInternalGroupItem
		{
			bool Equals(IGroupItem other);
			int GetStateHashCode();
		}

		private class Group<T> : IGroup<T>, IInternalGroup
			where T : IDisposable
		{
			private class Dummy : IDisposable
			{
				void IDisposable.Dispose()
				{
				}
			}

			private class Item<TState> : IGroupItem, IInternalGroupItem
			{
				private readonly TState _state;
				private readonly Func<TState, TState, bool> _compareStates;
				private readonly Action<T, TState> _action;

				public virtual void Do(IDisposable scope)
				{
					_action((T)scope, _state);
				}

				public Action<Exception> PostAction { get; protected set; }
				public bool CanBatch { get; protected set; }
				public bool BreakBatchOnError { get; protected set; }

				protected Item()
				{
				}

				public Item(Action<T, TState> action, TState state, Action<Exception> postAction, bool canBatch, bool breakBatchOnError, Func<TState, TState, bool> compareStates)
				{
					_state = state;
					_compareStates = compareStates;
					_action = action;

					PostAction = postAction;
					CanBatch = canBatch;
					BreakBatchOnError = breakBatchOnError;
				}

				bool IInternalGroupItem.Equals(IGroupItem other)
				{
					if (_compareStates == null)
						return false;

					var item = other as Item<TState>;

					if (item == null)
						return false;

					return _compareStates(_state, item._state);
				}

				int IInternalGroupItem.GetStateHashCode()
				{
					return typeof(TState).GetHashCode() ^ (_state == null ? 0 : 1) ^ (_compareStates == null ? 0 : 1);
				}
			}

			private class FlushItem : Item<object>
			{
				private readonly SyncObject _syncObject = new SyncObject();
				private bool _isProcessed;
				private Exception _err;

				public FlushItem(DelayAction parent, bool dispose)
				{
					if (parent == null)
						throw new ArgumentNullException(nameof(parent));

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

				public override void Do(IDisposable scope)
				{
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

			private readonly DelayAction _parent;
			private readonly Func<T> _init;
			private readonly SynchronizedList<IGroupItem> _actions = new SynchronizedList<IGroupItem>();
			private readonly Dummy _dummy = new Dummy();

			public Group(DelayAction parent, Func<T> init)
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

				Add((IDisposable s) => action(), postAction, canBatch, breakBatchOnError);
			}

			public void Add(Action<IDisposable> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				Add((T scope) => action(scope), postAction, canBatch, breakBatchOnError);
			}

			public void Add(Action<T> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
			{
				if (action == null)
					throw new ArgumentNullException(nameof(action));

				Add<object>((scope, state) => action(scope), null, postAction, canBatch, breakBatchOnError);
			}

			public void Add<TState>(Action<T, TState> action, TState state, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true, Func<TState, TState, bool> compareStates = null)
			{
				Add(new Item<TState>(action, state, postAction, canBatch, breakBatchOnError, compareStates));
			}

			private void Add<TState>(Item<TState> item)
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

			public IGroupItem[] GetItemsAndClear()
			{
				lock (_actions.SyncRoot)
					return _actions.CopyAndClear();
			}
		}

		private readonly Action<Exception> _errorHandler;

		private Timer _flushTimer;
		private bool _isFlushing;

		private readonly CachedSynchronizedList<IGroup> _groups = new CachedSynchronizedList<IGroup>();

		public DelayAction(Action<Exception> errorHandler)
		{
			if (errorHandler == null)
				throw new ArgumentNullException(nameof(errorHandler));

			_errorHandler = errorHandler;
			DefaultGroup = CreateGroup<IDisposable>(null);
		}

		public IGroup DefaultGroup { get; }

		private TimeSpan _flushInterval = TimeSpan.FromSeconds(1);

		public TimeSpan FlushInterval
		{
			get => _flushInterval;
			set
			{
				_flushInterval = value;

				lock (_groups.SyncRoot)
				{
					if (_flushTimer == null)
						return;

					_flushTimer.Interval(_flushInterval);
				}
			}
		}

		public IGroup<T> CreateGroup<T>(Func<T> init)
			where T : IDisposable
		{
			var group = new Group<T>(this, init);
			_groups.Add(group);
			return group;
		}

		public void DeleteGroup(IGroup group)
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
			get => _maxBatchSize;
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
				IGroup[] groups;

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
					Debug.WriteLine($"Groups: {groups.Length}");

					foreach (var group in groups)
					{
						if (IsDisposed)
							break;

						var items = ((IInternalGroup)group).GetItemsAndClear();

						if (items.Length == 0)
							continue;

						Debug.WriteLine($"Flushing: {items.Length}");

						hasItems = true;

						var list = new List<IGroupItem>();

						foreach (var item in items)
						{
							if (!item.CanBatch)
							{
								Debug.WriteLine($"!!! Iterrupt: {list.Count}");

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

		private void Flush(IGroupItem item)
		{
			Exception error;

			try
			{
				item.Do(null);
				error = null;
			}
			catch (Exception ex)
			{
				_errorHandler(ex);
				error = ex;
			}

			item.PostAction?.Invoke(error);
		}

		private class ItemComparer : IEqualityComparer<IGroupItem>
		{
			bool IEqualityComparer<IGroupItem>.Equals(IGroupItem x, IGroupItem y)
			{
				return ((IInternalGroupItem)x).Equals(y);
			}

			int IEqualityComparer<IGroupItem>.GetHashCode(IGroupItem obj)
			{
				return ((IInternalGroupItem)obj).GetStateHashCode();
			}
		}

		private static readonly IEqualityComparer<IGroupItem> _itemComparer = new ItemComparer();

		private void BatchFlushAndClear(IGroup group, ICollection<IGroupItem> actions)
		{
			if (actions.IsEmpty())
				return;

			Debug.WriteLine($"Batch: {actions.Count}");

			Exception error = null;

			try
			{
				using (var scope = ((IInternalGroup)group).Init())
				{
					foreach (var packet in actions.Distinct(_itemComparer).Batch(MaxBatchSize))
					{
						using (var batch = BeginBatch(group))
						{
							foreach (var action in packet)
							{
								if (action.BreakBatchOnError)
									action.Do(scope);
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

		protected virtual IBatchContext BeginBatch(IGroup group)
		{
			return _batchContext;
		}
	}
}