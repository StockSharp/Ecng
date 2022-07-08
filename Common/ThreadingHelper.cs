namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Nito.AsyncEx;

	public static class ThreadingHelper
	{
		public static Timer TimerInvariant(this Action handler)
		{
			return handler.AsInvariant().Timer();
		}

		public static Timer Timer(this Action handler)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler());
		}

		public static Timer Timer<T>(this Action<T> handler, T arg)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg));
		}

		public static Timer Timer<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2  arg2)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg1, arg2));
		}

		public static Timer Timer<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg1, arg2, arg3));
		}

		public static Timer Timer<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg1, arg2, arg3, arg4));
		}

		private static readonly Dictionary<Timer, TimeSpan> _intervals = new();

		public static TimeSpan Interval(this Timer timer)
		{
			lock (_intervals)
			{
				_intervals.TryGetValue(timer, out var interval);
				return interval;
			}
		}

		public static Timer Interval(this Timer timer, TimeSpan interval)
		{
			return timer.Interval(interval, interval);
		}

		public static Timer Interval(this Timer timer, TimeSpan start, TimeSpan interval)
		{
			if (timer is null)
				throw new ArgumentNullException(nameof(timer));

			timer.Change(start, interval);

			lock(_intervals)
				_intervals[timer] = interval;

			return timer;
		}

		private static Timer CreateTimer(TimerCallback callback)
		{
			return new Timer(callback);
		}

		public static Thread ThreadInvariant(this Action handler)
		{
			return handler.AsInvariant().Thread();
		}

		public static Thread Thread(this Action handler)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler());
		}

		public static Thread Thread<T>(this Action<T> handler, T arg)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg));
		}

		public static Thread Thread<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2 arg2)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg1, arg2));
		}

		public static Thread Thread<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg1, arg2, arg3));
		}

		public static Thread Thread<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg1, arg2, arg3, arg4));
		}

		private static Thread CreateThread(ThreadStart start)
		{
			return new Thread(start) { IsBackground = true };
		}

		public static Thread Name(this Thread thread, string name)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			thread.Name = name;
			return thread;
		}

		[Obsolete]
		public static Thread Culture(this Thread thread, CultureInfo culture)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			if (culture is null)
				throw new ArgumentNullException(nameof(culture));

			thread.CurrentCulture = culture.TypedClone();
			return thread;
		}

		[Obsolete]
		public static Thread UICulture(this Thread thread, CultureInfo culture)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			if (culture is null)
				throw new ArgumentNullException(nameof(culture));

			thread.CurrentUICulture = culture.TypedClone();
			return thread;
		}

		public static Thread Background(this Thread thread, bool isBackground)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			thread.IsBackground = isBackground;
			return thread;
		}

		public static Thread Launch(this Thread thread)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			thread.Start();
			return thread;
		}

		public static void Sleep(this TimeSpan timeOut)
		{
			if (timeOut != TimeSpan.Zero)
				System.Threading.Thread.Sleep(timeOut);
		}

		public static Thread Priority(this Thread thread, ThreadPriority priority)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			thread.Priority = priority;
			return thread;
		}

		public static Thread STA(this Thread thread)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			thread.SetApartmentState(ApartmentState.STA);
			return thread;
		}

		public static Thread MTA(this Thread thread)
		{
			if (thread is null)
				throw new ArgumentNullException(nameof(thread));

			thread.SetApartmentState(ApartmentState.MTA);
			return thread;
		}

		public static void InvokeAsSTA(this Action action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));

			InvokeAsSTA<object>(() =>
			{
				action();
				return null;
			});
		}

		// http://stackoverflow.com/questions/518701/clipboard-gettext-returns-null-empty-string
		public static T InvokeAsSTA<T>(this Func<T> func)
		{
			if (func is null)
				throw new ArgumentNullException(nameof(func));

			T retVal = default;
			Exception threadEx = null;

			var staThread = Thread(() =>
			{
				try
				{
					retVal = func();
				}
				catch (Exception ex)
				{
					threadEx = ex;
				}
			})
			.STA()
			.Launch();

			staThread.Join();

			if (threadEx != null)
				throw threadEx;

			return retVal;
		}

		public static void Write(this ReaderWriterLockSlim rw, Action handler)
		{
			rw.TryWrite(handler, -1);
		}

		public static bool TryWrite(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
		{
			if (rw is null)
				throw new ArgumentNullException(nameof(rw));

			return Try(rw.TryEnterWriteLock, rw.ExitWriteLock, handler, timeOut);
		}

		public static void Read(this ReaderWriterLockSlim rw, Action handler)
		{
			rw.TryRead(handler, -1);
		}

		public static bool TryRead(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
		{
			if (rw is null)
				throw new ArgumentNullException(nameof(rw));

			return Try(rw.TryEnterReadLock, rw.ExitReadLock, handler, timeOut);
		}

		public static void Upgrade(this ReaderWriterLockSlim rw, Action handler)
		{
			rw.TryUpgrade(handler, -1);
		}

		public static bool TryUpgrade(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
		{
			if (rw is null)
				throw new ArgumentNullException(nameof(rw));

			return Try(rw.TryEnterUpgradeableReadLock, rw.ExitUpgradeableReadLock, handler, timeOut);
		}

		private static bool Try(Func<int, bool> enter, Action exit, Action handler, int timeOut = 0)
		{
			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			if (!enter(timeOut))
				return false;

			try
			{
				handler();
			}
			finally
			{
				exit();
			}

			return true;
		}

		private struct ReaderWriterLockSlimDispose : IDisposable
		{
			private readonly ReaderWriterLockSlim _rwLock;
			private readonly bool _isRead;

			public ReaderWriterLockSlimDispose(ReaderWriterLockSlim rwLock, bool isRead)
			{
				_rwLock = rwLock ?? throw new ArgumentNullException(nameof(rwLock));
				_isRead = isRead;

				if (_isRead)
					rwLock.EnterReadLock();
				else
					rwLock.EnterWriteLock();
			}

			void IDisposable.Dispose()
			{
				if (_isRead)
					_rwLock.ExitReadLock();
				else
					_rwLock.ExitWriteLock();
			}
		}

		public static IDisposable WriterLock(this ReaderWriterLockSlim rwLock)
			=> new ReaderWriterLockSlimDispose(rwLock, false);

		public static IDisposable ReaderLock(this ReaderWriterLockSlim rwLock)
			=> new ReaderWriterLockSlimDispose(rwLock, true);

		public static bool TryGetUniqueMutex(string name, out Mutex mutex)
		{
			mutex = new Mutex(false, name);

			try
			{
				if (!mutex.WaitOne(1))
					return false;

				mutex = new Mutex(true, name);
			}
			catch (AbandonedMutexException)
			{
				// http://stackoverflow.com/questions/15456986/how-to-gracefully-get-out-of-abandonedmutexexception
				// пред процесс был закрыт, не освободив мьютекс. при получении исключения текущий
				// процесс автоматически становится его владельцем
			}

			return true;
		}

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

		private class CultureHolder : Disposable
		{
			private readonly CultureInfo _culture;

			public CultureHolder() => _culture = System.Threading.Thread.CurrentThread.CurrentCulture;

			protected override void DisposeManaged()
			{
				System.Threading.Thread.CurrentThread.CurrentCulture = _culture;
				base.DisposeManaged();
			}
		}

		public static IDisposable WithCulture(CultureInfo culture)
		{
			var holder = new CultureHolder();
			System.Threading.Thread.CurrentThread.CurrentCulture = culture;
			return holder;
		}

		public static IDisposable WithInvariantCulture() => WithCulture(CultureInfo.InvariantCulture);

		public static Task Delay(this TimeSpan delay, CancellationToken cancellationToken = default)
			=> Task.Delay(delay, cancellationToken);

		public static T GetResult<T>(this Task task)
			=> (T)task.GetType().GetProperty("Result").GetValue(task);

		public static TaskCompletionSource<TValue> ToCompleteSource<TValue>(this TValue value)
		{
			var source = new TaskCompletionSource<TValue>();
			source.SetResult(value);
			return source;
		}

		public static Task<TValue> FromResult<TValue>(this TValue value) => Task.FromResult(value);

		// https://stackoverflow.com/a/61260053
		public static async ValueTask AsValueTask<T>(this ValueTask<T> valueTask)
			=> await valueTask;

		public static ValueTask<T> AsValueTask<T>(this Task<T> task)
			=> new(task);

		public static ValueTask AsValueTask<T>(this Task task)
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
	}
}