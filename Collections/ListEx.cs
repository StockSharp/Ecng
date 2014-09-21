namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	public class ListEx<T> : BaseListEx<T>
	{
		private readonly List<T> _innerList = new List<T>();
		private readonly Dictionary<string, Func<T, object>> _properties = new Dictionary<string, Func<T, object>>(); 

		public ListEx()
		{
		}

		public ListEx(IEnumerable<T> items)
		{
			this.AddRange(items);
		}

		public override IEnumerable<T> GetRange(long startIndex, long count, string sortExpression, ListSortDirection directions)
		{
			if (startIndex >= _innerList.Count)
				return Enumerable.Empty<T>();
			else
			{
				if (sortExpression.IsEmpty())
				{
					if ((startIndex + count) > _innerList.Count)
						count = _innerList.Count - startIndex;

					return _innerList.GetRange((int)startIndex, (int)count);
				}
				else
				{
					var items = (IEnumerable<T>)_innerList;

					var func = _properties.SafeAdd(sortExpression, key => i => typeof(T).GetProperty(key).GetValue(i, null));

					items = directions == ListSortDirection.Ascending ? items.OrderBy(func) : items.OrderByDescending(func);

					items = items.Skip((int)startIndex);
					
					if (count >= 0)
						items = items.Take((int)count);

					return items;
				}
			}
		}

		public override void CopyTo(T[] array, int index)
		{
			_innerList.CopyTo(array, index);
		}

		public override void Add(T item)
		{
			_innerList.Add(item);
		}

		public override void Clear()
		{
			_innerList.Clear();
		}

		public override bool Contains(T item)
		{
			return _innerList.Contains(item);
		}

		public override bool Remove(T item)
		{
			return _innerList.Remove(item);
		}

		public override int Count
		{
			get { return _innerList.Count; }
		}

		public override bool IsReadOnly
		{
			get { return false; }
		}

		public override IEnumerator<T> GetEnumerator()
		{
			return _innerList.GetEnumerator();
		}

		public override int IndexOf(T item)
		{
			return _innerList.IndexOf(item);
		}

		public override void Insert(int index, T item)
		{
			_innerList.Insert(index, item);
		}

		public override void RemoveAt(int index)
		{
			_innerList.RemoveAt(index);
		}

		public override T this[int index]
		{
			get { return _innerList[index]; }
			set { _innerList[index] = value; }
		}
	}
}