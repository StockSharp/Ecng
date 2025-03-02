namespace Ecng.Interop.Dde
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;

	using NDde.Server;

	/// <summary>
	/// Provides a DDE server for Excel that handles poke requests and advises clients of updated data.
	/// </summary>
	[CLSCompliant(false)]
	public class XlsDdeServer(string service, Action<string, IList<IList<object>>> poke, Action<Exception> error) : DdeServer(service)
	{
		/// <summary>
		/// Private helper class that dispatches events on dedicated threads.
		/// </summary>
		private class EventDispatcher(Action<Exception> errorHandler) : Disposable
		{
			private readonly Action<Exception> _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
			private readonly SynchronizedDictionary<string, BlockingQueue<Action>> _events = [];

			/// <summary>
			/// Adds an event to be executed.
			/// </summary>
			/// <param name="evt">The event action to add.</param>
			public void Add(Action evt)
			{
				Add(evt, string.Empty);
			}

			/// <summary>
			/// Adds an event to be executed with a synchronization token.
			/// </summary>
			/// <param name="evt">The event action to add.</param>
			/// <param name="syncToken">The synchronization token to group events.</param>
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

			/// <summary>
			/// Disposes the managed resources by closing all event queues.
			/// </summary>
			protected override void DisposeManaged()
			{
				_events.SyncDo(d => d.ForEach(p => p.Value.Close()));
				base.DisposeManaged();
			}
		}

		private readonly SyncObject _registerWait = new();
		private Timer _adviseTimer;
		private readonly EventDispatcher _dispather = new(error);
		private readonly Action<string, IList<IList<object>>> _poke = poke ?? throw new ArgumentNullException(nameof(poke));
		private readonly Action<Exception> _error = error ?? throw new ArgumentNullException(nameof(error));

		/// <summary>
		/// Starts the DDE server and initializes the timer to advise clients.
		/// </summary>
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

		/// <summary>
		/// Handles poke requests from DDE conversations.
		/// </summary>
		/// <param name="conversation">The DDE conversation instance.</param>
		/// <param name="item">The item name requested.</param>
		/// <param name="data">The data payload in byte array format.</param>
		/// <param name="format">The format of the data received.</param>
		/// <returns>A result indicating that the poke has been processed.</returns>
		protected override PokeResult OnPoke(DdeConversation conversation, string item, byte[] data, int format)
		{
			_dispather.Add(() =>
			{
				var rows = XlsDdeSerializer.Deserialize(data);
				_poke(conversation.Topic, rows);
			}, conversation.Topic);

			return PokeResult.Processed;
		}

		/// <summary>
		/// Releases the unmanaged resources and, optionally, the managed resources.
		/// </summary>
		/// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
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