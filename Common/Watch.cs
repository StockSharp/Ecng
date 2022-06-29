namespace Ecng.Common
{
	using System;
	using System.Diagnostics;

	public static class Watch
	{
		public static TimeSpan Do(Action action)
		{
			var watch = Stopwatch.StartNew();
			action();
			watch.Stop();
			return watch.Elapsed;
		}
	}
}