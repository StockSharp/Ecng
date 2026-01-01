namespace Ecng.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;

using Ecng.Common;

/// <summary>
/// In-memory implementation of <see cref="IFileSystem"/> useful for tests and environments without disk access.
/// Thread-safe for basic operations.
/// </summary>
public class MemoryFileSystem : IFileSystem
{
	private class Node
	{
		public bool IsDirectory;
		public byte[] Data; // file content
		public Dictionary<string, Node> Children; // directory children
		public DateTime CreatedUtc;
		public DateTime UpdatedUtc;
		public DateTime LastClosedUtc; // for eviction: when file was last closed
		public FileAttributes Attributes;
		public long Length => Data?.LongLength ?? 0L;
		public List<FileHandle> OpenHandles; // tracks open file handles for FileShare enforcement
		public string FullPath; // for eviction: need to know path to delete
	}

	private class FileHandle
	{
		public FileAccess Access;
		public FileShare Share;
	}

	private readonly Node _root = new()
	{
		IsDirectory = true,
		Attributes = FileAttributes.Directory,
		Children = new(StringComparer.OrdinalIgnoreCase),
		CreatedUtc = DateTime.UtcNow,
		UpdatedUtc = DateTime.UtcNow
	};

	private readonly Lock _lock = new();
	private long _totalSize;

	/// <summary>
	/// Maximum total size of all files in bytes. Zero or negative means unlimited.
	/// </summary>
	public long MaxSize { get; set; }

	/// <summary>
	/// Behavior when <see cref="MaxSize"/> limit is exceeded.
	/// </summary>
	public FileSystemOverflowBehavior OverflowBehavior { get; set; } = FileSystemOverflowBehavior.ThrowException;

	/// <summary>
	/// Current total size of all files in bytes.
	/// </summary>
	public long TotalSize
	{
		get
		{
			using (_lock.EnterScope())
				return _totalSize;
		}
	}

