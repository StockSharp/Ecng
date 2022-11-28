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
		private const string _countString = "Count";

		// This must agree with Binding.IndexerName.  It is declared separately
		// here so as to avoid a dependency on PresentationFramework.dll.
		private const string _indexerName = "Item[]";

		private readonly List<TItem> _items = new();

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

			OnPropertyChanged(_countString);
			OnPropertyChanged(_indexerName);

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
			AddRange(new[] { item });
		}

		/// <inheritdoc />
		public virtual bool Remove(TItem item)
		{
			var index = _items.IndexOf(item);

			if (index == -1)
				return false;

			_items.RemoveAt(index);

			OnRemove(new[] { item }, index);
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

			OnPropertyChanged(_countString);
			OnPropertyChanged(_indexerName);
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
			throw new NotSupportedException();
		}

		/// <inheritdoc cref="IList{T}" />
		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
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
			set => throw new NotSupportedException();
		}

		private void OnRemove(IList<TItem> items, int index)
		{
			OnPropertyChanged(_countString);
			OnPropertyChanged(_indexerName);

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

		/// <summary>
		/// Helper to raise CollectionChanged event to any listeners
		/// </summary>
		private void OnCollectionChanged(NotifyCollectionChangedAction action, IList<TItem> items, int index)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			switch (action)
			{
				case NotifyCollectionChangedAction.Add:
					_addedRange?.Invoke(items);
					break;
				case NotifyCollectionChangedAction.Remove:
					_removedRange?.Invoke(items);
					break;
				case NotifyCollectionChangedAction.Reset:
					break;
			}

			var evt = CollectionChanged;

			if (evt == null)
				return;

			ProcessCollectionChanged(
				evt.GetInvocationList().Cast<NotifyCollectionChangedEventHandler>(),
				action, items, index);
		}

		protected virtual void ProcessCollectionChanged(IEnumerable<NotifyCollectionChangedEventHandler> subscribers, NotifyCollectionChangedAction action, IList<TItem> items, int index)
		{
			var e = new NotifyCollectionChangedEventArgs(action, (IList)items, index);

			foreach (var subscriber in subscribers)
				subscriber(this, e);
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