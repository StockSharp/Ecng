namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Common;

	using Wintellect.PowerCollections;

	[Serializable]
	public abstract class BaseCollection<TItem, TCollection> : CollectionBase<TItem>, INotifyList<TItem>, IList
		where TCollection : ICollection<TItem>
	{
		protected BaseCollection(TCollection innerCollection)
		{
			if (innerCollection.IsNull())
				throw new ArgumentNullException(nameof(innerCollection));

			InnerCollection = innerCollection;
		}

		public bool CheckNullableItems { get; set; }

		protected TCollection InnerCollection { get; }

		protected abstract TItem OnGetItem(int index);
		protected abstract void OnInsert(int index, TItem item);
		protected abstract void OnRemoveAt(int index);

		protected virtual void OnAdd(TItem item)
		{
			InnerCollection.Add(item);
		}

		protected virtual bool OnRemove(TItem item)
		{
			return InnerCollection.Remove(item);
		}

		protected virtual void OnClear()
		{
			InnerCollection.Clear();
		}

		public override IEnumerator<TItem> GetEnumerator()
		{
			return InnerCollection.GetEnumerator();
		}

		public override int Count => InnerCollection.Count;

		public override bool Contains(TItem item)
		{
			return InnerCollection.Contains(item);
		}

		private void CheckIndex(int index)
		{
			if (index < 0 || index > Count)
#if SILVERLIGHT
				throw new ArgumentOutOfRangeException("index");
#else
				throw new ArgumentOutOfRangeException(nameof(index), index, "Index has incorrect value.");
#endif
		}

		public virtual TItem this[int index]
		{
			get => OnGetItem(index);
			set
			{
				CheckIndex(index);

				if (index < Count)
				{
					RemoveAt(index);
					Insert(index, value);
				}
				else
					Add(value);
			}
		}

		public abstract int IndexOf(TItem item);

		public virtual void RemoveAt(int index)
		{
			if (OnRemovingAt(index))
			{
				OnRemoveAt(index);
				OnRemovedAt(index);
			}
		}

		public virtual void Insert(int index, TItem item)
		{
			CheckIndex(index);

			if (index == Count)
				Add(item);
			else
			{
				OnInserting(index, item);
				OnInsert(index, item);
				OnInserted(index, item);
			}
		}

		public virtual bool IsReadOnly => false;

		public override void Add(TItem item)
		{
			if (CheckNullableItems && item.IsNull())
				throw new ArgumentNullException(nameof(item));

			if (OnAdding(item))
			{
				OnAdd(item);
				OnAdded(item);
			}
		}

		public override void Clear()
		{
			if (OnClearing())
			{
				OnClear();
				OnCleared();
			}
		}

		public override bool Remove(TItem item)
		{
			if (CheckNullableItems && item.IsNull())
				throw new ArgumentNullException(nameof(item));

			if (OnRemoving(item))
			{
				var retVal = OnRemove(item);
				OnRemoved(item);
				return retVal;
			}

			return false;
		}

		public event Func<TItem, bool> Adding;
		public event Action<TItem> Added;
		public event Func<TItem, bool> Removing;
		public event Action<TItem> Removed;
		public event Func<int, bool> RemovingAt;
		public event Action<int> RemovedAt;
		public event Func<bool> Clearing;
		public event Action Cleared;
		public event Func<int, TItem, bool> Inserting;
		public event Action<int, TItem> Inserted;
		public event Action Changed;

		protected virtual bool OnInserting(int index, TItem item)
		{
			return Inserting?.Invoke(index, item) ?? true;
		}

		protected virtual void OnInserted(int index, TItem item)
		{
			Inserted?.Invoke(index, item);
			OnChanged();
		}

		protected virtual bool OnAdding(TItem item)
		{
			return Adding?.Invoke(item) ?? true;
		}

		protected virtual void OnAdded(TItem item)
		{
			Added?.Invoke(item);
			OnChanged();
		}

		protected virtual bool OnClearing()
		{
			return Clearing?.Invoke() ?? true;
		}

		protected virtual void OnCleared()
		{
			Cleared?.Invoke();
			OnChanged();
		}

		protected virtual bool OnRemoving(TItem item)
		{
			return Removing?.Invoke(item) ?? true;
		}

		protected virtual void OnRemoved(TItem item)
		{
			Removed?.Invoke(item);
			OnChanged();
		}

		protected virtual bool OnRemovingAt(int index)
		{
			return RemovingAt?.Invoke(index) ?? true;
		}

		protected virtual void OnRemovedAt(int index)
		{
			RemovedAt?.Invoke(index);
			OnChanged();
		}

		protected virtual void OnChanged()
		{
			Changed?.Invoke();
		}

		#region IList Members

		bool IList.Contains(object value)
		{
			if (!IsCompatible(value))
				return false;

			return Contains((TItem)value);
		}

		int IList.Add(object value)
		{
			if (!IsCompatible(value))
				throw new ArgumentNullException(nameof(value));

			Add((TItem)value);
			return Count;
		}

		bool IList.IsReadOnly => false;

		object IList.this[int index]
		{
			get => this[index];
			set
			{
				if (!IsCompatible(value))
					throw new ArgumentNullException(nameof(value));

				this[index] = (TItem)value;
			}
		}

		void IList.Clear()
		{
			((ICollection<TItem>)this).Clear();
		}

		int IList.IndexOf(object value)
		{
			if (!IsCompatible(value))
				return -1;

			return IndexOf((TItem)value);
		}

		void IList.Insert(int index, object value)
		{
			if (!IsCompatible(value))
				throw new ArgumentNullException(nameof(value));

			Insert(index, (TItem)value);
		}

		void IList.Remove(object value)
		{
			if (!IsCompatible(value))
				return;

			Remove((TItem)value);
		}

		void IList.RemoveAt(int index)
		{
			RemoveAt(index);
		}

		bool IList.IsFixedSize => false;

		#endregion

		private static readonly bool _isValueType = typeof(TItem).IsValueType;

		private static bool IsCompatible(object value)
		{
			return !_isValueType || value != null;
		}
	}
}