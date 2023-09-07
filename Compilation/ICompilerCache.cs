namespace Ecng.Compilation;

using System;
using System.Collections.Generic;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Security;

public interface ICompilerCache
{
	bool TryGetBuild(IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly);
	void AddBuild(IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly);
	void Clear();
	int Count { get; }
}

public class InMemoryCompilerCache : ICompilerCache
{
	private readonly SynchronizedDictionary<string, byte[]> _cache = new();

	protected static string GetKey(IEnumerable<string> sources, IEnumerable<string> refs)
	{
		if (sources is null)	throw new ArgumentNullException(nameof(sources));
		if (refs is null)		throw new ArgumentNullException(nameof(refs));

		return (sources.JoinN() + refs.JoinN()).UTF8().Sha512();
	}

	public virtual void AddBuild(IEnumerable<string> sources, IEnumerable<string> refs, byte[] assembly)
		=> _cache[GetKey(sources, refs)] = assembly;

	public virtual bool TryGetBuild(IEnumerable<string> sources, IEnumerable<string> refs, out byte[] assembly)
		=> _cache.TryGetValue(GetKey(sources, refs), out assembly);

	public virtual int Count => _cache.Count;

	public virtual void Clear() => _cache.Clear();
}