namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public interface INotifyList<TItem> : IList<TItem>
	{
		event Action<TItem> Adding;

		event Action<TItem> Added;

		event Action<TItem> Removing;

		event Action<TItem> Removed;

		event Action Clearing;

		event Action Cleared;

		event Action<int, TItem> Inserting;

		event Action<int, TItem> Inserted;

		event Action Changed;
	}
}