namespace Ecng.Interop
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading;

	using Ecng.Common;

#if !__STOCKSHARP__
	public
#endif
		static class InteropHelper
	{
		// http://social.msdn.microsoft.com/Forums/eu/windowssearch/thread/55582d9d-77ea-47d9-91ce-cff7ca7ef528
		public static bool BlockDeleteDir(string dir, bool isRecursive = false, int iterCount = 1000, int sleep = 0)
		{
			if (isRecursive)
			{
				// https://stackoverflow.com/a/329502/8029915

				var files = Directory.GetFiles(dir);
				var dirs = Directory.GetDirectories(dir);

				foreach (var file in files)
				{
					File.SetAttributes(file, FileAttributes.Normal);
					File.Delete(file);
				}

				foreach (var sub in dirs)
				{
					BlockDeleteDir(sub, true, iterCount, sleep);
				}
			}

			// https://stackoverflow.com/a/1703799/8029915

			try
			{
				Directory.Delete(dir, false);
			}
			catch (IOException) 
			{
				Directory.Delete(dir, false);
			}
			catch (UnauthorizedAccessException)
			{
				Directory.Delete(dir, false);
			}

			var limit = iterCount;

			while (Directory.Exists(dir) && limit-- > 0)
				Thread.Sleep(sleep);

			return Directory.Exists(dir);
		}

		public static bool OpenLink(this string url, bool raiseError)
		{
			if (url.IsEmpty())
				throw new ArgumentNullException(nameof(url));

			// https://stackoverflow.com/a/21836079

			try
			{
				Process.Start(url);
				return true;
			}
			catch (Win32Exception)
			{
				try
				{
					var launcher = url.StartsWithIgnoreCase("http") ? "IExplore.exe" : "explorer.exe";
					Process.Start(launcher, url);
					return true;
				}
				catch
				{
					if (raiseError)
						throw;

					return false;
				}
			}
		}

		public static IEnumerable<string> GetDirectories(string path,
			string searchPattern = "*",
			SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			return !Directory.Exists(path)
				? Enumerable.Empty<string>()
				: Directory.EnumerateDirectories(path, searchPattern, searchOption);
		}

		public static DateTime GetTimestamp(this Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			return GetTimestamp(assembly.Location);
		}

		public static DateTime GetTimestamp(string filePath)
		{
			var b = new byte[2048];

			using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				s.Read(b, 0, b.Length);

			const int peHeaderOffset = 60;
			const int linkerTimestampOffset = 8;
			var i = BitConverter.ToInt32(b, peHeaderOffset);
			var secondsSince1970 = (long)BitConverter.ToInt32(b, i + linkerTimestampOffset);

			return secondsSince1970.FromUnix().ToLocalTime();
		}

		public static bool IsDirectory(this string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

#if !__STOCKSHARP__
		public static Platforms GetPlatform(this Type type) => type.GetAttribute<TargetPlatformAttribute>()?.Platform ?? Platforms.AnyCPU;
#endif
	}
}