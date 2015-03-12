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
				throw new ArgumentNullException("path");

			return Path.GetFullPath(path);
		}

		public static string AddRelative(this string path, string relativePart)
		{
			return (path + relativePart).ToFullPath();
		}

		public static int Execute(string fileName, string arg, Action<string> output, Action<string> error)
		{
			if (output == null)
				throw new ArgumentNullException("output");

			if (error == null)
				throw new ArgumentNullException("error");

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
	}
}