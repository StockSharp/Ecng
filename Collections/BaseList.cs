namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public abstract class BaseList<TItem> : BaseCollection<TItem, IList<TItem>>
	{
		protected BaseList()
			: this([])
		{
		}

		protected BaseList(IList<TItem> innerList)
			: base(innerList) 
		{
		}

		protected override TItem OnGetItem(int index)
		{
			return InnerCollection[index];
		}

		protected override void OnInsert(int index, TItem item)
		{
			InnerCollection.Insert(index, item);
		}

		protected override void OnRemoveAt(int index)
		{
			InnerCollection.RemoveAt(index);
		}

		public override int IndexOf(TItem item)
		{
			return InnerCollection.IndexOf(item);
		}
	}
}