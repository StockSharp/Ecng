namespace Ecng.Compilation;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

public static class AssemblyLoadContextExtensions
{
	private class AssemblyLoadContextWrapper(AssemblyLoadContext context) : Disposable, ICompilerContext
	{
		private readonly AssemblyLoadContext _context = context ?? throw new ArgumentNullException(nameof(context));

		Assembly ICompilerContext.LoadFromBinary(byte[] body)
			=> _context.LoadFromBinary(body);

		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			_context.Unload();
		}
	}

	public static Assembly LoadFromBinary(this AssemblyLoadContext visitor, byte[] assembly)
		=> visitor.CheckOnNull(nameof(visitor)).LoadFromStream(assembly.To<Stream>());

	public static ICompilerContext ToContext(this AssemblyLoadContext context)
		=> new AssemblyLoadContextWrapper(context);
}