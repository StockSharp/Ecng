namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.IO;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Security;

public interface ICompilerCache
{
	int Count { get; }

	bool TryGet(IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly);
	void Add(IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly);
	bool Remove(IEnumerable<string> sources, IEnumerable<string> refs);
	void Clear();
	void Init();
}

public class InMemoryCompilerCache : ICompilerCache
{
	private readonly SynchronizedDictionary<string, byte[]> _cache = new();

	public virtual int Count => _cache.Count;

	protected static string GetKey(IEnumerable<string> sources, IEnumerable<string> refs)
	{
		if (sources is null)	throw new ArgumentNullException(nameof(sources));
		if (refs is null)		throw new ArgumentNullException(nameof(refs));

		return (sources.JoinN() + refs.JoinN()).UTF8().Sha512();
	}

	protected bool Remove(string key)
		=> _cache.Remove(key);

	protected void Set(string key, byte[] assembly)
		=> _cache[key] = assembly ?? throw new ArgumentNullException(nameof(assembly));

	protected bool TryGet(string key, out byte[] assembly)
		=> _cache.TryGetValue(key, out assembly);

	public virtual void Add(IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly)
		=> Set(GetKey(sources, refs), assembly);

	public virtual bool Remove(IEnumerable<string> sources, IEnumerable<string> refs)
		=> Remove(GetKey(sources, refs));

	public virtual bool TryGet(IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly)
		=> TryGet(GetKey(sources, refs), out assembly);

	public virtual void Clear() => _cache.Clear();
	public virtual void Init() { }
}

public class FileCompilerCache : InMemoryCompilerCache
{
	private readonly string _path;

	public FileCompilerCache(string path)
    {
		_path = path.IsEmpty(Directory.GetCurrentDirectory());
	}

	public override void Init()
	{
		base.Init();

		Directory.CreateDirectory(_path);
	}

	private string GetFileName(string key)
		=> Path.Combine(_path, $"{key}.dll");

	public override void Add(IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly)
	{
		var key = GetKey(sources, refs);
		var fileName = GetFileName(key);

		File.WriteAllBytes(fileName, assembly);
		Set(key, assembly);
	}

	public override bool Remove(IEnumerable<string> sources, IEnumerable<string> refs)
	{
		var key = GetKey(sources, refs);
		var fileName = GetFileName(key);

		if (File.Exists(fileName))
			File.Delete(fileName);

		return Remove(key);
	}

	public override bool TryGet(IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly)
	{
		var key = GetKey(sources, refs);

		if (TryGet(key, out assembly))
			return true;

		var fileName = GetFileName(key);

		if (!File.Exists(fileName))
			return false;

		assembly = File.ReadAllBytes(fileName);
		Set(key, assembly);
		return true;
	}

	public override void Clear()
	{
		base.Clear();

		if (Directory.Exists(_path))
			Directory.Delete(_path, true);
	}
}