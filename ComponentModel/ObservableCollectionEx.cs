namespace Ecng.ComponentModel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	/// <summary>
	/// </summary>
	public class ObservableCollectionEx<TItem> : IListEx<TItem>, IList, INotifyCollectionChanged, INotifyPropertyChanged
	{
		private readonly List<TItem> _items = [];

		/// <inheritdoc />
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		/// <inheritdoc />
		public event PropertyChangedEventHandler PropertyChanged;

		private Action<IEnumerable<TItem>> _addedRange;

		/// <summary>
		/// </summary>
		public event Action<IEnumerable<TItem>> AddedRange
		{
			add => _addedRange += value;
			remove => _addedRange -= value;
		}

		private Action<IEnumerable<TItem>> _removedRange;

		/// <summary>
		/// </summary>
		public event Action<IEnumerable<TItem>> RemovedRange
		{
			add => _removedRange += value;
			remove => _removedRange -= value;
		}

		/// <summary>
		/// </summary>
		public virtual void AddRange(IEnumerable<TItem> items)
		{
			var arr = items.ToArray();

			if (arr.Length == 0)
				return;

			var index = _items.Count;

			_items.AddRange(arr);

			OnCountPropertyChanged();
			OnIndexerPropertyChanged();

			OnCollectionChanged(NotifyCollectionChangedAction.Add, arr, index);
		}

		/// <summary>
		/// </summary>
		public virtual void RemoveRange(IEnumerable<TItem> items)
		{
			items = items.ToArray();

			if (items.Count() > 10000 || items.Count() > Count * 0.1)
			{
				var temp = new HashSet<TItem>(_items);
				temp.RemoveRange(items);

				Clear();
				AddRange(temp);

				return;
			}

			items.ForEach(i => Remove(i));
		}

		/// <summary>
		/// </summary>
		public virtual int RemoveRange(int index, int count)
		{
			var items = _items.GetRange(index, count).ToArray();

			if (items.Length == 0)
				return 0;

			_items.RemoveRange(index, count);

			OnRemove(items, index);

			return items.Length;
		}

		/// <inheritdoc />
		public IEnumerator<TItem> GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <inheritdoc />
		public virtual void Add(TItem item)
		{
			AddRange([item]);
		}

		/// <inheritdoc />
		public virtual bool Remove(TItem item)
		{
			var index = _items.IndexOf(item);

			if (index == -1)
				return false;

			_items.RemoveAt(index);

			OnRemove([item], index);
			return true;
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
			_items.Clear();

			OnCountPropertyChanged();
			OnIndexerPropertyChanged();
			OnCollectionReset();
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
			return _items.Contains(item);
		}

		/// <inheritdoc />
		public void CopyTo(TItem[] array, int arrayIndex)
		{
			_items.CopyTo(array, arrayIndex);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array is not TItem[] items)
				items = array.Cast<TItem>().ToArray();

			CopyTo(items, index);
		}

		/// <inheritdoc cref="ICollection{T}" />
		public int Count => _items.Count;

		object ICollection.SyncRoot => this;

		bool ICollection.IsSynchronized => true;

		/// <inheritdoc cref="ICollection{T}" />
		public bool IsReadOnly => false;

		bool IList.IsFixedSize => false;

		/// <inheritdoc />
		public int IndexOf(TItem item)
		{
			return _items.IndexOf(item);
		}

		/// <inheritdoc />
		public void Insert(int index, TItem item)
		{
			_items.Insert(index, item);

			OnCountPropertyChanged();
			OnIndexerPropertyChanged();

			OnCollectionChanged(NotifyCollectionChangedAction.Add, [item], index);
		}

		/// <inheritdoc cref="IList{T}" />
		public void RemoveAt(int index)
		{
			var item = _items[index];
			_items.RemoveAt(index);

			OnRemove([item], index);
		}

		object IList.this[int index]
		{
			get => this[index];
			set => this[index] = (TItem)value;
		}

		/// <inheritdoc />
		public TItem this[int index]
		{
			get => _items[index];
			set
			{
				var originalItem = _items[index];
				_items[index] = value;

				OnIndexerPropertyChanged();
				OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, value, index);
			}
		}

		private void OnRemove(IList<TItem> items, int index)
		{
			OnCountPropertyChanged();
			OnIndexerPropertyChanged();

			OnCollectionChanged(NotifyCollectionChangedAction.Remove, items, index);
		}

		/// <summary>
		/// Helper to raise a PropertyChanged event.
		/// </summary>
		protected void OnPropertyChanged(string propertyName)
		{
			var evt = PropertyChanged;
			evt?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void OnCountPropertyChanged() => OnPropertyChanged("Count");

		// This must agree with Binding.IndexerName.  It is declared separately
		// here so as to avoid a dependency on PresentationFramework.dll.
		private void OnIndexerPropertyChanged() => OnPropertyChanged("Item[]");

		private void OnCollectionChanged(NotifyCollectionChangedAction action, IList<TItem> items, int index)              => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, (IList)items, index));
        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)                     => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
		private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)       => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
		private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)  => OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));

		private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
		{
			if(args.OldItems?.Count > 0)
				_removedRange?.Invoke(args.OldItems.Cast<TItem>());

			if(args.NewItems?.Count > 0)
				_addedRange?.Invoke(args.NewItems.Cast<TItem>());

			var evt = CollectionChanged;
			if (evt == null)
				return;

			ProcessCollectionChanged(evt.GetInvocationList().Cast<NotifyCollectionChangedEventHandler>(), args);
		}

		protected virtual void ProcessCollectionChanged(IEnumerable<NotifyCollectionChangedEventHandler> subscribers, NotifyCollectionChangedEventArgs args)
		{
			foreach (var subscriber in subscribers)
				subscriber(this, args);
		}

		/// <summary>
		/// Helper to raise CollectionChanged event with action == Reset to any listeners
		/// </summary>
		private void OnCollectionReset()
		{
			var evt = CollectionChanged;
			evt?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}
}
