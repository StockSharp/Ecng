using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

	private readonly ILogger _log;
	private readonly Channel<Msg> _channel;
	private readonly AsyncLocal<bool> _channelFlag = new();

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

	public AsyncMessageChannel(string name, Action<UnboundedChannelOptions> initOptions = null, ILogger log = null)
	{
		_log = log;
		Name = name;

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
			_log?.LogDebug("{name}: channel is canceled", Name);

			if (RethrowChannelCancellation)
				throw;
		}
		catch (ChannelClosedException e)
		{
			if(e.InnerException == null)
			{
				_log?.LogDebug("{name}: channel is closed", Name);
				if (RethrowChannelClose)
					throw;
			}
			else
			{
				_log?.LogError("{name}: channel is closed with error {errtype}: {msg}", Name, e.InnerException.GetType().Name, e.InnerException.Message);
				if (RethrowChannelClose || RethrowChannelCloseError)
					throw;
			}
		}
		catch (Exception e) // should never happen
		{
			_log?.LogError("{name}: unexpected channel error: {err}", Name, e);
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
				_log?.LogError("{name} channel finished with error: {err}", Name, e);
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
			_log?.LogError("{name}: action '{msgname}' error: {err}", Name, msg.Name, e);

			if (msg.HandleError != null)
				msg.HandleError(e);
			else
				HandleActionError?.Invoke(msg, e);
		}

		void handleCancel(Exception e)
		{
			_log?.LogWarning("{name}: action '{msgname}' canceled: {err}", Name, msg.Name, e);

			if (msg.HandleCancel != null)
				msg.HandleCancel(e);
			else
				HandleActionCancel?.Invoke(msg, e);
		}

		await AsyncHelper.CatchHandle(
			() => {
				var token = msg.Token.IsDefault() ? DefaultActionToken : msg.Token;
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
				_log?.LogDebug("{name}: null message '{msgname}'. stopping...", Name, msg?.Name);
				break;
			}

			await Handle(msg).ConfigureAwait(false);
		}
	}
}
