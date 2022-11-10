using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Nito.AsyncEx;

#if NET5_0_OR_GREATER
using Nito.AsyncEx.Synchronous;
#endif

namespace Ecng.Common;

public static class AsyncHelper
{
	// https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md
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

	public static (CancellationTokenSource cts, CancellationToken token) CreateChildToken(this CancellationToken token, TimeSpan? delay = null)
	{
		var cts = delay == null ? new CancellationTokenSource() : new CancellationTokenSource(delay.Value);
		return (cts, CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token).Token);
	}

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

	public static Task<TValue> FromResult<TValue>(this TValue value) => Task.FromResult(value);

	// https://stackoverflow.com/a/61260053
	public static async ValueTask AsValueTask<T>(this ValueTask<T> valueTask)
		=> await valueTask;

	public static ValueTask<T> AsValueTask<T>(this Task<T> task)
		=> new(task);

	public static ValueTask AsValueTask(this Task task)
		=> new(task);

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
				results[i] = await source[i].ConfigureAwait(false);
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
				await source[i].ConfigureAwait(false);
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

	public static Task Delay(this TimeSpan delay, CancellationToken cancellationToken = default)
		=> Task.Delay(delay, cancellationToken);

	public static T GetResult<T>(this Task task)
		=> (T)task.GetType().GetProperty(nameof(Task<object>.Result)).GetValue(task);

	public static TaskCompletionSource<TValue> ToCompleteSource<TValue>(this TValue value)
	{
		var source = new TaskCompletionSource<TValue>();
		source.SetResult(value);
		return source;
	}

	public static CancellationToken CreateTimeoutToken(this TimeSpan timeout) => new CancellationTokenSource(timeout).Token;

	public static CancellationTokenSource CreateDelayedToken(this CancellationToken source, TimeSpan timeout)
	{
		var cts = new CancellationTokenSource();
		source.Register(() => Task.Delay(timeout, cts.Token).ContinueWith(_ => cts.Cancel(), cts.Token));
		return cts;
	}

	public static ValueTask CheckNull(this Task task) => new(task ?? Task.CompletedTask);
	public static ValueTask CheckNull(this ValueTask? task) => task ?? default;

	public static ValueTask CatchHandle(this Task task,
		CancellationToken token,
		Action<Exception> handleError = null,
		Action<Exception> handleCancel = null,
		Action finalizer = null,
		bool rethrowErr = false,
		bool rethrowCancel = false)
	{
		return CatchHandle(
			() => new(task),
			token,
			handleError:   handleError,
			handleCancel:  handleCancel,
			finalizer:     finalizer,
			rethrowErr:    rethrowErr,
			rethrowCancel: rethrowCancel
		);
	}

	public static ValueTask CatchHandle(this ValueTask task,
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

	public static ValueTask CatchHandle(Func<Task> getTask,
		CancellationToken token,
		Action<Exception> handleError = null,
		Action<Exception> handleCancel = null,
		Action finalizer = null,
		bool rethrowErr = false,
		bool rethrowCancel = false)
	{
		return CatchHandle(
			() => new(getTask()),
			token,
			handleError:   handleError,
			handleCancel:  handleCancel,
			finalizer:     finalizer,
			rethrowErr:    rethrowErr,
			rethrowCancel: rethrowCancel
		);
	}

	public static async ValueTask CatchHandle(Func<ValueTask> getTask,
		CancellationToken token,
		Action<Exception> handleError = null,
		Action<Exception> handleCancel = null,
		Action finalizer = null,
		bool rethrowErr = false,
		bool rethrowCancel = false)
	{
		try
		{
			await getTask().ConfigureAwait(false);
		}
		catch (Exception e) when (e.IsCancellation(token, out var flattened))
		{
			handleCancel?.Invoke(flattened);
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

	public static TaskCompletionSource<T> CreateTaskCompletionSource<T>(bool forceAsync = true) => new(forceAsync ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

	public static void TrySetFrom<T>(this TaskCompletionSource<T> tcs, Exception e, CancellationToken token)
	{
		if(e.IsCancellation(token))
			tcs.TrySetCanceled();
		else
			tcs.TrySetException(e);
	}

	public static Task WhenCanceled(this CancellationToken token) => CreateTaskCompletionSource<object>().Task.WaitAsync(token);

	public static bool IsCancellation(this Exception e, CancellationToken token) => e.IsCancellation(token, out _);

	public static bool IsCancellation(this Exception e, CancellationToken token, out Exception flattened)
	{
		switch (e)
		{
			case OperationCanceledException:
				flattened = e;
				return token.IsCancellationRequested;
			case AggregateException ae:
				flattened = ae.Flatten();
				return token.IsCancellationRequested && ((AggregateException)flattened).InnerExceptions.All(ie => ie is OperationCanceledException);
			default:
				flattened = e;
				return false;
		}
	}

	public static void Run(Func<ValueTask> getTask)
	{
		if (getTask is null)
			throw new ArgumentNullException(nameof(getTask));

		AsyncContext.Run(() => getTask().AsTask());
	}

	public static T Run<T>(Func<ValueTask<T>> getTask)
	{
		if (getTask is null)
			throw new ArgumentNullException(nameof(getTask));

		return AsyncContext.Run(() => getTask().AsTask());
	}

#if NET5_0_OR_GREATER

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

	public static void TrySetFrom(this TaskCompletionSource tcs, Exception e, CancellationToken token)
	{
		if(e.IsCancellation(token))
			tcs.TrySetCanceled();
		else
			tcs.TrySetException(e);
	}

	public static TaskCompletionSource CreateTaskCompletionSource(bool forceAsync = true) => new(forceAsync ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);

	#else

	public static bool IsCompletedSuccessfully(this Task t) => t.Status == TaskStatus.RanToCompletion;
	public static bool IsCompletedSuccessfully<T>(this Task<T> t) => t.Status == TaskStatus.RanToCompletion;

	#endif
}
