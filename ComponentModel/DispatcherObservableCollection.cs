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
			Wait
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

			public CollectionAction(Func<object> convert)
			{
				Type = ActionTypes.Wait;
				Items = [];
				Convert = convert ?? throw new ArgumentNullException(nameof(convert));
			}

			public ActionTypes Type { get; }
			public TItem[] Items { get; }
			public int Index { get; }
			public int Count { get; }
			public SyncObject SyncRoot { get; set; }

			public Func<object> Convert { get; }
			public object ConvertResult { get; set; }
		}

		private readonly Queue<CollectionAction> _pendingActions = new();
		private int _pendingCount;
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
				AddAction(new CollectionAction(ActionTypes.Add, items.ToArray()));
				return;
			}

			Items.AddRange(items);
			_pendingCount = Items.Count;
			CheckCount();
		}

		/// <summary>
		/// </summary>
		public virtual void RemoveRange(IEnumerable<TItem> items)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new CollectionAction(ActionTypes.Remove, items.ToArray()));
				return;
			}

			Items.RemoveRange(items);
			_pendingCount = Items.Count;
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
				var realCount = _pendingCount;
				realCount -= index;
				AddAction(new CollectionAction(index, count));
				return (realCount.Min(count)).Max(0);
			}

			return Items.RemoveRange(index, count);
		}

		/// <inheritdoc />
		public IEnumerator<TItem> GetEnumerator()
		{
			return Items.GetEnumerator();
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
				AddAction(new CollectionAction(ActionTypes.Add, item));
				return;
			}

			Items.Add(item);
			_pendingCount = Items.Count;
			CheckCount();
		}

		/// <inheritdoc />
		public virtual bool Remove(TItem item)
		{
			if (!Dispatcher.CheckAccess())
			{
				AddAction(new CollectionAction(ActionTypes.Remove, item));
				return true;
			}

			var removed = Items.Remove(item);
			_pendingCount = Items.Count;
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
				AddAction(new CollectionAction(ActionTypes.Clear));
				return;
			}

			Items.Clear();
			_pendingCount = 0;
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
		{
			if (!Dispatcher.CheckAccess())
				throw new NotSupportedException();

			return Items.Contains(item);
		}

		/// <inheritdoc />
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			if (!Dispatcher.CheckAccess())
				throw new NotSupportedException();

			Items.CopyTo(array, arrayIndex);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((TItem[])array, index);
		}

		/// <inheritdoc cref="ICollection{T}" />
		public override int Count
		{
			get
			{
				if (!Dispatcher.CheckAccess())
					throw new NotSupportedException();

				return Items.Count;
			}
		}

		object ICollection.SyncRoot => SyncRoot;

		bool ICollection.IsSynchronized => true;

		/// <inheritdoc cref="IList{T}" />
		public bool IsReadOnly => false;

		bool IList.IsFixedSize => false;

		/// <inheritdoc />
		public int IndexOf(TItem item)
		{
			if (!Dispatcher.CheckAccess())
			{
				// NOTE: DevExpress.Data.Helpers.BindingListAdapterBase.RaiseChangedIfNeeded access to IndexOf
				// https://pastebin.com/4X8yPmwa

				return (int)Do(() => IndexOf(item));
				//throw new NotSupportedException();
			}

			return Items.IndexOf(item);
		}

		/// <inheritdoc />
		public void Insert(int index, TItem item)
		{
			if (!Dispatcher.CheckAccess())
				throw new NotSupportedException();

			Items.Insert(index, item);
			_pendingCount = Items.Count;
		}

		/// <inheritdoc cref="IList{T}" />
		public void RemoveAt(int index)
		{
			if (!Dispatcher.CheckAccess())
				throw new NotSupportedException();

			Items.RemoveAt(index);
			_pendingCount = Items.Count;
		}

		object IList.this[int index]
		{
			get => this[index];
			set => this[index] = (TItem)value;
		}

		/// <inheritdoc />
		public TItem this[int index]
		{
			get
			{
				if (!Dispatcher.CheckAccess())
				{
					return (TItem)Do(() => this[index]);
					//throw new NotSupportedException();
				}

				return Items[index];
			}
			set
			{
				if (!Dispatcher.CheckAccess())
					throw new NotSupportedException();

				Items[index] = value;
			}
		}

		/// <summary>
		/// </summary>
		public object Do(Func<object> func)
		{
			if (func == null)
				throw new ArgumentNullException(nameof(func));

			var action = new CollectionAction(func) { SyncRoot = new SyncObject() };
			AddAction(action);

			lock (action.SyncRoot)
			{
				if (action.ConvertResult == null)
					action.SyncRoot.Wait();

				return action.ConvertResult;
			}
		}

		private void AddAction(CollectionAction item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (SyncRoot)
			{
				switch (item.Type)
				{
					case ActionTypes.Add:
						_pendingCount += item.Count;
						break;
					case ActionTypes.Remove:
						if (item.Items == null)
							_pendingCount -= item.Count;
						else
							_pendingCount -= item.Items.Length;
						break;
					case ActionTypes.Clear:
						_pendingCount = 0;
						break;
				}

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
					_isTimerStarted = false;
					actions = [.. _pendingActions];
					_pendingActions.Clear();
				}

				foreach (var action in actions)
				{
					switch (action.Type)
					{
						case ActionTypes.Add:
						case ActionTypes.Remove:
							pendingActions.Add(action);
							break;
						case ActionTypes.Clear:
							pendingActions.Clear();
							hasClear = true;
							break;
						case ActionTypes.Wait:
							pendingActions.Add(action);
							//Dispatcher.AddAction(action.SyncRoot.Pulse);
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
					Items.Clear();

				foreach (var action in pendingActions)
				{
					switch (action.Type)
					{
						case ActionTypes.Add:
							Items.AddRange(action.Items);
							CheckCount();
							break;
						case ActionTypes.Remove:
						{
							if (action.Items != null)
								Items.RemoveRange(action.Items);
							else
								Items.RemoveRange(action.Index, action.Count);

							break;
						}
						case ActionTypes.Wait:
						{
							var result = action.Convert();

							lock (action.SyncRoot)
							{
								action.ConvertResult = result;
								action.SyncRoot.Pulse();
							}

							break;
						}
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				AfterUpdate?.Invoke();

				if (error != null)
					throw error;
			});
		}

		/// <summary>
		/// </summary>
		public SyncObject SyncRoot { get; } = new SyncObject();
	}
}
