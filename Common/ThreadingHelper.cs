namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Threading;

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

		private static readonly Dictionary<Timer, TimeSpan> _intervals = [];

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
	}
}