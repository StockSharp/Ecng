namespace Ecng.Common;

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides utility methods to execute functions and actions under the invariant culture.
/// </summary>
public static class Do
{
	private class CultureHolder : Disposable
	{
		private readonly CultureInfo _culture;

		public CultureHolder() => _culture = Thread.CurrentThread.CurrentCulture;

		protected override void DisposeManaged()
		{
			Thread.CurrentThread.CurrentCulture = _culture;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// Temporarily sets the current thread's culture to the specified culture.
	/// </summary>
	/// <param name="culture">The CultureInfo to be set for the current thread.</param>
	/// <returns>An IDisposable that, when disposed, restores the original culture.</returns>
	public static IDisposable WithCulture(CultureInfo culture)
	{
		var holder = new CultureHolder();
		Thread.CurrentThread.CurrentCulture = culture;
		return holder;
	}

	/// <summary>
	/// Temporarily sets the current thread's culture to the invariant culture.
	/// </summary>
	/// <returns>An IDisposable that, when disposed, restores the original culture.</returns>
	public static IDisposable WithInvariantCulture() => WithCulture(CultureInfo.InvariantCulture);

	/// <summary>
	/// Tries to get a unique Mutex with the specified name.
	/// </summary>
	/// <param name="name">The name of the Mutex.</param>
	/// <param name="mutex">When this method returns, contains the Mutex if successful.</param>
	/// <returns>True if the Mutex is unique and acquired; otherwise, false.</returns>
	public static bool TryGetUniqueMutex(string name, out Mutex mutex)
	{
		mutex = new Mutex(false, name);

		try
		{
			if (!mutex.WaitOne(1))
				return false;

			mutex.Dispose();
			mutex = new Mutex(true, name);
		}
		catch (AbandonedMutexException)
		{
			// http://stackoverflow.com/questions/15456986/how-to-gracefully-get-out-of-abandonedmutexexception
			// The previous process did not release the mutex.
			// When catching the exception, the current process becomes the owner.
		}

		return true;
	}

	/// <summary>
	/// Executes the specified function under the invariant culture and returns its result.
	/// </summary>
	/// <typeparam name="T">The type of the result produced by the function.</typeparam>
	/// <param name="func">The function to execute.</param>
	/// <returns>The result of executing the function.</returns>
	public static T Invariant<T>(Func<T> func)
		=> CultureInfo.InvariantCulture.DoInCulture(func);

	/// <summary>
	/// Executes the specified action under the invariant culture.
	/// </summary>
	/// <param name="action">The action to execute.</param>
	public static void Invariant(Action action)
		=> CultureInfo.InvariantCulture.DoInCulture(action);

	/// <summary>
	/// Executes the specified asynchronous function under the invariant culture and returns its result.
	/// </summary>
	/// <typeparam name="T">The type of the result produced by the asynchronous function.</typeparam>
	/// <param name="funcAsync">The asynchronous function to execute.</param>
	/// <returns>A task representing the asynchronous operation with the result.</returns>
	public static async Task<T> InvariantAsync<T>(Func<Task<T>> funcAsync)
	{
		if (funcAsync is null)
			throw new ArgumentNullException(nameof(funcAsync));

		using (WithInvariantCulture())
			return await funcAsync().NoWait();
	}

	/// <summary>
	/// Executes the specified asynchronous action under the invariant culture.
	/// </summary>
	/// <param name="actionAsync">The asynchronous action to execute.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task InvariantAsync(Func<Task> actionAsync)
	{
		if (actionAsync is null)
			throw new ArgumentNullException(nameof(actionAsync));

		using (WithInvariantCulture())
			await actionAsync().NoWait();
	}

	/// <summary>
	/// A no-op action that does nothing. Use instead of <c>() => {}</c>.
	/// </summary>
	public static void Nothing() { }

	/// <summary>
	/// A no-op action that does nothing. Use instead of <c>_ => {}</c>.
	/// </summary>
	public static void Nothing<T>(T _) { }

	/// <summary>
	/// A no-op action that does nothing. Use instead of <c>(_, _) => {}</c>.
	/// </summary>
	public static void Nothing<T1, T2>(T1 _1, T2 _2) { }

	/// <summary>
	/// A no-op action that does nothing. Use instead of <c>(_, _, _) => {}</c>.
	/// </summary>
	public static void Nothing<T1, T2, T3>(T1 _1, T2 _2, T3 _3) { }

	/// <summary>
	/// A no-op action that does nothing. Use instead of <c>(_, _, _, _) => {}</c>.
	/// </summary>
	public static void Nothing<T1, T2, T3, T4>(T1 _1, T2 _2, T3 _3, T4 _4) { }
}