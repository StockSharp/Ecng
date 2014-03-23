namespace Ecng.Interop
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;

	using Ecng.Common;

	public static class InteropHelper
	{
		public static bool CreateDirIfNotExists(this string fullPath)
		{
			var directory = Path.GetDirectoryName(fullPath);

			if (!directory.IsEmpty() && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
				return true;
			}

			return false;
		}

		// http://social.msdn.microsoft.com/Forums/eu/windowssearch/thread/55582d9d-77ea-47d9-91ce-cff7ca7ef528
		public static bool BlockDeleteDir(string dir, int iterCount = 1000, int sleep = 0)
		{
			Directory.Delete(dir);

			var limit = iterCount;

			while (Directory.Exists(dir) && limit-- > 0)
				Thread.Sleep(sleep);

			return Directory.Exists(dir);
		}

		public static void OpenLinkInBrowser(this Uri address)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			Process.Start(new ProcessStartInfo(address.ToString()));
		}
	}
}