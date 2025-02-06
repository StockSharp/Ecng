namespace Ecng.Interop.Dde
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	using NDde.Server;

	[CLSCompliant(false)]
	public class XlsDdeServer(string service, Action<string, IList<IList<object>>> poke, Action<Exception> error) : DdeServer(service)
	{
		private class EventDispatcher(Action<Exception> errorHandler) : Disposable
		{
			private readonly Action<Exception> _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
			private readonly SynchronizedDictionary<string, BlockingQueue<Action>> _events = [];

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

		private readonly SyncObject _registerWait = new();
		private Timer _adviseTimer;
		private readonly EventDispatcher _dispather = new EventDispatcher(error);
		private readonly Action<string, IList<IList<object>>> _poke = poke ?? throw new ArgumentNullException(nameof(poke));
		private readonly Action<Exception> _error = error ?? throw new ArgumentNullException(nameof(error));

		public void Start()
		{
			Exception error = null;

			var regLock = new SyncObject();

			lock (regLock)
			{
				ThreadingHelper
					.Thread(() =>
					{
						try
						{
							Register();
							regLock.Pulse();

							_registerWait.Wait();
						}
						catch (Exception ex)
						{
							error = ex;
							regLock.Pulse();
						}
					})
					.Name("Dde thread")
					.Launch();

				Monitor.Wait(regLock);
			}

			if (error != null)
				throw new InvalidOperationException("Ошибка запуска DDE сервера.", error);

			// Create a timer that will be used to advise clients of new data.
			_adviseTimer = ThreadingHelper.Timer(() =>
			{
				try
				{
					// Advise all topic name and item name pairs.
					Advise("*", "*");
				}
				catch (Exception ex)
				{
					_error(ex);
				}
			})
			.Interval(TimeSpan.FromSeconds(1));
		}

		protected override PokeResult OnPoke(DdeConversation conversation, string item, byte[] data, int format)
		{
			_dispather.Add(() =>
			{
				var rows = XlsDdeSerializer.Deserialize(data);
				_poke(conversation.Topic, rows);
			}, conversation.Topic);

			return PokeResult.Processed;
		}

		protected override void Dispose(bool disposing)
		{
			_dispather.Dispose();

			if (disposing)
			{
				if (!_adviseTimer.IsNull())
					_adviseTimer.Dispose();

				_registerWait.Pulse();
			}

			base.Dispose(disposing);
		}
	}
}