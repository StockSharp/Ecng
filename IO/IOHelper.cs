namespace Ecng.IO;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

using Ecng.Common;

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
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static DirectoryInfo ClearDirectory(string path, Func<string, bool> filter = null)
		=> LocalFileSystem.Instance.ClearDirectory(path, filter);

	/// <summary>
	/// Asynchronously clears the specified directory by deleting its files and subdirectories.
	/// </summary>
	/// <param name="path">The directory path to clear.</param>
	/// <param name="filter">Optional filter to determine which files to delete.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation, containing a DirectoryInfo for the cleared directory.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static Task<DirectoryInfo> ClearDirectoryAsync(string path, Func<string, bool> filter = null, CancellationToken cancellationToken = default)
		=> LocalFileSystem.Instance.ClearDirectoryAsync(path, filter, cancellationToken);

	/// <summary>
	/// Copies the content of one directory to another.
	/// </summary>
	/// <param name="sourcePath">The source directory path.</param>
	/// <param name="destPath">The destination directory path.</param>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static void CopyDirectory(string sourcePath, string destPath)
		=> LocalFileSystem.Instance.CopyDirectory(sourcePath, destPath);

	/// <summary>
	/// Asynchronously copies the content of one directory to another.
	/// </summary>
	/// <param name="sourcePath">The source directory path.</param>
	/// <param name="destPath">The destination directory path.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static Task CopyDirectoryAsync(string sourcePath, string destPath, CancellationToken cancellationToken = default)
		=> LocalFileSystem.Instance.CopyDirectoryAsync(sourcePath, destPath, cancellationToken);

	/// <summary>
	/// Copies a file to the specified destination and makes the copy writable.
	/// </summary>
	/// <param name="fileName">The source file path.</param>
	/// <param name="destPath">The destination directory path.</param>
	/// <returns>The destination file path.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static string CopyAndMakeWritable(string fileName, string destPath)
		=> LocalFileSystem.Instance.CopyAndMakeWritable(fileName, destPath);

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
		return Path.Combine(path, relativePart).ToFullPath();
	}

	/// <summary>
	/// Creates the directory for the specified file if it does not already exist.
	/// </summary>
	/// <param name="fullPath">The full path to the file.</param>
	/// <returns>True if the directory was created; otherwise, false.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static bool CreateDirIfNotExists(this string fullPath)
		=> LocalFileSystem.Instance.CreateDirIfNotExists(fullPath);

	/// <summary>
	/// Safely deletes a directory.
	/// </summary>
	/// <param name="path">The directory path.</param>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static void SafeDeleteDir(this string path)
		=> LocalFileSystem.Instance.SafeDeleteDir(path);

	/// <summary>
	/// Checks if the specified installation directory exists and contains files or subdirectories.
	/// </summary>
	/// <param name="path">The installation directory path.</param>
	/// <returns>True if the installation is valid; otherwise, false.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static bool CheckInstallation(string path)
		=> LocalFileSystem.Instance.CheckInstallation(path);

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
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static void CreateFile(string rootPath, string relativePath, string fileName, byte[] content)
		=> LocalFileSystem.Instance.CreateFile(rootPath, relativePath, fileName, content);

	/// <summary>
	/// Recursively deletes empty directories starting from the specified directory.
	/// </summary>
	/// <param name="dir">The root directory to check and delete if empty.</param>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static void DeleteEmptyDirs(string dir)
		=> LocalFileSystem.Instance.DeleteEmptyDirs(dir);

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
	/// Retrieves the directories within the specified path matching the search pattern.
	/// </summary>
	/// <param name="path">The root directory to search.</param>
	/// <param name="searchPattern">The search pattern.</param>
	/// <param name="searchOption">Search option to determine whether to search subdirectories.</param>
	/// <returns>An enumerable of matching directory paths.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static IEnumerable<string> GetDirectories(string path,
		string searchPattern = "*",
		SearchOption searchOption = SearchOption.TopDirectoryOnly)
		=> LocalFileSystem.Instance.GetDirectories(path, searchPattern, searchOption);

	/// <summary>
	/// Asynchronously retrieves the directories within the specified path matching the search pattern.
	/// This method emulates async behavior by running the synchronous enumeration on the thread-pool.
	/// </summary>
	/// <param name="path">The root directory to search.</param>
	/// <param name="searchPattern">The search pattern.</param>
	/// <param name="searchOption">Search option to determine whether to search subdirectories.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A task producing an enumerable of matching directory paths.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static Task<IEnumerable<string>> GetDirectoriesAsync(
		string path,
		string searchPattern = "*",
		SearchOption searchOption = SearchOption.TopDirectoryOnly,
		CancellationToken cancellationToken = default)
		=> LocalFileSystem.Instance.GetDirectoriesAsync(path, searchPattern, searchOption, cancellationToken);

	/// <summary>
	/// Asynchronously retrieves the files within the specified path matching the search pattern.
	/// This method emulates async behavior by running the synchronous enumeration on the thread-pool.
	/// </summary>
	/// <param name="path">The root directory to search.</param>
	/// <param name="searchPattern">The search pattern.</param>
	/// <param name="searchOption">Search option to determine whether to search subdirectories.</param>
	/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
	/// <returns>A task producing an enumerable of matching file paths.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static Task<IEnumerable<string>> GetFilesAsync(
		string path,
		string searchPattern = "*",
		SearchOption searchOption = SearchOption.TopDirectoryOnly,
		CancellationToken cancellationToken = default)
		=> LocalFileSystem.Instance.GetFilesAsync(path, searchPattern, searchOption, cancellationToken);

	/// <summary>
	/// Gets the timestamp of the specified assembly.
	/// </summary>
	/// <param name="assembly">The assembly.</param>
	/// <returns>The timestamp when the assembly was built.</returns>
	public static DateTime GetTimestamp(this Assembly assembly)
	{
		if (assembly is null)
			throw new ArgumentNullException(nameof(assembly));

		return LocalFileSystem.Instance.GetTimestamp(assembly.Location);
	}

	/// <summary>
	/// Gets the timestamp of the specified file.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <returns>The timestamp representing when the file was built.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static DateTime GetTimestamp(string filePath)
		=> LocalFileSystem.Instance.GetTimestamp(filePath);

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
		ReadBytes(stream, buffer.AsMemory(pos, len));
		return buffer;
	}

	/// <summary>
	/// Reads a specified number of bytes from a stream.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer to fill.</param>
	public static void ReadBytes(this Stream stream, Memory<byte> buffer)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (buffer.IsEmpty)
			return;

		var totalRead = 0;

