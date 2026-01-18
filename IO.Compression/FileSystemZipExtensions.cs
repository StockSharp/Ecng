namespace Ecng.IO.Compression;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="IFileSystem"/> to work with ZIP archives.
/// </summary>
public static class FileSystemZipExtensions
{
	/// <summary>
	/// Extracts a ZIP archive to the specified directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipPath">Path to the ZIP archive file.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	public static void UnzipTo(this IFileSystem fs, string zipPath, string destPath, bool overwrite = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipPath.IsEmpty()) throw new ArgumentNullException(nameof(zipPath));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var zipStream = fs.OpenRead(zipPath);
		fs.UnzipTo(zipStream, destPath, overwrite);
	}

	/// <summary>
	/// Extracts a ZIP archive stream to the specified directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipStream">Stream containing the ZIP archive.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	public static void UnzipTo(this IFileSystem fs, Stream zipStream, string destPath, bool overwrite = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipStream is null) throw new ArgumentNullException(nameof(zipStream));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		fs.CreateDirectory(destPath);

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

		foreach (var entry in archive.Entries)
		{
			var fullPath = GetSafeExtractPath(destPath, entry.FullName);
			if (fullPath is null)
				continue; // Skip dangerous entries

			// Create directory if entry is a directory
			if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
			{
				fs.CreateDirectory(fullPath);
				continue;
			}

			// Ensure parent directory exists
			var dir = Path.GetDirectoryName(fullPath);
			if (!dir.IsEmpty() && !fs.DirectoryExists(dir))
				fs.CreateDirectory(dir);

			if (!overwrite && fs.FileExists(fullPath))
				continue;

			using var entryStream = entry.Open();
			using var fileStream = fs.OpenWrite(fullPath);
			entryStream.CopyTo(fileStream);
		}
	}

	/// <summary>
	/// Extracts a ZIP archive to the specified directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipPath">Path to the ZIP archive file.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task UnzipToAsync(this IFileSystem fs, string zipPath, string destPath, bool overwrite = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipPath.IsEmpty()) throw new ArgumentNullException(nameof(zipPath));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var zipStream = fs.OpenRead(zipPath);
		await fs.UnzipToAsync(zipStream, destPath, overwrite, cancellationToken);
	}

	/// <summary>
	/// Extracts a ZIP archive stream to the specified directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipStream">Stream containing the ZIP archive.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task UnzipToAsync(this IFileSystem fs, Stream zipStream, string destPath, bool overwrite = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipStream is null) throw new ArgumentNullException(nameof(zipStream));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		fs.CreateDirectory(destPath);

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

		foreach (var entry in archive.Entries)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var fullPath = GetSafeExtractPath(destPath, entry.FullName);
			if (fullPath is null)
				continue; // Skip dangerous entries

			// Create directory if entry is a directory
			if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
			{
				fs.CreateDirectory(fullPath);
				continue;
			}

			// Ensure parent directory exists
			var dir = Path.GetDirectoryName(fullPath);
			if (!dir.IsEmpty() && !fs.DirectoryExists(dir))
				fs.CreateDirectory(dir);

			if (!overwrite && fs.FileExists(fullPath))
				continue;

			using var entryStream = entry.Open();
			using var fileStream = fs.OpenWrite(fullPath);
			await entryStream.CopyToAsync(fileStream, cancellationToken);
		}
	}

	/// <summary>
	/// Creates a ZIP archive from a directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="sourcePath">Source directory path to archive.</param>
	/// <param name="zipPath">Path for the output ZIP file.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="includeBaseDirectory">Whether to include the base directory name in the archive.</param>
	public static void ZipFrom(this IFileSystem fs, string sourcePath, string zipPath, CompressionLevel level = CompressionLevel.Optimal, bool includeBaseDirectory = false)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (sourcePath.IsEmpty()) throw new ArgumentNullException(nameof(sourcePath));
		if (zipPath.IsEmpty()) throw new ArgumentNullException(nameof(zipPath));

		using var zipStream = fs.OpenWrite(zipPath);
		fs.ZipFrom(sourcePath, zipStream, level, includeBaseDirectory);
	}

	/// <summary>
	/// Creates a ZIP archive from a directory and writes to a stream.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="sourcePath">Source directory path to archive.</param>
	/// <param name="zipStream">Output stream for the ZIP archive.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="includeBaseDirectory">Whether to include the base directory name in the archive.</param>
	public static void ZipFrom(this IFileSystem fs, string sourcePath, Stream zipStream, CompressionLevel level = CompressionLevel.Optimal, bool includeBaseDirectory = false)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (sourcePath.IsEmpty()) throw new ArgumentNullException(nameof(sourcePath));
		if (zipStream is null) throw new ArgumentNullException(nameof(zipStream));

		var basePath = includeBaseDirectory ? Path.GetDirectoryName(sourcePath) : sourcePath;

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);

		foreach (var file in fs.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
		{
			var relativePath = GetRelativePath(basePath, file);
			var entry = archive.CreateEntry(relativePath.Replace('\\', '/'), level);

			using var entryStream = entry.Open();
			using var fileStream = fs.OpenRead(file);
			fileStream.CopyTo(entryStream);
		}
	}

	/// <summary>
	/// Creates a ZIP archive from a directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="sourcePath">Source directory path to archive.</param>
	/// <param name="zipPath">Path for the output ZIP file.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="includeBaseDirectory">Whether to include the base directory name in the archive.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task ZipFromAsync(this IFileSystem fs, string sourcePath, string zipPath, CompressionLevel level = CompressionLevel.Optimal, bool includeBaseDirectory = false, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (sourcePath.IsEmpty()) throw new ArgumentNullException(nameof(sourcePath));
		if (zipPath.IsEmpty()) throw new ArgumentNullException(nameof(zipPath));

		using var zipStream = fs.OpenWrite(zipPath);
		await fs.ZipFromAsync(sourcePath, zipStream, level, includeBaseDirectory, cancellationToken);
	}

	/// <summary>
	/// Creates a ZIP archive from a directory and writes to a stream asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="sourcePath">Source directory path to archive.</param>
	/// <param name="zipStream">Output stream for the ZIP archive.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="includeBaseDirectory">Whether to include the base directory name in the archive.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task ZipFromAsync(this IFileSystem fs, string sourcePath, Stream zipStream, CompressionLevel level = CompressionLevel.Optimal, bool includeBaseDirectory = false, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (sourcePath.IsEmpty()) throw new ArgumentNullException(nameof(sourcePath));
		if (zipStream is null) throw new ArgumentNullException(nameof(zipStream));

		var basePath = includeBaseDirectory ? Path.GetDirectoryName(sourcePath) : sourcePath;

		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);

		foreach (var file in fs.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
		{
			cancellationToken.ThrowIfCancellationRequested();

			var relativePath = GetRelativePath(basePath, file);
			var entry = archive.CreateEntry(relativePath.Replace('\\', '/'), level);

			using var entryStream = entry.Open();
			using var fileStream = fs.OpenRead(file);
			await fileStream.CopyToAsync(entryStream, cancellationToken);
		}
	}

	/// <summary>
	/// Creates a ZIP archive from a collection of entries and writes to a file.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipPath">Path for the output ZIP file.</param>
	/// <param name="entries">The entries to include in the archive.</param>
	/// <param name="level">Compression level.</param>
	public static void Zip(this IFileSystem fs, string zipPath, IEnumerable<(string name, Stream body)> entries, CompressionLevel level = CompressionLevel.Optimal)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipPath.IsEmpty()) throw new ArgumentNullException(nameof(zipPath));
		if (entries is null) throw new ArgumentNullException(nameof(entries));

		using var zipStream = fs.OpenWrite(zipPath);
		entries.Zip(zipStream, level, leaveOpen: false);
	}

	/// <summary>
	/// Creates a ZIP archive from a collection of entries and writes to a stream.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipStream">Output stream for the ZIP archive.</param>
	/// <param name="entries">The entries to include in the archive.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="leaveOpen">Whether to leave the stream open after creating the archive.</param>
	public static void Zip(this IFileSystem fs, Stream zipStream, IEnumerable<(string name, Stream body)> entries, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipStream is null) throw new ArgumentNullException(nameof(zipStream));
		if (entries is null) throw new ArgumentNullException(nameof(entries));

		entries.Zip(zipStream, level, leaveOpen);
	}

	/// <summary>
	/// Creates a ZIP archive from a collection of entries and writes to a file asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipPath">Path for the output ZIP file.</param>
	/// <param name="entries">The entries to include in the archive.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task ZipAsync(this IFileSystem fs, string zipPath, IEnumerable<(string name, Stream body)> entries, CompressionLevel level = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipPath.IsEmpty()) throw new ArgumentNullException(nameof(zipPath));
		if (entries is null) throw new ArgumentNullException(nameof(entries));

		using var zipStream = fs.OpenWrite(zipPath);
		await entries.ZipAsync(zipStream, level, leaveOpen: false, cancellationToken);
	}

	/// <summary>
	/// Creates a ZIP archive from a collection of entries and writes to a stream asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipStream">Output stream for the ZIP archive.</param>
	/// <param name="entries">The entries to include in the archive.</param>
	/// <param name="level">Compression level.</param>
	/// <param name="leaveOpen">Whether to leave the stream open after creating the archive.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static Task ZipAsync(this IFileSystem fs, Stream zipStream, IEnumerable<(string name, Stream body)> entries, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipStream is null) throw new ArgumentNullException(nameof(zipStream));
		if (entries is null) throw new ArgumentNullException(nameof(entries));

		return entries.ZipAsync(zipStream, level, leaveOpen, cancellationToken);
	}

	/// <summary>
	/// Opens a ZIP archive file and returns its entries as streams.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="zipPath">Path to the ZIP archive file.</param>
	/// <param name="filter">Optional filter function for entry names.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Unzip(this IFileSystem fs, string zipPath, Func<string, bool> filter = null)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (zipPath.IsEmpty()) throw new ArgumentNullException(nameof(zipPath));

		return fs.OpenRead(zipPath).Unzip(leaveOpen: false, filter: filter);
	}

	/// <summary>
	/// Reads all files from a directory and returns them as a collection of entries.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="directoryPath">Directory path to read files from.</param>
	/// <param name="searchPattern">Search pattern for files.</param>
	/// <param name="searchOption">Whether to search subdirectories.</param>
	/// <returns>A collection of file entries with relative paths and streams.</returns>
	public static IEnumerable<(string name, Stream body)> ReadEntries(this IFileSystem fs, string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (directoryPath.IsEmpty()) throw new ArgumentNullException(nameof(directoryPath));

		foreach (var file in fs.EnumerateFiles(directoryPath, searchPattern, searchOption))
		{
			var relativePath = GetRelativePath(directoryPath, file).Replace('\\', '/');
			yield return (relativePath, fs.OpenRead(file));
		}
	}

	/// <summary>
	/// Writes a collection of entries to a directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="directoryPath">Destination directory path.</param>
	/// <param name="entries">The entries to write.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	public static void WriteEntries(this IFileSystem fs, string directoryPath, IEnumerable<(string name, Stream body)> entries, bool overwrite = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (directoryPath.IsEmpty()) throw new ArgumentNullException(nameof(directoryPath));
		if (entries is null) throw new ArgumentNullException(nameof(entries));

		fs.CreateDirectory(directoryPath);

		foreach (var (name, body) in entries)
		{
			var fullPath = Path.Combine(directoryPath, name.Replace('/', Path.DirectorySeparatorChar));

			var dir = Path.GetDirectoryName(fullPath);
			if (!dir.IsEmpty() && !fs.DirectoryExists(dir))
				fs.CreateDirectory(dir);

			if (!overwrite && fs.FileExists(fullPath))
				continue;

			using var fileStream = fs.OpenWrite(fullPath);
			body.CopyTo(fileStream);
		}
	}

	/// <summary>
	/// Writes a collection of entries to a directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="directoryPath">Destination directory path.</param>
	/// <param name="entries">The entries to write.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task WriteEntriesAsync(this IFileSystem fs, string directoryPath, IEnumerable<(string name, Stream body)> entries, bool overwrite = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (directoryPath.IsEmpty()) throw new ArgumentNullException(nameof(directoryPath));
		if (entries is null) throw new ArgumentNullException(nameof(entries));

		fs.CreateDirectory(directoryPath);

		foreach (var (name, body) in entries)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var fullPath = Path.Combine(directoryPath, name.Replace('/', Path.DirectorySeparatorChar));

			var dir = Path.GetDirectoryName(fullPath);
			if (!dir.IsEmpty() && !fs.DirectoryExists(dir))
				fs.CreateDirectory(dir);

			if (!overwrite && fs.FileExists(fullPath))
				continue;

			using var fileStream = fs.OpenWrite(fullPath);
			await body.CopyToAsync(fileStream, cancellationToken);
		}
	}

	private static string GetSafeExtractPath(string destPath, string entryName)
	{
		// Reject entries with absolute paths
		if (Path.IsPathRooted(entryName))
			return null;

		// Normalize the entry name to use the current platform's directory separator
		var normalizedEntry = entryName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

		// Reject entries containing path traversal sequences
		var segments = normalizedEntry.Split(Path.DirectorySeparatorChar);
		foreach (var segment in segments)
		{
			if (segment == "..")
				return null;
		}

		// Combine paths and get the full path
		var fullPath = Path.Combine(destPath, normalizedEntry);
		var resolvedPath = Path.GetFullPath(fullPath);
		var resolvedDest = Path.GetFullPath(destPath);

		// Ensure the destination ends with a separator for proper prefix matching
		if (!resolvedDest.EndsWith(Path.DirectorySeparatorChar.ToString()))
			resolvedDest += Path.DirectorySeparatorChar;

		// Verify the resolved path is within the destination directory
		if (!resolvedPath.StartsWith(resolvedDest, StringComparison.OrdinalIgnoreCase))
			return null;

		return fullPath;
	}

	private static string GetRelativePath(string basePath, string fullPath)
	{
#if NETSTANDARD2_0
		if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !basePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
			basePath += Path.DirectorySeparatorChar;

		var baseUri = new Uri(basePath);
		var fullUri = new Uri(fullPath);
		return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
#else
		return Path.GetRelativePath(basePath, fullPath);
#endif
	}
}
