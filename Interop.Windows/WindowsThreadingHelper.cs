namespace Ecng.Interop
{
	using System;
	using System.Threading;

	using Ecng.Common;

	public static class WindowsThreadingHelper
	{
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

			var staThread = ThreadingHelper.Thread(() =>
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
	}
}