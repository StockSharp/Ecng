namespace Ecng.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows.Data;

	using Ecng.Collections;
	using Ecng.Common;

	public class ThreadSafeObservableCollection<TItem> : BaseObservableCollection, ISynchronizedCollection<TItem>, IList<TItem>, ICollectionEx<TItem>, IList, INotifyCollectionChanged, INotifyPropertyChanged
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
			public CollectionAction(ActionTypes type, TItem[] items, int index)
			{
				if (items == null)
					throw new ArgumentNullException("items");

				Type = type;
				Items = items;
				Index = index;
			}

			public ActionTypes Type { get; private set; }
			public TItem[] Items { get; private set; }
			public int Index { get; private set; }
			public SyncObject SyncRoot { get; set; }
		}

		private const string _countString = "Count";

		// This must agree with Binding.IndexerName.  It is declared separately
		// here so as to avoid a dependency on PresentationFramework.dll.
		private const string _indexerName = "Item[]";

		private readonly SynchronizedList<TItem> _items = new SynchronizedList<TItem>();
		private readonly Queue<CollectionAction> _pendingActions = new Queue<CollectionAction>();
		private bool _isTimerStarted;
		private const int _maxDiff = 10;
		private int _guiCount;

		private GuiDispatcher _dispatcher = GuiDispatcher.GlobalDispatcher;

		public GuiDispatcher Dispatcher
		{
			get { return _dispatcher; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_dispatcher = value;
			}
		}

		public virtual void AddRange(IEnumerable<TItem> items)
		{
			var arr = items.ToArray();

			if (arr.Length == 0)
				return;

			try
			{
				int index;

				lock (SyncRoot)
				{
					index = _items.Count;

					_items.AddRange(arr);

					if (!Dispatcher.Dispatcher.CheckAccess())
					{
						AddAction(new CollectionAction(ActionTypes.Add, arr, index));
						return;
					}
				}

				OnAdd(arr, index);
			}
			finally
			{
				CheckCount();
			}
		}

		public virtual IEnumerable<TItem> RemoveRange(IEnumerable<TItem> items)
		{
			return CollectionHelper.RemoveRange(this, items);
		}

		public override void RemoveRange(int index, int count)
		{
			TItem[] items;

			lock (SyncRoot)
			{
				items = _items.GetRange(index, count).ToArray();

				if (items.Length == 0)
					return;

				_items.RemoveRange(index, count);

				if (!Dispatcher.Dispatcher.CheckAccess())
				{
					AddAction(new CollectionAction(ActionTypes.Remove, items, index));
					return;
				}
			}

			OnRemove(items, index);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<TItem> GetEnumerator()
		{
			return _items.GetEnumerator();
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
			try
			{
				int index;

				lock (SyncRoot)
				{
					index = _items.Count;
					_items.Add(item);

					if (!Dispatcher.Dispatcher.CheckAccess())
					{
						AddAction(ActionTypes.Add, index, item);
						return;
					}
				}

				OnAdd(new[] { item }, index);
			}
			finally
			{
				CheckCount();
			}
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
			int index;

			lock (SyncRoot)
			{
				index = _items.IndexOf(item);

				if (index == -1)
					return false;

				_items.RemoveAt(index);

				if (!Dispatcher.Dispatcher.CheckAccess())
				{
					AddAction(ActionTypes.Remove, index, item);
					return true;
				}
			}

			OnRemove(new[] { item }, index);
			return true;
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
			lock (SyncRoot)
			{
				_items.Clear();

				if (!Dispatcher.Dispatcher.CheckAccess())
				{
					AddAction(ActionTypes.Clear, -1);
					return;
				}
			}

			OnClear();
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
			return _items.Contains(item);
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			_items.CopyTo(array, arrayIndex);
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
			get { return _guiCount; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
		/// </summary>
		/// <returns>
		/// An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
		/// </returns>
		object ICollection.SyncRoot
		{
			get { return _items.SyncRoot; }
		}

		/// <summary>
		/// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
		/// </summary>
		/// <returns>
		/// true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.
		/// </returns>
		bool ICollection.IsSynchronized
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
		/// </returns>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> has a fixed size.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.IList"/> has a fixed size; otherwise, false.
		/// </returns>
		bool IList.IsFixedSize
		{
			get { return false; }
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
		/// </summary>
		/// <returns>
		/// The index of <paramref name="item"/> if found in the list; otherwise, -1.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		public int IndexOf(TItem item)
		{
			return _items.IndexOf(item);
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
			get { return this[index]; }
			set { this[index] = (TItem)value; }
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
			get { return _items[index]; }
			set { throw new NotSupportedException(); }
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;
		public event PropertyChangedEventHandler PropertyChanged;

		public void Wait()
		{
			var syncRoot = new SyncObject();

			lock (SyncRoot)
				AddAction(new CollectionAction(ActionTypes.Wait, ArrayHelper<TItem>.EmptyArray, -1) { SyncRoot = syncRoot });

			syncRoot.Wait();
		}

		private void AddAction(ActionTypes type, int index, params TItem[] items)
		{
			AddAction(new CollectionAction(type, items, index));
		}

		private void AddAction(CollectionAction item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			//if (item.Type != ActionTypes.Wait && Dispatcher.Dispatcher.CheckAccess())
			//{
			//	switch (item.Type)
			//	{
			//		case ActionTypes.Add:
			//			{
			//				OnAdd(item.Items);
			//				break;
			//			}
			//		case ActionTypes.Remove:
			//			{
			//				OnRemove(item.Items, item.Index);
			//				break;
			//			}
			//		case ActionTypes.Clear:
			//			{
			//				OnClear();
			//				break;
			//			}
			//		default:
			//			throw new ArgumentOutOfRangeException();
			//	}

			//	return;
			//}

			//lock (SyncRoot)
			//{
			_pendingActions.Enqueue(item);

			if (_isTimerStarted)
				return;

			_isTimerStarted = true;
			//}

			ThreadingHelper
				.Timer(OnFlush)
				.Interval(TimeSpan.FromMilliseconds(300), new TimeSpan(-1));
		}

		//public event Action<Exception> ErrorHandler;

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
							Dispatcher.AddAction(action.SyncRoot.Pulse);
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
				if (hasClear)
					OnClear();

				foreach (var action in pendingActions)
				{
					switch (action.Type)
					{
						case ActionTypes.Add:
							OnAdd(action.Items, action.Index);
							break;
						case ActionTypes.Remove:
							OnRemove(action.Items, action.Index);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (error != null)
					throw error;
			});
		}

		private void OnAdd(IList<TItem> items, int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException("index");

			_guiCount += items.Count;

			OnPropertyChanged(_countString);
			OnPropertyChanged(_indexerName);

			OnCollectionChanged(NotifyCollectionChangedAction.Add, items, index);
		}

		private void OnRemove(IList<TItem> items, int index)
		{
			_guiCount -= items.Count;

			if (_guiCount < 0)
				throw new InvalidOperationException();

			OnPropertyChanged(_countString);
			OnPropertyChanged(_indexerName);

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, items, index);
		}

		//protected virtual IEnumerable<TItem> GetRange(int index, int count)
		//{
		//	return _items.GetRange(index, count);
		//}

		private void OnClear()
		{
			//_items.Clear();
			_guiCount = 0;

			OnPropertyChanged(_countString);
			OnPropertyChanged(_indexerName);
			OnCollectionReset();
		}

		///// <summary>
		///// Raise CollectionChanged event to any listeners.
		///// Properties/methods modifying this ObservableCollection will raise
		///// a collection changed event through this virtual method.
		///// </summary>
		//protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		//{
		//	var evt = CollectionChanged;

		//	if (evt != null)
		//	{
		//		//using (BlockReentrancy())
		//		//{
		//		evt(this, e);
		//		//}
		//	}
		//}

		/// <summary>
		/// Helper to raise a PropertyChanged event.
		/// </summary>
		protected void OnPropertyChanged(string propertyName)
		{
			var evt = PropertyChanged;

			if (evt != null)
			{
				evt(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Helper to raise CollectionChanged event to any listeners
		/// </summary>
		private void OnCollectionChanged(NotifyCollectionChangedAction action, IList<TItem> items, int index)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			if (index < 0)
				throw new ArgumentOutOfRangeException("index");

			var evt = CollectionChanged;

			if (evt == null)
				return;

			var e = new NotifyCollectionChangedEventArgs(action, (IList)items, index);

			// http://geekswithblogs.net/NewThingsILearned/archive/2008/01/16/listcollectionviewcollectionview-doesnt-support-notifycollectionchanged-with-multiple-items.aspx

			foreach (var handler in evt.GetInvocationList().Cast<NotifyCollectionChangedEventHandler>())
			{
				var view = handler.Target as CollectionView;

				if (view != null)
				{
					if (items.Count > _maxDiff)
						view.Refresh();
					else
					{
						var localIndex = index;

						foreach (var item in items)
						{
							// http://stackoverflow.com/questions/670577/observablecollection-doesnt-support-addrange-method-so-i-get-notified-for-each
							handler(this, new NotifyCollectionChangedEventArgs(action, item, localIndex));

							if (action == NotifyCollectionChangedAction.Add)
								localIndex++;
						}
					}
				}
				else
					handler(this, e);
			}
		}

		/// <summary>
		/// Helper to raise CollectionChanged event with action == Reset to any listeners
		/// </summary>
		private void OnCollectionReset()
		{
			var evt = CollectionChanged;

			if (evt == null)
				return;

			evt(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public SyncObject SyncRoot
		{
			get { return _items.SyncRoot; }
		}
	}
}