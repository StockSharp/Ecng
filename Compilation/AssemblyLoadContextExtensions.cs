namespace Ecng.Compilation;

using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

public static class AssemblyLoadContextExtensions
{
	public static Assembly LoadFromStream(this AssemblyLoadContext visitor, byte[] assembly)
		=> visitor.CheckOnNull(nameof(visitor)).LoadFromStream(assembly.To<Stream>());
}