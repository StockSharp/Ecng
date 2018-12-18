namespace Ecng.Common
{
	using System;
	using System.Diagnostics;
	using System.IO;

	using MoreLinq;

	public static class IOHelper
	{
		public static DirectoryInfo ClearDirectory(string releasePath)
		{
			var releaseDir = new DirectoryInfo(releasePath);
			releaseDir.GetFiles().ForEach(f => f.Delete());
			releaseDir.GetDirectories().ForEach(d => d.Delete(true));
			return releaseDir;
		}

		public static void CopyDirectory(string sourcePath, string destPath)
		{
			Directory.CreateDirectory(destPath);

			Directory
				.GetFiles(sourcePath)
				.ForEach(fileName => CopyAndMakeWritable(fileName, destPath));

			foreach (var directory in Directory.GetDirectories(sourcePath))
			{
				CopyDirectory(directory, Path.Combine(destPath, Path.GetFileName(directory)));
			}
		}

		public static string CopyAndMakeWritable(string fileName, string destPath)
		{
			var destFile = Path.Combine(destPath, Path.GetFileName(fileName));

			File.Copy(fileName, destFile, true);
			new FileInfo(destFile).IsReadOnly = false;

			return destFile;
		}

		public static string ToFullPath(this string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			return Path.GetFullPath(path);
		}

		public static string AddRelative(this string path, string relativePart)
		{
			return (path + relativePart).ToFullPath();
		}

		public static int Execute(string fileName, string arg, Action<string> output, Action<string> error)
		{
			if (output == null)
				throw new ArgumentNullException(nameof(output));

			if (error == null)
				throw new ArgumentNullException(nameof(error));

			var procInfo = new ProcessStartInfo(fileName, arg)
			{
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			using (var process = new Process
			{
				EnableRaisingEvents = true,
				StartInfo = procInfo
			})
			{
				process.OutputDataReceived += (a, e) =>
				{
					if (!e.Data.IsEmptyOrWhiteSpace())
						output(e.Data);
				};
				process.ErrorDataReceived += (a, e) =>
				{
					if (!e.Data.IsEmptyOrWhiteSpace())
						error(e.Data);
				};

				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				process.WaitForExit();

				return process.ExitCode;
			}
		}

		public static bool CreateDirIfNotExists(this string fullPath)
		{
			var directory = Path.GetDirectoryName(fullPath);

			if (directory.IsEmpty() || Directory.Exists(directory))
				return false;

			Directory.CreateDirectory(directory);
			return true;
		}

		public static string ToHumanReadableFileSize(this long byteCount)
		{
			string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB

			if (byteCount == 0)
				return "0" + suf[0];

			var bytes = Math.Abs(byteCount);
			var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
			var num = Math.Round(bytes / Math.Pow(1024, place), 1);
			return (Math.Sign(byteCount) * num) + suf[place];
		}
	}
}