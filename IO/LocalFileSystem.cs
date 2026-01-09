namespace Ecng.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Implementation of <see cref="IFileSystem"/> over the local file system (<see cref="System.IO"/>).
/// </summary>
public class LocalFileSystem : IFileSystem
{
	/// <summary>
	/// Singleton instance (without size limits).
	/// </summary>
	public static LocalFileSystem Instance { get; } = new();

	private readonly Lock _lock = new();
	private long _totalSize;

	/// <summary>
	/// Maximum total size of all files in bytes. Zero or negative means unlimited.
	/// When set to a positive value, streams are wrapped to track writes.
	/// </summary>
	public long MaxSize { get; set; }

	/// <summary>
	/// Behavior when <see cref="MaxSize"/> limit is exceeded.
	/// </summary>
	public FileSystemOverflowBehavior OverflowBehavior { get; set; } = FileSystemOverflowBehavior.ThrowException;

	/// <summary>
	/// Current total size tracked for limit enforcement.
	/// Only meaningful when <see cref="MaxSize"/> is positive.
	/// </summary>
	public long TotalSize
	{
		get
		{
			using (_lock.EnterScope())
				return _totalSize;
		}
	}

	/// <inheritdoc />
	public bool FileExists(string path) => File.Exists(path);
	/// <inheritdoc />
	public bool DirectoryExists(string path) => Directory.Exists(path);

	/// <inheritdoc />
	public Stream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None)
	{
		var stream = File.Open(path, mode, access, share);

		// No limit - return raw FileStream for maximum performance
		if (MaxSize <= 0)
			return stream;

		// With limit - wrap stream for tracking
		if (access == FileAccess.Read)
			return stream; // Read-only doesn't need tracking

		var oldSize = mode is FileMode.Create or FileMode.CreateNew or FileMode.Truncate
			? 0L
			: (FileExists(path) ? new FileInfo(path).Length : 0L);

		return new SizeLimitedStream(stream, this, oldSize);
	}

	private class SizeLimitedStream(FileStream inner, LocalFileSystem fs, long oldSize) : Stream
	{
		private bool _disposed;

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (!fs.CheckAndReserveSpace(count))
			{
				if (fs.OverflowBehavior == FileSystemOverflowBehavior.ThrowException)
					throw new IOException($"LocalFileSystem size limit exceeded. Limit: {fs.MaxSize}, Current: {fs.TotalSize}, Required: {fs.TotalSize + count}");
				return; // IgnoreWrites
			}

			inner.Write(buffer, offset, count);
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!fs.CheckAndReserveSpace(count))
			{
				if (fs.OverflowBehavior == FileSystemOverflowBehavior.ThrowException)
					throw new IOException($"LocalFileSystem size limit exceeded. Limit: {fs.MaxSize}, Current: {fs.TotalSize}, Required: {fs.TotalSize + count}");
				return;
			}

			await inner.WriteAsync(buffer, offset, count, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				_disposed = true;

				// Adjust total size: subtract old size, new size is already tracked via writes
				using (fs._lock.EnterScope())
					fs._totalSize -= oldSize;

				inner.Dispose();
			}
			base.Dispose(disposing);
		}

		public override void Flush() => inner.Flush();
		public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
		public override void SetLength(long value) => inner.SetLength(value);
		public override bool CanRead => inner.CanRead;
		public override bool CanSeek => inner.CanSeek;
		public override bool CanWrite => inner.CanWrite;
		public override long Length => inner.Length;
		public override long Position { get => inner.Position; set => inner.Position = value; }
	}

	private bool CheckAndReserveSpace(long bytes)
	{
		using (_lock.EnterScope())
		{
			if (_totalSize + bytes > MaxSize)
				return false;

			_totalSize += bytes;
			return true;
		}
	}

	/// <inheritdoc />
	public void CreateDirectory(string path) => Directory.CreateDirectory(path);
	/// <inheritdoc />
	public void DeleteDirectory(string path, bool recursive = false)
	{
		if (MaxSize > 0 && Directory.Exists(path))
		{
			var size = CalculateDirectorySize(path);
			using (_lock.EnterScope())
				_totalSize -= size;
		}
		Directory.Delete(path, recursive);
	}

	private static long CalculateDirectorySize(string path)
	{
		long size = 0;
		foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
			size += new FileInfo(file).Length;
		return size;
	}

	/// <inheritdoc />
	public void DeleteFile(string path)
	{
		if (MaxSize > 0 && File.Exists(path))
		{
			var size = new FileInfo(path).Length;
			using (_lock.EnterScope())
				_totalSize -= size;
		}
		File.Delete(path);
	}

	/// <inheritdoc />
	public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		var dir = Path.GetDirectoryName(destFileName);

		if (!dir.IsEmpty() && !Directory.Exists(dir))
			Directory.CreateDirectory(dir);

		if (overwrite && File.Exists(destFileName))
		{
			if (MaxSize > 0)
			{
				var size = new FileInfo(destFileName).Length;
				using (_lock.EnterScope())
					_totalSize -= size;
			}
			File.Delete(destFileName);
		}

		File.Move(sourceFileName, destFileName);
	}

	/// <inheritdoc />
	public void MoveDirectory(string sourceDirName, string destDirName)
	{
		var parentDir = Path.GetDirectoryName(destDirName);

		if (!parentDir.IsEmpty() && !Directory.Exists(parentDir))
			Directory.CreateDirectory(parentDir);

		Directory.Move(sourceDirName, destDirName);
	}

	/// <inheritdoc />
	public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		var dir = Path.GetDirectoryName(destFileName);

		if (!dir.IsEmpty() && !Directory.Exists(dir))
			Directory.CreateDirectory(dir);

		long oldDestSize = 0;
		if (MaxSize > 0 && overwrite && File.Exists(destFileName))
			oldDestSize = new FileInfo(destFileName).Length;

		File.Copy(sourceFileName, destFileName, overwrite);

		if (MaxSize > 0)
		{
			var newSize = new FileInfo(destFileName).Length;
			using (_lock.EnterScope())
				_totalSize += newSize - oldDestSize;
		}
	}

	/// <inheritdoc />
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
		=> Directory.EnumerateFiles(path, searchPattern, searchOption);

	/// <inheritdoc />
	public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
		=> Directory.EnumerateDirectories(path, searchPattern, searchOption);

	/// <inheritdoc />
	public DateTime GetCreationTimeUtc(string path) => File.GetCreationTimeUtc(path);
	/// <inheritdoc />
	public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
	/// <inheritdoc />
	public long GetFileLength(string path) => new FileInfo(path).Length;

	/// <inheritdoc />
	public void SetReadOnly(string path, bool isReadOnly)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));

		var attrs = File.GetAttributes(path);

		if (isReadOnly)
			attrs |= FileAttributes.ReadOnly;
		else
			attrs &= ~FileAttributes.ReadOnly;

		File.SetAttributes(path, attrs);
	}

	/// <inheritdoc />
	public FileAttributes GetAttributes(string path)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));

		return File.GetAttributes(path);
	}
}