	private static bool CanOpenWithExistingHandles(Node node, FileAccess newAccess, FileShare newShare)
	{
		if (node.OpenHandles == null || node.OpenHandles.Count == 0)
			return true;

		foreach (var existing in node.OpenHandles)
		{
			// Check if existing handle allows the new access
			if (newAccess.HasFlag(FileAccess.Read) && !existing.Share.HasFlag(FileShare.Read))
				return false;
			if (newAccess.HasFlag(FileAccess.Write) && !existing.Share.HasFlag(FileShare.Write))
				return false;

			// Check if new handle allows the existing access
			if (existing.Access.HasFlag(FileAccess.Read) && !newShare.HasFlag(FileShare.Read))
				return false;
			if (existing.Access.HasFlag(FileAccess.Write) && !newShare.HasFlag(FileShare.Write))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Collects all file nodes that can be evicted (closed, not read-only).
	/// </summary>
	private List<Node> GetEvictableCandidates()
	{
		var result = new List<Node>();
		CollectFiles(_root, result);
		return result;

		static void CollectFiles(Node node, List<Node> list)
		{
			if (node.Children == null)
				return;

			foreach (var child in node.Children.Values)
			{
				if (child.IsDirectory)
				{
					CollectFiles(child, list);
				}
				else if ((child.OpenHandles == null || child.OpenHandles.Count == 0) &&
				         !child.Attributes.HasFlag(FileAttributes.ReadOnly))
				{
					list.Add(child);
				}
			}
		}
	}

	/// <summary>
	/// Tries to free up space by evicting oldest closed files.
	/// </summary>
	/// <param name="requiredSpace">Amount of space needed.</param>
	/// <returns>True if enough space was freed; false otherwise.</returns>
	private bool TryEvictOldest(long requiredSpace)
	{
		var candidates = GetEvictableCandidates();
		if (candidates.Count == 0)
			return false;

		// Sort by LastClosedUtc (oldest first), then by UpdatedUtc as fallback
		candidates.Sort((a, b) =>
		{
			var cmp = a.LastClosedUtc.CompareTo(b.LastClosedUtc);
			return cmp != 0 ? cmp : a.UpdatedUtc.CompareTo(b.UpdatedUtc);
		});

		long freedSpace = 0;
		foreach (var candidate in candidates)
		{
			if (candidate.FullPath == null)
				continue;

			var size = candidate.Length;
			DeleteFileInternal(candidate.FullPath);
			freedSpace += size;

			if (freedSpace >= requiredSpace)
				return true;
		}

		return freedSpace >= requiredSpace;
	}

	/// <summary>
	/// Internal delete without lock (caller must hold lock).
	/// </summary>
	private void DeleteFileInternal(string path)
	{
		var parts = Split(path);
		if (parts.Length == 0)
			return;

		var dir = _root;
		for (int i = 0; i < parts.Length - 1; i++)
		{
			if (!dir.IsDirectory || dir.Children == null || !dir.Children.TryGetValue(parts[i], out var next))
				return;
			dir = next;
		}

		var fileName = parts[parts.Length - 1];
		if (dir.Children != null && dir.Children.TryGetValue(fileName, out var fileNode))
		{
			_totalSize -= fileNode.Length;
			dir.Children.Remove(fileName);
		}
	}

	/// <summary>
	/// Handles size limit check and returns whether the write should proceed.
	/// Must be called with lock held.
	/// </summary>
	/// <param name="oldSize">Previous file size.</param>
	/// <param name="newSize">New file size.</param>
	/// <returns>True if write is allowed; false if it should be ignored.</returns>
	private bool HandleSizeLimit(long oldSize, long newSize)
	{
		if (MaxSize <= 0)
			return true; // No limit

		var delta = newSize - oldSize;
		if (delta <= 0)
			return true; // Shrinking or same size

		var projectedTotal = _totalSize + delta;
		if (projectedTotal <= MaxSize)
			return true; // Within limit

		// Limit exceeded
		switch (OverflowBehavior)
		{
			case FileSystemOverflowBehavior.ThrowException:
				throw new IOException($"MemoryFileSystem size limit exceeded. Limit: {MaxSize}, Current: {_totalSize}, Required: {projectedTotal}");

			case FileSystemOverflowBehavior.IgnoreWrites:
				return false;

			case FileSystemOverflowBehavior.EvictOldest:
				var needed = projectedTotal - MaxSize;
				if (TryEvictOldest(needed))
					return true;
				// Could not free enough space
				throw new IOException($"MemoryFileSystem size limit exceeded and cannot evict enough files. Limit: {MaxSize}, Current: {_totalSize}, Required: {projectedTotal}");

			default:
				return true;
		}
	}

	private static bool CanDeleteWithExistingHandles(Node node)
	{
		if (node.OpenHandles == null || node.OpenHandles.Count == 0)
			return true;

		foreach (var existing in node.OpenHandles)
		{
			if (!existing.Share.HasFlag(FileShare.Delete))
				return false;
		}

		return true;
	}

	private static string[] Split(string path)
	{
		path = path?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) ?? string.Empty;
		path = path.Trim();
		if (path.Length == 0 || path == Path.DirectorySeparatorChar.ToString())
			return [];
		return path.Split([Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
	}

	private Node Traverse(string path, bool createDirs, bool forFile, out string fileName)
	{
		fileName = null;
		var parts = Split(path);
		var cur = _root;
		for (int i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			var isLast = i == parts.Length - 1;
			if (isLast && forFile)
			{
				fileName = part;
				break;
			}
			if (!cur.IsDirectory)
				throw new IOException("Path segment is not a directory: " + part);
			cur.Children ??= new(StringComparer.OrdinalIgnoreCase);
			if (!cur.Children.TryGetValue(part, out var next))
			{
				if (!createDirs)
					return null;
				next = new Node
				{
					IsDirectory = true,
					Attributes = FileAttributes.Directory,
					Children = new(StringComparer.OrdinalIgnoreCase),
					CreatedUtc = DateTime.UtcNow,
					UpdatedUtc = DateTime.UtcNow
				};
				cur.Children[part] = next;
			}
			cur = next;
		}
		return cur;
	}

	private Node GetNode(string path)
	{
		var parts = Split(path);
		var cur = _root;
		foreach (var part in parts)
		{
			if (!cur.IsDirectory)
				return null;
			if (cur.Children == null || !cur.Children.TryGetValue(part, out var next))
				return null;
			cur = next;
		}
		return cur;
	}

	/// <inheritdoc />
	public bool FileExists(string path)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path);
			return node != null && !node.IsDirectory;
		}
	}

