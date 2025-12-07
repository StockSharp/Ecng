namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Nito.AsyncEx;

#if NET5_0_OR_GREATER
using Nito.AsyncEx.Synchronous;
#endif

/// <summary>
/// Provides helper methods for asynchronous operations, including cancellation support, task conversion, and exception handling.
/// </summary>
public static class AsyncHelper
{
	// https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md

	/// <summary>
	/// Awaits the task and supports cancellation by throwing an <see cref="OperationCanceledException"/> if the cancellation token is triggered.
	/// </summary>
	/// <typeparam name="T">The type of the task result.</typeparam>
	/// <param name="task">The task to await.</param>
	/// <param name="cancellationToken">The token used to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation that returns a result of type T.</returns>
	/// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
	public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

		// This disposes the registration as soon as one of the tasks trigger
		using (cancellationToken.Register(state => { ((TaskCompletionSource<object>)state).TrySetResult(null); }, tcs))
		{
			var resultTask = await Task.WhenAny(task, tcs.Task);
			if (resultTask == tcs.Task)
				throw new OperationCanceledException(cancellationToken); // Operation cancelled

			return await task;
		}
	}

	/// <summary>
	/// Creates a child cancellation token linked with the provided token, optionally with a delay.
	/// </summary>
	/// <param name="token">The parent cancellation token.</param>
	/// <param name="delay">Optional delay after which the child token cancels.</param>
	/// <returns>A tuple containing the newly created CancellationTokenSource and the linked CancellationToken.</returns>
	public static (CancellationTokenSource cts, CancellationToken token) CreateChildToken(this CancellationToken token, TimeSpan? delay = null)
	{
		var cts = delay == null ? new CancellationTokenSource() : new CancellationTokenSource(delay.Value);
		return (cts, CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token).Token);
	}

	/// <summary>
	/// Awaits the task and supports cancellation by throwing an <see cref="OperationCanceledException"/> if the cancellation token is triggered.
	/// </summary>
	/// <param name="task">The task to await.</param>
	/// <param name="cancellationToken">The token used to cancel the operation.</param>
	/// <exception cref="OperationCanceledException">Thrown when the cancellation token is triggered.</exception>
	public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

		// This disposes the registration as soon as one of the tasks trigger
		using (cancellationToken.Register(state => { ((TaskCompletionSource<object>)state).TrySetResult(null); }, tcs))
		{
			var resultTask = await Task.WhenAny(task, tcs.Task);
			if (resultTask == tcs.Task)
				throw new OperationCanceledException(cancellationToken); // Operation cancelled

			await task;
		}
	}

	/// <summary>
	/// Returns a completed task with the specified result.
	/// </summary>
	/// <typeparam name="TValue">The type of the result.</typeparam>
	/// <param name="value">The value to be used as the task result.</param>
	/// <returns>A task that has completed successfully with the given result.</returns>
	public static Task<TValue> FromResult<TValue>(this TValue value) => Task.FromResult(value);

	// https://stackoverflow.com/a/61260053

	/// <summary>
	/// Awaits the specified <see cref="ValueTask{T}"/> and returns its result.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="valueTask">The ValueTask to await.</param>
	/// <returns>The result of the ValueTask.</returns>
	public static async ValueTask AsValueTask<T>(this ValueTask<T> valueTask)
		=> await valueTask;

	/// <summary>
	/// Converts a <see cref="Task{T}"/> to a <see cref="ValueTask{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="task">The task to convert.</param>
	/// <returns>A ValueTask representing the task.</returns>
	public static ValueTask<T> AsValueTask<T>(this Task<T> task)
		=> new(task);

	/// <summary>
	/// Converts a <see cref="Task"/> to a <see cref="ValueTask"/>.
	/// </summary>
	/// <param name="task">The task to convert.</param>
	/// <returns>A ValueTask representing the task.</returns>
	public static ValueTask AsValueTask(this Task task)
		=> new(task);

	/// <summary>
	/// Awaits all the provided <see cref="ValueTask{T}"/> tasks and returns an array of their results.
	/// </summary>
	/// <typeparam name="T">The type of the task result.</typeparam>
	/// <param name="tasks">The collection of ValueTasks to await.</param>
	/// <returns>An array of the results from the tasks.</returns>
	/// <exception cref="ArgumentNullException">Thrown if tasks is null.</exception>
	/// <exception cref="AggregateException">Thrown if one or more tasks fail.</exception>
	public static async ValueTask<T[]> WhenAll<T>(this IEnumerable<ValueTask<T>> tasks)
	{
		if (tasks is null)
			throw new ArgumentNullException(nameof(tasks));

		// We don't allocate the list if no task throws
		List<Exception> exceptions = null;

		var source = tasks.ToArray();

		var results = new T[source.Length];

		for (var i = 0; i < source.Length; i++)
		{
			try
			{
				results[i] = await source[i].NoWait();
			}
			catch (Exception ex)
			{
				exceptions ??= new List<Exception>(source.Length);
				exceptions.Add(ex);
			}
		}

		if (exceptions is not null)
			throw new AggregateException(exceptions);

		return results;
	}

	/// <summary>
	/// Awaits all the provided <see cref="ValueTask"/> tasks.
	/// </summary>
	/// <param name="tasks">The collection of ValueTasks to await.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if tasks is null.</exception>
	/// <exception cref="AggregateException">Thrown if one or more tasks fail.</exception>
	public static async ValueTask WhenAll(this IEnumerable<ValueTask> tasks)
	{
		if (tasks is null)
			throw new ArgumentNullException(nameof(tasks));

		// We don't allocate the list if no task throws
		List<Exception> exceptions = null;

		var source = tasks.ToArray();

		for (var i = 0; i < source.Length; i++)
		{
			try
			{
				await source[i].NoWait();
			}
			catch (Exception ex)
			{
				exceptions ??= new List<Exception>(source.Length);
				exceptions.Add(ex);
			}
		}

		if (exceptions is not null)
			throw new AggregateException(exceptions);
	}

	/// <summary>
	/// Creates a delay for the specified time interval.
	/// </summary>
	/// <param name="delay">The time interval to wait.</param>
	/// <param name="cancellationToken">A token to observe while waiting.</param>
	/// <returns>A task that completes after the specified time delay.</returns>
	public static Task Delay(this TimeSpan delay, CancellationToken cancellationToken = default)
		=> Task.Delay(delay, cancellationToken);

	/// <summary>
	/// Retrieves the result of a completed task.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="task">The completed task.</param>
	/// <returns>The result of the task.</returns>
	public static T GetResult<T>(this Task task)
		=> (T)task.GetType().GetProperty(nameof(Task<object>.Result)).GetValue(task);

	/// <summary>
	/// Creates a <see cref="TaskCompletionSource{TValue}"/> that is already completed with the specified value.
	/// </summary>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="value">The value to complete the task with.</param>
	/// <returns>A TaskCompletionSource completed with the given value.</returns>
	public static TaskCompletionSource<TValue> ToCompleteSource<TValue>(this TValue value)
	{
		var source = new TaskCompletionSource<TValue>();
		source.SetResult(value);
		return source;
	}

	/// <summary>
	/// Creates a cancellation token that will be canceled after the specified timeout.
	/// </summary>
	/// <param name="timeout">The timeout after which the token is canceled.</param>
	/// <returns>A CancellationToken that is canceled after the timeout.</returns>
	public static CancellationToken CreateTimeoutToken(this TimeSpan timeout) => new CancellationTokenSource(timeout).Token;

	/// <summary>
	/// Returns a <see cref="ValueTask"/> that completes when the provided task is not null.
	/// </summary>
	/// <param name="task">The task to check for null.</param>
	/// <returns>A ValueTask representing the operation.</returns>
	public static ValueTask CheckNull(this Task task) => new(task ?? Task.CompletedTask);

	/// <summary>
	/// Returns a <see cref="ValueTask"/> from the provided nullable <see cref="ValueTask"/>.
	/// </summary>
	/// <param name="task">The nullable ValueTask.</param>
	/// <returns>A ValueTask representing the operation.</returns>
	public static ValueTask CheckNull(this ValueTask? task) => task ?? default;

	/// <summary>
	/// Executes the task, handles errors and cancellation, and optionally rethrows exceptions.
	/// </summary>
	/// <param name="task">The task to execute.</param>
	/// <param name="token">The cancellation token to observe.</param>
	/// <param name="handleError">An action to handle errors.</param>
	/// <param name="handleCancel">An action to handle cancellation scenarios.</param>
	/// <param name="finalizer">An action to execute in the finally block.</param>
	/// <param name="rethrowErr">Indicates whether to rethrow general exceptions.</param>
	/// <param name="rethrowCancel">Indicates whether to rethrow cancellation exceptions.</param>
	/// <returns>A ValueTask representing the asynchronous operation.</returns>
	public static ValueTask CatchHandle(
		this Task task,
		CancellationToken token,
		Action<Exception> handleError = null,
		Action<Exception> handleCancel = null,
		Action finalizer = null,
		bool rethrowErr = false,
		bool rethrowCancel = false)
	{
		return CatchHandle(
			() => new ValueTask(task),
			token,
			handleError:   handleError,
			handleCancel:  handleCancel,
			finalizer:     finalizer,
			rethrowErr:    rethrowErr,
			rethrowCancel: rethrowCancel
		);
	}

	/// <summary>
	/// Executes the ValueTask, handles errors and cancellation, and optionally rethrows exceptions.
	/// </summary>
	/// <param name="task">The ValueTask to execute.</param>
	/// <param name="token">The cancellation token to observe.</param>
	/// <param name="handleError">An action to handle errors.</param>
	/// <param name="handleCancel">An action to handle cancellation scenarios.</param>
	/// <param name="finalizer">An action to execute in the finally block.</param>
	/// <param name="rethrowErr">Indicates whether to rethrow general exceptions.</param>
	/// <param name="rethrowCancel">Indicates whether to rethrow cancellation exceptions.</param>
	/// <returns>A ValueTask representing the asynchronous operation.</returns>
	public static ValueTask CatchHandle(
		this ValueTask task,
		CancellationToken token,
		Action<Exception> handleError = null,
		Action<Exception> handleCancel = null,
		Action finalizer = null,
		bool rethrowErr = false,
		bool rethrowCancel = false)
	{
		return CatchHandle(
			() => task,
			token,
			handleError:   handleError,
			handleCancel:  handleCancel,
			finalizer:     finalizer,
			rethrowErr:    rethrowErr,
			rethrowCancel: rethrowCancel
		);
	}

	/// <summary>
	/// Executes the task obtained from the provided function, handles errors and cancellation, and optionally rethrows exceptions.
	/// </summary>
	/// <param name="getTask">A function that returns a Task.</param>
	/// <param name="token">The cancellation token to observe.</param>
	/// <param name="handleError">An action to handle errors.</param>
	/// <param name="handleCancel">An action to handle cancellation scenarios.</param>
	/// <param name="finalizer">An action to execute in the finally block.</param>
	/// <param name="rethrowErr">Indicates whether to rethrow general exceptions.</param>
	/// <param name="rethrowCancel">Indicates whether to rethrow cancellation exceptions.</param>
	/// <returns>A ValueTask representing the asynchronous operation.</returns>
	public static ValueTask CatchHandle(
		Func<Task> getTask,
		CancellationToken token,
		Action<Exception> handleError = null,
		Action<Exception> handleCancel = null,
		Action finalizer = null,
		bool rethrowErr = false,
		bool rethrowCancel = false)
	{
		return CatchHandle(
			() => new ValueTask(getTask()),
			token,
			handleError:   handleError,
			handleCancel:  handleCancel,
			finalizer:     finalizer,
			rethrowErr:    rethrowErr,
			rethrowCancel: rethrowCancel
		);
	}

	/// <summary>
	/// Executes the ValueTask obtained from the provided function, handles errors and cancellation, and optionally rethrows exceptions.
	/// </summary>
	/// <param name="getTask">A function that returns a ValueTask.</param>
	/// <param name="token">The cancellation token to observe.</param>
	/// <param name="handleError">An action to handle errors.</param>
	/// <param name="handleCancel">An action to handle cancellation scenarios.</param>
	/// <param name="finalizer">An action to execute in the finally block.</param>
	/// <param name="rethrowErr">Indicates whether to rethrow general exceptions.</param>
	/// <param name="rethrowCancel">Indicates whether to rethrow cancellation exceptions.</param>
	/// <returns>A ValueTask representing the asynchronous operation.</returns>
	public static async ValueTask CatchHandle(
		Func<ValueTask> getTask,
		CancellationToken token,
		Action<Exception> handleError = null,
		Action<Exception> handleCancel = null,
		Action finalizer = null,
		bool rethrowErr = false,
		bool rethrowCancel = false)
	{
		try
		{
			await getTask().NoWait();
		}
		catch (Exception e) when (token.IsCancellationRequested)
		{
			handleCancel?.Invoke(e);

			if (rethrowCancel)
				throw;
		}
		catch (Exception e)
		{
			handleError?.Invoke(e);

			if (rethrowErr)
				throw;
		}
		finally
		{
			finalizer?.Invoke();
		}
	}

	/// <summary>
	/// Creates a new TaskCompletionSource with the specified configuration.
	/// </summary>
	/// <typeparam name="TValue">The type of the task result.</typeparam>
	/// <param name="forceAsync">If true, uses asynchronous continuations.</param>
	/// <returns>A new TaskCompletionSource with the provided configuration.</returns>
	public static TaskCompletionSource<TValue> CreateTaskCompletionSource<TValue>(bool forceAsync = true)
		=> new(forceAsync ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

	/// <summary>
	/// Returns a task that completes when the cancellation token is canceled.
	/// </summary>
	/// <param name="token">The cancellation token.</param>
	/// <returns>A task that completes when the token is canceled.</returns>
	public static Task WhenCanceled(this CancellationToken token)
		=> CreateTaskCompletionSource<object>().Task.WaitAsync(token);

	/// <summary>
	/// Executes the provided asynchronous function in a synchronous context.
	/// </summary>
	/// <param name="getTask">A function that returns a ValueTask.</param>
	public static void Run(Func<ValueTask> getTask)
	{
		if (getTask is null)
			throw new ArgumentNullException(nameof(getTask));

		AsyncContext.Run(() => getTask().AsTask());
	}

	/// <summary>
	/// Executes the provided asynchronous function in a synchronous context and returns the result.
	/// </summary>
	/// <typeparam name="T">The type of the result.</typeparam>
	/// <param name="getTask">A function that returns a ValueTask{T}.</param>
	/// <returns>The result from the asynchronous operation.</returns>
	public static T Run<T>(Func<ValueTask<T>> getTask)
	{
		if (getTask is null)
			throw new ArgumentNullException(nameof(getTask));

		return AsyncContext.Run(() => getTask().AsTask());
	}

#if NET5_0_OR_GREATER

	/// <summary>
	/// Attempts to complete the TaskCompletionSource using the state of the completed task.
	/// </summary>
	/// <param name="tcs">The TaskCompletionSource to complete.</param>
	/// <param name="task">The completed task whose state is used.</param>
	/// <returns>True if TaskCompletionSource was updated successfully; otherwise, false.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the task is not completed or in an invalid state.</exception>
	public static bool TryCompleteFromCompletedTask(this TaskCompletionSource tcs, Task task)
	{
		if (!task.IsCompleted)
			throw new InvalidOperationException("task is not completed");

		if (task.IsFaulted)
			return tcs.TrySetException(task.Exception!.InnerExceptions);

		if (!task.IsCanceled)
			return tcs.TrySetResult();

		try
		{
			task.WaitAndUnwrapException();
		}
		catch (OperationCanceledException exception)
		{
			var token = exception.CancellationToken;
			return token.IsCancellationRequested ? tcs.TrySetCanceled(token) : tcs.TrySetCanceled();
		}

		throw new InvalidOperationException("invalid task state " + task.Status); // should never happen
	}

	/// <summary>
	/// Creates a new TaskCompletionSource with the specified configuration.
	/// </summary>
	/// <param name="forceAsync">If true, uses asynchronous continuations.</param>
	/// <returns>A new TaskCompletionSource with the provided configuration.</returns>
	public static TaskCompletionSource CreateTaskCompletionSource(bool forceAsync = true) => new(forceAsync ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

#else

	/// <summary>
	/// Determines whether the task has completed successfully.
	/// </summary>
	/// <param name="t">The task to check.</param>
	/// <returns>True if the task ran to completion; otherwise, false.</returns>
	public static bool IsCompletedSuccessfully(this Task t) => t.Status == TaskStatus.RanToCompletion;

	/// <summary>
	/// Determines whether the task has completed successfully.
	/// </summary>
	/// <typeparam name="T">The type of the task result.</typeparam>
	/// <param name="t">The task to check.</param>
	/// <returns>True if the task ran to completion; otherwise, false.</returns>
	public static bool IsCompletedSuccessfully<T>(this Task<T> t) => t.Status == TaskStatus.RanToCompletion;

#endif

	// https://stackoverflow.com/a/58234950/8029915

	/// <summary>
	/// Returns an asynchronous enumerable that enforces cancellation using the provided token.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the enumerable.</typeparam>
	/// <param name="source">The source asynchronous enumerable.</param>
	/// <param name="cancellationToken">The cancellation token to observe.</param>
	/// <returns>An asynchronous enumerable that throws an exception if cancellation is requested.</returns>
	/// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
	public static async IAsyncEnumerable<T> WithEnforcedCancellation<T>(this IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		cancellationToken.ThrowIfCancellationRequested();

		await foreach (var item in source)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return item;
		}
	}

	/// <summary>
	/// Configures the awaiter for the specified <see cref="Task"/> to not capture the current synchronization context.
	/// </summary>
	/// <param name="task">The task to configure.</param>
	/// <returns>A configured awaitable for the task.</returns>
	public static ConfiguredTaskAwaitable NoWait(this Task task)
		=> task.ConfigureAwait(false);

	/// <summary>
	/// Configures the awaiter for the specified <see cref="Task{T}"/> to not capture the current synchronization context.
	/// </summary>
	/// <typeparam name="T">The type of the result produced by the task.</typeparam>
	/// <param name="task">The task to configure.</param>
	/// <returns>A configured awaitable for the task.</returns>
	public static ConfiguredTaskAwaitable<T> NoWait<T>(this Task<T> task)
		=> task.ConfigureAwait(false);

	/// <summary>
	/// Configures the awaiter for the specified <see cref="ValueTask"/> to not capture the current synchronization context.
	/// </summary>
	/// <param name="task">The value task to configure.</param>
	/// <returns>A configured awaitable for the value task.</returns>
	public static ConfiguredValueTaskAwaitable NoWait(this ValueTask task)
		=> task.ConfigureAwait(false);

	/// <summary>
	/// Configures the awaiter for the specified <see cref="ValueTask{T}"/> to not capture the current synchronization context.
	/// </summary>
	/// <typeparam name="T">The type of the result produced by the value task.</typeparam>
	/// <param name="task">The value task to configure.</param>
	/// <returns>A configured awaitable for the value task.</returns>
	public static ConfiguredValueTaskAwaitable<T> NoWait<T>(this ValueTask<T> task)
		=> task.ConfigureAwait(false);

	/// <summary>
	/// Configures the awaiter for the specified <see cref="IAsyncEnumerable{T}"/> to not capture the current synchronization context.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the asynchronous enumerable.</typeparam>
	/// <param name="enumerable">The asynchronous enumerable to configure.</param>
	/// <returns>A configured cancellable asynchronous enumerable.</returns>
	public static ConfiguredCancelableAsyncEnumerable<T> NoWait<T>(this IAsyncEnumerable<T> enumerable)
		=> enumerable.ConfigureAwait(false);

	/// <summary>
	/// Starts a periodic timer that executes the specified action at the given interval.
	/// </summary>
	/// <param name="handler">The action to be executed periodically.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <param name="cancellationToken">Optional cancellation token to stop the timer.</param>
	/// <returns>A Task representing the timer operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
	public static Task StartPeriodicTimer(Action handler, TimeSpan interval, CancellationToken cancellationToken = default)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return StartPeriodicTimer(() =>
		{
			handler();
			return Task.CompletedTask;
		}, interval, cancellationToken);
	}

	/// <summary>
	/// Starts a periodic timer that executes the specified action with one argument at the given interval.
	/// </summary>
	/// <typeparam name="T">The type of the argument.</typeparam>
	/// <param name="handler">The action to be executed periodically.</param>
	/// <param name="arg">The argument passed to the action.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <param name="cancellationToken">Optional cancellation token to stop the timer.</param>
	/// <returns>A Task representing the timer operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
	public static Task StartPeriodicTimer<T>(Action<T> handler, T arg, TimeSpan interval, CancellationToken cancellationToken = default)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return StartPeriodicTimer(() =>
		{
			handler(arg);
			return Task.CompletedTask;
		}, interval, cancellationToken);
	}

	/// <summary>
	/// Starts a periodic timer that executes the specified asynchronous function at the given interval.
	/// </summary>
	/// <param name="handler">The asynchronous function to be executed periodically.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <param name="cancellationToken">Optional cancellation token to stop the timer.</param>
	/// <returns>A Task representing the timer operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
	public static Task StartPeriodicTimer(Func<Task> handler, TimeSpan interval, CancellationToken cancellationToken = default)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return StartPeriodicTimerCore(handler, interval, TimeSpan.Zero, cancellationToken);
	}

	/// <summary>
	/// Starts a periodic timer that executes the specified action at the given interval with an initial delay.
	/// </summary>
	/// <param name="handler">The action to be executed periodically.</param>
	/// <param name="start">The delay before the first execution.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <param name="cancellationToken">Optional cancellation token to stop the timer.</param>
	/// <returns>A Task representing the timer operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
	public static Task StartPeriodicTimer(Action handler, TimeSpan start, TimeSpan interval, CancellationToken cancellationToken = default)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return StartPeriodicTimer(() =>
		{
			handler();
			return Task.CompletedTask;
		}, start, interval, cancellationToken);
	}

	/// <summary>
	/// Starts a periodic timer that executes the specified asynchronous function at the given interval with an initial delay.
	/// </summary>
	/// <param name="handler">The asynchronous function to be executed periodically.</param>
	/// <param name="start">The delay before the first execution.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <param name="cancellationToken">Optional cancellation token to stop the timer.</param>
	/// <returns>A Task representing the timer operation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
	public static Task StartPeriodicTimer(Func<Task> handler, TimeSpan start, TimeSpan interval, CancellationToken cancellationToken = default)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return StartPeriodicTimerCore(handler, interval, start, cancellationToken);
	}

	/// <summary>
	/// Core implementation for the periodic timer.
	/// </summary>
	/// <param name="handler">The asynchronous function to be executed periodically.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <param name="initialDelay">The delay before the first execution.</param>
	/// <param name="cancellationToken">Cancellation token to stop the timer.</param>
	/// <returns>A Task representing the timer operation.</returns>
	private static async Task StartPeriodicTimerCore(Func<Task> handler, TimeSpan interval, TimeSpan initialDelay, CancellationToken cancellationToken)
	{
		using var timer = new PeriodicTimer(interval);

		// Wait for initial delay if specified
		if (initialDelay > TimeSpan.Zero)
			await initialDelay.Delay(cancellationToken).NoWait();

		while (!cancellationToken.IsCancellationRequested)
		{
			// Wait for the next tick first
			try
			{
				if (!await timer.WaitForNextTickAsync(cancellationToken).NoWait())
					break;
			}
			catch (OperationCanceledException)
			{
				// Timer was cancelled, exit gracefully
				break;
			}

			// Then execute handler
			try
			{
				await handler().NoWait();
			}
			catch when (cancellationToken.IsCancellationRequested)
			{
				// Timer was cancelled during handler execution, exit gracefully
				break;
			}
			catch
			{
				// Allow exceptions from handler to propagate
				throw;
			}
		}
	}

	/// <summary>
	/// Creates a periodic timer that can be started, stopped, and have its interval changed.
	/// </summary>
	/// <param name="handler">The action to be executed periodically.</param>
	/// <returns>A ControllablePeriodicTimer instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
	public static ControllablePeriodicTimer CreatePeriodicTimer(Action handler)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return new ControllablePeriodicTimer(() =>
		{
			handler();
			return Task.CompletedTask;
		});
	}

	/// <summary>
	/// Creates a periodic timer that can be started, stopped, and have its interval changed.
	/// </summary>
	/// <param name="handler">The asynchronous function to be executed periodically.</param>
	/// <returns>A ControllablePeriodicTimer instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
	public static ControllablePeriodicTimer CreatePeriodicTimer(Func<Task> handler)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return new ControllablePeriodicTimer(handler);
	}
}
