namespace Ecng.Collections
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Web.UI.WebControls;

	public abstract class BaseListEx<T> : IListEx<T>, IListEx
	{
		#region IListEx Members

		IEnumerable IListEx.GetRange(long startIndex, long count, string sortExpression, SortDirection directions)
		{
			return GetRange(startIndex, count, sortExpression, directions);
		}

		#endregion

		#region IListEx<T> Members

		public abstract IEnumerable<T> GetRange(long startIndex, long count, string sortExpression, SortDirection directions);

		#endregion

		#region ICollection Members

		int ICollection.Count
		{
			get { return Count; }
		}

		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((T[])array, index);
		}

		private object _syncRoot;

		object ICollection.SyncRoot
		{
			get { return _syncRoot ?? (_syncRoot = new object()); }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		#endregion

		#region ICollection<T> Members

		public abstract void Add(T item);
		public abstract void Clear();
		public abstract bool Contains(T item);

		public abstract void CopyTo(T[] array, int index);

		public abstract bool Remove(T item);
		public abstract int Count { get; }

		public virtual bool IsReadOnly
		{
			get { return false; }
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}

		#endregion

		#region IEnumerable<T> Members

		public abstract IEnumerator<T> GetEnumerator();

		#endregion

		#region IList Members

		bool IList.Contains(object value)
		{
			if (!IsCompatible(value))
				return false;

			return Contains((T)value);
		}

		int IList.Add(object value)
		{
			if (!IsCompatible(value))
				throw new ArgumentNullException("value");

			Add((T)value);
			return Count;
		}

		bool IList.IsReadOnly
		{
			get { return IsReadOnly; }
		}

		object IList.this[int index]
		{
			get { return this[index]; }
			set
			{
				if (!IsCompatible(value))
					throw new ArgumentNullException("value");

				this[index] = (T)value;
			}
		}

		void IList.Clear()
		{
			((ICollection<T>)this).Clear();
		}

		int IList.IndexOf(object value)
		{
			if (!IsCompatible(value))
				return -1;

			return IndexOf((T)value);
		}

		void IList.Insert(int index, object value)
		{
			if (!IsCompatible(value))
				throw new ArgumentNullException("value");

			Insert(index, (T)value);
		}

		void IList.Remove(object value)
		{
			if (!IsCompatible(value))
				return;

			Remove((T)value);
		}

		void IList.RemoveAt(int index)
		{
			RemoveAt(index);
		}

		bool IList.IsFixedSize
		{
			get { throw new NotImplementedException(); }
		}

		private static readonly bool _isValueType = typeof(T).IsValueType;

		private static bool IsCompatible(object value)
		{
			return !_isValueType || value != null;
		}

		#endregion

		#region IList<T> Members

		public abstract int IndexOf(T item);
		public abstract void Insert(int index, T item);
		public abstract void RemoveAt(int index);
		public abstract T this[int index] { get; set; }

		#endregion
	}
}