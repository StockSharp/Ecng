namespace Ecng.Common
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Threading;

	public static class ThreadingHelper
	{
		public static Timer Timer(this Action handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler());
		}

		public static Timer Timer<T>(this Action<T> handler, T arg)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg));
		}

		public static Timer Timer<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2  arg2)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg1, arg2));
		}

		public static Timer Timer<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg1, arg2, arg3));
		}

		public static Timer Timer<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateTimer(s => handler(arg1, arg2, arg3, arg4));
		}

		private static readonly Dictionary<Timer, TimeSpan> _intervals = new Dictionary<Timer,TimeSpan>();

		public static TimeSpan Interval(this Timer timer)
		{
			TimeSpan interval;
			_intervals.TryGetValue(timer, out interval);
			return interval;
		}

		public static Timer Interval(this Timer timer, TimeSpan interval)
		{
			return timer.Interval(interval, interval);
		}

		public static Timer Interval(this Timer timer, TimeSpan start, TimeSpan interval)
		{
			if (timer == null)
				throw new ArgumentNullException(nameof(timer));

			timer.Change(start, interval);
			_intervals[timer] = interval;
			return timer;
		}

		private static Timer CreateTimer(TimerCallback callback)
		{
			return new Timer(callback);
		}

		public static Thread Thread(this Action handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler());
		}

		public static Thread Thread<T>(this Action<T> handler, T arg)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg));
		}

		public static Thread Thread<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2 arg2)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg1, arg2));
		}

		public static Thread Thread<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg1, arg2, arg3));
		}

		public static Thread Thread<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			return CreateThread(() => handler(arg1, arg2, arg3, arg4));
		}

		private static Thread CreateThread(ThreadStart start)
		{
			return new Thread(start) { IsBackground = true };
		}

		public static Thread Name(this Thread thread, string name)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			thread.Name = name;
			return thread;
		}

		public static Thread Culture(this Thread thread, CultureInfo culture)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			if (culture == null)
				throw new ArgumentNullException(nameof(culture));

			thread.CurrentCulture = (CultureInfo)culture.Clone();
			return thread;
		}

		public static Thread UICulture(this Thread thread, CultureInfo culture)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			if (culture == null)
				throw new ArgumentNullException(nameof(culture));

			thread.CurrentUICulture = (CultureInfo)culture.Clone();
			return thread;
		}

		public static Thread Background(this Thread thread, bool isBackground)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			thread.IsBackground = isBackground;
			return thread;
		}

		public static Thread Launch(this Thread thread)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			thread.Start();
			return thread;
		}

		public static void Sleep(this TimeSpan timeOut)
		{
			if (timeOut != TimeSpan.Zero)
				System.Threading.Thread.Sleep(timeOut);
		}

#if !SILVERLIGHT
		public static Thread Priority(this Thread thread, ThreadPriority priority)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			thread.Priority = priority;
			return thread;
		}

		public static Thread STA(this Thread thread)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			thread.SetApartmentState(ApartmentState.STA);
			return thread;
		}

		public static Thread MTA(this Thread thread)
		{
			if (thread == null)
				throw new ArgumentNullException(nameof(thread));

			thread.SetApartmentState(ApartmentState.MTA);
			return thread;
		}

		public static void InvokeAsSTA(this Action action)
		{
			if (action == null)
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
			if (func == null)
				throw new ArgumentNullException(nameof(func));

			var retVal = default(T);
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
			if (rw == null)
				throw new ArgumentNullException(nameof(rw));

			return Try(rw.TryEnterWriteLock, rw.ExitWriteLock, handler, timeOut);
		}

		public static void Read(this ReaderWriterLockSlim rw, Action handler)
		{
			rw.TryRead(handler, -1);
		}

		public static bool TryRead(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
		{
			if (rw == null)
				throw new ArgumentNullException(nameof(rw));

			return Try(rw.TryEnterReadLock, rw.ExitReadLock, handler, timeOut);
		}

		public static void Upgrade(this ReaderWriterLockSlim rw, Action handler)
		{
			rw.TryUpgrade(handler, -1);
		}

		public static bool TryUpgrade(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
		{
			if (rw == null)
				throw new ArgumentNullException(nameof(rw));

			return Try(rw.TryEnterUpgradeableReadLock, rw.ExitUpgradeableReadLock, handler, timeOut);
		}

		private static bool Try(Func<int, bool> enter, Action exit, Action handler, int timeOut = 0)
		{
			if (handler == null)
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

		public static CultureInfo GetSystemCulture()
		{
			CultureInfo culture = null;

			CultureInfo.CurrentCulture.ClearCachedData();
			Thread(() => { culture = CultureInfo.CurrentCulture; }).Launch().Join();

			return culture;
		}

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
#endif
	}
}