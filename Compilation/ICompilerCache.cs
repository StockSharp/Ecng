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
	private readonly SynchronizedDictionary<string, (DateTime till, byte[] assembly)> _cache = [];

	public InMemoryCompilerCache(TimeSpan timeout)
    {
		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Must be positive");

		Timeout = timeout;
	}

	public TimeSpan Timeout { get; }

	protected DateTime GetTill()
	{
		if (Timeout == TimeSpan.MaxValue)
			return DateTime.MaxValue;

		return DateTime.UtcNow + Timeout;
	}

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
		=> _cache[key] = (GetTill(), assembly ?? throw new ArgumentNullException(nameof(assembly)));

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

	public virtual void Add(IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly)
		=> Set(GetKey(sources, refs), assembly);

	public virtual bool Remove(IEnumerable<string> sources, IEnumerable<string> refs)
		=> Remove(GetKey(sources, refs));

	public virtual bool TryGet(IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly)
		=> TryGet(GetKey(sources, refs), out assembly);

	public virtual void Clear() => _cache.Clear();
	public virtual void Init() { }
}

public class FileCompilerCache(string path, TimeSpan timeout) : InMemoryCompilerCache(timeout)
{
	private readonly string _path = path.IsEmpty(Directory.GetCurrentDirectory());
	private const string _ext = "dll";

	public override void Init()
	{
		base.Init();

		Directory.CreateDirectory(_path);

		var till = DateTime.UtcNow;

		foreach (var fileName in Directory.GetFiles(_path, $"*.{_ext}"))
		{
			if ((till - File.GetLastWriteTimeUtc(fileName)) > Timeout)
			{
				File.Delete(fileName);
				continue;
			}

			Set(Path.GetFileNameWithoutExtension(fileName), File.ReadAllBytes(fileName));
		}
	}

	private string GetFileName(string key)
		=> Path.Combine(_path, $"{key}.{_ext}");

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