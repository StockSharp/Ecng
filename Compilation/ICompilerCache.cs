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
/// An in-memory implementation of <see cref="ICompilerCache"/> that stores compiled assemblies in memory.
/// </summary>
public class InMemoryCompilerCache : ICompilerCache
{
	private readonly SynchronizedDictionary<string, (DateTime till, byte[] assembly)> _cache = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryCompilerCache"/> class with a specified timeout.
	/// </summary>
	/// <param name="timeout">Cache timeout.</param>
	public InMemoryCompilerCache(TimeSpan timeout)
	{
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
		=> Set(GetKey(ext, sources, refs), assembly);

	/// <inheritdoc />
	public virtual bool Remove(string ext, IEnumerable<string> sources, IEnumerable<string> refs)
		=> Remove(GetKey(ext, sources, refs));

	/// <inheritdoc />
	public virtual bool TryGet(string ext, IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly)
		=> TryGet(GetKey(ext, sources, refs), out assembly);

	/// <inheritdoc />
	public virtual void Clear() => _cache.Clear();

	/// <inheritdoc />
	public virtual void Init() { }
}

/// <summary>
/// A file-based implementation of <see cref="InMemoryCompilerCache"/> that persists cached assemblies to disk.
/// </summary>
public class FileCompilerCache(string path, TimeSpan timeout) : InMemoryCompilerCache(timeout)
{
	private readonly string _path = path.IsEmpty(Directory.GetCurrentDirectory());

	/// <summary>
	/// Initializes the file-based cache by ensuring the directory exists and loading valid cached files.
	/// </summary>
	public override void Init()
	{
		base.Init();

		Directory.CreateDirectory(_path);

		var till = DateTime.UtcNow;

		foreach (var fileName in Directory.GetFiles(_path, $"*{FileExts.Bin}"))
		{
			if ((till - File.GetLastWriteTimeUtc(fileName)) > Timeout)
			{
				File.Delete(fileName);
				continue;
			}

			Set(Path.GetFileNameWithoutExtension(fileName), File.ReadAllBytes(fileName));
		}
	}

	/// <summary>
	/// Generates the full path for a cache file based on the key.
	/// </summary>
	/// <param name="key">The cache key.</param>
	/// <returns>The full file path corresponding to the key.</returns>
	private string GetFileName(string key)
		=> Path.Combine(_path, $"{key}{FileExts.Bin}");

	/// <summary>
	/// Adds a compiled assembly to the file-based cache and persists it on disk.
	/// </summary>
	/// <param name="ext">The file extension used in key generation.</param>
	/// <param name="sources">The source code files.</param>
	/// <param name="refs">The referenced assemblies.</param>
	/// <param name="assembly">The compiled assembly bytes to cache.</param>
	/// <exception cref="ArgumentNullException">Thrown when assembly is null.</exception>
	public override void Add(string ext, IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly)
	{
		if (assembly is null)
			throw new ArgumentNullException(nameof(assembly));

		var key = GetKey(ext, sources, refs);
		var fileName = GetFileName(key);

		File.WriteAllBytes(fileName, assembly);
		Set(key, assembly);
	}

	/// <summary>
	/// Removes a cached assembly from both the file system and the in-memory cache.
	/// </summary>
	/// <param name="ext">The file extension used in key generation.</param>
	/// <param name="sources">The source code files.</param>
	/// <param name="refs">The referenced assemblies.</param>
	/// <returns>true if the assembly was removed; otherwise, false.</returns>
	public override bool Remove(string ext, IEnumerable<string> sources, IEnumerable<string> refs)
	{
		var key = GetKey(ext, sources, refs);
		var fileName = GetFileName(key);

		if (File.Exists(fileName))
			File.Delete(fileName);

		return Remove(key);
	}

	/// <summary>
	/// Tries to retrieve a cached assembly from the in-memory cache or from disk if not present in memory.
	/// </summary>
	/// <param name="ext">The file extension used in key generation.</param>
	/// <param name="sources">The source code files.</param>
	/// <param name="refs">The referenced assemblies.</param>
	/// <param name="assembly">When this method returns, contains the cached assembly if found; otherwise, null.</param>
	/// <returns>true if the assembly was retrieved; otherwise, false.</returns>
	public override bool TryGet(string ext, IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly)
	{
		var key = GetKey(ext, sources, refs);

		if (TryGet(key, out assembly))
			return true;

		var fileName = GetFileName(key);

		if (!File.Exists(fileName))
			return false;

		assembly = File.ReadAllBytes(fileName);
		Set(key, assembly);
		return true;
	}

	/// <summary>
	/// Clears the in-memory cache and deletes all cached files from disk.
	/// </summary>
	public override void Clear()
	{
		base.Clear();

		if (Directory.Exists(_path))
			Directory.Delete(_path, true);
	}
}