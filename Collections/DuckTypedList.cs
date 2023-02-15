namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	[Obsolete("Due to poor performance, this collection will no longer be supported. The applied code should be corrected to avoid unnecessary type conversion.")]
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
			_innerList = innerList ?? throw new ArgumentNullException(nameof(innerList));
			_onlyCompatible = onlyCompatible;

			_innerList.Adding += OnAdding;
			_innerList.Added += OnAdded;
			_innerList.Inserting += OnInserting;
			_innerList.Inserted += OnInserted;
			_innerList.Removing += OnRemoving;
			_innerList.RemovingAt += OnRemovingAt;
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

		int ICollection<TOuter>.Count => _innerList.Count;

		bool ICollection<TOuter>.IsReadOnly => _innerList.IsReadOnly;

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
			get => _innerList[index].To<TOuter>();
			set => _innerList[index] = value.To<TInner>();
		}

		private Func<TOuter, bool> _adding;

		event Func<TOuter, bool> INotifyCollection<TOuter>.Adding
		{
			add => _adding += value;
			remove => _adding -= value;
		}

		private Action<TOuter> _added;

		event Action<TOuter> INotifyCollection<TOuter>.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		private Func<int, TOuter, bool> _inserting;

		event Func<int, TOuter, bool> INotifyCollection<TOuter>.Inserting
		{
			add => _inserting += value;
			remove => _inserting -= value;
		}

		private Action<int, TOuter> _inserted;

		event Action<int, TOuter> INotifyCollection<TOuter>.Inserted
		{
			add => _inserted += value;
			remove => _inserted -= value;
		}

		private Func<TOuter, bool> _removing;

		event Func<TOuter, bool> INotifyCollection<TOuter>.Removing
		{
			add => _removing += value;
			remove => _removing -= value;
		}

		private Func<int, bool> _removingAt;

		event Func<int, bool> INotifyCollection<TOuter>.RemovingAt
		{
			add => _removingAt += value;
			remove => _removingAt -= value;
		}

		private Action<TOuter> _removed;

		event Action<TOuter> INotifyCollection<TOuter>.Removed
		{
			add => _removed += value;
			remove => _removed -= value;
		}

		private Func<bool> _clearing;

		event Func<bool> INotifyCollection<TOuter>.Clearing
		{
			add => _clearing += value;
			remove => _clearing -= value;
		}

		private Action _cleared;

		event Action INotifyCollection<TOuter>.Cleared
		{
			add => _cleared += value;
			remove => _cleared -= value;
		}

		private Action _changed;

		event Action INotifyCollection<TOuter>.Changed
		{
			add => _changed += value;
			remove => _changed -= value;
		}

		private void OnRemoved(TInner item)
		{
			if (CanProcess(item))
			{
				_removed?.Invoke(item.To<TOuter>());
				_changed?.Invoke();
			}
		}

		private bool OnRemoving(TInner item)
		{
			if (CanProcess(item))
				return _removing?.Invoke(item.To<TOuter>()) ?? true;

			return false;
		}

		private bool OnRemovingAt(int index)
		{
			//if (CanProcess(this[index]))
			return _removingAt?.Invoke(index) ?? true;
		}

		private void OnAdded(TInner item)
		{
			if (CanProcess(item))
			{
				_added?.Invoke(item.To<TOuter>());
				_changed?.Invoke();
			}
		}

		private bool OnAdding(TInner item)
		{
			if (CanProcess(item))
				return _adding?.Invoke(item.To<TOuter>()) ?? true;

			return false;
		}

		private bool OnInserting(int index, TInner item)
		{
			if (CanProcess(item))
				return _inserting?.Invoke(index, item.To<TOuter>()) ?? true;

			return false;
		}

		private void OnInserted(int index, TInner item)
		{
			if (CanProcess(item))
			{
				_inserted?.Invoke(index, item.To<TOuter>());
				_changed?.Invoke();
			}
		}

		private bool OnClearing()
		{
			return _clearing?.Invoke() ?? true;
		}

		private void OnCleared()
		{
			_cleared?.Invoke();
			_changed?.Invoke();
		}

		protected override void DisposeManaged()
		{
			_innerList.Adding -= OnAdding;
			_innerList.Added -= OnAdded;
			_innerList.Inserting -= OnInserting;
			_innerList.Inserted -= OnInserted;
			_innerList.Removing -= OnRemoving;
			_innerList.RemovingAt -= OnRemovingAt;
			_innerList.Removed -= OnRemoved;
			_innerList.Clearing -= OnClearing;
			_innerList.Cleared -= OnCleared;

			base.DisposeManaged();
		}
	}
}