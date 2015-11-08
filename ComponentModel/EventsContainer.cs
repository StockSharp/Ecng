using Ecng.Collections;

namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	
	using Ecng.Common;

	public abstract class EventsContainer : Disposable
	{
		private readonly Guid _id = Guid.NewGuid();

		[ThreadStatic]
		private static Dictionary<Guid, object> _events;

		[ThreadStatic]
		protected static bool HasNewItems;

		private static readonly List<EventsContainer> _containers = new List<EventsContainer>();

		protected EventsContainer()
		{
			_containers.Add(this);
		}

		public static void BeginSuspend()
		{
			if (_events != null)
				throw new InvalidOperationException();

			_events = new Dictionary<Guid, object>();
		}

		public static void EndSuspend()
		{
			if (_events == null)
				throw new InvalidOperationException();

			while (HasNewItems)
			{
				HasNewItems = false;
				_containers.ForEach(c => c.Flush());
			}

			_events = null;
		}

		public static void Clear()
		{
			_containers.ForEach(c => c.Dispose());
		}

		protected abstract void Flush();

		protected HashSet<T> GetItems<T>(bool create)
		{
			if (_events == null)
				return null;
			else
			{
				var items = (HashSet<T>)_events.TryGetValue(_id);

				if (items == null && create)
				{
					items = new HashSet<T>();
					_events.Add(_id, items);
				}

				return items;
			}
		}

		protected override void DisposeManaged()
		{
			_containers.Remove(this);
			base.DisposeManaged();
		}
	}

	public sealed class EventsContainer<TItem> : EventsContainer
	{
		private readonly Action<Exception> _processDataError;

		public EventsContainer(Action<Exception> processDataError)
		{
			if (processDataError == null)
				throw new ArgumentNullException(nameof(processDataError));

			_processDataError = processDataError;
		}

		public event Action<IEnumerable<TItem>> Event;

		public void Push(IEnumerable<TItem> newItems)
		{
			if (newItems == null)
				throw new ArgumentNullException(nameof(newItems));

			if (newItems.IsEmpty())
				return;

			var items = GetItems<TItem>(true);

			if (items != null)
			{
				foreach (var item in newItems)
				{
					if (items.Add(item))
						HasNewItems = true;
				}
			}
			else
				Raise(newItems);
		}

		protected override void Flush()
		{
			var items = GetItems<TItem>(false);

			if (items == null)
				return;

			Raise(items.CopyAndClear());
		}

		private void Raise(IEnumerable<TItem> items)
		{
			try
			{
				Event.SafeInvoke(items);
			}
			catch (Exception ex)
			{
				_processDataError(ex);
			}
		}
	}
}
