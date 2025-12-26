namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;

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
		public long Length => Data?.LongLength ?? 0L;
	}

	private readonly Node _root = new() { IsDirectory = true, Children = new(StringComparer.OrdinalIgnoreCase), CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
	private readonly Lock _lock = new();

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
				next = new Node { IsDirectory = true, Children = new(StringComparer.OrdinalIgnoreCase), CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
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
		// Note: FileShare is accepted for API compatibility but not enforced in memory implementation
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
				return new MemoryStream(node.Data ?? [], false);
			}

			var append = mode == FileMode.Append;
			var truncate = mode is FileMode.Create or FileMode.CreateNew or FileMode.Truncate;

			var dir = Traverse(path, createDirs: true, forFile: true, out var fileName);
			if (!dir.IsDirectory)
				throw new IOException("Path's parent is not a directory.");
			dir.Children ??= new(StringComparer.OrdinalIgnoreCase);
			if (!dir.Children.TryGetValue(fileName, out var fileNode))
			{
				fileNode = new Node { IsDirectory = false, Data = [], CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
				dir.Children[fileName] = fileNode;
			}
			if (fileNode.IsDirectory)
				throw new IOException("Path points to a directory.");

			var baseData = truncate ? [] : (fileNode.Data ?? []);
			var ms = new MemoryStream();
			if (baseData.Length > 0)
				ms.Write(baseData, 0, baseData.Length);

			var appendStart = append ? ms.Length : -1L;
			if (append)
				ms.Seek(0, SeekOrigin.End);

			return new CommittingStream(ms, bytes =>
			{
				fileNode.Data = bytes;
				fileNode.UpdatedUtc = DateTime.UtcNow;
			}, access, appendStart);
		}
	}

	private class CommittingStream(MemoryStream inner, Action<byte[]> commit, FileAccess access, long appendStart) : Stream
	{
		private bool _disposed;

		protected override void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				_disposed = true;
				commit(inner.ToArray());
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
			// remove from parent
			var parent = stack[stack.Count - 1];
			parent.Children.Remove(names[names.Count - 1]);
		}
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
			dir.Children?.Remove(parts[parts.Length - 1]);
		}
	}

	/// <inheritdoc />
	public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		using (_lock.EnterScope())
		{
			if (!FileExists(sourceFileName))
				throw new FileNotFoundException(sourceFileName);
			if (!overwrite && FileExists(destFileName))
				throw new IOException("Destination file exists.");

			// Delete destination first if overwriting, then copy source content
			if (overwrite && FileExists(destFileName))
				DeleteFile(destFileName);

			using (var src = this.OpenRead(sourceFileName))
			using (var dst = this.OpenWrite(destFileName, append: false))
			{
				src.CopyTo(dst);
			}
			DeleteFile(sourceFileName);
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