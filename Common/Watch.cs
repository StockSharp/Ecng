namespace Ecng.Common
{
	using System;
	using System.Diagnostics;

	public static class Watch
	{
		public static TimeSpan Do(Action action)
		{
			var watch = new Stopwatch();
			watch.Start();
			action();
			watch.Stop();
			return watch.Elapsed;
		}
	}
}