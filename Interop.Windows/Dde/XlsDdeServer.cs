namespace Ecng.Interop.Dde;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
	/// Private helper class that dispatches events on dedicated tasks.
	/// </summary>
	private class EventDispatcher(Action<Exception> errorHandler) : Disposable
	{
		private readonly Action<Exception> _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		private readonly SynchronizedDictionary<string, Channel<Action>> _events = [];

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

			var channel = _events.SafeAdd(syncToken, CreateNewTaskChannelPair);

			channel.Writer.TryWrite(() =>
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

		private static Channel<Action> CreateNewTaskChannelPair(string syncToken)
		{
			var channel = Channel.CreateUnbounded<Action>();

			_ = Task.Factory.StartNew(async () =>
			{
				var reader = channel.Reader;

				while (await reader.WaitToReadAsync().NoWait())
				{
					while (reader.TryRead(out var evt))
					{
						evt();
					}
				}
			}, TaskCreationOptions.LongRunning);

			return channel;
		}

		/// <summary>
		/// Disposes the managed resources by completing all event channels.
		/// </summary>
		protected override void DisposeManaged()
		{
			_events.SyncDo(d => d.ForEach(p => p.Value.Writer.Complete()));
			base.DisposeManaged();
		}
	}

	private readonly SyncObject _registerWait = new();
	private ControllablePeriodicTimer _adviseTimer;
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
			_ = Task.Factory.StartNew(() =>
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
			}, TaskCreationOptions.LongRunning);

			Monitor.Wait(regLock);
		}

		if (error != null)
			throw new InvalidOperationException("������ ������� DDE �������.", error);

		// Create a timer that will be used to advise clients of new data.
		_adviseTimer = AsyncHelper.CreatePeriodicTimer(async () =>
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

			await Task.CompletedTask;
		});
		_adviseTimer.Start(TimeSpan.FromSeconds(1));
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