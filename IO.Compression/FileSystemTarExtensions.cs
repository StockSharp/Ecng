namespace Ecng.IO.Compression;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="IFileSystem"/> and streams to work with TAR archives.
/// </summary>
public static class FileSystemTarExtensions
{
	#region TarEntries

	private class TarEntries : IEnumerable<(string name, Stream body)>
	{
		private readonly Stream _input;
		private readonly bool _leaveOpen;
		private readonly Func<string, bool> _filter;

		public TarEntries(Stream input, bool leaveOpen, Func<string, bool> filter)
		{
			_input = input;
			_leaveOpen = leaveOpen;
			_filter = filter;
		}

		private class Enumerator : IEnumerator<(string name, Stream body)>
		{
			private readonly Stream _input;
			private readonly bool _leaveOpen;
			private readonly Func<string, bool> _filter;
			private TarReader _reader;
			private (string name, Stream body) _current;

			public Enumerator(Stream input, bool leaveOpen, Func<string, bool> filter)
			{
				_input = input;
				_leaveOpen = leaveOpen;
				_filter = filter;
			}

			public (string name, Stream body) Current => _current;
			object IEnumerator.Current => _current;

			public bool MoveNext()
			{
				_reader ??= new TarReader(_input, _leaveOpen);

				while (true)
				{
					var entry = _reader.GetNextEntry();
					if (entry is null)
						return false;

					// Skip directories
					if (entry.EntryType == TarEntryType.Directory)
						continue;

					// Apply filter
					if (!_filter(entry.Name))
						continue;

					// Copy entry data to a new stream since TarReader reuses buffers
					var ms = new MemoryStream();
					entry.DataStream?.CopyTo(ms);
					ms.Position = 0;

					_current = (entry.Name, ms);
					return true;
				}
			}

			public void Reset() => throw new NotSupportedException();

			public void Dispose()
			{
				_reader?.Dispose();
			}
		}

		public IEnumerator<(string name, Stream body)> GetEnumerator()
			=> new Enumerator(_input, _leaveOpen, _filter);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	#endregion

	/// <summary>
	/// Extracts entries from a TAR archive contained in the specified byte array.
	/// </summary>
	/// <param name="input">The byte array containing the TAR archive.</param>
	/// <param name="filter">A function to filter entries by name. Only entries returning true will be processed.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Untar(this byte[] input, Func<string, bool> filter = null)
	{
		return input.To<MemoryStream>().Untar(filter: filter);
	}

	/// <summary>
	/// Extracts entries from a TAR archive contained in the specified stream.
	/// </summary>
	/// <param name="input">The stream containing the TAR archive.</param>
	/// <param name="leaveOpen">Whether to leave the underlying stream open after processing.</param>
	/// <param name="filter">A function to filter entries by name. Only entries returning true will be processed.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Untar(this Stream input, bool leaveOpen = false, Func<string, bool> filter = null)
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		return new TarEntries(input, leaveOpen, filter ?? (_ => true));
	}

