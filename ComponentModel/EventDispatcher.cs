namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	public class EventDispatcher : Disposable
	{
		private readonly Action<Exception> _errorHandler;
		private readonly SynchronizedDictionary<string, BlockingQueue<Action>> _events = new SynchronizedDictionary<string, BlockingQueue<Action>>();

		public EventDispatcher(Action<Exception> errorHandler)
		{
			if (errorHandler == null)
				throw new ArgumentNullException("errorHandler");

			_errorHandler = errorHandler;
		}

		public void Add(Action evt)
		{
			Add(evt, string.Empty);
		}

		public virtual void Add(Action evt, string syncToken)
		{
			if (evt == null)
				throw new ArgumentNullException("evt");

			var queue = _events.SafeAdd(syncToken, CreateNewThreadQueuePair);

			queue.Enqueue(() =>
			{
				try
				{
					evt();
				}
				catch (Exception ex)
				{
					_errorHandler(ex);
				}
			});
		}

		private static BlockingQueue<Action> CreateNewThreadQueuePair(string syncToken)
		{
			var queue = new BlockingQueue<Action>();

			ThreadingHelper
				.Thread(() =>
				{
					while (!queue.IsClosed)
					{
						Action evt;

						if (!queue.TryDequeue(out evt))
							break;

						evt();
					}
				})
				.Name("EventDispatcher thread #" + syncToken)
				.Start();

			return queue;
		}

		protected override void DisposeManaged()
		{
			_events.SyncDo(d => d.ForEach(p => p.Value.Close()));
			base.DisposeManaged();
		}
	}
}