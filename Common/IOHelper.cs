namespace Ecng.Common;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Globalization;

using Nito.AsyncEx;

/// <summary>
/// Provides helper methods for file and directory operations.
/// </summary>
public static class IOHelper
{
	/// <summary>
	/// Clears the specified directory by deleting its files and subdirectories.
	/// </summary>
	/// <param name="path">The directory path to clear.</param>
	/// <param name="filter">Optional filter to determine which files to delete.</param>
	/// <returns>A DirectoryInfo for the cleared directory.</returns>
	public static DirectoryInfo ClearDirectory(string path, Func<string, bool> filter = null)
		=> AsyncContext.Run(() => ClearDirectoryAsync(path, filter));

	/// <summary>
	/// Asynchronously clears the specified directory by deleting its files and subdirectories.
	/// </summary>
	/// <param name="path">The directory path to clear.</param>
	/// <param name="filter">Optional filter to determine which files to delete.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation, containing a DirectoryInfo for the cleared directory.</returns>
	public static Task<DirectoryInfo> ClearDirectoryAsync(string path, Func<string, bool> filter = null, CancellationToken cancellationToken = default)
	{
		var parentDir = new DirectoryInfo(path);

		foreach (var file in parentDir.EnumerateFiles())
		{
			if (filter != null && !filter(file.FullName))
				continue;

			file.Delete();

			cancellationToken.ThrowIfCancellationRequested();
		}

		foreach (var dir in parentDir.EnumerateDirectories())
		{
			dir.Delete(true);

			cancellationToken.ThrowIfCancellationRequested();
		}

		return parentDir.FromResult();
	}

	/// <summary>
	/// Copies the content of one directory to another.
	/// </summary>
	/// <param name="sourcePath">The source directory path.</param>
	/// <param name="destPath">The destination directory path.</param>
	public static void CopyDirectory(string sourcePath, string destPath)
		=> AsyncContext.Run(() => CopyDirectoryAsync(sourcePath, destPath));

	/// <summary>
	/// Asynchronously copies the content of one directory to another.
	/// </summary>
	/// <param name="sourcePath">The source directory path.</param>
	/// <param name="destPath">The destination directory path.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public static async Task CopyDirectoryAsync(string sourcePath, string destPath, CancellationToken cancellationToken = default)
	{
		Directory.CreateDirectory(destPath);

		foreach (var fileName in Directory.GetFiles(sourcePath))
		{
			CopyAndMakeWritable(fileName, destPath);

			cancellationToken.ThrowIfCancellationRequested();
		}

		foreach (var directory in Directory.GetDirectories(sourcePath))
		{
			await CopyDirectoryAsync(directory, Path.Combine(destPath, Path.GetFileName(directory)), cancellationToken);
		}
	}

	/// <summary>
	/// Copies a file to the specified destination and makes the copy writable.
	/// </summary>
	/// <param name="fileName">The source file path.</param>
	/// <param name="destPath">The destination directory path.</param>
	/// <returns>The destination file path.</returns>
	public static string CopyAndMakeWritable(string fileName, string destPath)
	{
		var destFile = Path.Combine(destPath, Path.GetFileName(fileName));

		File.Copy(fileName, destFile, true);
		new FileInfo(destFile).IsReadOnly = false;

		return destFile;
	}

