namespace Ecng.ComponentModel
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;

	public class EventDispatcher : Disposable
	{
		private readonly Action<Exception> _errorHandler;
		private readonly SynchronizedDictionary<string, BlockingQueue<Action>> _events = [];

		public EventDispatcher(Action<Exception> errorHandler)
		{
			_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		}

		public void Add(Action evt)
		{
			Add(evt, string.Empty);
		}

		public virtual void Add(Action evt, string syncToken)
		{
			if (evt is null)
				throw new ArgumentNullException(nameof(evt));

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
						if (!queue.TryDequeue(out var evt))
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