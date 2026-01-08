namespace Ecng.ComponentModel;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// The interface for a group of operations executed by <see cref="ChannelExecutor"/>.
/// </summary>
public interface IChannelExecutorGroup
{
	/// <summary>
	/// Add operation to the group.
	/// </summary>
	/// <param name="action">Operation to execute.</param>
	void Add(Action action);

	/// <summary>
	/// Add operation to the group asynchronously.
	/// </summary>
	/// <param name="action">Operation to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	ValueTask AddAsync(Action action, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sequential operation executor based on channels.
/// Ensures operations are executed sequentially to prevent file access conflicts.
/// </summary>
public class ChannelExecutor : IAsyncDisposable, IChannelExecutorGroup
{
	private sealed class Operation
	{
		public Action Action { get; set; }
		public TaskCompletionSource<bool> CompletionSource { get; set; }
		public Group Group { get; set; }
	}

	private class Group(ChannelExecutor executor, Action begin, Action end) : IChannelExecutorGroup
	{
		private readonly ChannelExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));

		public Action Begin { get; } = begin ?? throw new ArgumentNullException(nameof(begin));
		public Action End { get; } = end ?? throw new ArgumentNullException(nameof(end));

		void IChannelExecutorGroup.Add(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_executor.Enqueue(new Operation
			{
				Action = action,
				Group = this
			});
		}

		ValueTask IChannelExecutorGroup.AddAsync(Action action, CancellationToken cancellationToken)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			return _executor.EnqueueAsync(new Operation
			{
				Action = action,
				Group = this
			}, cancellationToken);
		}
	}

	private readonly Channel<Operation> _channel;
	private readonly Action<Exception> _errorHandler;
	private readonly TimeSpan _flushInterval;
	private Task _processingTask;
	private CancellationTokenSource _internalCts;
	private int _pendingCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelExecutor"/>.
	/// </summary>
	/// <param name="errorHandler">Error handler for unhandled exceptions.</param>
	/// <param name="flushInterval">Interval for batch processing. Use TimeSpan.Zero for immediate execution.</param>
	public ChannelExecutor(Action<Exception> errorHandler, TimeSpan flushInterval)
	{
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
		_flushInterval = flushInterval;
		_channel = Channel.CreateUnbounded<Operation>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false
		});
	}

	/// <summary>
	/// Creates a new operation group with begin/end callbacks.
	/// </summary>
	/// <param name="begin">Action called before first operation in the group.</param>
	/// <param name="end">Action called after last operation in the group (always, like finally).</param>
	/// <returns>Group object to add operations to.</returns>
	public IChannelExecutorGroup CreateGroup(Action begin, Action end)
	{
		return new Group(this, begin, end);
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
		Exception fatalException = null;
		var pendingOperations = new List<Operation>();

		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				// Read all available operations first
				while (_channel.Reader.TryRead(out var op))
				{
					Interlocked.Decrement(ref _pendingCount);
					pendingOperations.Add(op);

					// For immediate mode, process after each read
					if (_flushInterval == TimeSpan.Zero)
						break;
				}

				// If we have operations, process them
				if (pendingOperations.Count > 0)
				{
					FlushOperations(pendingOperations);
					pendingOperations.Clear();
					continue;
				}

				// No operations - wait for data
				if (_flushInterval > TimeSpan.Zero)
				{
					// Interval mode: wait for interval period
					try
					{
						await Task.Delay(_flushInterval, cancellationToken).NoWait();
					}
					catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
					{
						break;
					}

					// Check if channel is completed
					if (_channel.Reader.Completion.IsCompleted)
						break;
				}
				else
				{
					// Immediate mode: wait for data
					if (!await _channel.Reader.WaitToReadAsync(cancellationToken).NoWait())
						break; // Channel closed
				}
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// Expected
		}
		catch (Exception ex)
		{
			fatalException = ex;
			_errorHandler(ex);
		}
		finally
		{
			// Process any remaining operations
			while (_channel.Reader.TryRead(out var op))
			{
				Interlocked.Decrement(ref _pendingCount);
				pendingOperations.Add(op);
			}

			if (pendingOperations.Count > 0)
				FlushOperations(pendingOperations);

			// Complete any pending TCS with exception
			var completionException = fatalException ?? new OperationCanceledException(cancellationToken);
			foreach (var op in pendingOperations)
				op.CompletionSource?.TrySetException(completionException);
		}
	}

	private void FlushOperations(List<Operation> operations)
	{
		Group currentGroup = null;

		void SafeBegin(Group g)
		{
			try
			{ g?.Begin?.Invoke(); }
			catch (Exception ex) { _errorHandler(ex); }
		}

		void SafeEnd(Group g)
		{
			try
			{ g?.End?.Invoke(); }
			catch (Exception ex) { _errorHandler(ex); }
		}

		foreach (var op in operations)
		{
			if (!ReferenceEquals(op.Group, currentGroup))
			{
				if (currentGroup != null)
					SafeEnd(currentGroup);

				currentGroup = op.Group;

				if (currentGroup != null)
					SafeBegin(currentGroup);
			}

			ExecuteOperation(op);
		}

		if (currentGroup != null)
			SafeEnd(currentGroup);
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

		Enqueue(new() { Action = action });
	}

	/// <summary>
	/// Add operation to the execution queue asynchronously.
	/// </summary>
	/// <param name="action">Operation to execute.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	public ValueTask AddAsync(Action action, CancellationToken cancellationToken = default)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return EnqueueAsync(new() { Action = action }, cancellationToken);
	}

	private void Enqueue(Operation operation)
	{
		Interlocked.Increment(ref _pendingCount);

		if (!_channel.Writer.TryWrite(operation))
		{
			Interlocked.Decrement(ref _pendingCount);
			throw new ChannelClosedException();
		}
	}

	private async ValueTask EnqueueAsync(Operation operation, CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref _pendingCount);

		try
		{
			await _channel.Writer.WriteAsync(operation, cancellationToken).NoWait();
		}
		catch
		{
			Interlocked.Decrement(ref _pendingCount);
			throw;
		}
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

		await EnqueueAsync(new Operation
		{
			Action = action,
			CompletionSource = tcs
		}, cancellationToken).NoWait();

		await tcs.Task.NoWait();
	}

	/// <summary>
	/// Wait for all pending operations to complete.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	public async Task WaitFlushAsync(CancellationToken cancellationToken = default)
	{
		var tcs = new TaskCompletionSource<bool>();

		await EnqueueAsync(new Operation
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

		// Wait for processing to complete
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
