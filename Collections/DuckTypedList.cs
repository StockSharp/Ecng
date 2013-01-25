namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	public class DuckTypedList<TInner, TOuter> : Disposable, INotifyList<TOuter>
	{
		private readonly INotifyList<TInner> _innerList;
		private readonly bool _onlyCompatible;

		public DuckTypedList(INotifyList<TInner> innerList)
			: this(innerList, false)
		{
		}

		public DuckTypedList(INotifyList<TInner> innerList, bool onlyCompatible)
		{
			if (innerList == null)
				throw new ArgumentNullException("innerList");

			_innerList = innerList;
			_onlyCompatible = onlyCompatible;

			_innerList.Adding += OnAdding;
			_innerList.Added += OnAdded;
			_innerList.Inserting += OnInserting;
			_innerList.Inserted += OnInserted;
			_innerList.Removing += OnRemoving;
			_innerList.Removed += OnRemoved;
			_innerList.Clearing += OnClearing;
			_innerList.Cleared += OnCleared;
		}

		private bool CanProcess(TInner inner)
		{
			if (_onlyCompatible)
				return inner is TOuter;
			else
				return true;
		}

		private bool CanProcess(TOuter outer)
		{
			if (_onlyCompatible)
				return outer is TInner;
			else
				return true;
		}

		IEnumerator<TOuter> IEnumerable<TOuter>.GetEnumerator()
		{
			return (_onlyCompatible ? _innerList.OfType<TOuter>() : _innerList.Cast<TOuter>()).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<TOuter>)_innerList).GetEnumerator();
		}

		void ICollection<TOuter>.Add(TOuter item)
		{
			if (CanProcess(item))
				_innerList.Add(item.To<TInner>());
		}

		void ICollection<TOuter>.Clear()
		{
			_innerList.Clear();
		}

		bool ICollection<TOuter>.Contains(TOuter item)
		{
			if (!CanProcess(item))
				return false;

			return _innerList.Contains(item.To<TInner>());
		}

		void ICollection<TOuter>.CopyTo(TOuter[] array, int arrayIndex)
		{
			var outerElems = _innerList.OfType<TOuter>().ToArray();
			Array.Copy(outerElems, 0, array, arrayIndex, outerElems.Length);
		}

		bool ICollection<TOuter>.Remove(TOuter item)
		{
			if (!CanProcess(item))
				return false;

			return _innerList.Remove(item.To<TInner>());
		}

		int ICollection<TOuter>.Count
		{
			get { return _innerList.Count; }
		}

		bool ICollection<TOuter>.IsReadOnly
		{
			get { return _innerList.IsReadOnly; }
		}

		int IList<TOuter>.IndexOf(TOuter item)
		{
			if (!CanProcess(item))
				return -1;

			return _innerList.IndexOf(item.To<TInner>());
		}

		void IList<TOuter>.Insert(int index, TOuter item)
		{
			if (!CanProcess(item))
				return;

			_innerList.Insert(index, item.To<TInner>());
		}

		void IList<TOuter>.RemoveAt(int index)
		{
			_innerList.RemoveAt(index);
		}

		TOuter IList<TOuter>.this[int index]
		{
			get { return _innerList[index].To<TOuter>(); }
			set { _innerList[index] = value.To<TInner>(); }
		}

		private Action<TOuter> _adding;

		event Action<TOuter> INotifyList<TOuter>.Adding
		{
			add { _adding += value; }
			remove { _adding -= value; }
		}

		private Action<TOuter> _added;

		event Action<TOuter> INotifyList<TOuter>.Added
		{
			add { _added += value; }
			remove { _added -= value; }
		}

		private Action<int, TOuter> _inserting;

		event Action<int, TOuter> INotifyList<TOuter>.Inserting
		{
			add { _inserting += value; }
			remove { _inserting -= value; }
		}

		private Action<int, TOuter> _inserted;

		event Action<int, TOuter> INotifyList<TOuter>.Inserted
		{
			add { _inserted += value; }
			remove { _inserted -= value; }
		}

		private Action<TOuter> _removing;

		event Action<TOuter> INotifyList<TOuter>.Removing
		{
			add { _removing += value; }
			remove { _removing -= value; }
		}

		private Action<TOuter> _removed;

		event Action<TOuter> INotifyList<TOuter>.Removed
		{
			add { _removed += value; }
			remove { _removed -= value; }
		}

		private Action _clearing;

		event Action INotifyList<TOuter>.Clearing
		{
			add { _clearing += value; }
			remove { _clearing -= value; }
		}

		private Action _cleared;

		event Action INotifyList<TOuter>.Cleared
		{
			add { _cleared += value; }
			remove { _cleared -= value; }
		}

		private Action _changed;

		event Action INotifyList<TOuter>.Changed
		{
			add { _changed += value; }
			remove { _changed -= value; }
		}

		private void OnRemoved(TInner item)
		{
			if (CanProcess(item))
			{
				_removed.SafeInvoke(item.To<TOuter>());
				_changed.SafeInvoke();
			}
		}

		private void OnRemoving(TInner item)
		{
			if (CanProcess(item))
				_removing.SafeInvoke(item.To<TOuter>());
		}

		private void OnAdded(TInner item)
		{
			if (CanProcess(item))
			{
				_added.SafeInvoke(item.To<TOuter>());
				_changed.SafeInvoke();
			}
		}

		private void OnAdding(TInner item)
		{
			if (CanProcess(item))
				_adding.SafeInvoke(item.To<TOuter>());
		}

		private void OnInserting(int index, TInner item)
		{
			if (CanProcess(item))
				_inserting.SafeInvoke(index, item.To<TOuter>());
		}

		private void OnInserted(int index, TInner item)
		{
			if (CanProcess(item))
			{
				_inserted.SafeInvoke(index, item.To<TOuter>());
				_changed.SafeInvoke();
			}
		}

		private void OnClearing()
		{
			_clearing.SafeInvoke();
		}

		private void OnCleared()
		{
			_cleared.SafeInvoke();
			_changed.SafeInvoke();
		}

		protected override void DisposeManaged()
		{
			_innerList.Adding -= OnAdding;
			_innerList.Added -= OnAdded;
			_innerList.Inserting -= OnInserting;
			_innerList.Inserted -= OnInserted;
			_innerList.Removing -= OnRemoving;
			_innerList.Removed -= OnRemoved;
			_innerList.Clearing -= OnClearing;
			_innerList.Cleared -= OnCleared;

			base.DisposeManaged();
		}
	}
}