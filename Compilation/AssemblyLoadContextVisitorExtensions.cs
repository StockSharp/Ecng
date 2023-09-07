namespace Ecng.Compilation;

using System.IO;
using System.Reflection;

using Ecng.Common;

public static class AssemblyLoadContextVisitorExtensions
{
	public static Assembly LoadFromStream(this AssemblyLoadContextVisitor visitor, byte[] assembly)
		=> visitor.CheckOnNull(nameof(visitor)).LoadFromStream(assembly.To<Stream>());
}