namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
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
		public bool IsBatchStart { get; set; }
		public bool IsBatchEnd { get; set; }
	}

	/// <summary>
	/// Default batch threshold.
	/// </summary>
	public const int DefaultBatchThreshold = 10;

	private readonly Channel<Operation> _channel;
	private readonly Action<Exception> _errorHandler;
	private readonly Action _batchBegin;
	private readonly Action _batchEnd;
	private readonly int _batchThreshold;
	private Task _processingTask;
	private CancellationTokenSource _internalCts;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelExecutor"/>.
	/// </summary>
	/// <param name="errorHandler">Error handler for unhandled exceptions.</param>
	/// <param name="batchBegin">Optional action called before processing a batch of operations.</param>
	/// <param name="batchEnd">Optional action called after processing a batch of operations.</param>
	/// <param name="batchThreshold">Minimum number of pending operations to trigger batch mode. Default is 10.</param>
	public ChannelExecutor(
		Action<Exception> errorHandler,
		Action batchBegin = null,
		Action batchEnd = null,
		int batchThreshold = DefaultBatchThreshold)
	{
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		_batchBegin = batchBegin;
		_batchEnd = batchEnd;
		_batchThreshold = batchThreshold;
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
		var hasBatchCallbacks = _batchBegin != null && _batchEnd != null;
		var inBatch = false;
		Exception fatalException = null;
		Operation currentOp = null;

		try
		{
			await foreach (var operation in _channel.Reader.ReadAllAsync(cancellationToken).NoWait())
			{
				currentOp = operation;

				// Check for auto batch (pending items >= threshold) - only if not already in explicit batch
				if (!inBatch && hasBatchCallbacks && _channel.Reader.Count >= _batchThreshold - 1)
				{
					_batchBegin();
					ExecuteOperation(operation);

					// Process all currently available operations
					while (_channel.Reader.TryRead(out var nextOp))
						ExecuteOperation(nextOp);

					_batchEnd();
					currentOp = null;
					continue;
				}

				// Handle explicit batch markers
				if (hasBatchCallbacks && operation.IsBatchStart && !inBatch)
				{
					_batchBegin();
					inBatch = true;
				}

				ExecuteOperation(operation);

				if (hasBatchCallbacks && operation.IsBatchEnd && inBatch)
				{
					_batchEnd();
					inBatch = false;
				}

				currentOp = null;
			}
		}
		catch (Exception ex)
		{
			fatalException = ex;
			if (!cancellationToken.IsCancellationRequested)
				_errorHandler(ex);
		}
		finally
		{
			// Complete current operation if it was interrupted
			var completionException = fatalException ?? new OperationCanceledException(cancellationToken);
			currentOp?.CompletionSource?.TrySetException(completionException);

			// Complete all remaining pending operations
			while (_channel.Reader.TryRead(out var pendingOp))
				pendingOp.CompletionSource?.TrySetException(completionException);
		}
	}

	private void ExecuteOperation(Operation operation)
	{
		try
		{
			operation.Action();
			operation.CompletionSource?.TrySetResult(true);
		}
		catch (Exception ex)
		{
			operation.CompletionSource?.TrySetException(ex);
			_errorHandler(ex);
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
	/// Add operation to the execution queue and wait for it to complete.
	/// </summary>
	/// <param name="action">Operation to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task that completes when the operation has been executed.</returns>
	public async Task AddAndWaitAsync(Action action, CancellationToken cancellationToken = default)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		var tcs = new TaskCompletionSource<bool>();

		await _channel.Writer.WriteAsync(new Operation
		{
			Action = action,
			CompletionSource = tcs
		}, cancellationToken).NoWait();

		await tcs.Task.NoWait();
	}

	/// <summary>
	/// Add a batch of operations to the execution queue.
	/// All operations in the batch will be executed together with batch begin/end callbacks.
	/// </summary>
	/// <param name="actions">Operations to execute as a batch.</param>
	public void AddBatch(IEnumerable<Action> actions)
	{
		if (actions == null)
			throw new ArgumentNullException(nameof(actions));

		var list = actions.ToList();
		if (list.Count == 0)
			return;

		for (var i = 0; i < list.Count; i++)
		{
			var action = list[i];
			if (action == null)
				throw new ArgumentNullException(nameof(actions), $"Action at index {i} is null");

			var isFirst = i == 0;
			var isLast = i == list.Count - 1;

			if (!_channel.Writer.TryWrite(new Operation
			{
				Action = action,
				IsBatchStart = isFirst,
				IsBatchEnd = isLast
			}))
			{
				throw new InvalidOperationException("Channel is closed");
			}
		}
	}

	/// <summary>
	/// Add a batch of operations to the execution queue asynchronously.
	/// All operations in the batch will be executed together with batch begin/end callbacks.
	/// </summary>
	/// <param name="actions">Operations to execute as a batch.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	public async ValueTask AddBatchAsync(IEnumerable<Action> actions, CancellationToken cancellationToken = default)
	{
		if (actions == null)
			throw new ArgumentNullException(nameof(actions));

		var list = actions.ToList();
		if (list.Count == 0)
			return;

		for (var i = 0; i < list.Count; i++)
		{
			var action = list[i];
			if (action == null)
				throw new ArgumentNullException(nameof(actions), $"Action at index {i} is null");

			var isFirst = i == 0;
			var isLast = i == list.Count - 1;

			await _channel.Writer.WriteAsync(new Operation
			{
				Action = action,
				IsBatchStart = isFirst,
				IsBatchEnd = isLast
			}, cancellationToken).NoWait();
		}
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

		// Wait for processing to complete with timeout
		if (_processingTask != null)
		{
			try
			{
				await _processingTask.NoWait();
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