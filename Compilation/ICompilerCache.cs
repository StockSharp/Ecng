namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.IO;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Security;

/// <summary>
/// Defines methods for caching compiled assemblies.
/// </summary>
public interface ICompilerCache
{
	/// <summary>
	/// Gets the number of cached assemblies.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Tries to retrieve a cached assembly based on extension, sources, and references.
	/// </summary>
	/// <param name="ext">The file extension used in key generation.</param>
	/// <param name="sources">The source code files.</param>
	/// <param name="refs">The referenced assemblies.</param>
	/// <param name="assembly">When this method returns, contains the cached assembly if found; otherwise, null.</param>
	/// <returns>true if the assembly was retrieved from the cache; otherwise, false.</returns>
	bool TryGet(string ext, IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly);

	/// <summary>
	/// Adds a compiled assembly to the cache.
	/// </summary>
	/// <param name="ext">The file extension used in key generation.</param>
	/// <param name="sources">The source code files.</param>
	/// <param name="refs">The referenced assemblies.</param>
	/// <param name="assembly">The compiled assembly bytes to cache.</param>
	void Add(string ext, IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly);

	/// <summary>
	/// Removes a cached assembly based on extension, sources, and references.
	/// </summary>
	/// <param name="ext">The file extension used in key generation.</param>
	/// <param name="sources">The source code files.</param>
	/// <param name="refs">The referenced assemblies.</param>
	/// <returns>true if the assembly was removed; otherwise, false.</returns>
	bool Remove(string ext, IEnumerable<string> sources, IEnumerable<string> refs);

	/// <summary>
	/// Clears all cached assemblies.
	/// </summary>
	void Clear();

	/// <summary>
	/// Initializes the cache.
	/// </summary>
	void Init();
}

/// <summary>
/// A unified implementation of <see cref="ICompilerCache"/> that stores compiled assemblies using an injected file system.
/// </summary>
public class CompilerCache : ICompilerCache
{
	private readonly SynchronizedDictionary<string, (DateTime till, byte[] assembly)> _cache = [];
	private readonly IFileSystem _fileSystem;
	private readonly string _path;

	/// <summary>
	/// Initializes a new instance of the <see cref="CompilerCache"/> class with a specified timeout.
	/// </summary>
	/// <param name="fileSystem">The file system to use for storing cached assemblies.</param>
	/// <param name="path">The path where cached assemblies will be stored.</param>
	/// <param name="timeout">Cache timeout.</param>
	public CompilerCache(IFileSystem fileSystem, string path, TimeSpan timeout)
	{
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		_path = path.IsEmpty(Directory.GetCurrentDirectory());

		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Must be positive");

		Timeout = timeout;
	}

	/// <summary>
	/// Gets the cache timeout.
	/// </summary>
	public TimeSpan Timeout { get; }

	/// <summary>
	/// Gets the expiration time for a cache entry.
	/// </summary>
	/// <returns>The expiration time for a cache entry.</returns>
	protected DateTime GetTill()
	{
		if (Timeout == TimeSpan.MaxValue)
			return DateTime.MaxValue;

		return DateTime.UtcNow + Timeout;
	}

	/// <inheritdoc />
	public virtual int Count => _cache.Count;

	/// <summary>
	/// Generates a cache key based on the file extension, source code files, and referenced assemblies.
	/// </summary>
	/// <param name="ext">The file extension used in key generation.</param>
	/// <param name="sources">The source code files.</param>
	/// <param name="refs">The referenced assemblies.</param>
	/// <returns>The key.</returns>
	protected static string GetKey(string ext, IEnumerable<string> sources, IEnumerable<string> refs)
	{
		if (ext.IsEmpty())		throw new ArgumentNullException(nameof(ext));
		if (sources is null)	throw new ArgumentNullException(nameof(sources));
		if (refs is null)		throw new ArgumentNullException(nameof(refs));

		return $"{ext.Substring(1)}{(sources.JoinN() + refs.JoinN()).UTF8().Sha512()}";
	}

	/// <summary>
	/// Removes a cached assembly based on the key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <returns>Operation result.</returns>
	protected bool Remove(string key)
		=> _cache.Remove(key);

	/// <summary>
	/// Adds a compiled assembly to the in-memory cache.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="assembly">Compiled assembly.</param>
	protected void Set(string key, byte[] assembly)
		=> _cache[key] = (GetTill(), assembly ?? throw new ArgumentNullException(nameof(assembly)));

	/// <summary>
	/// Tries to retrieve a cached assembly based on the key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="assembly">Compiled assembly.</param>
	/// <returns>Operation result.</returns>
	protected bool TryGet(string key, out byte[] assembly)
	{
		if (_cache.TryGetValue(key, out var t) && t.till >= DateTime.UtcNow)
		{
			assembly = t.assembly;
			return true;
		}

		assembly = null;
		return false;
	}

	/// <inheritdoc />
	public virtual void Add(string ext, IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly)
	{
		var key = GetKey(ext, sources, refs);
		var fileName = GetFileName(key);

		using (var s = _fileSystem.OpenWrite(fileName))
			s.Write(assembly ?? throw new ArgumentNullException(nameof(assembly)), 0, assembly.Length);

		Set(key, assembly);
	}

	/// <inheritdoc />
	public virtual bool Remove(string ext, IEnumerable<string> sources, IEnumerable<string> refs)
	{
		var key = GetKey(ext, sources, refs);
		var fileName = GetFileName(key);

		if (_fileSystem.FileExists(fileName))
			_fileSystem.DeleteFile(fileName);

		return Remove(key);
	}

	/// <inheritdoc />
	public virtual bool TryGet(string ext, IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly)
	{
		var key = GetKey(ext, sources, refs);

		if (TryGet(key, out assembly))
			return true;

		var fileName = GetFileName(key);

		if (!_fileSystem.FileExists(fileName))
			return false;

		using (var stream = _fileSystem.OpenRead(fileName))
		using (var ms = new MemoryStream())
		{
			stream.CopyTo(ms);
			assembly = ms.ToArray();
		}

		Set(key, assembly);
		return true;
	}

	/// <inheritdoc />
	public virtual void Clear()
	{
		_cache.Clear();
		if (_fileSystem.DirectoryExists(_path))
			_fileSystem.DeleteDirectory(_path, true);
	}

	/// <inheritdoc />
	public virtual void Init()
	{
		_fileSystem.CreateDirectory(_path);

		var till = DateTime.UtcNow;

		foreach (var fileName in _fileSystem.EnumerateFiles(_path, "*.bin"))
		{
			if ((till - _fileSystem.GetLastWriteTimeUtc(fileName)) > Timeout)
			{
				_fileSystem.DeleteFile(fileName);
				continue;
			}

			var key = Path.GetFileNameWithoutExtension(fileName);

			using var stream = _fileSystem.OpenRead(fileName);
			using var ms = new MemoryStream();

			stream.CopyTo(ms);
			Set(key, ms.ToArray());
		}
	}

	private string GetFileName(string key)
		=> Path.Combine(_path, $"{key}.bin");
}

/// <summary>
/// Backward-compatible wrapper for legacy usage; uses local file system under the hood.
/// </summary>
[Obsolete("Use CompilerCache with LocalFileSystem instead.")]
public class FileCompilerCache(string path, TimeSpan timeout)
	: CompilerCache(new LocalFileSystem(), path, timeout)
{
}
