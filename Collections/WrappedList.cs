namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Wintellect.PowerCollections;

	public class WrappedList<TInner, TOuter> : CollectionBase<TOuter>, IList<TOuter>
		where TOuter : TInner
	{
		private readonly IList<TInner> _inner;

		public WrappedList(IList<TInner> inner)
		{
			if (inner == null)
				throw new ArgumentNullException(nameof(inner));

			_inner = inner;
		}

		public override void Add(TOuter item)
		{
			_inner.Add(item);
		}

		public override void Clear()
		{
			_inner.Clear();
		}

		public override bool Remove(TOuter item)
		{
			return _inner.Remove(item);
		}

		public override int Count => _inner.Count;

		public override IEnumerator<TOuter> GetEnumerator()
		{
			return _inner.Cast<TOuter>().GetEnumerator();
		}

		public int IndexOf(TOuter item)
		{
			return _inner.IndexOf(item);
		}

		public void Insert(int index, TOuter item)
		{
			_inner.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			_inner.RemoveAt(index);
		}

		public TOuter this[int index]
		{
			get { return (TOuter)_inner[index]; }
			set { _inner[index] = value; }
		}
	}
}