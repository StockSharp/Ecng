namespace Ecng.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ecng.Collections;
using Ecng.Common;

/// <summary>
/// Provides delayed action execution with batching support.
/// </summary>
[Obsolete("Use Channels instead.")]
public class DelayAction : Disposable
{
	/// <summary>
	/// Represents a group of delayed actions.
	/// </summary>
	public interface IGroup
	{
		/// <summary>
		/// Adds an action to the group.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="postAction">An action to execute after the main action, with an exception parameter if an error occurred.</param>
		/// <param name="canBatch">Determines if the action can be batched.</param>
		/// <param name="breakBatchOnError">Determines if batching should break on error.</param>
		void Add(Action action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true);

		/// <summary>
		/// Adds an action that receives an IDisposable scope to the group.
		/// </summary>
		/// <param name="action">The action to execute with the provided scope.</param>
		/// <param name="postAction">An action to execute after the main action, with an exception parameter if an error occurred.</param>
		/// <param name="canBatch">Determines if the action can be batched.</param>
		/// <param name="breakBatchOnError">Determines if batching should break on error.</param>
		void Add(Action<IDisposable> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true);

		/// <summary>
		/// Waits until all actions in the group have been flushed.
		/// </summary>
		/// <param name="dispose">Determines if the DelayAction instance should be disposed after flushing.</param>
		void WaitFlush(bool dispose);
	}

	/// <summary>
	/// Represents a group of delayed actions with a specific group state.
	/// </summary>
	/// <typeparam name="T">The type of the group state, which implements IDisposable.</typeparam>
	public interface IGroup<T> : IGroup
		where T : IDisposable
	{
		/// <summary>
		/// Adds an action that receives the group state.
		/// </summary>
		/// <param name="action">The action to execute with the group state.</param>
		/// <param name="postAction">An action to execute after the main action, with an exception parameter if an error occurred.</param>
		/// <param name="canBatch">Determines if the action can be batched.</param>
		/// <param name="breakBatchOnError">Determines if batching should break on error.</param>
		void Add(Action<T> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true);

		/// <summary>
		/// Adds an action that receives the group state and an additional state parameter.
		/// </summary>
		/// <typeparam name="TState">The type of the additional state.</typeparam>
		/// <param name="action">The action to execute with the group state and additional state.</param>
		/// <param name="state">The additional state for the action.</param>
		/// <param name="postAction">An action to execute after the main action, with an exception parameter if an error occurred.</param>
		/// <param name="canBatch">Determines if the action can be batched.</param>
		/// <param name="breakBatchOnError">Determines if batching should break on error.</param>
		/// <param name="compareStates">A function to compare two state values for batching decisions.</param>
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

	private class Group<T>(DelayAction parent, Func<T> init) : IGroup<T>, IInternalGroup
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
				if (_compareStates is null)
					return false;


				if (other is not Item<TState> item)
					return false;

				return _compareStates(_state, item._state);
			}