	/// <inheritdoc />
	public bool DirectoryExists(string path)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path);
			return node != null && node.IsDirectory;
		}
	}

	/// <inheritdoc />
	public Stream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None)
	{
		using (_lock.EnterScope())
		{
			var exists = FileExists(path);

			switch (mode)
			{
				case FileMode.CreateNew:
					if (exists)
						throw new IOException("File already exists.");
					break;
				case FileMode.Create:
					break;
				case FileMode.Open:
					if (!exists)
						throw new FileNotFoundException(path);
					break;
				case FileMode.OpenOrCreate:
					break;
				case FileMode.Truncate:
					if (!exists)
						throw new FileNotFoundException(path);
					break;
				case FileMode.Append:
					if (access.HasFlag(FileAccess.Read))
						throw new ArgumentException("Append mode cannot be used with Read access.", nameof(access));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode));
			}

			if (access == FileAccess.Read)
			{
				var node = GetNode(path) ?? throw new FileNotFoundException(path);
				if (node.IsDirectory)
					throw new UnauthorizedAccessException("Cannot open directory for reading.");

				// Check FileShare compatibility
				if (!CanOpenWithExistingHandles(node, access, share))
					throw new IOException("The process cannot access the file because it is being used by another process.");

				// Register handle and return read-only stream
				var handle = new FileHandle { Access = access, Share = share };
				node.OpenHandles ??= [];
				node.OpenHandles.Add(handle);

				return new ReadOnlyHandleStream(new MemoryStream(node.Data ?? [], false), () =>
				{
					using (_lock.EnterScope())
					{
						node.OpenHandles?.Remove(handle);
					}
				});
			}

			var append = mode == FileMode.Append;
			var truncate = mode is FileMode.Create or FileMode.CreateNew or FileMode.Truncate;

			var dir = Traverse(path, createDirs: false, forFile: true, out var fileName);
			if (dir == null)
				throw new DirectoryNotFoundException($"Could not find a part of the path '{path}'.");
			if (!dir.IsDirectory)
				throw new IOException("Path's parent is not a directory.");
			dir.Children ??= new(StringComparer.OrdinalIgnoreCase);
			var normalizedPath = Normalize(path);
			if (!dir.Children.TryGetValue(fileName, out var fileNode))
			{
				fileNode = new Node
				{
					IsDirectory = false,
					Attributes = FileAttributes.Normal,
					Data = [],
					CreatedUtc = DateTime.UtcNow,
					UpdatedUtc = DateTime.UtcNow,
					FullPath = normalizedPath
				};
				dir.Children[fileName] = fileNode;
			}
			else
			{
				fileNode.FullPath = normalizedPath;
			}

			if (fileNode.IsDirectory)
				throw new IOException("Path points to a directory.");

			if (exists && fileNode.Attributes.HasFlag(FileAttributes.ReadOnly) && access.HasFlag(FileAccess.Write))
				throw new UnauthorizedAccessException("Access to the path is denied.");

			// Check FileShare compatibility
			if (!CanOpenWithExistingHandles(fileNode, access, share))
				throw new IOException("The process cannot access the file because it is being used by another process.");

			// Register handle
			var writeHandle = new FileHandle { Access = access, Share = share };
			fileNode.OpenHandles ??= [];
			fileNode.OpenHandles.Add(writeHandle);

			var baseData = truncate ? [] : (fileNode.Data ?? []);
			var oldSize = fileNode.Length;
			var ms = new MemoryStream();
			if (baseData.Length > 0)
				ms.Write(baseData, 0, baseData.Length);

			var appendStart = append ? ms.Length : -1L;
			if (append)
				ms.Seek(0, SeekOrigin.End);
			else
				ms.Seek(0, SeekOrigin.Begin);

			return new CommittingStream(ms, bytes =>
			{
				using (_lock.EnterScope())
				{
					var newSize = bytes.LongLength;
					if (HandleSizeLimit(oldSize, newSize))
					{
						_totalSize += newSize - oldSize;
						fileNode.Data = bytes;
						fileNode.UpdatedUtc = DateTime.UtcNow;
					}
					// else: IgnoreWrites - keep old data
				}
			}, () =>
			{
				using (_lock.EnterScope())
				{
					fileNode.OpenHandles?.Remove(writeHandle);
					fileNode.LastClosedUtc = DateTime.UtcNow;
				}
			}, access, appendStart);
		}
	}

	private class ReadOnlyHandleStream(MemoryStream inner, Action onDispose) : Stream
	{
		private bool _disposed;

		protected override void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				_disposed = true;
				onDispose();
				inner.Dispose();
			}
			base.Dispose(disposing);
		}

		public override void Flush() => inner.Flush();
		public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override bool CanRead => true;
		public override bool CanSeek => inner.CanSeek;
		public override bool CanWrite => false;
		public override long Length => inner.Length;
		public override long Position { get => inner.Position; set => inner.Position = value; }
	}

	private class CommittingStream(MemoryStream inner, Action<byte[]> commit, Action onDispose, FileAccess access, long appendStart) : Stream
	{
		private bool _disposed;

		protected override void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				_disposed = true;
				commit(inner.ToArray());
				onDispose();
				inner.Dispose();
			}
			base.Dispose(disposing);
		}
		public override void Flush() => inner.Flush();

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (!CanRead)
				throw new NotSupportedException("Stream does not support reading.");
			return inner.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			var newPos = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => inner.Position + offset,
				SeekOrigin.End => inner.Length + offset,
				_ => throw new ArgumentOutOfRangeException(nameof(origin))
			};

			if (appendStart >= 0 && newPos < appendStart)
				throw new IOException("Unable to seek before the append start position.");

			return inner.Seek(offset, origin);
		}

		public override void SetLength(long value) => inner.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (!CanWrite)
				throw new NotSupportedException("Stream does not support writing.");
			inner.Write(buffer, offset, count);
		}
		public override bool CanRead => access.HasFlag(FileAccess.Read);
		public override bool CanSeek => inner.CanSeek;
		public override bool CanWrite => access.HasFlag(FileAccess.Write);
		public override long Length => inner.Length;
		public override long Position { get => inner.Position; set => inner.Position = value; }
	}

	/// <inheritdoc />
	public void CreateDirectory(string path)
	{
		using (_lock.EnterScope())
		{
			var node = Traverse(path, createDirs: true, forFile: false, out _);
			if (node != null && !node.IsDirectory)
				throw new IOException("Path exists and is not a directory.");
		}
	}

	/// <inheritdoc />
	public void DeleteDirectory(string path, bool recursive = false)
	{
		using (_lock.EnterScope())
		{
			if (path.IsEmpty() || path == Path.DirectorySeparatorChar.ToString())
				throw new IOException("Cannot delete root.");
			var parts = Split(path);
			var stack = new List<Node>();
			var names = new List<string>();
			var cur = _root;
			foreach (var part in parts)
			{
				if (!cur.IsDirectory || cur.Children == null || !cur.Children.TryGetValue(part, out var next))
					throw new DirectoryNotFoundException(path);
				stack.Add(cur);
				names.Add(part);
				cur = next;
			}
			if (!cur.IsDirectory)
				throw new IOException("Path is a file.");
			if (!recursive && cur.Children != null && cur.Children.Count > 0)
				throw new IOException("Directory is not empty.");

			// Calculate total size of all files in the directory tree
			_totalSize -= CalculateTreeSize(cur);

			// remove from parent
			var parent = stack[stack.Count - 1];
			parent.Children.Remove(names[names.Count - 1]);
		}
	}

	private static long CalculateTreeSize(Node node)
	{
		if (!node.IsDirectory)
			return node.Length;

		long total = 0;
		if (node.Children != null)
		{
			foreach (var child in node.Children.Values)
				total += CalculateTreeSize(child);
		}
		return total;
	}

	/// <inheritdoc />
	public void DeleteFile(string path)
	{
		using (_lock.EnterScope())
		{
			var parts = Split(path);
			if (parts.Length == 0)
				throw new IOException("Invalid file path.");
			var dir = _root;
			for (int i = 0; i < parts.Length - 1; i++)
			{
				if (!dir.IsDirectory || dir.Children == null || !dir.Children.TryGetValue(parts[i], out var next))
					return; // nothing to delete
				dir = next;
			}

			var fileName = parts[parts.Length - 1];
			if (dir.Children != null && dir.Children.TryGetValue(fileName, out var fileNode))
			{
				if (fileNode.Attributes.HasFlag(FileAttributes.ReadOnly))
					throw new UnauthorizedAccessException("Access to the path is denied.");

				if (!CanDeleteWithExistingHandles(fileNode))
					throw new IOException("The process cannot access the file because it is being used by another process.");

				_totalSize -= fileNode.Length;
				dir.Children.Remove(fileName);
			}
		}
	}

	/// <inheritdoc />
	public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		using (_lock.EnterScope())
		{
			var sourceNode = GetNode(sourceFileName);
			if (sourceNode == null || sourceNode.IsDirectory)
				throw new FileNotFoundException(sourceFileName);
			if (!overwrite && FileExists(destFileName))
				throw new IOException("Destination file exists.");

			// Check if source file can be deleted (moved)
			if (!CanDeleteWithExistingHandles(sourceNode))
				throw new IOException("The process cannot access the file because it is being used by another process.");

			// Delete destination first if overwriting
			if (overwrite && FileExists(destFileName))
				DeleteFile(destFileName);

			// Create destination file with source data
			var destDir = Traverse(destFileName, createDirs: true, forFile: true, out var destName);
			if (!destDir.IsDirectory)
				throw new IOException("Path's parent is not a directory.");
			destDir.Children ??= new(StringComparer.OrdinalIgnoreCase);
			destDir.Children[destName] = new Node
			{
				IsDirectory = false,
				Data = sourceNode.Data,
				Attributes = sourceNode.Attributes,
				CreatedUtc = sourceNode.CreatedUtc,
				UpdatedUtc = DateTime.UtcNow
			};

			// Remove source
			var sourceParts = Split(sourceFileName);
			var sourceDir = _root;
			for (int i = 0; i < sourceParts.Length - 1; i++)
				sourceDir = sourceDir.Children[sourceParts[i]];
			sourceDir.Children.Remove(sourceParts[sourceParts.Length - 1]);
		}
	}

	/// <inheritdoc />
	public void MoveDirectory(string sourceDirName, string destDirName)
	{
		using (_lock.EnterScope())
		{
			var sourceParts = Split(sourceDirName);
			var destParts = Split(destDirName);

			if (sourceParts.Length == 0)
				throw new IOException("Cannot move root directory.");

			// Find source node and its parent
			var sourceParent = _root;
			for (int i = 0; i < sourceParts.Length - 1; i++)
			{
				if (!sourceParent.IsDirectory || sourceParent.Children == null || !sourceParent.Children.TryGetValue(sourceParts[i], out var next))
					throw new DirectoryNotFoundException(sourceDirName);
				sourceParent = next;
			}

			var sourceNodeName = sourceParts[sourceParts.Length - 1];
			if (sourceParent.Children == null || !sourceParent.Children.TryGetValue(sourceNodeName, out var sourceNode))
				throw new DirectoryNotFoundException(sourceDirName);

			if (!sourceNode.IsDirectory)
				throw new IOException("Source path is not a directory.");

			// Create destination parent directories
			var destParent = Traverse(destDirName, createDirs: true, forFile: true, out var destNodeName);
			if (destParent == null || !destParent.IsDirectory)
				throw new IOException("Cannot create destination directory.");

			destParent.Children ??= new(StringComparer.OrdinalIgnoreCase);

			if (destParent.Children.ContainsKey(destNodeName))
				throw new IOException("Destination directory already exists.");

			// Move the node
			destParent.Children[destNodeName] = sourceNode;
			sourceParent.Children.Remove(sourceNodeName);
		}
	}

	/// <inheritdoc />
	public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		using (_lock.EnterScope())
		{
			if (!FileExists(sourceFileName))
				throw new FileNotFoundException(sourceFileName);
			if (!overwrite && FileExists(destFileName))
				throw new IOException("Destination file exists.");

			// Delete destination first if overwriting
			if (overwrite && FileExists(destFileName))
				DeleteFile(destFileName);

			using var src = this.OpenRead(sourceFileName);
			using var dst = this.OpenWrite(destFileName, append: false);
			src.CopyTo(dst);
		}
	}

	/// <inheritdoc />
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path) ?? throw new DirectoryNotFoundException(path);
			if (!node.IsDirectory)
				throw new IOException("Path is not a directory.");
			IEnumerable<string> Enumerate(Node n, string prefix)
			{
				if (n.Children == null)
					yield break;
				foreach (var kv in n.Children)
				{
					var name = Path.Combine(prefix, kv.Key);
					if (!kv.Value.IsDirectory)
						yield return name;
					if (searchOption == SearchOption.AllDirectories)
					{
						foreach (var inner in Enumerate(kv.Value, name))
							yield return inner;
					}
				}
			}
			return [.. Enumerate(node, Normalize(path)).Where(p => Matches(Path.GetFileName(p), searchPattern))];
		}
	}

	/// <inheritdoc />
	public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path) ?? throw new DirectoryNotFoundException(path);
			if (!node.IsDirectory)
				throw new IOException("Path is not a directory.");
			IEnumerable<string> Enumerate(Node n, string prefix)
			{
				if (n.Children == null)
					yield break;
				foreach (var kv in n.Children)
				{
					var name = Path.Combine(prefix, kv.Key);
					if (kv.Value.IsDirectory)
						yield return name;
					if (searchOption == SearchOption.AllDirectories)
					{
						foreach (var inner in Enumerate(kv.Value, name))
							yield return inner;
					}
				}
			}
			return [.. Enumerate(node, Normalize(path)).Where(p => Matches(Path.GetFileName(p), searchPattern))];
		}
	}

	/// <inheritdoc />
	public DateTime GetCreationTimeUtc(string path)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);
			return node.CreatedUtc;
		}
	}

	/// <inheritdoc />
	public DateTime GetLastWriteTimeUtc(string path)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);
			return node.UpdatedUtc;
		}
	}

	/// <inheritdoc />
	public long GetFileLength(string path)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);
			if (node.IsDirectory)
				throw new IOException("Path is a directory.");
			return node.Length;
		}
	}

	/// <inheritdoc />
	public void SetReadOnly(string path, bool isReadOnly)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path);

			// As per interface contract: missing file should be handled gracefully.
			if (node == null)
				return;

			if (isReadOnly)
			{
				node.Attributes |= FileAttributes.ReadOnly;
				node.Attributes &= ~FileAttributes.Normal;
			}
			else
			{
				node.Attributes &= ~FileAttributes.ReadOnly;

				// If it's a file and no other flags are set, keep it as Normal.
				if (!node.IsDirectory)
				{
					var otherFlags = node.Attributes & ~(FileAttributes.Normal | FileAttributes.Directory);
					if (otherFlags == 0)
						node.Attributes = FileAttributes.Normal;
				}
			}

			node.UpdatedUtc = DateTime.UtcNow;
		}
	}

	/// <inheritdoc />
	public FileAttributes GetAttributes(string path)
	{
		using (_lock.EnterScope())
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);

			var attrs = node.Attributes;

			if (node.IsDirectory)
			{
				attrs |= FileAttributes.Directory;
				attrs &= ~FileAttributes.Normal; // "Normal" is not used for directories
				return attrs;
			}

			attrs &= ~FileAttributes.Directory;

			// If any flag other than Normal is present, "Normal" must not be combined with it.
			var otherThanNormal = attrs & ~FileAttributes.Normal;
			if (otherThanNormal != 0)
				attrs &= ~FileAttributes.Normal;

			// If nothing left, treat as Normal.
			if (attrs == 0)
				attrs = FileAttributes.Normal;

			return attrs;
		}
	}

	private static bool Matches(string name, string pattern)
	{
		// Simple wildcard match: * and ? only
		pattern ??= "*";
		if (pattern == "*" || string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase))
			return true;
		static string RegexEscape(string s) => Regex.Escape(s).Replace("\\*", ".*").Replace("\\?", ".");
		var re = "^" + RegexEscape(pattern) + "$";
		return Regex.IsMatch(name, re, RegexOptions.IgnoreCase);
	}

	private static string Normalize(string path)
		=> (path ?? string.Empty).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
}