using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ecng.Common;

public class AsyncMessageChannel
{
	public class Msg
	{
		public Func<CancellationToken, ValueTask> Action  { get; }
		public Action<Exception> HandleError              { get; init; }
		public Action<Exception> HandleCancel             { get; init; }
		public Action Finalizer                           { get; init; }
		public CancellationToken Token                    { get; init; }
		public string Name                                { get; init; }

		public Msg(Func<CancellationToken, Task> action)       => Action = t => new(action(t));
		public Msg(Func<Task> action)                          : this(_ => action()) {}

		public Msg(Func<CancellationToken, ValueTask> action)  => Action = action;
		public Msg(Func<ValueTask> action)                     : this(_ => action()) {}
	}

	private readonly Channel<Msg> _channel;
	private readonly AsyncLocal<bool> _channelFlag = new();
	private readonly Action<string, object[]> _logDebug;
	private readonly Action<string, object[]> _logWarning;
	private readonly Action<string, object[]> _logError;

	public bool RethrowChannelCancellation   { get; init; }
	public bool RethrowChannelClose          { get; init; }
	public bool RethrowChannelCloseError     { get; init; } = true;

	public string ThrowMessageOnClosedChannelWrite { get; init; }

	public Action<Msg, Exception> HandleActionCancel  { get; init; } // Rethrow* is ignored if not null and handler returns true
	public Action<Msg, Exception> HandleActionError   { get; init; } // Rethrow* is ignored if not null and handler returns true

	public CancellationToken DefaultActionToken { get; init; }

	public Task ChannelTask { get; private set; }
	public string Name { get; }

	public bool IsInChannel => _channelFlag.Value;
	public bool IsComplete { get; private set; }

	// handle remaining messages when stopping.
	public bool HandleRemainingMessages { get; set; }

	public AsyncMessageChannel(string name, Action<UnboundedChannelOptions> initOptions = null, Action<string, object[]> logDebug = null, Action<string, object[]> logWarning = null, Action<string, object[]> logError = null)
	{
		Name = name;

		_logDebug = logDebug;
		_logWarning = logWarning;
		_logError = logError;

		var opts = new UnboundedChannelOptions();
		initOptions?.Invoke(opts);

		_channel = Channel.CreateUnbounded<Msg>(opts);
	}

	public void EnsureStarted()
	{
		if (ChannelTask != null)
			return;

		lock (this)
		{
			if (ChannelTask != null)
				return;

			// ReSharper disable once MethodSupportsCancellation
			ChannelTask = Task.Run(RunChannelAsync);
		}
	}

	public void TryStopChannel(Exception err = null)
		=> IsComplete |= _channel.Writer.TryComplete(err);

	public bool Post(Msg msg)
	{
		var result = _channel.Writer.TryWrite(msg);
		if(!result && !ThrowMessageOnClosedChannelWrite.IsEmptyOrWhiteSpace())
			throw new InvalidOperationException($"{Name}: " + ThrowMessageOnClosedChannelWrite);

		return result;
	}

	private async ValueTask<Msg> ReadMsgAsync()
	{
		try
		{
			// ReSharper disable once MethodSupportsCancellation
			return await _channel.Reader.ReadAsync().ConfigureAwait(false);
		}
		catch (Exception e) when (e.IsCancellation())
		{
			_logDebug?.Invoke("{name}: channel is canceled", new object[] { Name });

			if (RethrowChannelCancellation)
				throw;
		}
		catch (ChannelClosedException e)
		{
			if(e.InnerException == null)
			{
				_logDebug?.Invoke("{name}: channel is closed", new object[] { Name });
				if (RethrowChannelClose)
					throw;
			}
			else
			{
				_logError?.Invoke("{name}: channel is closed with error {errtype}: {msg}", new object[] { Name, e.InnerException.GetType().Name, e.InnerException.Message });
				if (RethrowChannelClose || RethrowChannelCloseError)
					throw;
			}
		}
		catch (Exception e) // should never happen
		{
			_logError?.Invoke("{name}: unexpected channel error: {err}", new object[] { Name, e });
			throw;
		}

		return default;
	}

	private async Task RunChannelAsync()
	{
		Exception e = null;

		_channelFlag.Value = true;

		try
		{
			await RunChannelAsyncImpl();
		}
		catch (Exception ex)
		{
			if(!ex.IsCancellation())
			{
				e = ex;
				_logError?.Invoke("{name} channel finished with error: {err}", new object[] { Name, e });
			}

			throw;
		}
		finally
		{
			TryStopChannel(e);
		}
	}

	private async ValueTask Handle(Msg msg)
	{
		void handleError(Exception e)
		{
			_logError?.Invoke("{name}: action '{msgname}' error: {err}", new object[] { Name, msg.Name, e });

			if (msg.HandleError != null)
				msg.HandleError(e);
			else
				HandleActionError?.Invoke(msg, e);
		}

		void handleCancel(Exception e)
		{
			_logWarning?.Invoke("{name}: action '{msgname}' canceled: {err}", new object[] { Name, msg.Name, e });

			if (msg.HandleCancel != null)
				msg.HandleCancel(e);
			else
				HandleActionCancel?.Invoke(msg, e);
		}

		await AsyncHelper.CatchHandle(
			() => {
				var token = msg.Token == default ? DefaultActionToken : msg.Token;
				token.ThrowIfCancellationRequested();

				if (IsComplete && !HandleRemainingMessages)
					throw new InvalidOperationException("channel is closed");

				return msg.Action(token);
			},
			handleError:   handleError,
			handleCancel:  handleCancel,
			rethrowErr:    false,
			rethrowCancel: false,
			finalizer:     msg.Finalizer
		);
	}

	private async Task RunChannelAsyncImpl()
	{
		while(true)
		{
			var msg = await ReadMsgAsync().ConfigureAwait(false);
			if (msg?.Action == null)
			{
				_logDebug?.Invoke("{name}: null message '{msgname}'. stopping...", new object[] { Name, msg?.Name });
				break;
			}

			await Handle(msg).ConfigureAwait(false);
		}
	}
}
