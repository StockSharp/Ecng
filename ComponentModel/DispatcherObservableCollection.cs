namespace Ecng.ComponentModel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class DispatcherObservableCollection<TItem>(IDispatcher dispatcher, IListEx<TItem> items) : BaseObservableCollection, ISynchronizedCollection<TItem>, IListEx<TItem>, IList
	{
		private enum ActionTypes
		{
			Add,
			Remove,
			Clear,
			CopyTo,
			Insert,
			RemoveAt,
			Set
		}

		private class CollectionAction
		{
			public CollectionAction(ActionTypes type, params TItem[] items)
			{
				Type = type;
				Items = items ?? throw new ArgumentNullException(nameof(items));
			}

			public CollectionAction(int index, int count)
			{
				Type = ActionTypes.Remove;
				Index = index;
				Count = count;
			}

			public ActionTypes Type { get; }
			public TItem[] Items { get; }
			public int Index { get; set; }
			public int Count { get; }
		}

		private readonly SynchronizedList<TItem> _syncCopy = [];
		private readonly Queue<CollectionAction> _pendingActions = [];
		private bool _isTimerStarted;

		/// <summary>
		/// </summary>
		public event Action BeforeUpdate;
		/// <summary>
		/// </summary>
		public event Action AfterUpdate;

		/// <summary>
		/// </summary>
		public IListEx<TItem> Items { get; } = items ?? throw new ArgumentNullException(nameof(items));

		/// <summary>
		/// </summary>
		public IDispatcher Dispatcher { get; } = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

		/// <summary>
		/// </summary>
		public event Action<IEnumerable<TItem>> AddedRange
		{
			add => Items.AddedRange += value;
			remove => Items.AddedRange -= value;
		}

		/// <summary>
		/// </summary>
		public event Action<IEnumerable<TItem>> RemovedRange
		{
			add => Items.RemovedRange += value;
			remove => Items.RemovedRange -= value;
		}

		/// <summary>
		/// </summary>
		public virtual void AddRange(IEnumerable<TItem> items)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.Add, items.ToArray()));
				return;
			}

			Items.AddRange(items);
			_syncCopy.AddRange(items);
			CheckCount();
		}

		/// <summary>
		/// </summary>
		public virtual void RemoveRange(IEnumerable<TItem> items)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.Remove, items.ToArray()));
				return;
			}

			Items.RemoveRange(items);
			_syncCopy.RemoveRange(items);
		}

		/// <summary>
		/// </summary>
		public override int RemoveRange(int index, int count)
		{
			if (index < -1)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(index, count));
				return -1;
			}

			var retVal = Items.RemoveRange(index, count);
			_syncCopy.RemoveRange(index, count);
			return retVal;
		}

		/// <inheritdoc />
		public IEnumerator<TItem> GetEnumerator()
		{
			return _syncCopy.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <inheritdoc />
		public virtual void Add(TItem item)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.Add, item));
				return;
			}

			Items.Add(item);
			_syncCopy.Add(item);
			CheckCount();
		}

		/// <inheritdoc />
		public virtual bool Remove(TItem item)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.Remove, item));
				return true;
			}

			var removed = Items.Remove(item);
			_syncCopy.Remove(item);
			return removed;
		}

		int IList.Add(object value)
		{
			Add((TItem)value);
			return Count - 1;
		}

		bool IList.Contains(object value)
		{
			return Contains((TItem)value);
		}

		/// <inheritdoc cref="ICollection{T}" />
		public virtual void Clear()
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.Clear));
				return;
			}

			Items.Clear();
			_syncCopy.Clear();
		}

		int IList.IndexOf(object value)
		{
			return IndexOf((TItem)value);
		}

		void IList.Insert(int index, object value)
		{
			Insert(index, (TItem)value);
		}

		void IList.Remove(object value)
		{
			Remove((TItem)value);
		}

		/// <inheritdoc />
		public bool Contains(TItem item)
			=> _syncCopy.Contains(item);

		/// <inheritdoc />
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.CopyTo, array) { Index = arrayIndex });
				return;
			}

			Items.CopyTo(array, arrayIndex);
			_syncCopy.CopyTo(array, arrayIndex);
		}

		void ICollection.CopyTo(Array array, int index)
			=> CopyTo((TItem[])array, index);

		/// <inheritdoc cref="ICollection{T}" />
		public override int Count => _syncCopy.Count;

		object ICollection.SyncRoot => SyncRoot;

		bool ICollection.IsSynchronized => true;

		/// <inheritdoc cref="IList{T}" />
		public bool IsReadOnly => false;

		bool IList.IsFixedSize => false;

		/// <inheritdoc />
		public int IndexOf(TItem item)
		{
			//if (!Dispatcher.CheckAccess())
			//{
			//	// NOTE: DevExpress.Data.Helpers.BindingListAdapterBase.RaiseChangedIfNeeded access to IndexOf
			//	// https://pastebin.com/4X8yPmwa
			//	//throw new NotSupportedException();
			//}

			return _syncCopy.IndexOf(item);
		}

		/// <inheritdoc />
		public void Insert(int index, TItem item)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.Insert, item) { Index = index });
				return;
			}

			Items.Insert(index, item);
			_syncCopy.Insert(index, item);
		}

		/// <inheritdoc cref="IList{T}" />
		public void RemoveAt(int index)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new(ActionTypes.RemoveAt) { Index = index });
				return;
			}

			Items.RemoveAt(index);
			_syncCopy.RemoveAt(index);
		}

		object IList.this[int index]
		{
			get => this[index];
			set => this[index] = (TItem)value;
		}

		/// <inheritdoc />
		public TItem this[int index]
		{
			get => _syncCopy[index];
			set
			{
				if (!Dispatcher.CheckAccess())
				{
					AddAction(new(ActionTypes.Set, value) { Index = index });
					return;
				}

				Items[index] = value;
				_syncCopy[index] = value;
			}
		}

		private void AddAction(CollectionAction item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (SyncRoot)
			{
				_pendingActions.Enqueue(item);

				if (_isTimerStarted)
					return;

				_isTimerStarted = true;
			}

			ThreadingHelper
				.Timer(OnFlush)
				.Interval(TimeSpan.FromMilliseconds(300), new TimeSpan(-1));
		}

		private void OnFlush()
		{
			var pendingActions = new List<CollectionAction>();
			var hasClear = false;
			Exception error = null;

			try
			{
				CollectionAction[] actions;

				lock (SyncRoot)
				{
					actions = [.. _pendingActions];
					_pendingActions.Clear();
					_isTimerStarted = false;
				}

				foreach (var action in actions)
				{
					switch (action.Type)
					{
						case ActionTypes.Add:
						case ActionTypes.Remove:
						case ActionTypes.CopyTo:
						case ActionTypes.Insert:
						case ActionTypes.RemoveAt:
						case ActionTypes.Set:
							pendingActions.Add(action);
							break;
						case ActionTypes.Clear:
							pendingActions.Clear();
							hasClear = true;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
			catch (Exception ex)
			{
				error = ex;
			}

			Dispatcher.InvokeAsync(() =>
			{
				BeforeUpdate?.Invoke();

				if (hasClear)
				{
					Items.Clear();
					_syncCopy.Clear();
				}

				foreach (var action in pendingActions)
				{
					switch (action.Type)
					{
						case ActionTypes.Add:
							Items.AddRange(action.Items);
							_syncCopy.AddRange(action.Items);
							CheckCount();
							break;
						case ActionTypes.Remove:
						{
							if (action.Items != null)
							{
								Items.RemoveRange(action.Items);
								_syncCopy.RemoveRange(action.Items);
							}
							else
							{
								Items.RemoveRange(action.Index, action.Count);
								_syncCopy.RemoveRange(action.Index, action.Count);
							}

							break;
						}
						case ActionTypes.CopyTo:
						{
							Items.CopyTo(action.Items, action.Index);
							_syncCopy.CopyTo(action.Items, action.Index);
							break;
						}
						case ActionTypes.Insert:
						{
							Items.Insert(action.Index, action.Items[0]);
							_syncCopy.Insert(action.Index, action.Items[0]);
							break;
						}
						case ActionTypes.RemoveAt:
						{
							Items.RemoveAt(action.Index);
							_syncCopy.RemoveAt(action.Index);
							break;
						}
						case ActionTypes.Set:
						{
							Items[action.Index] = action.Items[0];
							_syncCopy[action.Index] = action.Items[0];
							break;
						}
						default:
							throw new ArgumentOutOfRangeException(action.Type.To<string>());
					}
				}

				AfterUpdate?.Invoke();

				if (error != null)
					throw error;
			});
		}

		/// <summary>
		/// </summary>
		public SyncObject SyncRoot => _syncCopy.SyncRoot;
	}
}
