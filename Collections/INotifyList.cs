namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public interface INotifyList<TItem> : IList<TItem>
	{
		event Func<TItem, bool> Adding;

		event Action<TItem> Added;

		event Func<TItem, bool> Removing;

		event Func<int, bool> RemovingAt;

		event Action<TItem> Removed;

		event Func<bool> Clearing;

		event Action Cleared;

		event Func<int, TItem, bool> Inserting;

		event Action<int, TItem> Inserted;

		event Action Changed;
	}

	public interface INotifyListEx<TItem> : INotifyList<TItem>, IListEx<TItem>
	{
	}
}