	/// <summary>
	/// Extracts a TAR archive to the specified directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tarPath">Path to the TAR archive file.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	public static void UntarTo(this IFileSystem fs, string tarPath, string destPath, bool overwrite = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tarPath.IsEmpty()) throw new ArgumentNullException(nameof(tarPath));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var tarStream = fs.OpenRead(tarPath);
		fs.UntarTo(tarStream, destPath, overwrite);
	}

	/// <summary>
	/// Extracts a TAR archive stream to the specified directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tarStream">Stream containing the TAR archive.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	public static void UntarTo(this IFileSystem fs, Stream tarStream, string destPath, bool overwrite = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tarStream is null) throw new ArgumentNullException(nameof(tarStream));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		fs.CreateDirectory(destPath);

		using var reader = new TarReader(tarStream, leaveOpen: true);

		while (reader.GetNextEntry() is { } entry)
		{
			// Skip directories
			if (entry.EntryType == TarEntryType.Directory)
			{
				var dirPath = GetSafeExtractPath(destPath, entry.Name);
				if (dirPath is not null)
					fs.CreateDirectory(dirPath);
				continue;
			}

			var fullPath = GetSafeExtractPath(destPath, entry.Name);
			if (fullPath is null)
				continue; // Skip dangerous entries

			// Ensure parent directory exists
			var dir = Path.GetDirectoryName(fullPath);
			if (!dir.IsEmpty() && !fs.DirectoryExists(dir))
				fs.CreateDirectory(dir);

			if (!overwrite && fs.FileExists(fullPath))
				continue;

			if (entry.DataStream is not null)
			{
				using var fileStream = fs.OpenWrite(fullPath);
				entry.DataStream.CopyTo(fileStream);
			}
		}
	}

	/// <summary>
	/// Extracts a TAR archive to the specified directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tarPath">Path to the TAR archive file.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task UntarToAsync(this IFileSystem fs, string tarPath, string destPath, bool overwrite = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tarPath.IsEmpty()) throw new ArgumentNullException(nameof(tarPath));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var tarStream = fs.OpenRead(tarPath);
		await fs.UntarToAsync(tarStream, destPath, overwrite, cancellationToken);
	}

	/// <summary>
	/// Extracts a TAR archive stream to the specified directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tarStream">Stream containing the TAR archive.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task UntarToAsync(this IFileSystem fs, Stream tarStream, string destPath, bool overwrite = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tarStream is null) throw new ArgumentNullException(nameof(tarStream));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		fs.CreateDirectory(destPath);

		using var reader = new TarReader(tarStream, leaveOpen: true);

		while (await reader.GetNextEntryAsync(cancellationToken: cancellationToken) is { } entry)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Skip directories
			if (entry.EntryType == TarEntryType.Directory)
			{
				var dirPath = GetSafeExtractPath(destPath, entry.Name);
				if (dirPath is not null)
					fs.CreateDirectory(dirPath);
				continue;
			}

			var fullPath = GetSafeExtractPath(destPath, entry.Name);
			if (fullPath is null)
				continue; // Skip dangerous entries

			// Ensure parent directory exists
			var dir = Path.GetDirectoryName(fullPath);
			if (!dir.IsEmpty() && !fs.DirectoryExists(dir))
				fs.CreateDirectory(dir);

			if (!overwrite && fs.FileExists(fullPath))
				continue;

			if (entry.DataStream is not null)
			{
				using var fileStream = fs.OpenWrite(fullPath);
				await entry.DataStream.CopyToAsync(fileStream, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Opens a TAR archive file and returns its entries as streams.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tarPath">Path to the TAR archive file.</param>
	/// <param name="filter">Optional filter function for entry names.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Untar(this IFileSystem fs, string tarPath, Func<string, bool> filter = null)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tarPath.IsEmpty()) throw new ArgumentNullException(nameof(tarPath));

		return fs.OpenRead(tarPath).Untar(leaveOpen: false, filter: filter);
	}

	/// <summary>
	/// Extracts a GZip-compressed TAR archive (.tar.gz or .tgz) to the specified directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tgzPath">Path to the .tar.gz archive file.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	public static void UntgzTo(this IFileSystem fs, string tgzPath, string destPath, bool overwrite = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tgzPath.IsEmpty()) throw new ArgumentNullException(nameof(tgzPath));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var tgzStream = fs.OpenRead(tgzPath);
		fs.UntgzTo(tgzStream, destPath, overwrite);
	}

	/// <summary>
	/// Extracts a GZip-compressed TAR archive stream (.tar.gz) to the specified directory.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tgzStream">Stream containing the .tar.gz archive.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	public static void UntgzTo(this IFileSystem fs, Stream tgzStream, string destPath, bool overwrite = true)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tgzStream is null) throw new ArgumentNullException(nameof(tgzStream));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var gzipStream = new System.IO.Compression.GZipStream(tgzStream, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
		fs.UntarTo(gzipStream, destPath, overwrite);
	}

	/// <summary>
	/// Extracts a GZip-compressed TAR archive (.tar.gz or .tgz) to the specified directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tgzPath">Path to the .tar.gz archive file.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task UntgzToAsync(this IFileSystem fs, string tgzPath, string destPath, bool overwrite = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tgzPath.IsEmpty()) throw new ArgumentNullException(nameof(tgzPath));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var tgzStream = fs.OpenRead(tgzPath);
		await fs.UntgzToAsync(tgzStream, destPath, overwrite, cancellationToken);
	}

	/// <summary>
	/// Extracts a GZip-compressed TAR archive stream (.tar.gz) to the specified directory asynchronously.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tgzStream">Stream containing the .tar.gz archive.</param>
	/// <param name="destPath">Destination directory path.</param>
	/// <param name="overwrite">Whether to overwrite existing files.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public static async Task UntgzToAsync(this IFileSystem fs, Stream tgzStream, string destPath, bool overwrite = true, CancellationToken cancellationToken = default)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tgzStream is null) throw new ArgumentNullException(nameof(tgzStream));
		if (destPath.IsEmpty()) throw new ArgumentNullException(nameof(destPath));

		using var gzipStream = new System.IO.Compression.GZipStream(tgzStream, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
		await fs.UntarToAsync(gzipStream, destPath, overwrite, cancellationToken);
	}

	/// <summary>
	/// Extracts entries from a GZip-compressed TAR archive (.tar.gz) contained in the specified stream.
	/// </summary>
	/// <param name="input">The stream containing the .tar.gz archive.</param>
	/// <param name="leaveOpen">Whether to leave the underlying stream open after processing.</param>
	/// <param name="filter">A function to filter entries by name. Only entries returning true will be processed.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Untgz(this Stream input, bool leaveOpen = false, Func<string, bool> filter = null)
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		var gzipStream = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress, leaveOpen);
		return new TarEntriesWithGzip(gzipStream, filter ?? (_ => true));
	}

	/// <summary>
	/// Opens a GZip-compressed TAR archive file (.tar.gz or .tgz) and returns its entries as streams.
	/// </summary>
	/// <param name="fs">The file system to use.</param>
	/// <param name="tgzPath">Path to the .tar.gz archive file.</param>
	/// <param name="filter">Optional filter function for entry names.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Untgz(this IFileSystem fs, string tgzPath, Func<string, bool> filter = null)
	{
		if (fs is null) throw new ArgumentNullException(nameof(fs));
		if (tgzPath.IsEmpty()) throw new ArgumentNullException(nameof(tgzPath));

		return fs.OpenRead(tgzPath).Untgz(leaveOpen: false, filter: filter);
	}

	#region TarEntriesWithGzip

	private class TarEntriesWithGzip : IEnumerable<(string name, Stream body)>
	{
		private readonly System.IO.Compression.GZipStream _gzipStream;
		private readonly Func<string, bool> _filter;

		public TarEntriesWithGzip(System.IO.Compression.GZipStream gzipStream, Func<string, bool> filter)
		{
			_gzipStream = gzipStream;
			_filter = filter;
		}

		private class Enumerator : IEnumerator<(string name, Stream body)>
		{
			private readonly System.IO.Compression.GZipStream _gzipStream;
			private readonly Func<string, bool> _filter;
			private TarReader _reader;
			private (string name, Stream body) _current;

			public Enumerator(System.IO.Compression.GZipStream gzipStream, Func<string, bool> filter)
			{
				_gzipStream = gzipStream;
				_filter = filter;
			}

			public (string name, Stream body) Current => _current;
			object IEnumerator.Current => _current;

			public bool MoveNext()
			{
				_reader ??= new TarReader(_gzipStream, leaveOpen: false);

				while (true)
				{
					var entry = _reader.GetNextEntry();
					if (entry is null)
						return false;

					// Skip directories
					if (entry.EntryType == TarEntryType.Directory)
						continue;

					// Apply filter
					if (!_filter(entry.Name))
						continue;

					// Copy entry data to a new stream since TarReader reuses buffers
					var ms = new MemoryStream();
					entry.DataStream?.CopyTo(ms);
					ms.Position = 0;

					_current = (entry.Name, ms);
					return true;
				}
			}

			public void Reset() => throw new NotSupportedException();

			public void Dispose()
			{
				_reader?.Dispose();
				_gzipStream?.Dispose();
			}
		}

		public IEnumerator<(string name, Stream body)> GetEnumerator()
			=> new Enumerator(_gzipStream, _filter);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	#endregion

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
}
