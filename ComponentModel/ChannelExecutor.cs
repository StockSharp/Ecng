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
	/// Add async operation to the group.
	/// </summary>
	/// <param name="action">Async operation to execute (receives CancellationToken).</param>
	void Add(Func<CancellationToken, ValueTask> action);

	/// <summary>
	/// Add async operation to the group asynchronously.
	/// </summary>
	/// <param name="action">Async operation to execute (receives CancellationToken).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	ValueTask AddAsync(Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sequential operation executor based on channels.
/// Ensures operations are executed sequentially to prevent file access conflicts.
/// </summary>
public class ChannelExecutor : AsyncDisposable, IChannelExecutorGroup
{
	private sealed class Operation
	{
		public Func<CancellationToken, ValueTask> Action { get; set; }
		public TaskCompletionSource<bool> CompletionSource { get; set; }
		public Group Group { get; set; }
	}

	private class Group(ChannelExecutor executor, Func<CancellationToken, ValueTask> begin, Func<CancellationToken, ValueTask> end) : IChannelExecutorGroup
	{
		private readonly ChannelExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));

		public Func<CancellationToken, ValueTask> Begin { get; } = begin ?? throw new ArgumentNullException(nameof(begin));
		public Func<CancellationToken, ValueTask> End { get; } = end ?? throw new ArgumentNullException(nameof(end));

		void IChannelExecutorGroup.Add(Func<CancellationToken, ValueTask> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_executor.Enqueue(new Operation
			{
				Action = action,
				Group = this
			});
		}

		ValueTask IChannelExecutorGroup.AddAsync(Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken)
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
	/// Creates a new operation group with async begin/end callbacks.
	/// </summary>
	/// <param name="begin">Async action called before first operation in the group (receives CancellationToken).</param>
	/// <param name="end">Async action called after last operation in the group (always, like finally; receives CancellationToken).</param>
	/// <returns>Group object to add operations to.</returns>
	public IChannelExecutorGroup CreateGroup(Func<CancellationToken, ValueTask> begin, Func<CancellationToken, ValueTask> end)
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
					await FlushOperationsAsync(pendingOperations, cancellationToken).NoWait();
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
				await FlushOperationsAsync(pendingOperations, CancellationToken.None).NoWait();

			// Complete any pending TCS with exception
			var completionException = fatalException ?? new OperationCanceledException(cancellationToken);
			foreach (var op in pendingOperations)
				op.CompletionSource?.TrySetException(completionException);
		}
	}

	private async ValueTask FlushOperationsAsync(List<Operation> operations, CancellationToken cancellationToken)
	{
		Group currentGroup = null;

		async ValueTask SafeBeginAsync(Group g)
		{
			try
			{
				if (g?.Begin != null)
					await g.Begin(cancellationToken).NoWait();
			}
			catch (Exception ex) { _errorHandler(ex); }
		}

		async ValueTask SafeEndAsync(Group g)
		{
			try
			{
				if (g?.End != null)
					await g.End(cancellationToken).NoWait();
			}
			catch (Exception ex) { _errorHandler(ex); }
		}

		foreach (var op in operations)
		{
			if (!ReferenceEquals(op.Group, currentGroup))
			{
				if (currentGroup != null)
					await SafeEndAsync(currentGroup).NoWait();

				currentGroup = op.Group;

				if (currentGroup != null)
					await SafeBeginAsync(currentGroup).NoWait();
			}

			await ExecuteOperationAsync(op, cancellationToken).NoWait();
		}

		if (currentGroup != null)
			await SafeEndAsync(currentGroup).NoWait();
	}

	private async ValueTask ExecuteOperationAsync(Operation operation, CancellationToken cancellationToken)
	{
		try
		{
			await operation.Action(cancellationToken).NoWait();
			operation.CompletionSource?.TrySetResult(true);
		}
		catch (Exception ex)
		{
			operation.CompletionSource?.TrySetException(ex);
			_errorHandler(ex);
		}
	}

	/// <summary>
	/// Add async operation to the execution queue.
	/// </summary>
	/// <param name="action">Async operation to execute (receives CancellationToken).</param>
	public void Add(Func<CancellationToken, ValueTask> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		Enqueue(new() { Action = action });
	}

	/// <summary>
	/// Add async operation to the execution queue asynchronously.
	/// </summary>
	/// <param name="action">Async operation to execute (receives CancellationToken).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task.</returns>
	public ValueTask AddAsync(Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default)
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
	/// Add async operation to the execution queue and wait for it to complete.
	/// </summary>
	/// <param name="action">Async operation to execute (receives CancellationToken).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Task that completes when the operation has been executed.</returns>
	public async Task AddAndWaitAsync(Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default)
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
			Action = _ => default,
			CompletionSource = tcs
		}, cancellationToken).NoWait();

		await tcs.Task.NoWait();
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeManaged()
	{
		// Complete the channel to signal no more items (TryComplete to avoid exception on double-dispose)
		_channel.Writer.TryComplete();

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
	}
}
