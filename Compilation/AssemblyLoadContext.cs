#if NETSTANDARD2_0
namespace System.Runtime.Loader;

using System;
using System.IO;
using System.Reflection;

public class AssemblyLoadContext
{
	public AssemblyLoadContext()
		: this(default, false)
	{
	}

	public AssemblyLoadContext(string name, bool isCollectible)
	{
		throw new NotSupportedException();
	}

	public Assembly LoadFromStream(Stream stream)
		=> throw new NotSupportedException();

	public void Unload()
		=> throw new NotSupportedException();
}
#endif