#if NET5_0_OR_GREATER
		while (totalRead < buffer.Length)
		{
			var read = stream.Read(buffer[totalRead..].Span);

			if (read <= 0)
				throw new IOException($"Stream returned '{read}' bytes. Expected {buffer.Length - totalRead} more bytes.");

			totalRead += read;
		}
#else
		if (MemoryMarshal.TryGetArray<byte>(buffer, out var segment))
		{
			while (totalRead < buffer.Length)
            {
                var read = stream.Read(segment.Array, segment.Offset + totalRead, buffer.Length - totalRead);

                if (read <= 0)
					throw new IOException($"Stream returned '{read}' bytes. Expected {buffer.Length - totalRead} more bytes.");

                totalRead += read;
            }
		}
		else
		{
			var tempBuffer = new byte[Math.Min(8192, buffer.Length)];

            while (totalRead < buffer.Length)
            {
                var bytesToRead = Math.Min(tempBuffer.Length, buffer.Length - totalRead);

                var read = stream.Read(tempBuffer, 0, bytesToRead);

				if (read <= 0)
					throw new IOException($"Stream returned '{read}' bytes. Expected {buffer.Length - totalRead} more bytes.");

                tempBuffer.AsSpan(0, read).CopyTo(buffer.Span.Slice(totalRead));
                totalRead += read;
            }
		}
#endif
	}

	/// <summary>
	/// Reads exactly the specified number of bytes from the stream into a byte array.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="size">The number of bytes to read.</param>
	/// <returns>A byte array containing the data read from the stream.</returns>
	[Obsolete("Use Stream.ReadExactly extension method instead.")]
	public static byte[] ReadBuffer(this Stream stream, int size)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (size < 0)
			throw new ArgumentOutOfRangeException(nameof(size), $"Size has negative value '{size}'.");

		var buffer = new byte[size];

		try
		{
			stream.ReadExactly(buffer, 0, size);
		}
		catch (EndOfStreamException ex)
		{
			throw new ArgumentException($"Insufficient stream size '{size}'.", nameof(stream), ex);
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

		byte[] buffer;

		if (size > 0)
		{
			buffer = new byte[size];
			stream.ReadExactly(buffer, 0, size);
		}
		else
		{
			buffer = [];
		}

		if (type == typeof(byte[]))
			return buffer;
		else
			return buffer.To(type);
	}

	#endregion

	/// <summary>
	/// Saves the content of the stream to a file specified by fileName.
	/// </summary>
	/// <param name="stream">The stream whose contents to save.</param>
	/// <param name="fileName">The file path to save the stream's contents to.</param>
	/// <returns>The original stream.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static Stream Save(this Stream stream, string fileName)
		=> LocalFileSystem.Instance.Save(stream, fileName);

	/// <summary>
	/// Saves the byte array to a file specified by fileName.
	/// </summary>
	/// <param name="data">The byte array to save.</param>
	/// <param name="fileName">The file path to save the data to.</param>
	/// <returns>The original byte array.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static byte[] Save(this byte[] data, string fileName)
		=> LocalFileSystem.Instance.Save(data, fileName);

	/// <summary>
	/// Attempts to save the byte array to a file and handles any exceptions using the provided errorHandler.
	/// </summary>
	/// <param name="data">The byte array to save.</param>
	/// <param name="fileName">The file path to save the data to.</param>
	/// <param name="errorHandler">The action to handle exceptions.</param>
	/// <returns>True if the save operation was successful; otherwise, false.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static bool TrySave(this byte[] data, string fileName, Action<Exception> errorHandler)
		=> LocalFileSystem.Instance.TrySave(data, fileName, errorHandler);

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
#if !NETSTANDARD2_0
	[Obsolete("Use Span/Memory overloads instead.")]
#endif
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
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static bool CheckDirContainFiles(string path)
		=> LocalFileSystem.Instance.CheckDirContainFiles(path);

	/// <summary>
	/// Checks whether the directory contains any files or subdirectories.
	/// </summary>
	/// <param name="path">The directory path to check.</param>
	/// <returns>True if the directory contains any entries; otherwise, false.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static bool CheckDirContainsAnything(string path)
		=> LocalFileSystem.Instance.CheckDirContainsAnything(path);

	/// <summary>
	/// Determines whether the file specified by the path is locked by another process.
	/// </summary>
	/// <param name="path">The path to the file to check.</param>
	/// <returns>True if the file is locked; otherwise, false.</returns>
	[Obsolete("Use overload with IFileSystem parameter.")]
	public static bool IsFileLocked(string path)
		=> LocalFileSystem.Instance.IsFileLocked(path);

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
			).NoWait();

			if (bytesRead == 0)
				break;

			totalRead += bytesRead;
		}

		if (totalRead < bytesToRead)
			throw new IOException("Connection dropped.");

		return totalRead;
	}
}
