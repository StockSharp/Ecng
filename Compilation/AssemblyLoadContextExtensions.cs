namespace Ecng.Compilation;

using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

public static class AssemblyLoadContextExtensions
{
	private class AssemblyLoadContextWrapper(AssemblyLoadContext context) : ICompilerContext
	{
		private readonly AssemblyLoadContext _context = context ?? throw new System.ArgumentNullException(nameof(context));

		Assembly ICompilerContext.LoadFromBinary(byte[] body)
			=> _context.LoadFromBinary(body);
	}

	public static Assembly LoadFromBinary(this AssemblyLoadContext visitor, byte[] assembly)
		=> visitor.CheckOnNull(nameof(visitor)).LoadFromStream(assembly.To<Stream>());

	public static ICompilerContext ToContext(this AssemblyLoadContext context)
		=> new AssemblyLoadContextWrapper(context);
}