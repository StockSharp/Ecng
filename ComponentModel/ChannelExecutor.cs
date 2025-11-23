namespace Ecng.ComponentModel;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Sequential operation executor based on channels.
/// Ensures operations are executed sequentially to prevent file access conflicts.
/// </summary>
public class ChannelExecutor : IAsyncDisposable
{
	private sealed class Operation
	{
		public Action Action { get; set; }
		public TaskCompletionSource<bool> CompletionSource { get; set; }
	}

	private readonly Channel<Operation> _channel;
	private readonly Action<Exception> _errorHandler;
	private Task _processingTask;
	private CancellationTokenSource _internalCts;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelExecutor"/>.
	/// </summary>
	/// <param name="errorHandler">Error handler for unhandled exceptions. If null, exceptions are ignored.</param>
	public ChannelExecutor(Action<Exception> errorHandler = null)
	{
		_errorHandler = errorHandler;
		_channel = Channel.CreateUnbounded<Operation>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false
		});
	}

	/// <summary>
	/// Starts the channel processor.
	/// </summary>
	/// <param name="cancellationToken">External cancellation token to stop processing.</param>
	/// <returns>Task that completes when processing is stopped.</returns>
	public Task RunAsync(CancellationToken cancellationToken = default)
	{
		if (_processingTask != null)
			throw new InvalidOperationException("Already running");

		_internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_processingTask = Task.Run(() => ProcessOperationsAsync(_internalCts.Token), _internalCts.Token);

		return _processingTask;
	}

	private async Task ProcessOperationsAsync(CancellationToken cancellationToken)
	{
		try
		{
			await foreach (var operation in _channel.Reader.ReadAllAsync(cancellationToken).NoWait())
			{
				try
				{
					operation.Action();
					operation.CompletionSource?.TrySetResult(true);
				}
				catch (Exception ex)
				{
					operation.CompletionSource?.TrySetException(ex);
					_errorHandler?.Invoke(ex);
				}
			}
		}
		catch (Exception ex)
		{
			if (!cancellationToken.IsCancellationRequested)
				_errorHandler?.Invoke(ex);
		}
	}

	/// <summary>
	/// Add operation to the execution queue.
	/// </summary>
	/// <param name="action">Operation to execute.</param>
	public void Add(Action action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		if (!_channel.Writer.TryWrite(new Operation
		{
			Action = action
		}))
		{
			throw new InvalidOperationException("Channel is closed");
		}
	}

	/// <summary>
	/// Add operation to the execution queue asynchronously.
	/// </summary>
	/// <param name="action">Operation to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	public async ValueTask AddAsync(Action action, CancellationToken cancellationToken = default)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		await _channel.Writer.WriteAsync(new Operation
		{
			Action = action
		}, cancellationToken).NoWait();
	}

	/// <summary>
	/// Wait for all pending operations to complete.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	public async Task WaitFlushAsync(CancellationToken cancellationToken = default)
	{
		var tcs = new TaskCompletionSource<bool>();

		await _channel.Writer.WriteAsync(new Operation
		{
			Action = () => { },
			CompletionSource = tcs
		}, cancellationToken).NoWait();

		await tcs.Task.NoWait();
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		// Complete the channel to signal no more items
		_channel.Writer.Complete();

		// Cancel the processing task
		_internalCts?.Cancel();

		// Wait for processing to complete with timeout
		if (_processingTask != null)
		{
			try
			{
				await _processingTask.WaitAsync(TimeSpan.FromSeconds(5)).NoWait();
			}
			catch (TimeoutException)
			{
				// Ignore timeout - processing task will be abandoned
			}
			catch (OperationCanceledException)
			{
				// Expected during cancellation
			}
		}

		// Cleanup
		_internalCts?.Dispose();

		GC.SuppressFinalize(this);
	}
}