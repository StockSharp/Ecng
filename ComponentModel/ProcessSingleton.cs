namespace Ecng.ComponentModel;

using System;
using System.Threading;

using Ecng.Common;
using Ecng.Security;

public static class ProcessSingleton
{
	private static Locker _isRunningMutex;

	/// <summary>
	/// Check if an instance of the application already started.
	/// </summary>
	/// <returns>Check result.</returns>
	public static bool StartIsRunning(string appKey)
	{
		if (_isRunningMutex != null)
			throw new InvalidOperationException("mutex was already initialized");

		try
		{
			_isRunningMutex = new(appKey);
		}
		catch (Exception)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Release all resources allocated by <see cref="StartIsRunning()"/>.
	/// </summary>
	public static void StopIsRunning()
	{
		_isRunningMutex?.Dispose();
		_isRunningMutex = null;
	}

	private class Locker : Disposable
	{
		private readonly ManualResetEvent _stop = new(false);
		private readonly ManualResetEvent _stopped = new(false);

		public Locker(string key)
		{
			Exception error = null;
			var started = new ManualResetEvent(false);

			// mutex должен освобождаться из того же потока, в котором захвачен. некоторые приложения вызывают StopIsRunning из другого потока нежели StartIsRunning
			// выделяя отдельный поток, обеспечивается гарантия корректной работы в любом случае
			ThreadingHelper.Thread(() =>
			{
				Mutex mutex;

				try
				{
					var mutexName = key.UTF8().Md5();
					if (!ThreadingHelper.TryGetUniqueMutex(mutexName, out mutex))
						throw new InvalidOperationException($"can't acquire the mutex {mutexName}, (key={key})");
				}
				catch (Exception e)
				{
					error = e;
					_stopped.Set();
					return;
				}
				finally
				{
					started.Set();
				}

				try
				{
					_stop.WaitOne();
					mutex.ReleaseMutex();
				}
				finally
				{
					_stopped.Set();
				}
			})
			.Name("process_singleton")
			.Launch();

			started.WaitOne();
			if (error != null)
				throw error;
		}

		protected override void DisposeManaged()
		{
			_stop.Set();
			_stopped.WaitOne();
			base.DisposeManaged();
		}
	}
}