namespace Ecng.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	public class ThreadSafeObservableCollection<TItem> : BaseObservableCollection, ISynchronizedCollection<TItem>, IListEx<TItem>, IList
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
				if (items == null)
					throw new ArgumentNullException(nameof(items));

				Type = type;
				Items = items;
			}

			public CollectionAction(int index, int count)
			{
				Type = ActionTypes.Remove;
				Index = index;
				Count = count;
			}

			public CollectionAction(Func<object> convert)
			{
				if (convert == null)
					throw new ArgumentNullException(nameof(convert));

				Type = ActionTypes.Wait;
				Items = ArrayHelper.Empty<TItem>();
				Convert = convert;
			}

			public ActionTypes Type { get; }
			public TItem[] Items { get; }
			public int Index { get; }
			public int Count { get; }
			public SyncObject SyncRoot { get; set; }

			public Func<object> Convert { get; set; }
			public object ConvertResult { get; set; }
		}

		private readonly Queue<CollectionAction> _pendingActions = new Queue<CollectionAction>();
		private int _pendingCount;
		private bool _isTimerStarted;

		public event Action BeforeUpdate;

		public event Action AfterUpdate;

		public ThreadSafeObservableCollection(IListEx<TItem> items)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			Items = items;
		}

		public IListEx<TItem> Items { get; }

		private GuiDispatcher _dispatcher = GuiDispatcher.GlobalDispatcher;

		public GuiDispatcher Dispatcher
		{
			get => _dispatcher;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_dispatcher = value;
			}
		}

		public event Action<IEnumerable<TItem>> AddedRange
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		public event Action<IEnumerable<TItem>> RemovedRange
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		public virtual void AddRange(IEnumerable<TItem> items)
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
			{
				AddAction(new CollectionAction(ActionTypes.Add, items.ToArray()));
				return;
			}

			Items.AddRange(items);
			_pendingCount = Items.Count;
			CheckCount();
		}

		public virtual void RemoveRange(IEnumerable<TItem> items)
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
			{
				AddAction(new CollectionAction(ActionTypes.Remove, items.ToArray()));
				return;
			}

			Items.RemoveRange(items);
			_pendingCount = Items.Count;
		}

		public override int RemoveRange(int index, int count)
		{
			if (index < -1)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (!Dispatcher.Dispatcher.CheckAccess())
			{
				var realCount = _pendingCount;
				realCount -= index;
				AddAction(new CollectionAction(index, count));
				return (realCount.Min(count)).Max(0);
			}

			return Items.RemoveRange(index, count);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public virtual void Add(TItem item)
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
			{
				AddAction(new CollectionAction(ActionTypes.Add, item));
				return;
			}

			Items.Add(item);
			_pendingCount = Items.Count;
			CheckCount();
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public virtual bool Remove(TItem item)
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
			{
				AddAction(new CollectionAction(ActionTypes.Remove, item));
				return true;
			}

			var removed = Items.Remove(item);
			_pendingCount = Items.Count;
			return removed;
		}

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.IList"/>.
		/// </summary>
		/// <returns>
		/// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection,
		/// </returns>
		/// <param name="value">The object to add to the <see cref="T:System.Collections.IList"/>. </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
		int IList.Add(object value)
		{
			Add((TItem)value);
			return Count - 1;
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.IList"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Object"/> is found in the <see cref="T:System.Collections.IList"/>; otherwise, false.
		/// </returns>
		/// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"/>. </param>
		bool IList.Contains(object value)
		{
			return Contains((TItem)value);
		}

		/// <summary>
		/// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
		public virtual void Clear()
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
			{
				AddAction(new CollectionAction(ActionTypes.Clear));
				return;
			}

			Items.Clear();
			_pendingCount = 0;
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.IList"/>.
		/// </summary>
		/// <returns>
		/// The index of <paramref name="value"/> if found in the list; otherwise, -1.
		/// </returns>
		/// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"/>. </param>
		int IList.IndexOf(object value)
		{
			return IndexOf((TItem)value);
		}

		/// <summary>
		/// Inserts an item to the <see cref="T:System.Collections.IList"/> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted. </param><param name="value">The object to insert into the <see cref="T:System.Collections.IList"/>. </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.IList"/>. </exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception><exception cref="T:System.NullReferenceException"><paramref name="value"/> is null reference in the <see cref="T:System.Collections.IList"/>.</exception>
		void IList.Insert(int index, object value)
		{
			Insert(index, (TItem)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList"/>.
		/// </summary>
		/// <param name="value">The object to remove from the <see cref="T:System.Collections.IList"/>. </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
		void IList.Remove(object value)
		{
			Remove((TItem)value);
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		public bool Contains(TItem item)
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
				throw new NotSupportedException();

			return Items.Contains(item);
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
				throw new NotSupportedException();

			Items.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing. </param><param name="index">The zero-based index in <paramref name="array"/> at which copying begins. </param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than zero. </exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>.-or-The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((TItem[])array, index);
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		public override int Count
		{
			get
			{
				if (!Dispatcher.Dispatcher.CheckAccess())
					throw new NotSupportedException();

				return Items.Count;
			}
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
		/// </summary>
		/// <returns>
		/// An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
		/// </returns>
		object ICollection.SyncRoot => SyncRoot;

		/// <summary>
		/// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
		/// </summary>
		/// <returns>
		/// true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.
		/// </returns>
		bool ICollection.IsSynchronized => true;

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
		/// </returns>
		public bool IsReadOnly => false;

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> has a fixed size.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.IList"/> has a fixed size; otherwise, false.
		/// </returns>
		bool IList.IsFixedSize => false;

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
		/// </summary>
		/// <returns>
		/// The index of <paramref name="item"/> if found in the list; otherwise, -1.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		public int IndexOf(TItem item)
		{
			if (!Dispatcher.Dispatcher.CheckAccess())
			{
				// NOTE: DevExpress.Data.Helpers.BindingListAdapterBase.RaiseChangedIfNeeded access to IndexOf
				// https://pastebin.com/4X8yPmwa

				return (int)Do(() => IndexOf(item));
				//throw new NotSupportedException();
			}

			return Items.IndexOf(item);
		}

		/// <summary>
		/// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param><param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public void Insert(int index, TItem item)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <returns>
		/// The element at the specified index.
		/// </returns>
		/// <param name="index">The zero-based index of the element to get or set. </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.IList"/>. </exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.IList"/> is read-only. </exception>
		object IList.this[int index]
		{
			get => this[index];
			set => this[index] = (TItem)value;
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <returns>
		/// The element at the specified index.
		/// </returns>
		/// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public TItem this[int index]
		{
			get
			{
				if (!Dispatcher.Dispatcher.CheckAccess())
				{
					return (TItem)Do(() => this[index]);
					//throw new NotSupportedException();
				}

				return Items[index];
			}
			set => throw new NotSupportedException();
		}

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
					actions = _pendingActions.ToArray();
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

			Dispatcher.AddAction(() =>
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

		private readonly SyncObject _syncRoot = new SyncObject();

		public SyncObject SyncRoot => _syncRoot;
	}
}