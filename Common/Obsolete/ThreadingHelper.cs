namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

/// <summary>
/// Provides helper methods for thread and timer operations.
/// </summary>
public static class ThreadingHelper
{
	/// <summary>
	/// Creates a Timer that executes the specified action with invariant culture.
	/// </summary>
	/// <param name="handler">The action to be executed by the timer.</param>
	/// <returns>A new Timer instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer TimerInvariant(this Action handler)
	{
		return handler.AsInvariant().Timer();
	}

	/// <summary>
	/// Creates a Timer that executes the specified action.
	/// </summary>
	/// <param name="handler">The action to be executed by the timer.</param>
	/// <returns>A new Timer instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer Timer(this Action handler)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateTimer(s => handler());
	}

	/// <summary>
	/// Creates a Timer that executes the specified action with one argument.
	/// </summary>
	/// <typeparam name="T">The type of the argument.</typeparam>
	/// <param name="handler">The action to be executed by the timer.</param>
	/// <param name="arg">The argument passed to the action.</param>
	/// <returns>A new Timer instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer Timer<T>(this Action<T> handler, T arg)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateTimer(s => handler(arg));
	}

	/// <summary>
	/// Creates a Timer that executes the specified action with two arguments.
	/// </summary>
	/// <typeparam name="T1">The type of the first argument.</typeparam>
	/// <typeparam name="T2">The type of the second argument.</typeparam>
	/// <param name="handler">The action to be executed by the timer.</param>
	/// <param name="arg1">The first argument passed to the action.</param>
	/// <param name="arg2">The second argument passed to the action.</param>
	/// <returns>A new Timer instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer Timer<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2 arg2)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateTimer(s => handler(arg1, arg2));
	}

	/// <summary>
	/// Creates a Timer that executes the specified action with three arguments.
	/// </summary>
	/// <typeparam name="T1">The type of the first argument.</typeparam>
	/// <typeparam name="T2">The type of the second argument.</typeparam>
	/// <typeparam name="T3">The type of the third argument.</typeparam>
	/// <param name="handler">The action to be executed by the timer.</param>
	/// <param name="arg1">The first argument passed to the action.</param>
	/// <param name="arg2">The second argument passed to the action.</param>
	/// <param name="arg3">The third argument passed to the action.</param>
	/// <returns>A new Timer instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer Timer<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateTimer(s => handler(arg1, arg2, arg3));
	}

	/// <summary>
	/// Creates a Timer that executes the specified action with four arguments.
	/// </summary>
	/// <typeparam name="T1">The type of the first argument.</typeparam>
	/// <typeparam name="T2">The type of the second argument.</typeparam>
	/// <typeparam name="T3">The type of the third argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth argument.</typeparam>
	/// <param name="handler">The action to be executed by the timer.</param>
	/// <param name="arg1">The first argument passed to the action.</param>
	/// <param name="arg2">The second argument passed to the action.</param>
	/// <param name="arg3">The third argument passed to the action.</param>
	/// <param name="arg4">The fourth argument passed to the action.</param>
	/// <returns>A new Timer instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer Timer<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateTimer(s => handler(arg1, arg2, arg3, arg4));
	}

	private static readonly Dictionary<Timer, TimeSpan> _intervals = [];
	private static readonly Lock _intervalsLock = new();

	/// <summary>
	/// Gets the interval associated with the specified timer.
	/// </summary>
	/// <param name="timer">The timer whose interval is retrieved.</param>
	/// <returns>The TimeSpan interval of the timer.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static TimeSpan Interval(this Timer timer)
	{
		using (_intervalsLock.EnterScope())
		{
			_intervals.TryGetValue(timer, out var interval);
			return interval;
		}
	}

	/// <summary>
	/// Sets the start and execution interval for the specified timer.
	/// </summary>
	/// <param name="timer">The timer to configure.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <returns>The configured timer.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer Interval(this Timer timer, TimeSpan interval)
	{
		return timer.Interval(interval, interval);
	}

	/// <summary>
	/// Sets the start delay and execution interval for the specified timer.
	/// </summary>
	/// <param name="timer">The timer to configure.</param>
	/// <param name="start">The start delay before the timer begins execution.</param>
	/// <param name="interval">The interval between timer executions.</param>
	/// <returns>The configured timer.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Timer Interval(this Timer timer, TimeSpan start, TimeSpan interval)
	{
		if (timer is null)
			throw new ArgumentNullException(nameof(timer));

		timer.Change(start, interval);

		using (_intervalsLock.EnterScope())
			_intervals[timer] = interval;

		return timer;
	}

	/// <summary>
	/// Creates a Timer using the specified callback.
	/// </summary>
	/// <param name="callback">The TimerCallback to be executed.</param>
	/// <returns>A new Timer instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	private static Timer CreateTimer(TimerCallback callback)
	{
		return new Timer(callback);
	}

	/// <summary>
	/// Creates a Thread that executes the specified action with invariant culture.
	/// </summary>
	/// <param name="handler">The action to be executed by the thread.</param>
	/// <returns>A new Thread instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread ThreadInvariant(this Action handler)
	{
		return handler.AsInvariant().Thread();
	}

	/// <summary>
	/// Creates a Thread that executes the specified action.
	/// </summary>
	/// <param name="handler">The action to be executed by the thread.</param>
	/// <returns>A new Thread instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Thread(this Action handler)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateThread(() => handler());
	}

	/// <summary>
	/// Creates a Thread that executes the specified action with one argument.
	/// </summary>
	/// <typeparam name="T">The type of the argument.</typeparam>
	/// <param name="handler">The action to be executed by the thread.</param>
	/// <param name="arg">The argument passed to the action.</param>
	/// <returns>A new Thread instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Thread<T>(this Action<T> handler, T arg)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateThread(() => handler(arg));
	}

	/// <summary>
	/// Creates a Thread that executes the specified action with two arguments.
	/// </summary>
	/// <typeparam name="T1">The type of the first argument.</typeparam>
	/// <typeparam name="T2">The type of the second argument.</typeparam>
	/// <param name="handler">The action to be executed by the thread.</param>
	/// <param name="arg1">The first argument passed to the action.</param>
	/// <param name="arg2">The second argument passed to the action.</param>
	/// <returns>A new Thread instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Thread<T1, T2>(this Action<T1, T2> handler, T1 arg1, T2 arg2)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateThread(() => handler(arg1, arg2));
	}

	/// <summary>
	/// Creates a Thread that executes the specified action with three arguments.
	/// </summary>
	/// <typeparam name="T1">The type of the first argument.</typeparam>
	/// <typeparam name="T2">The type of the second argument.</typeparam>
	/// <typeparam name="T3">The type of the third argument.</typeparam>
	/// <param name="handler">The action to be executed by the thread.</param>
	/// <param name="arg1">The first argument passed to the action.</param>
	/// <param name="arg2">The second argument passed to the action.</param>
	/// <param name="arg3">The third argument passed to the action.</param>
	/// <returns>A new Thread instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Thread<T1, T2, T3>(this Action<T1, T2, T3> handler, T1 arg1, T2 arg2, T3 arg3)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateThread(() => handler(arg1, arg2, arg3));
	}

	/// <summary>
	/// Creates a Thread that executes the specified action with four arguments.
	/// </summary>
	/// <typeparam name="T1">The type of the first argument.</typeparam>
	/// <typeparam name="T2">The type of the second argument.</typeparam>
	/// <typeparam name="T3">The type of the third argument.</typeparam>
	/// <typeparam name="T4">The type of the fourth argument.</typeparam>
	/// <param name="handler">The action to be executed by the thread.</param>
	/// <param name="arg1">The first argument passed to the action.</param>
	/// <param name="arg2">The second argument passed to the action.</param>
	/// <param name="arg3">The third argument passed to the action.</param>
	/// <param name="arg4">The fourth argument passed to the action.</param>
	/// <returns>A new Thread instance.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Thread<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		if (handler is null)
			throw new ArgumentNullException(nameof(handler));

		return CreateThread(() => handler(arg1, arg2, arg3, arg4));
	}

	/// <summary>
	/// Creates a new background thread using the specified start method.
	/// </summary>
	/// <param name="start">The ThreadStart delegate that represents the method to be invoked when the thread begins executing.</param>
	/// <returns>A new Thread instance set as a background thread.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	private static Thread CreateThread(ThreadStart start)
	{
		return new Thread(start) { IsBackground = true };
	}

	/// <summary>
	/// Sets the name of the specified thread.
	/// </summary>
	/// <param name="thread">The thread to name.</param>
	/// <param name="name">The name to set.</param>
	/// <returns>The thread with the assigned name.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Name(this Thread thread, string name)
	{
		if (thread is null)
			throw new ArgumentNullException(nameof(thread));

		thread.Name = name;
		return thread;
	}

	/// <summary>
	/// Sets whether the specified thread is a background thread.
	/// </summary>
	/// <param name="thread">The thread to configure.</param>
	/// <param name="isBackground">True to set the thread as a background thread; otherwise, false.</param>
	/// <returns>The configured thread.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Background(this Thread thread, bool isBackground)
	{
		if (thread is null)
			throw new ArgumentNullException(nameof(thread));

		thread.IsBackground = isBackground;
		return thread;
	}

	/// <summary>
	/// Starts the specified thread.
	/// </summary>
	/// <param name="thread">The thread to start.</param>
	/// <returns>The started thread.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Launch(this Thread thread)
	{
		if (thread is null)
			throw new ArgumentNullException(nameof(thread));

		thread.Start();
		return thread;
	}

	/// <summary>
	/// Suspends the current thread for the specified time interval.
	/// </summary>
	/// <param name="timeOut">The TimeSpan for which to sleep.</param>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static void Sleep(this TimeSpan timeOut)
	{
		if (timeOut != TimeSpan.Zero)
			System.Threading.Thread.Sleep(timeOut);
	}

	/// <summary>
	/// Sets the priority of the specified thread.
	/// </summary>
	/// <param name="thread">The thread whose priority is to be set.</param>
	/// <param name="priority">The thread priority to set.</param>
	/// <returns>The thread with the updated priority.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static Thread Priority(this Thread thread, ThreadPriority priority)
	{
		if (thread is null)
			throw new ArgumentNullException(nameof(thread));

		thread.Priority = priority;
		return thread;
	}

	/// <summary>
	/// Executes the specified action within a write lock.
	/// </summary>
	/// <param name="rw">The ReaderWriterLockSlim instance.</param>
	/// <param name="handler">The action to execute within the lock.</param>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static void Write(this ReaderWriterLockSlim rw, Action handler)
	{
		rw.TryWrite(handler, -1);
	}

	/// <summary>
	/// Attempts to execute the specified action within a write lock.
	/// </summary>
	/// <param name="rw">The ReaderWriterLockSlim instance.</param>
	/// <param name="handler">The action to execute within the lock.</param>
	/// <param name="timeOut">The timeout in milliseconds to wait for acquiring the lock.</param>
	/// <returns>True if the lock was acquired and the action executed; otherwise, false.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static bool TryWrite(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
	{
		if (rw is null)
			throw new ArgumentNullException(nameof(rw));

		return Try(rw.TryEnterWriteLock, rw.ExitWriteLock, handler, timeOut);
	}

	/// <summary>
	/// Executes the specified action within a read lock.
	/// </summary>
	/// <param name="rw">The ReaderWriterLockSlim instance.</param>
	/// <param name="handler">The action to execute within the read lock.</param>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static void Read(this ReaderWriterLockSlim rw, Action handler)
	{
		rw.TryRead(handler, -1);
	}

	/// <summary>
	/// Attempts to execute the specified action within a read lock.
	/// </summary>
	/// <param name="rw">The ReaderWriterLockSlim instance.</param>
	/// <param name="handler">The action to execute within the read lock.</param>
	/// <param name="timeOut">The timeout in milliseconds to wait for acquiring the lock.</param>
	/// <returns>True if the lock was acquired and the action executed; otherwise, false.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static bool TryRead(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
	{
		if (rw is null)
			throw new ArgumentNullException(nameof(rw));

		return Try(rw.TryEnterReadLock, rw.ExitReadLock, handler, timeOut);
	}

	/// <summary>
	/// Executes the specified action within an upgradeable read lock.
	/// </summary>
	/// <param name="rw">The ReaderWriterLockSlim instance.</param>
	/// <param name="handler">The action to execute within the upgradeable read lock.</param>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static void Upgrade(this ReaderWriterLockSlim rw, Action handler)
	{
		rw.TryUpgrade(handler, -1);
	}

	/// <summary>
	/// Attempts to execute the specified action within an upgradeable read lock.
	/// </summary>
	/// <param name="rw">The ReaderWriterLockSlim instance.</param>
	/// <param name="handler">The action to execute within the upgradeable read lock.</param>
	/// <param name="timeOut">The timeout in milliseconds to wait for acquiring the lock.</param>
	/// <returns>True if the lock was acquired and the action executed; otherwise, false.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static bool TryUpgrade(this ReaderWriterLockSlim rw, Action handler, int timeOut = 0)
	{
		if (rw is null)
			throw new ArgumentNullException(nameof(rw));

		return Try(rw.TryEnterUpgradeableReadLock, rw.ExitUpgradeableReadLock, handler, timeOut);
	}

	/// <summary>
	/// Helper method to attempt entering a lock, executing an action, and releasing the lock.
	/// </summary>
	/// <param name="enter">Function to attempt entering the lock.</param>
	/// <param name="exit">Action to exit the lock.</param>
	/// <param name="handler">The action to execute once the lock is acquired.</param>
	/// <param name="timeOut">The timeout in milliseconds to wait for the lock.</param>
	/// <returns>True if the action is executed; otherwise, false.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
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

	/// <summary>
	/// Provides a disposable wrapper for ReaderWriterLockSlim that enters a lock on creation and exits on disposal.
	/// </summary>
	private readonly struct ReaderWriterLockSlimDispose : IDisposable
	{
		private readonly ReaderWriterLockSlim _rwLock;
		private readonly bool _isRead;

		/// <summary>
		/// Initializes a new instance of the ReaderWriterLockSlimDispose struct.
		/// </summary>
		/// <param name="rwLock">The ReaderWriterLockSlim instance.</param>
		/// <param name="isRead">True to enter a read lock; false to enter a write lock.</param>
		public ReaderWriterLockSlimDispose(ReaderWriterLockSlim rwLock, bool isRead)
		{
			_rwLock = rwLock ?? throw new ArgumentNullException(nameof(rwLock));
			_isRead = isRead;

			if (_isRead)
				rwLock.EnterReadLock();
			else
				rwLock.EnterWriteLock();
		}

		/// <summary>
		/// Exits the lock when disposed.
		/// </summary>
		readonly void IDisposable.Dispose()
		{
			if (_isRead)
				_rwLock.ExitReadLock();
			else
				_rwLock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Acquires a write lock and returns an IDisposable that releases the lock when disposed.
	/// </summary>
	/// <param name="rwLock">The ReaderWriterLockSlim instance.</param>
	/// <returns>An IDisposable that releases the write lock.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static IDisposable WriterLock(this ReaderWriterLockSlim rwLock)
		=> new ReaderWriterLockSlimDispose(rwLock, false);

	/// <summary>
	/// Acquires a read lock and returns an IDisposable that releases the lock when disposed.
	/// </summary>
	/// <param name="rwLock">The ReaderWriterLockSlim instance.</param>
	/// <returns>An IDisposable that releases the read lock.</returns>
	[Obsolete("This class is obsolete. Use Task-based asynchronous programming (System.Threading.Tasks.Task) instead of Thread and Timer for better performance and maintainability.")]
	public static IDisposable ReaderLock(this ReaderWriterLockSlim rwLock)
		=> new ReaderWriterLockSlimDispose(rwLock, true);

	/// <summary>
	/// Tries to get a unique Mutex with the specified name.
	/// </summary>
	/// <param name="name">The name of the Mutex.</param>
	/// <param name="mutex">When this method returns, contains the Mutex if successful.</param>
	/// <returns>True if the Mutex is unique and acquired; otherwise, false.</returns>
	[Obsolete("Use Do.TryGetUniqueMutex instead.")]
	public static bool TryGetUniqueMutex(string name, out Mutex mutex)
		=> Do.TryGetUniqueMutex(name, out mutex);

	/// <summary>
	/// Temporarily sets the current thread's culture to the specified culture.
	/// </summary>
	/// <param name="culture">The CultureInfo to be set for the current thread.</param>
	/// <returns>An IDisposable that, when disposed, restores the original culture.</returns>
	[Obsolete("Use Do.WithCulture instead.")]
	public static IDisposable WithCulture(CultureInfo culture)
		=> Do.WithCulture(culture);

	/// <summary>
	/// Temporarily sets the current thread's culture to the invariant culture.
	/// </summary>
	/// <returns>An IDisposable that, when disposed, restores the original culture.</returns>
	[Obsolete("Use Do.WithInvariantCulture instead.")]
	public static IDisposable WithInvariantCulture()
		=> Do.WithInvariantCulture();
}
