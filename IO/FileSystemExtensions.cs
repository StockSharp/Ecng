namespace Ecng.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="IFileSystem"/>.
/// </summary>
public static class FileSystemExtensions
{
	/// <summary>
	/// Opens a readable stream for the file at the specified path.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <returns>A read-only stream.</returns>
	public static Stream OpenRead(this IFileSystem fs, string path)
		=> fs.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

	/// <summary>
	/// Opens a writable stream for the file at the specified path.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="append">If true, appends to the file; otherwise overwrites.</param>
	/// <returns>A write-capable stream.</returns>
	public static Stream OpenWrite(this IFileSystem fs, string path, bool append = false)
		=> fs.Open(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);

	/// <summary>
	/// Reads all text from a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <returns>File contents as string.</returns>
	public static string ReadAllText(this IFileSystem fs, string path, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

		return reader.ReadToEnd();
	}

	/// <summary>
	/// Reads all text from a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>File contents as string.</returns>
	public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string path, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

		return await reader.ReadToEndAsync(cancellationToken);
	}

	/// <summary>
	/// Writes text to a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="content">Text content.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	public static void WriteAllText(this IFileSystem fs, string path, string content, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

		writer.Write(content);
	}

	/// <summary>
	/// Asynchronously writes text to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to write.</param>
	/// <param name="content">The text content to write.</param>
	/// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task WriteAllTextAsync(this IFileSystem fs, string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

		await writer.WriteAsync(content.AsMemory(), cancellationToken);
		await writer.FlushAsync(cancellationToken);
	}

	/// <summary>
	/// Appends text to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to append to.</param>
	/// <param name="content">The text content to append.</param>
	/// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
	public static void AppendAllText(this IFileSystem fs, string path, string content, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path, append: true);
		using var writer = new StreamWriter(stream, encoding);

		writer.Write(content);
	}

	/// <summary>
	/// Asynchronously appends text to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to append to.</param>
	/// <param name="content">The text content to append.</param>
	/// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task AppendAllTextAsync(this IFileSystem fs, string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path, append: true);
		using var writer = new StreamWriter(stream, encoding);

		await writer.WriteAsync(content.AsMemory(), cancellationToken);
		await writer.FlushAsync(cancellationToken);
	}

	/// <summary>
	/// Reads all bytes from a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to read.</param>
	/// <returns>A byte array containing the file contents.</returns>
	public static byte[] ReadAllBytes(this IFileSystem fs, string path)
	{
		using var stream = fs.OpenRead(path);
		using var ms = new MemoryStream();

		stream.CopyTo(ms);

		return ms.ToArray();
	}

	/// <summary>
	/// Asynchronously reads all bytes from a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to read.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task that produces a byte array containing the file contents.</returns>
	public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
	{
		using var stream = fs.OpenRead(path);
		using var ms = new MemoryStream();

		await stream.CopyToAsync(ms, cancellationToken);

		return ms.ToArray();
	}

	/// <summary>
	/// Writes bytes to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to write.</param>
	/// <param name="bytes">The byte array to write.</param>
	public static void WriteAllBytes(this IFileSystem fs, string path, byte[] bytes)
	{
		using var stream = fs.OpenWrite(path);

		stream.Write(bytes, 0, bytes.Length);
	}

	/// <summary>
	/// Asynchronously writes bytes to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to write.</param>
	/// <param name="bytes">The byte array to write.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task WriteAllBytesAsync(this IFileSystem fs, string path, byte[] bytes, CancellationToken cancellationToken = default)
	{
		using var stream = fs.OpenWrite(path);

		await stream.WriteAsync(bytes, cancellationToken);
	}

	/// <summary>
	/// Reads all lines from a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to read.</param>
	/// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
	/// <returns>An array of strings representing the file lines.</returns>
	public static string[] ReadAllLines(this IFileSystem fs, string path, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

		var lines = new List<string>();

		while (reader.ReadLine() is { } line)
			lines.Add(line);

		return [.. lines];
	}

	/// <summary>
	/// Asynchronously reads all lines from a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to read.</param>
	/// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task that produces an array of strings representing the file lines.</returns>
	public static async Task<string[]> ReadAllLinesAsync(this IFileSystem fs, string path, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

		var lines = new List<string>();

		while (await reader.ReadLineAsync(cancellationToken) is { } line)
			lines.Add(line);

		return [.. lines];
	}

	/// <summary>
	/// Writes lines to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to write.</param>
	/// <param name="lines">The lines to write.</param>
	/// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
	public static void WriteAllLines(this IFileSystem fs, string path, IEnumerable<string> lines, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

		foreach (var line in lines)
			writer.WriteLine(line);
	}

	/// <summary>
	/// Asynchronously writes lines to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The path to the file to write.</param>
	/// <param name="lines">The lines to write.</param>
	/// <param name="encoding">The text encoding to use. If null, <see cref="Encoding.UTF8"/> is used.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task WriteAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> lines, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

		foreach (var line in lines)
		{
			await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
		}

		await writer.FlushAsync(cancellationToken);
	}

	/// <summary>
	/// Clears the specified directory by deleting its files and subdirectories using the provided file system.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The directory path to clear.</param>
	/// <param name="filter">Optional filter to determine which files to delete. If null, all files are deleted.</param>
	/// <returns>A <see cref="DirectoryInfo"/> for the cleared directory.</returns>
	public static DirectoryInfo ClearDirectory(this IFileSystem fs, string path, Func<string, bool> filter = null)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		foreach (var file in fs.EnumerateFiles(path))
		{
			if (filter != null && !filter(file))
				continue;

			fs.DeleteFile(file);
		}

		foreach (var dir in fs.EnumerateDirectories(path))
		{
			fs.DeleteDirectory(dir, true);
		}

		return new DirectoryInfo(path);
	}

	/// <summary>
	/// Asynchronously clears the specified directory by deleting its files and subdirectories using the provided file system.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The directory path to clear.</param>
	/// <param name="filter">Optional filter to determine which files to delete. If null, all files are deleted.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task that produces a <see cref="DirectoryInfo"/> for the cleared directory.</returns>
	public static Task<DirectoryInfo> ClearDirectoryAsync(this IFileSystem fs, string path, Func<string, bool> filter = null, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		foreach (var file in fs.EnumerateFiles(path))
		{
			if (filter != null && !filter(file))
				continue;

			fs.DeleteFile(file);
			cancellationToken.ThrowIfCancellationRequested();
		}

		foreach (var dir in fs.EnumerateDirectories(path))
		{
			fs.DeleteDirectory(dir, true);
			cancellationToken.ThrowIfCancellationRequested();
		}

		return new DirectoryInfo(path).FromResult();
	}

	/// <summary>
	/// Copies the content of one directory to another using the provided file system.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="sourcePath">The source directory path.</param>
	/// <param name="destPath">The destination directory path.</param>
	public static void CopyDirectory(this IFileSystem fs, string sourcePath, string destPath)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		fs.CreateDirectory(destPath);

		foreach (var fileName in fs.EnumerateFiles(sourcePath))
		{
			fs.CopyAndMakeWritable(fileName, destPath);
		}

		foreach (var directory in fs.EnumerateDirectories(sourcePath))
		{
			fs.CopyDirectory(directory, Path.Combine(destPath, Path.GetFileName(directory)));
		}
	}

	/// <summary>
	/// Asynchronously copies the content of one directory to another using the provided file system.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="sourcePath">The source directory path.</param>
	/// <param name="destPath">The destination directory path.</param>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task CopyDirectoryAsync(this IFileSystem fs, string sourcePath, string destPath, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		fs.CreateDirectory(destPath);

		foreach (var fileName in fs.EnumerateFiles(sourcePath))
		{
			fs.CopyAndMakeWritable(fileName, destPath);
			cancellationToken.ThrowIfCancellationRequested();
		}

		foreach (var directory in fs.EnumerateDirectories(sourcePath))
		{
			await fs.CopyDirectoryAsync(directory, Path.Combine(destPath, Path.GetFileName(directory)), cancellationToken).NoWait();
		}
	}

	/// <summary>
	/// Copies a file to the specified destination and makes the copy writable using the provided file system.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="fileName">The source file path.</param>
	/// <param name="destPath">The destination directory path.</param>
	/// <returns>The destination file path.</returns>
	public static string CopyAndMakeWritable(this IFileSystem fs, string fileName, string destPath)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		var destFile = Path.Combine(destPath, Path.GetFileName(fileName));

		fs.CopyFile(fileName, destFile, true);
		try { fs.SetReadOnly(destFile, false); } catch { }
		return destFile;
	}

	/// <summary>
	/// Creates the directory for the specified file if it does not already exist.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="fullPath">The full file path for which to ensure the directory exists.</param>
	/// <returns>True if the directory was created; otherwise false if it already existed or path has no directory part.</returns>
	public static bool CreateDirIfNotExists(this IFileSystem fs, string fullPath)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		var directory = Path.GetDirectoryName(fullPath);

		if (directory.IsEmpty() || fs.DirectoryExists(directory))
			return false;

		fs.CreateDirectory(directory);
		return true;
	}

	/// <summary>
	/// Safely deletes a directory if it exists.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The directory path to delete.</param>
	public static void SafeDeleteDir(this IFileSystem fs, string path)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		if (!fs.DirectoryExists(path))
			return;

		fs.DeleteDirectory(path, true);
	}

	/// <summary>
	/// Checks whether the specified installation directory exists and contains files or subdirectories.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The installation directory path to check.</param>
	/// <returns>True if the directory exists and contains files or subdirectories; otherwise false.</returns>
	public static bool CheckInstallation(this IFileSystem fs, string path)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		if (path.IsEmpty()) return false;
		if (!fs.DirectoryExists(path)) return false;
		return fs.EnumerateFiles(path).Any() || fs.EnumerateDirectories(path).Any();
	}

	/// <summary>
	/// Retrieves the directories within the specified path matching the search pattern.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The root directory to search.</param>
	/// <param name="searchPattern">The search pattern (supports '*' and '?').</param>
	/// <param name="searchOption">Search option specifying whether to search subdirectories.</param>
	/// <returns>An enumerable of matching directory paths. If the directory does not exist an empty enumerable is returned.</returns>
	public static IEnumerable<string> GetDirectories(this IFileSystem fs, string path,
		string searchPattern = "*",
		SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		return !fs.DirectoryExists(path) ? Array.Empty<string>() : fs.EnumerateDirectories(path, searchPattern, searchOption);
	}

	/// <summary>
	/// Asynchronously retrieves the directories within the specified path matching the search pattern.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The root directory to search.</param>
	/// <param name="searchPattern">The search pattern.</param>
	/// <param name="searchOption">Search option specifying whether to search subdirectories.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that produces an enumerable of matching directory paths.</returns>
	public static Task<IEnumerable<string>> GetDirectoriesAsync(this IFileSystem fs, string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (!fs.DirectoryExists(path)) return Enumerable.Empty<string>().FromResult();

		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			var arr = fs.EnumerateDirectories(path, searchPattern, searchOption).ToArray();
			cancellationToken.ThrowIfCancellationRequested();
			return (IEnumerable<string>)arr;
		}, cancellationToken);
	}

	/// <summary>
	/// Asynchronously retrieves the files within the specified path matching the search pattern.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The root directory to search.</param>
	/// <param name="searchPattern">The search pattern.</param>
	/// <param name="searchOption">Search option specifying whether to search subdirectories.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that produces an enumerable of matching file paths.</returns>
	public static Task<IEnumerable<string>> GetFilesAsync(this IFileSystem fs, string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (!fs.DirectoryExists(path)) return Enumerable.Empty<string>().FromResult();

		return Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			var arr = fs.EnumerateFiles(path, searchPattern, searchOption).ToArray();
			cancellationToken.ThrowIfCancellationRequested();
			return (IEnumerable<string>)arr;
		}, cancellationToken);
	}

	/// <summary>
	/// Gets the timestamp of the specified file using provided file system.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="filePath">The file path to inspect.</param>
	/// <returns>The timestamp representing the build time of the file (based on PE header), in local time.</returns>
	public static DateTime GetTimestamp(this IFileSystem fs, string filePath)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));

		var b = new byte[2048];

		using (var s = fs.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			s.ReadBytes(b, b.Length);

		const int peHeaderOffset = 60;
		const int linkerTimestampOffset = 8;
		var i = BitConverter.ToInt32(b, peHeaderOffset);
		var secondsSince1970 = (long)BitConverter.ToInt32(b, i + linkerTimestampOffset);

		return secondsSince1970.FromUnix().ToLocalTime();
	}

	/// <summary>
	/// Determines whether the file specified by the path is locked by another process.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The file path to check.</param>
	/// <returns>True if the file is locked; otherwise false.</returns>
	public static bool IsFileLocked(this IFileSystem fs, string path)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (!fs.FileExists(path)) return false;

		try { using var stream = fs.Open(path, FileMode.Open, FileAccess.Read, FileShare.None); }
		catch (IOException) { return true; }
		return false;
	}

	/// <summary>
	/// Checks whether the directory contains files or subdirectories that contain files.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The directory path to check.</param>
	/// <returns>True if the directory contains any files; otherwise false.</returns>
	public static bool CheckDirContainFiles(this IFileSystem fs, string path)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		return fs.DirectoryExists(path) && (fs.EnumerateFiles(path).Any() || fs.EnumerateDirectories(path).Any(d => fs.CheckDirContainFiles(d)));
	}

	/// <summary>
	/// Checks whether the directory contains any files or subdirectories.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="path">The directory path to check.</param>
	/// <returns>True if the directory contains any entries; otherwise false.</returns>
	public static bool CheckDirContainsAnything(this IFileSystem fs, string path)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (!fs.DirectoryExists(path)) return false;
		return fs.EnumerateFiles(path).Any() || fs.EnumerateDirectories(path).Any();
	}

	/// <summary>
	/// Determines whether the specified path represents a directory.
	/// </summary>
	/// <param name="fileSystem">The file system to use.</param>
	/// <param name="path">The path to check.</param>
	/// <returns>True if the path is a directory; otherwise false.</returns>
	public static bool IsDirectory(this IFileSystem fileSystem, string path)
	{
		if (fileSystem is null) throw new ArgumentNullException(nameof(fileSystem));
		return fileSystem.GetAttributes(path).HasFlag(FileAttributes.Directory);
	}

	/// <summary>
	/// Creates a temporary directory and returns its path.
	/// </summary>
	/// <param name="fileSystem">The file system to use.</param>
	/// <returns>The path to the new temporary directory.</returns>
	public static string CreateTempDir(this IFileSystem fileSystem)
	{
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Remove("-"));

		if (!fileSystem.DirectoryExists(path))
			fileSystem.CreateDirectory(path);

		return path;
	}

	/// <summary>
	/// Deletes a directory in a blocking manner.
	/// </summary>
	/// <param name="fileSystem">The file system to use.</param>
	/// <param name="dir">The directory to delete.</param>
	/// <param name="isRecursive">Indicates whether to delete subdirectories recursively.</param>
	/// <param name="iterCount">Number of iterations to attempt deletion.</param>
	/// <param name="sleep">Sleep duration between attempts in milliseconds.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>True if the directory still exists after deletion attempts; otherwise, false.</returns>
	public static async ValueTask<bool> BlockDeleteDirAsync(this IFileSystem fileSystem, string dir, bool isRecursive = false, int iterCount = 1000, int sleep = 0, CancellationToken cancellationToken = default)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		if (isRecursive)
		{
			var files = fileSystem.EnumerateFiles(dir);
			var dirs = fileSystem.EnumerateDirectories(dir);

			foreach (var file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				fileSystem.DeleteFile(file);
			}

			foreach (var sub in dirs)
			{
				await BlockDeleteDirAsync(fileSystem, sub, true, iterCount, sleep, cancellationToken);
			}
		}

		// https://stackoverflow.com/a/1703799/8029915
		// Attempt deletion.

		try
		{
			fileSystem.DeleteDirectory(dir, false);
		}
		catch (IOException)
		{
			fileSystem.DeleteDirectory(dir, false);
		}
		catch (UnauthorizedAccessException)
		{
			fileSystem.DeleteDirectory(dir, false);
		}

		var limit = iterCount;

		while (fileSystem.DirectoryExists(dir) && limit-- > 0)
			await Task.Delay(sleep, cancellationToken);

		return fileSystem.DirectoryExists(dir);
	}

	/// <summary>
	/// Recursively deletes empty directories starting from the specified directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="dir">The root directory to check and delete if empty.</param>
	public static void DeleteEmptyDirs(this IFileSystem fs, string dir)
	{
		if (fs is null)
			throw new ArgumentNullException(nameof(fs));

		if (dir.IsEmpty())
			throw new ArgumentNullException(nameof(dir));

		try
		{
			foreach (var d in fs.EnumerateDirectories(dir))
			{
				DeleteEmptyDirs(fs, d);
			}

			if (!fs.EnumerateFiles(dir).Any() && !fs.EnumerateDirectories(dir).Any())
			{
				try
				{
					fs.DeleteDirectory(dir);
				}
				catch (UnauthorizedAccessException) { }
				catch (DirectoryNotFoundException) { }
			}
		}
		catch (UnauthorizedAccessException) { }
	}

	/// <summary>
	/// Creates a file with the specified content.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="rootPath">The root path.</param>
	/// <param name="relativePath">The relative path to the file.</param>
	/// <param name="fileName">The file name.</param>
	/// <param name="content">The content as a byte array.</param>
	public static void CreateFile(this IFileSystem fs, string rootPath, string relativePath, string fileName, byte[] content)
	{
		if (fs is null)
			throw new ArgumentNullException(nameof(fs));

		string fullPath;

		if (relativePath.IsEmpty())
		{
			fullPath = Path.Combine(rootPath, fileName);
		}
		else
		{
			fullPath = Path.Combine(rootPath, relativePath, fileName);
			var dir = Path.GetDirectoryName(fullPath);
			if (!dir.IsEmpty())
				fs.CreateDirectory(dir);
		}

		using var stream = fs.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
		stream.Write(content, 0, content.Length);
	}

	/// <summary>
	/// Saves the byte array to a file specified by fileName.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="data">The byte array to save.</param>
	/// <param name="fileName">The file path to save the data to.</param>
	/// <returns>The original byte array.</returns>
	public static byte[] Save(this IFileSystem fs, byte[] data, string fileName)
	{
		fs.Save(data.To<Stream>(), fileName);
		return data;
	}

	/// <summary>
	/// Saves the content of the stream to a file specified by fileName.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="stream">The stream whose contents to save.</param>
	/// <param name="fileName">The file path to save the stream's contents to.</param>
	/// <returns>The original stream.</returns>
	public static Stream Save(this IFileSystem fs, Stream stream, string fileName)
	{
		if (fs is null)
			throw new ArgumentNullException(nameof(fs));

		var pos = stream.CanSeek ? stream.Position : 0;

		using (var file = fs.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
			stream.CopyTo(file);

		if (stream.CanSeek)
			stream.Position = pos;

		return stream;
	}

	/// <summary>
	/// Attempts to save the byte array to a file and handles any exceptions using the provided errorHandler.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="data">The byte array to save.</param>
	/// <param name="fileName">The file path to save the data to.</param>
	/// <param name="errorHandler">The action to handle exceptions.</param>
	/// <returns>True if the save operation was successful; otherwise, false.</returns>
	public static bool TrySave(this IFileSystem fs, byte[] data, string fileName, Action<Exception> errorHandler)
	{
		if (fs is null)
			throw new ArgumentNullException(nameof(fs));

		if (errorHandler is null)
			throw new ArgumentNullException(nameof(errorHandler));

		try
		{
			fs.Save(data, fileName);
			return true;
		}
		catch (Exception e)
		{
			errorHandler(e);
			return false;
		}
	}
}

