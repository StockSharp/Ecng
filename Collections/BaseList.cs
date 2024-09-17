namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public abstract class BaseList<TItem>(IList<TItem> innerList) : BaseCollection<TItem, IList<TItem>>(innerList)
	{
		protected BaseList()
			: this([])
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