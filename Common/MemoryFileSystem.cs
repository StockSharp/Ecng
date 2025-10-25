namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public long Length => Data?.LongLength ??0L;
	}

	private readonly Node _root = new() { IsDirectory = true, Children = new(StringComparer.OrdinalIgnoreCase), CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
	private readonly object _lock = new();

	private static string[] Split(string path)
	{
		path = path?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) ?? string.Empty;
		path = path.Trim();
		if (path.Length ==0 || path == Path.DirectorySeparatorChar.ToString())
			return [];
		return path.Split([Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
	}

	private Node Traverse(string path, bool createDirs, bool forFile, out string fileName)
	{
		fileName = null;
		var parts = Split(path);
		var cur = _root;
		for (int i =0; i < parts.Length; i++)
		{
			var part = parts[i];
			var isLast = i == parts.Length -1;
			if (isLast && forFile)
			{
				fileName = part;
				break;
			}
			if (!cur.IsDirectory) throw new IOException("Path segment is not a directory: " + part);
			cur.Children ??= new(StringComparer.OrdinalIgnoreCase);
			if (!cur.Children.TryGetValue(part, out var next))
			{
				if (!createDirs) return null;
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
			if (!cur.IsDirectory) return null;
			if (cur.Children == null || !cur.Children.TryGetValue(part, out var next)) return null;
			cur = next;
		}
		return cur;
	}

	/// <inheritdoc />
	public bool FileExists(string path)
	{
		lock (_lock)
		{
			var node = GetNode(path);
			return node != null && !node.IsDirectory;
		}
	}

	/// <inheritdoc />
	public bool DirectoryExists(string path)
	{
		lock (_lock)
		{
			var node = GetNode(path);
			return node != null && node.IsDirectory;
		}
	}

	/// <inheritdoc />
	public Stream OpenRead(string path)
	{
		lock (_lock)
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);
			if (node.IsDirectory) throw new UnauthorizedAccessException("Cannot open directory for reading.");
			return new MemoryStream(node.Data ?? [], false);
		}
	}

	/// <inheritdoc />
	public Stream OpenWrite(string path, bool append = false)
	{
		lock (_lock)
		{
			var dir = Traverse(path, createDirs: true, forFile: true, out var fileName);
			if (!dir.IsDirectory) throw new IOException("Path's parent is not a directory.");
			dir.Children ??= new(StringComparer.OrdinalIgnoreCase);
			if (!dir.Children.TryGetValue(fileName, out var fileNode))
			{
				fileNode = new Node { IsDirectory = false, Data = [], CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
				dir.Children[fileName] = fileNode;
			}
			if (fileNode.IsDirectory) throw new IOException("Path points to a directory.");

			var baseData = append && fileNode.Data != null ? fileNode.Data : [];
			var ms = new MemoryStream();
			if (baseData.Length >0) ms.Write(baseData,0, baseData.Length);
			return new CommittingStream(ms, bytes =>
			{
				fileNode.Data = bytes;
				fileNode.UpdatedUtc = DateTime.UtcNow;
			});
		}
	}

	private class CommittingStream(MemoryStream inner, Action<byte[]> commit) : Stream
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
		public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
		public override void SetLength(long value) => inner.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
		public override bool CanRead => inner.CanRead;
		public override bool CanSeek => inner.CanSeek;
		public override bool CanWrite => inner.CanWrite;
		public override long Length => inner.Length;
		public override long Position { get => inner.Position; set => inner.Position = value; }
	}

	/// <inheritdoc />
	public void CreateDirectory(string path)
	{
		lock (_lock)
		{
			var node = Traverse(path, createDirs: true, forFile: false, out _);
			if (node != null && !node.IsDirectory) throw new IOException("Path exists and is not a directory.");
		}
	}

	/// <inheritdoc />
	public void DeleteDirectory(string path, bool recursive = false)
	{
		lock (_lock)
		{
			if (string.IsNullOrEmpty(path) || path == Path.DirectorySeparatorChar.ToString()) throw new IOException("Cannot delete root.");
			var parts = Split(path);
			var stack = new List<Node>();
			var names = new List<string>();
			var cur = _root;
			foreach (var part in parts)
			{
				if (!cur.IsDirectory || cur.Children == null || !cur.Children.TryGetValue(part, out var next)) throw new DirectoryNotFoundException(path);
				stack.Add(cur); names.Add(part); cur = next;
			}
			if (!cur.IsDirectory) throw new IOException("Path is a file.");
			if (!recursive && cur.Children != null && cur.Children.Count >0) throw new IOException("Directory is not empty.");
			// remove from parent
			var parent = stack[stack.Count -1];
			parent.Children.Remove(names[names.Count -1]);
		}
	}

	/// <inheritdoc />
	public void DeleteFile(string path)
	{
		lock (_lock)
		{
			var parts = Split(path);
			if (parts.Length ==0) throw new IOException("Invalid file path.");
			var dir = _root;
			for (int i =0; i < parts.Length -1; i++)
			{
				if (!dir.IsDirectory || dir.Children == null || !dir.Children.TryGetValue(parts[i], out var next)) return; // nothing to delete
				dir = next;
			}
			dir.Children?.Remove(parts[parts.Length -1]);
		}
	}

	/// <inheritdoc />
	public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		lock (_lock)
		{
			if (!FileExists(sourceFileName)) throw new FileNotFoundException(sourceFileName);
			if (!overwrite && FileExists(destFileName)) throw new IOException("Destination file exists.");
			using (var src = OpenRead(sourceFileName))
			using (var dst = OpenWrite(destFileName, overwrite))
			{
				src.CopyTo(dst);
			}
			DeleteFile(sourceFileName);
		}
	}

	/// <inheritdoc />
	public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		lock (_lock)
		{
			if (!FileExists(sourceFileName)) throw new FileNotFoundException(sourceFileName);
			if (!overwrite && FileExists(destFileName)) throw new IOException("Destination file exists.");
			using var src = OpenRead(sourceFileName);
			using var dst = OpenWrite(destFileName, overwrite);
			src.CopyTo(dst);
		}
	}

	/// <inheritdoc />
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		lock (_lock)
		{
			var node = GetNode(path) ?? throw new DirectoryNotFoundException(path);
			if (!node.IsDirectory) throw new IOException("Path is not a directory.");
			IEnumerable<string> Enumerate(Node n, string prefix)
			{
				if (n.Children == null) yield break;
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
		lock (_lock)
		{
			var node = GetNode(path) ?? throw new DirectoryNotFoundException(path);
			if (!node.IsDirectory) throw new IOException("Path is not a directory.");
			IEnumerable<string> Enumerate(Node n, string prefix)
			{
				if (n.Children == null) yield break;
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
		lock (_lock)
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);
			return node.CreatedUtc;
		}
	}

	/// <inheritdoc />
	public DateTime GetLastWriteTimeUtc(string path)
	{
		lock (_lock)
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);
			return node.UpdatedUtc;
		}
	}

	/// <inheritdoc />
	public long GetFileLength(string path)
	{
		lock (_lock)
		{
			var node = GetNode(path) ?? throw new FileNotFoundException(path);
			if (node.IsDirectory) throw new IOException("Path is a directory.");
			return node.Length;
		}
	}

	private static bool Matches(string name, string pattern)
	{
		// Simple wildcard match: * and ? only
		pattern ??= "*";
		if (pattern == "*" || string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase)) return true;
		static string RegexEscape(string s) => Regex.Escape(s).Replace("\\*", ".*").Replace("\\?", ".");
		var re = "^" + RegexEscape(pattern) + "$";
		return Regex.IsMatch(name, re, RegexOptions.IgnoreCase);
	}

	private static string Normalize(string path)
		=> (path ?? string.Empty).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
}