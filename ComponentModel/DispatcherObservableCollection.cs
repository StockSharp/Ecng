namespace Ecng.ComponentModel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// The class represents a synchronized collection that can be used in WPF applications.
	/// </summary>
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

			public CollectionAction(ActionTypes type, int index, int count)
			{
				Type = type;
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
			var arr = items.ToArray();

			_syncCopy.AddRange(arr);

			if (Dispatcher.CheckAccess())
				Items.AddRange(arr);
			else
				AddAction(new(ActionTypes.Add, arr));

			CheckCount();
		}

		/// <summary>
		/// Remove range of items.
		/// </summary>
		/// <param name="items">Items.</param>
		public virtual void RemoveRange(IEnumerable<TItem> items)
		{
			var arr = items.ToArray();

			_syncCopy.RemoveRange(arr);

			if (Dispatcher.CheckAccess())
				Items.RemoveRange(arr);
			else
				AddAction(new(ActionTypes.Remove, arr));
		}

		/// <inheritdoc />
		public override int RemoveRange(int index, int count)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			var retVal = _syncCopy.RemoveRange(index, count);

			if (Dispatcher.CheckAccess())
				Items.RemoveRange(index, count);
			else
				AddAction(new(ActionTypes.Remove, index, count));

			return retVal;
		}

		/// <inheritdoc />
		public IEnumerator<TItem> GetEnumerator()
			=> _syncCopy.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <inheritdoc />
		public virtual void Add(TItem item)
		{
			_syncCopy.Add(item);

			if (Dispatcher.CheckAccess())
				Items.Add(item);
			else
				AddAction(new(ActionTypes.Add, item));

			CheckCount();
		}

		/// <inheritdoc />
		public virtual bool Remove(TItem item)
		{
			var removed = _syncCopy.Remove(item);

			if (removed)
			{
				if (Dispatcher.CheckAccess())
					Items.Remove(item);
				else
					AddAction(new(ActionTypes.Remove, item));
			}

			return removed;
		}

		int IList.Add(object value)
		{
			Add((TItem)value);
			return Count - 1;
		}

		bool IList.Contains(object value)
			=> Contains((TItem)value);

		/// <inheritdoc cref="ICollection{T}" />
		public virtual void Clear()
		{
			_syncCopy.Clear();

			if (Dispatcher.CheckAccess())
				Items.Clear();
			else
				AddAction(new(ActionTypes.Clear));
		}

		int IList.IndexOf(object value)
			=> IndexOf((TItem)value);

		void IList.Insert(int index, object value)
			=> Insert(index, (TItem)value);

		void IList.Remove(object value)
			=> Remove((TItem)value);

		/// <inheritdoc />
		public bool Contains(TItem item)
			=> _syncCopy.Contains(item);

		/// <inheritdoc />
		public void CopyTo(TItem[] array, int arrayIndex)
			=> _syncCopy.CopyTo(array, arrayIndex);

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
			=> _syncCopy.IndexOf(item);

		/// <inheritdoc />
		public void Insert(int index, TItem item)
		{
			_syncCopy.Insert(index, item);

			if (Dispatcher.CheckAccess())
				Items.Insert(index, item);
			else
				AddAction(new(ActionTypes.Insert, item) { Index = index });
		}

		/// <inheritdoc cref="IList{T}" />
		public void RemoveAt(int index)
		{
			_syncCopy.RemoveAt(index);

			if (Dispatcher.CheckAccess())
				Items.RemoveAt(index);
			else
				AddAction(new(ActionTypes.RemoveAt) { Index = index });
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
				_syncCopy[index] = value;

				if (Dispatcher.CheckAccess())
					Items[index] = value;
				else
					AddAction(new(ActionTypes.Set, value) { Index = index });
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
			List<CollectionAction> pendingActions;
			var hasClear = false;
			Exception error = null;

			try
			{
				lock (SyncRoot)
				{
					pendingActions = [.. _pendingActions];
					_pendingActions.Clear();
					_isTimerStarted = false;
				}

				for (var i = 0; i < pendingActions.Count; i++)
				{
					if (pendingActions[i].Type != ActionTypes.Clear)
						continue;

					pendingActions.RemoveRange(0, i + 1);
					hasClear = true;
					i = -1;
				}
			}
			catch (Exception ex)
			{
				error = ex;
				pendingActions = [];
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
							break;
						case ActionTypes.Remove:
							if (action.Items != null)
								Items.RemoveRange(action.Items);
							else
								Items.RemoveRange(action.Index, action.Count);
							break;
						case ActionTypes.CopyTo:
							Items.CopyTo(action.Items, action.Index);
							break;
						case ActionTypes.Insert:
							Items.Insert(action.Index, action.Items[0]);
							break;
						case ActionTypes.RemoveAt:
							Items.RemoveAt(action.Index);
							break;
						case ActionTypes.Set:
							Items[action.Index] = action.Items[0];
							break;
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
