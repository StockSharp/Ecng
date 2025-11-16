namespace Ecng.ComponentModel;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Security;

/// <summary>
/// Provides functionality to ensure that only one instance of the application is running.
/// </summary>
public static class ProcessSingleton
{
	private static Locker _isRunningMutex;

	/// <summary>
	/// Checks whether an instance of the application is already running.
	/// </summary>
	/// <param name="appKey">A unique key used to enforce the single instance.</param>
	/// <returns>
	/// True if this is the first instance and the mutex was successfully acquired; 
	/// otherwise, false.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the mutex has already been initialized.
	/// </exception>
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
	/// Releases all resources allocated by the StartIsRunning method.
	/// </summary>
	public static void StopIsRunning()
	{
		_isRunningMutex?.Dispose();
		_isRunningMutex = null;
	}

	// Although this class is private, internal details are documented for clarity.
	private class Locker : Disposable
	{
		private readonly ManualResetEvent _stop = new(false);
		private readonly ManualResetEvent _stopped = new(false);

		/// <summary>
		/// Initializes a new instance of the <see cref="Locker"/> class with the specified key.
		/// </summary>
		/// <param name="key">The key used to generate a unique mutex.</param>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the mutex cannot be acquired with the computed unique name.
		/// </exception>
		public Locker(string key)
		{
			Exception error = null;
			var started = new ManualResetEvent(false);

			// Mutex must be released from the same thread it was captured; launching a dedicated thread ensures correctness.
			_ = Task.Factory.StartNew(() =>
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
			}, TaskCreationOptions.LongRunning);

			started.WaitOne();
			if (error != null)
				throw error;
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			_stop.Set();
			_stopped.WaitOne();
			base.DisposeManaged();
		}
	}
}