			int IInternalGroupItem.GetStateHashCode()
			{
				return typeof(TState).GetHashCode() ^ (_state is null ? 0 : 1) ^ (_compareStates is null ? 0 : 1);
			}
		}

		private class FlushItem : Item<object>
		{
			private readonly object _syncObject = new();
			private bool _isProcessed;
			private Exception _err;

			/// <summary>
			/// Initializes a new instance of the FlushItem class.
			/// </summary>
			/// <param name="parent">The parent DelayAction.</param>
			/// <param name="dispose">Indicates if the parent should be disposed after flushing.</param>
			public FlushItem(DelayAction parent, bool dispose)
			{
				if (parent is null)
					throw new ArgumentNullException(nameof(parent));

				PostAction = err =>
				{
					_err = err;

					if (dispose)
						parent.Dispose();

					lock (_syncObject)
					{
						_isProcessed = true;
						Monitor.Pulse(_syncObject);
					}
				};
				CanBatch = true;
				BreakBatchOnError = true;
			}

			public override void Do(IDisposable scope)
			{
			}

			/// <summary>
			/// Waits for this flush item to be processed.
			/// </summary>
			public void Wait()
			{
				lock (_syncObject)
				{
					if (!_isProcessed)
						Monitor.Wait(_syncObject);
				}

				if (_err != null)
					throw _err;
			}
		}

		private readonly DelayAction _parent = parent ?? throw new ArgumentNullException(nameof(parent));
		private readonly Func<T> _init = init;
		private readonly SynchronizedList<IGroupItem> _actions = [];
		private readonly Dummy _dummy = new();

		public IDisposable Init()
		{
			if (_init is null)
				return _dummy;

			var state = _init.Invoke();

			if (state is null)
				throw new InvalidOperationException();

			return state;
		}

		public void Add(Action action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			Add((IDisposable s) => action(), postAction, canBatch, breakBatchOnError);
		}

		public void Add(Action<IDisposable> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			Add((T scope) => action(scope), postAction, canBatch, breakBatchOnError);
		}

		public void Add(Action<T> action, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			Add<object>((scope, state) => action(scope), null, postAction, canBatch, breakBatchOnError);
		}

		public void Add<TState>(Action<T, TState> action, TState state, Action<Exception> postAction = null, bool canBatch = true, bool breakBatchOnError = true, Func<TState, TState, bool> compareStates = null)
		{
			Add(new Item<TState>(action, state, postAction, canBatch, breakBatchOnError, compareStates));
		}

		private void Add<TState>(Item<TState> item)
		{
			if (item is null)
				throw new ArgumentNullException(nameof(item));

			using (_actions.SyncRoot.EnterScope())
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
			=> _actions.CopyAndClear();
	}

	private readonly Action<Exception> _errorHandler;

	private ControllablePeriodicTimer _flushTimer;
	private bool _isFlushing;

	private readonly CachedSynchronizedList<IGroup> _groups = [];

	/// <summary>
	/// Initializes a new instance of the DelayAction class.
	/// </summary>
	/// <param name="errorHandler">A delegate to handle errors that occur during execution.</param>
	public DelayAction(Action<Exception> errorHandler)
	{
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		DefaultGroup = CreateGroup<IDisposable>(null);
	}

	/// <summary>
	/// Gets the default group for delayed actions.
	/// </summary>
	public IGroup DefaultGroup { get; }

	private TimeSpan _flushInterval = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets the flush interval for batching actions.
	/// </summary>
	public TimeSpan FlushInterval
	{
		get => _flushInterval;
		set
		{
			_flushInterval = value;

			using (_groups.SyncRoot.EnterScope())
			{
				if (_flushTimer is null)
					return;

				_flushTimer.ChangeInterval(_flushInterval);
			}
		}
	}

	/// <summary>
	/// Creates a new group of delayed actions with a specific state.
	/// </summary>
	/// <typeparam name="T">The type of the group state, which implements IDisposable.</typeparam>
	/// <param name="init">
	/// A function to initialize the group state.
	/// If null, a dummy state is used.
	/// </param>
	/// <returns>A new group for handling delayed actions.</returns>
	public IGroup<T> CreateGroup<T>(Func<T> init)
		where T : IDisposable
	{
		var group = new Group<T>(this, init);
		_groups.Add(group);
		return group;
	}

	/// <summary>
	/// Deletes a previously created group of delayed actions.
	/// </summary>
	/// <param name="group">The group to delete.</param>
	public void DeleteGroup(IGroup group)
	{
		if (group is null)
			throw new ArgumentNullException(nameof(group));

		if (group == DefaultGroup)
			throw new ArgumentException();

		_groups.Remove(group);
	}

	private int _maxBatchSize = 1000;

	/// <summary>
	/// Gets or sets the maximum number of actions in a single batch.
	/// </summary>
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
		using (_groups.SyncRoot.EnterScope())
		{
			if (!_isFlushing && _flushTimer is null)
			{
				_flushTimer = AsyncHelper.CreatePeriodicTimer(() => Do.Invariant(OnFlush));
				_flushTimer.Start(_flushInterval);
			}
		}
	}

	/// <summary>
	/// Flushes all queued actions by executing them in batches.
	/// </summary>
	public void OnFlush()
	{
		try
		{
			IGroup[] groups;

			using (_groups.SyncRoot.EnterScope())
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
							Debug.WriteLine($"!!! Interrupt: {list.Count}");

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
				using (_groups.SyncRoot.EnterScope())
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

	/// <summary>
	/// Waits until all queued actions are flushed and optionally disposes the DelayAction instance.
	/// </summary>
	/// <param name="dispose">If set to true, disposes the DelayAction instance after flushing.</param>
	public void WaitFlush(bool dispose)
	{
		_groups.Cache.Where(g => g != DefaultGroup).ForEach(g => g.WaitFlush(false));
		DefaultGroup.WaitFlush(dispose);
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
			using var scope = ((IInternalGroup)group).Init();

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

	private readonly DummyBatchContext _batchContext = new();

	/// <summary>
	/// Begins a new batch for the specified group.
	/// </summary>
	/// <param name="group">The group for which to begin the batch.</param>
	/// <returns>An IBatchContext representing the batch operation.</returns>
	protected virtual IBatchContext BeginBatch(IGroup group)
	{
		return _batchContext;
	}
}