	/// <summary>
	/// Converts a relative or partial path to a fully qualified path.
	/// </summary>
	/// <param name="path">The input path.</param>
	/// <returns>The absolute path.</returns>
	public static string ToFullPath(this string path)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));

		return Path.GetFullPath(path);
	}

	/// <summary>
	/// Adds a relative segment to the current path and returns the fully qualified path.
	/// </summary>
	/// <param name="path">The base path.</param>
	/// <param name="relativePart">The relative segment to add.</param>
	/// <returns>The combined full path.</returns>
	public static string AddRelative(this string path, string relativePart)
	{
		return (path + relativePart).ToFullPath();
	}

	/// <summary>
	/// Executes a process with specified arguments and output handlers.
	/// </summary>
	/// <param name="fileName">The process file name.</param>
	/// <param name="arg">Arguments for the process.</param>
	/// <param name="output">Action to handle standard output.</param>
	/// <param name="error">Action to handle standard error.</param>
	/// <param name="infoHandler">Optional action to modify process start info.</param>
	/// <param name="waitForExit">TimeSpan to wait for process exit.</param>
	/// <param name="stdInput">Standard input to write to the process.</param>
	/// <param name="priority">Optional process priority.</param>
	/// <returns>The process exit code.</returns>
	public static int Execute(string fileName, string arg, Action<string> output, Action<string> error, Action<ProcessStartInfo> infoHandler = null, TimeSpan waitForExit = default, string stdInput = null, ProcessPriorityClass? priority = null)
	{
		var source = new CancellationTokenSource();

		if (waitForExit != default)
			source.CancelAfter(waitForExit);

		return AsyncContext.Run(() => ExecuteAsync(fileName, arg, output, error, infoHandler, stdInput, priority, source.Token));
	}

	/// <summary>
	/// Asynchronously executes a process with specified arguments and output handlers.
	/// </summary>
	/// <param name="fileName">The file name to execute.</param>
	/// <param name="arg">Arguments for the process.</param>
	/// <param name="output">Action to handle standard output.</param>
	/// <param name="error">Action to handle standard error.</param>
	/// <param name="infoHandler">Optional action to modify process start info.</param>
	/// <param name="stdInput">Standard input to send to the process.</param>
	/// <param name="priority">Optional process priority.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation, containing the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string fileName, string arg, Action<string> output, Action<string> error, Action<ProcessStartInfo> infoHandler = null, string stdInput = null, ProcessPriorityClass? priority = null, CancellationToken cancellationToken = default)
	{
		if (output is null)
			throw new ArgumentNullException(nameof(output));

		if (error is null)
			throw new ArgumentNullException(nameof(error));

		var input = !stdInput.IsEmpty();

		var procInfo = new ProcessStartInfo(fileName, arg)
		{
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			RedirectStandardInput = input,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		};

		infoHandler?.Invoke(procInfo);

		using var process = new Process
		{
			EnableRaisingEvents = true,
			StartInfo = procInfo
		};

		process.Start();

		// Set process priority if provided.
		// https://stackoverflow.com/a/1010377/8029915
		if (priority is not null)
			process.PriorityClass = priority.Value;

		var locker = new object();

		if (input)
		{
			process.StandardInput.WriteLine(stdInput);
			process.StandardInput.Close();
		}

		async Task ReadProcessOutput(TextReader reader, Action<string> action)
		{
			do
			{
				var str = await reader.ReadLineAsync().WithCancellation(cancellationToken);
				if (str is null)
					break;

				if (!str.IsEmptyOrWhiteSpace())
				{
					lock (locker)
						action(str);
				}

				cancellationToken.ThrowIfCancellationRequested();
			}
			while (true);
		}

		var task1 = ReadProcessOutput(process.StandardOutput, output);
		var task2 = ReadProcessOutput(process.StandardError, error);

		await task1;
		await task2;

		await process.WaitForExitAsync(cancellationToken);

		return process.ExitCode;
	}

	/// <summary>
	/// Creates the directory for the specified file if it does not already exist.
	/// </summary>
	/// <param name="fullPath">The full path to the file.</param>
	/// <returns>True if the directory was created; otherwise, false.</returns>
	public static bool CreateDirIfNotExists(this string fullPath)
	{
		var directory = Path.GetDirectoryName(fullPath);

		if (directory.IsEmpty() || Directory.Exists(directory))
			return false;

		Directory.CreateDirectory(directory);
		return true;
	}

	private static readonly string[] _suf = ["B", "KB", "MB", "GB", "TB", "PB", "EB"]; //Longs run out around EB

	/// <summary>
	/// Converts a byte count to a human-readable file size string.
	/// </summary>
	/// <param name="byteCount">The number of bytes.</param>
	/// <returns>A formatted string representing the file size.</returns>
	public static string ToHumanReadableFileSize(this long byteCount)
	{
		int place;
		int num;

		if (byteCount == 0)
		{
			num = 0;
			place = 0;
		}
		else
		{
			var bytes = byteCount.Abs();
			place = (int)Math.Log(bytes, FileSizes.KB).Floor();
			num = (int)(Math.Sign(byteCount) * Math.Round(bytes / Math.Pow(FileSizes.KB, place), 1));
		}

		return num + " " + _suf[place];
	}

	/// <summary>
	/// Safely deletes a directory.
	/// </summary>
	/// <param name="path">The directory path.</param>
	public static void SafeDeleteDir(this string path)
	{
		if (!Directory.Exists(path))
			return;

		Directory.Delete(path, true);
	}

	/// <summary>
	/// Creates a temporary directory and returns its path.
	/// </summary>
	/// <returns>The path to the new temporary directory.</returns>
	public static string CreateTempDir()
	{
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Remove("-"));

		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);

		return path;
	}

	/// <summary>
	/// Checks if the specified installation directory exists and contains files or subdirectories.
	/// </summary>
	/// <param name="path">The installation directory path.</param>
	/// <returns>True if the installation is valid; otherwise, false.</returns>
	public static bool CheckInstallation(string path)
	{
		if (path.IsEmpty())
			return false;

		if (!Directory.Exists(path))
			return false;

		var files = Directory.GetFiles(path);
		var directories = Directory.GetDirectories(path);
		return files.Any() || directories.Any();
	}

	/// <summary>
	/// Gets the relative path from a folder to a file.
	/// </summary>
	/// <param name="fileFull">The full file path.</param>
	/// <param name="folder">The base folder.</param>
	/// <returns>The relative file path.</returns>
	public static string GetRelativePath(this string fileFull, string folder)
	{
		var pathUri = new Uri(fileFull);

		if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			folder += Path.DirectorySeparatorChar;

		var folderUri = new Uri(folder);
		return folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar).DataUnEscape();
	}

	/// <summary>
	/// Gets the available free space on the specified drive.
	/// </summary>
	/// <param name="driveName">The drive name (e.g., "C:").</param>
	/// <returns>The amount of free space in bytes.</returns>
	public static long GetDiskFreeSpace(string driveName)
	{
		return new DriveInfo(driveName).TotalFreeSpace;
	}

	/// <summary>
	/// Creates a file with the specified content.
	/// </summary>
	/// <param name="rootPath">The root path.</param>
	/// <param name="relativePath">The relative path to the file.</param>
	/// <param name="fileName">The file name.</param>
	/// <param name="content">The content as a byte array.</param>
	public static void CreateFile(string rootPath, string relativePath, string fileName, byte[] content)
	{
		if (relativePath.IsEmpty())
		{
			File.WriteAllBytes(Path.Combine(rootPath, fileName), content);
		}
		else
		{
			var fullPath = Path.Combine(rootPath, relativePath, fileName);
			var fileInfo = new FileInfo(fullPath);
			fileInfo.Directory.Create();
			File.WriteAllBytes(fullPath, content);
		}
	}

	// https://stackoverflow.com/a/2811746/8029915

	/// <summary>
	/// Recursively deletes empty directories starting from the specified directory.
	/// </summary>
	/// <param name="dir">The root directory to check and delete if empty.</param>
	public static void DeleteEmptyDirs(string dir)
	{
		if (dir.IsEmpty())
			throw new ArgumentNullException(nameof(dir));

		try
		{
			foreach (var d in Directory.EnumerateDirectories(dir))
			{
				DeleteEmptyDirs(d);
			}

			var entries = Directory.EnumerateFileSystemEntries(dir);

			if (!entries.Any())
			{
				try
				{
					Directory.Delete(dir);
				}
				catch (UnauthorizedAccessException) { }
				catch (DirectoryNotFoundException) { }
			}
		}
		catch (UnauthorizedAccessException) { }
	}

	/// <summary>
	/// The %Documents% variable.
	/// </summary>
	public const string DocsVar = "%Documents%";

	/// <summary>
	/// Replaces the %Documents% variable in the path with the actual Documents folder path.
	/// </summary>
	/// <param name="path">The path containing the %Documents% variable.</param>
	/// <returns>The fully qualified path with the Documents folder.</returns>
	public static string ToFullPathIfNeed(this string path)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));

		return path.ReplaceIgnoreCase(DocsVar, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
	}

	/// <summary>
	/// Deletes a directory in a blocking manner.
	/// </summary>
	/// <param name="dir">The directory to delete.</param>
	/// <param name="isRecursive">Indicates whether to delete subdirectories recursively.</param>
	/// <param name="iterCount">Number of iterations to attempt deletion.</param>
	/// <param name="sleep">Sleep duration between attempts in milliseconds.</param>
	/// <returns>True if the directory still exists after deletion attempts; otherwise, false.</returns>
	public static bool BlockDeleteDir(string dir, bool isRecursive = false, int iterCount = 1000, int sleep = 0)
	{
		if (isRecursive)
		{
			// https://stackoverflow.com/a/329502/8029915
			// Delete files and directories recursively.
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
		// Attempt deletion.

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

	/// <summary>
	/// Opens the specified URL or file path using the default system launcher.
	/// </summary>
	/// <param name="url">The URL or file path to open.</param>
	/// <param name="raiseError">Determines if an exception should be raised if opening fails.</param>
	/// <returns>True if the operation is successful; otherwise, false.</returns>
	public static bool OpenLink(this string url, bool raiseError)
	{
		if (url.IsEmpty())
			throw new ArgumentNullException(nameof(url));

		// https://stackoverflow.com/a/21836079

		try
		{
			// https://github.com/dotnet/wpf/issues/2566

			var procInfo = new ProcessStartInfo(url)
			{
				UseShellExecute = true,
			};

			Process.Start(procInfo);
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

	/// <summary>
	/// Retrieves the directories within the specified path matching the search pattern.
	/// </summary>
	/// <param name="path">The root directory to search.</param>
	/// <param name="searchPattern">The search pattern.</param>
	/// <param name="searchOption">Search option to determine whether to search subdirectories.</param>
	/// <returns>An enumerable of matching directory paths.</returns>
	public static IEnumerable<string> GetDirectories(string path,
		string searchPattern = "*",
		SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		return !Directory.Exists(path)
			? []
			: Directory.EnumerateDirectories(path, searchPattern, searchOption);
	}

	/// <summary>
	/// Gets the timestamp of the specified assembly.
	/// </summary>
	/// <param name="assembly">The assembly.</param>
	/// <returns>The timestamp when the assembly was built.</returns>
	public static DateTime GetTimestamp(this Assembly assembly)
	{
		if (assembly is null)
			throw new ArgumentNullException(nameof(assembly));

		return GetTimestamp(assembly.Location);
	}

	/// <summary>
	/// Gets the timestamp of the specified file.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <returns>The timestamp representing when the file was built.</returns>
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

	/// <summary>
	/// Determines whether the specified path represents a directory.
	/// </summary>
	/// <param name="path">The file or directory path.</param>
	/// <returns>True if the path is a directory; otherwise, false.</returns>
	public static bool IsDirectory(this string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

	/// <summary>
	/// Writes the specified bytes to a stream.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="bytes">The byte array.</param>
	/// <param name="len">The number of bytes to write.</param>
	/// <param name="pos">The position in the array to start writing from.</param>
	public static void WriteBytes(this Stream stream, byte[] bytes, int len, int pos = 0)
	{
		stream.Write(bytes, pos, len);
	}

	/// <summary>
	/// Reads a specified number of bytes from a stream.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer to fill.</param>
	/// <param name="len">The number of bytes to read.</param>
	/// <param name="pos">The position in the buffer to start filling.</param>
	/// <returns>The buffer containing the read bytes.</returns>
	public static byte[] ReadBytes(this Stream stream, byte[] buffer, int len, int pos = 0)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		var totalRead = 0;

		while (totalRead < len)
		{
			var read = stream.Read(buffer, pos + totalRead, len - totalRead);

			if (read <= 0)
				throw new IOException($"Stream returned '{read}' bytes. Expected {len - totalRead} more bytes.");

			totalRead += read;
		}

		return buffer;
	}

#if NET5_0_OR_GREATER
	/// <summary>
	/// Reads a specified number of bytes from a stream.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer to fill.</param>
	/// <returns>The buffer containing the read bytes.</returns>
	public static Memory<byte> ReadBytes(this Stream stream, Memory<byte> buffer)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (buffer.IsEmpty)
			throw new ArgumentNullException(nameof(buffer));

		var totalRead = 0;

		while (totalRead < buffer.Length)
		{
			var read = stream.Read(buffer[totalRead..].Span);

			if (read <= 0)
				throw new IOException($"Stream returned '{read}' bytes. Expected {buffer.Length - totalRead} more bytes.");

			totalRead += read;
		}

		return buffer;
	}
#endif

	/// <summary>
	/// Reads a single byte from a stream.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer used for reading.</param>
	/// <returns>The byte read from the stream.</returns>
	public static byte ReadByteEx(this Stream stream, byte[] buffer)
	{
		return stream.ReadBytes(buffer, 1)[0];
	}

	/// <summary>
	/// Writes a single byte to a stream.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="buffer">The buffer used for writing.</param>
	/// <param name="value">The byte value to write.</param>
	public static void WriteByteEx(this Stream stream, byte[] buffer, byte value)
	{
		buffer[0] = value;
		stream.Write(buffer, 0, 1);
	}

	/// <summary>
	/// Writes a short value to a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="buffer">The buffer used for writing.</param>
	/// <param name="value">The short value to write.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	public static unsafe void WriteShort(this Stream stream, byte[] buffer, short value, bool isLittleEndian)
	{
		fixed (byte* b = buffer)
			*((short*)b) = value;

		stream.WriteBytes(buffer.ChangeOrder(2, isLittleEndian), 2);
	}

	/// <summary>
	/// Reads a short value from a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer used for reading.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <returns>The short value read.</returns>
	public static short ReadShort(this Stream stream, byte[] buffer, bool isLittleEndian)
	{
		return BitConverter.ToInt16(stream.ReadBytes(buffer, 2).ChangeOrder(2, isLittleEndian), 0);
	}

	/// <summary>
	/// Writes an unsigned short value to a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="buffer">The buffer used for writing.</param>
	/// <param name="value">The unsigned short value to write.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	[CLSCompliant(false)]
	public static unsafe void WriteUShort(this Stream stream, byte[] buffer, ushort value, bool isLittleEndian)
	{
		fixed (byte* b = buffer)
			*((ushort*)b) = value;

		stream.WriteBytes(buffer.ChangeOrder(2, isLittleEndian), 2);
	}

	/// <summary>
	/// Reads an unsigned short value from a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer used for reading.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <returns>The unsigned short value read.</returns>
	[CLSCompliant(false)]
	public static ushort ReadUShort(this Stream stream, byte[] buffer, bool isLittleEndian)
	{
		return BitConverter.ToUInt16(stream.ReadBytes(buffer, 2).ChangeOrder(2, isLittleEndian), 0);
	}

	/// <summary>
	/// Writes an integer value to a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="buffer">The buffer used for writing.</param>
	/// <param name="value">The integer value to write.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	public static unsafe void WriteInt(this Stream stream, byte[] buffer, int value, bool isLittleEndian)
	{
		fixed (byte* b = buffer)
			*((int*)b) = value;

		stream.WriteBytes(buffer.ChangeOrder(4, isLittleEndian), 4);
	}

	/// <summary>
	/// Reads an integer value from a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer used for reading.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <returns>The integer value read.</returns>
	public static int ReadInt(this Stream stream, byte[] buffer, bool isLittleEndian)
	{
		return BitConverter.ToInt32(stream.ReadBytes(buffer, 4).ChangeOrder(4, isLittleEndian), 0);
	}

	/// <summary>
	/// Writes an unsigned integer value to a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="buffer">The buffer used for writing.</param>
	/// <param name="value">The unsigned integer value to write.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	[CLSCompliant(false)]
	public static unsafe void WriteUInt(this Stream stream, byte[] buffer, uint value, bool isLittleEndian)
	{
		fixed (byte* b = buffer)
			*((uint*)b) = value;

		stream.WriteBytes(buffer.ChangeOrder(4, isLittleEndian), 4);
	}

	/// <summary>
	/// Reads an unsigned integer value from a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer used for reading.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <returns>The unsigned integer value read.</returns>
	[CLSCompliant(false)]
	public static uint ReadUInt(this Stream stream, byte[] buffer, bool isLittleEndian)
	{
		return BitConverter.ToUInt32(stream.ReadBytes(buffer, 4).ChangeOrder(4, isLittleEndian), 0);
	}

	/// <summary>
	/// Writes a long value to a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="buffer">The buffer used for writing.</param>
	/// <param name="value">The long value to write.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <param name="len">The length in bytes to write (default is 8).</param>
	public static unsafe void WriteLong(this Stream stream, byte[] buffer, long value, bool isLittleEndian, int len = 8)
	{
		fixed (byte* b = buffer)
			*((long*)b) = value;

		stream.WriteBytes(buffer.ChangeOrder(len, isLittleEndian), 8);
	}

	/// <summary>
	/// Reads a long value from a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer used for reading.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <param name="len">The length in bytes to read (default is 8).</param>
	/// <returns>The long value read.</returns>
	public static long ReadLong(this Stream stream, byte[] buffer, bool isLittleEndian, int len = 8)
	{
		return BitConverter.ToInt64(stream.ReadBytes(buffer, len).ChangeOrder(len, isLittleEndian), 0);
	}

	/// <summary>
	/// Writes an unsigned long value to a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="buffer">The buffer used for writing.</param>
	/// <param name="value">The unsigned long value to write.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <param name="len">The length in bytes to write (default is 8).</param>
	[CLSCompliant(false)]
	public static unsafe void WriteULong(this Stream stream, byte[] buffer, ulong value, bool isLittleEndian, int len = 8)
	{
		fixed (byte* b = buffer)
			*((ulong*)b) = value;

		stream.WriteBytes(buffer.ChangeOrder(len, isLittleEndian), 8);
	}

	/// <summary>
	/// Reads an unsigned long value from a stream with specified endianness.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer used for reading.</param>
	/// <param name="isLittleEndian">Indicates if the value is in little-endian format.</param>
	/// <param name="len">The length in bytes to read (default is 8).</param>
	/// <returns>The unsigned long value read.</returns>
	[CLSCompliant(false)]
	public static ulong ReadULong(this Stream stream, byte[] buffer, bool isLittleEndian, int len = 8)
	{
		return BitConverter.ToUInt64(stream.ReadBytes(buffer, len).ChangeOrder(4, isLittleEndian), 0);
	}


	/// <summary>
	/// Copies a specified number of bytes synchronously from the source stream to the destination stream.
	/// </summary>
	/// <param name="source">The source stream to copy from.</param>
	/// <param name="destination">The destination stream to copy to.</param>
	/// <param name="count">The number of bytes to copy.</param>
	public static void CopySync(this Stream source, Stream destination, int count)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (destination is null)
			throw new ArgumentNullException(nameof(destination));

		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, "Invalid value.");

		if (count == 0)
			return;

		var buffer = new byte[count.Min(short.MaxValue)];
		int read;

		while (count > 0 && (read = source.Read(buffer, 0, buffer.Length.Min(count))) > 0)
		{
			destination.Write(buffer, 0, read);
			count -= read;
		}
	}

	/// <summary>
	/// Reads exactly the specified number of bytes from the stream into a byte array.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="size">The number of bytes to read.</param>
	/// <returns>A byte array containing the data read from the stream.</returns>
	public static byte[] ReadBuffer(this Stream stream, int size)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (size < 0)
			throw new ArgumentOutOfRangeException(nameof(size), $"Size has negative value '{size}'.");

		var buffer = new byte[size];

		if (size == 1)
		{
			var b = stream.ReadByte();

			if (b == -1)
				throw new ArgumentException($"Insufficient stream size '{size}'.", nameof(stream));

			buffer[0] = (byte)b;
		}
		else
		{
			var offset = 0;
			do
			{
				var readBytes = stream.Read(buffer, offset, size - offset);

				if (readBytes == 0)
					throw new ArgumentException($"Insufficient stream size '{size}'.", nameof(stream));

				offset += readBytes;
			}
			while (offset < size);
		}

		return buffer;
	}

	/// <summary>
	/// Enumerates the lines in the stream using the specified encoding.
	/// </summary>
	/// <param name="stream">The stream to read lines from.</param>
	/// <param name="encoding">The encoding to use when reading the stream. Defaults to UTF8 if null.</param>
	/// <param name="leaveOpen">Indicates whether to leave the stream open after reading.</param>
	/// <returns>An enumerable collection of strings, each representing a line from the stream.</returns>
	public static IEnumerable<string> EnumerateLines(this Stream stream, Encoding encoding = null, bool leaveOpen = true)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		using var sr = new StreamReader(stream, encoding ?? Encoding.UTF8, true, -1, leaveOpen);

		while (!sr.EndOfStream)
			yield return sr.ReadLine();
	}

	/// <summary>
	/// Writes an extended representation of the provided object to the stream, prefixing its length.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="value">The object to write.</param>
	public static void WriteEx(this Stream stream, object value)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (value is Stream s)
			stream.WriteEx((int)s.Length);
		else if (value is byte[] a1)
			stream.WriteEx(a1.Length);
		else if (value is string str)
			stream.WriteEx(str.Length);

		stream.WriteRaw(value);
	}

	/// <summary>
	/// Writes the raw byte representation of the provided object to the stream.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="value">The object to write. Its byte representation will be determined.</param>
	public static void WriteRaw(this Stream stream, object value)
	{
		stream.WriteRaw(value.To<byte[]>());
	}

	/// <summary>
	/// Writes a raw byte array to the stream.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="buffer">The byte array to write.</param>
	public static void WriteRaw(this Stream stream, byte[] buffer)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		stream.Write(buffer, 0, buffer.Length);
	}

	#region Read

	/// <summary>
	/// Reads an object of type T from the stream.
	/// </summary>
	/// <typeparam name="T">The type of object to read.</typeparam>
	/// <param name="stream">The stream to read from.</param>
	/// <returns>The object read from the stream.</returns>
	public static T Read<T>(this Stream stream)
	{
		return (T)stream.Read(typeof(T));
	}

	/// <summary>
	/// Reads an object of the specified type from the stream.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="type">The type of object to read.</param>
	/// <returns>The object read from the stream.</returns>
	public static object Read(this Stream stream, Type type)
	{
		int size;

		if (type == typeof(byte[]) || type == typeof(string) || type == typeof(Stream))
			size = stream.Read<int>();
		else
			size = type.SizeOf();

		return stream.Read(type, size);
	}

	/// <summary>
	/// Reads an object of the specified type from the stream using the provided size.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="type">The type of object to read.</param>
	/// <param name="size">The size in bytes to read.</param>
	/// <returns>The object read from the stream.</returns>
	public static object Read(this Stream stream, Type type, int size)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (size < 0)
			throw new ArgumentOutOfRangeException(nameof(size), $"Size has negative value '{size}'.");

		if (size == 0 && !(type == typeof(string) || type == typeof(byte[]) || type == typeof(Stream)))
			throw new ArgumentOutOfRangeException(nameof(size), "Size has zero value.");

		if (type == typeof(string))
			size *= 2;

		if (size > int.MaxValue / 10)
			throw new ArgumentOutOfRangeException(nameof(size), $"Size has too big value {size}.");

		var buffer = size > 0 ? stream.ReadBuffer(size) : [];

		if (type == typeof(byte[]))
			return buffer;
		else
			return buffer.To(type);
	}

	#endregion

	/// <summary>
	/// Returns the size in bytes of an unmanaged type T.
	/// </summary>
	/// <typeparam name="T">The unmanaged type.</typeparam>
	/// <returns>The size in bytes of the specified type.</returns>
	public static int SizeOf<T>()
	{
		return SizeOf(typeof(T));
	}

	/// <summary>
	/// Returns the size in bytes of the specified unmanaged type.
	/// </summary>
	/// <param name="type">The type whose size is to be computed.</param>
	/// <returns>The size in bytes of the specified type.</returns>
	public static int SizeOf(this Type type)
	{
		if (type.IsDateTime())
			type = typeof(long);
		else if (type == typeof(TimeSpan))
			type = typeof(long);
		else if (type.IsEnum())
			type = type.GetEnumBaseType();
		else if (type == typeof(bool))
			type = typeof(byte);
		else if (type == typeof(char))
			type = typeof(short);

		return Marshal.SizeOf(type);
	}

	/// <summary>
	/// Saves the content of the stream to a file specified by fileName.
	/// </summary>
	/// <param name="stream">The stream whose contents to save.</param>
	/// <param name="fileName">The file path to save the stream's contents to.</param>
	/// <returns>The original stream.</returns>
	public static Stream Save(this Stream stream, string fileName)
	{
		var pos = stream.CanSeek ? stream.Position : 0;

		using (var file = File.Create(fileName))
			stream.CopyTo(file);

		if (stream.CanSeek)
			stream.Position = pos;

		return stream;
	}

	/// <summary>
	/// Saves the byte array to a file specified by fileName.
	/// </summary>
	/// <param name="data">The byte array to save.</param>
	/// <param name="fileName">The file path to save the data to.</param>
	/// <returns>The original byte array.</returns>
	public static byte[] Save(this byte[] data, string fileName)
	{
		data.To<Stream>().Save(fileName);
		return data;
	}

	/// <summary>
	/// Attempts to save the byte array to a file and handles any exceptions using the provided errorHandler.
	/// </summary>
	/// <param name="data">The byte array to save.</param>
	/// <param name="fileName">The file path to save the data to.</param>
	/// <param name="errorHandler">The action to handle exceptions.</param>
	/// <returns>True if the save operation was successful; otherwise, false.</returns>
	public static bool TrySave(this byte[] data, string fileName, Action<Exception> errorHandler)
	{
		if (errorHandler is null)
			throw new ArgumentNullException(nameof(errorHandler));

		try
		{
			data.To<Stream>().Save(fileName);
			return true;
		}
		catch (Exception e)
		{
			errorHandler(e);
			return false;
		}
	}

	/// <summary>
	/// Truncates the underlying stream used by the StreamWriter by clearing its content.
	/// </summary>
	/// <param name="writer">The StreamWriter whose stream is to be truncated.</param>
	public static void Truncate(this StreamWriter writer)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		writer.Flush();
		writer.BaseStream.SetLength(0);
	}

	/// <summary>
	/// Gets the actual buffer of the MemoryStream that contains the written data.
	/// </summary>
	/// <param name="stream">The MemoryStream to retrieve the buffer from.</param>
	/// <returns>An ArraySegment containing the actual data from the MemoryStream.</returns>
	public static ArraySegment<byte> GetActualBuffer(this MemoryStream stream)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		return new(stream.GetBuffer(), 0, (int)stream.Position);
	}

	/// <summary>
	/// Checks whether the directory contains files or subdirectories that contain files.
	/// </summary>
	/// <param name="path">The directory path to check.</param>
	/// <returns>True if the directory contains any files; otherwise, false.</returns>
	public static bool CheckDirContainFiles(string path)
	{
		return
			Directory.Exists(path) &&
			(Directory.GetFiles(path).Any() || Directory.GetDirectories(path).Any(CheckDirContainFiles));
	}

	/// <summary>
	/// Checks whether the directory contains any files or subdirectories.
	/// </summary>
	/// <param name="path">The directory path to check.</param>
	/// <returns>True if the directory contains any entries; otherwise, false.</returns>
	public static bool CheckDirContainsAnything(string path)
	{
		if (!Directory.Exists(path))
			return false;

		return Directory.EnumerateFileSystemEntries(path).Any();
	}

	/// <summary>
	/// Determines whether the file specified by the path is locked by another process.
	/// </summary>
	/// <param name="path">The path to the file to check.</param>
	/// <returns>True if the file is locked; otherwise, false.</returns>
	public static bool IsFileLocked(string path)
	{
		var info = new FileInfo(path);

		if (!info.Exists)
			return false;

		try
		{
			using var stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.None);
		}
		catch (IOException)
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Determines if the specified path refers to a directory.
	/// </summary>
	/// <param name="path">The path to check.</param>
	/// <returns>True if the path is a directory; otherwise, false.</returns>
	public static bool IsPathIsDir(this string path)
		=> File.GetAttributes(path).HasFlag(FileAttributes.Directory);

	/// <summary>
	/// Normalizes the provided file path for comparison purposes without converting to lowercase.
	/// </summary>
	/// <param name="path">The file path to normalize.</param>
	/// <returns>The normalized file path, or null if the input is empty or whitespace.</returns>
	public static string NormalizePathNoLowercase(this string path)
	{
		if (path.IsEmptyOrWhiteSpace())
			return null;

		path = Path.GetFullPath(path);

		return Path.GetFullPath(new Uri(path).LocalPath)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
	}

	/// <summary>
	/// Normalizes the provided file path for comparison purposes and converts it to lowercase based on the specified culture.
	/// </summary>
	/// <param name="path">The file path to normalize.</param>
	/// <param name="culture">The culture info to use for lowercasing. Defaults to InstalledUICulture if null.</param>
	/// <returns>The normalized and lowercased file path.</returns>
	public static string NormalizePath(this string path, CultureInfo culture = null)
	{
		return path.NormalizePathNoLowercase()?.ToLower(culture ?? CultureInfo.InstalledUICulture);
	}

	/// <summary>
	/// Compares two file paths for equality after normalization.
	/// </summary>
	/// <param name="path1">The first file path to compare.</param>
	/// <param name="path2">The second file path to compare.</param>
	/// <returns>True if both paths are equal; otherwise, false.</returns>
	public static bool IsPathsEqual(string path1, string path2) => path1.NormalizePath() == path2.NormalizePath();

	/// <summary>
	/// Reads the specified number of bytes from the stream into the provided buffer.
	/// </summary>
	/// <param name="stream">The source stream.</param>
	/// <param name="buffer">The buffer to store the data.</param>
	/// <param name="offset">The offset in the buffer.</param>
	/// <param name="bytesToRead">The number of bytes to read.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask{T}"/></returns>
	public static async ValueTask<int> ReadFullAsync(this Stream stream, byte[] buffer, int offset, int bytesToRead, CancellationToken cancellationToken)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		var totalRead = 0;

		while (totalRead < bytesToRead)
		{
			var bytesRead = await stream.ReadAsync(
#if NET5_0_OR_GREATER
				buffer.AsMemory(offset + totalRead, bytesToRead - totalRead)
#else
				buffer, offset + totalRead, bytesToRead - totalRead
#endif
				, cancellationToken
			).ConfigureAwait(false);

			if (bytesRead == 0)
				break;

			totalRead += bytesRead;
		}

		if (totalRead < bytesToRead)
			throw new IOException("Connection dropped.");

		return totalRead;
	}
}