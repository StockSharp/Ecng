namespace Ecng.Compilation;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

/// <summary>
/// Provides extension methods for AssemblyLoadContext related to compilation operations.
/// </summary>
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

	/// <summary>
	/// Loads an assembly from the provided binary data using a memory stream.
	/// </summary>
	/// <param name="visitor">The AssemblyLoadContext instance to load the assembly.</param>
	/// <param name="assembly">The byte array containing the assembly binary.</param>
	/// <returns>The loaded Assembly.</returns>
	public static Assembly LoadFromBinary(this AssemblyLoadContext visitor, byte[] assembly)
		=> visitor.CheckOnNull(nameof(visitor)).LoadFromStream(assembly.To<Stream>());

	/// <summary>
	/// Wraps the specified AssemblyLoadContext into an ICompilerContext so that the context can be used for loading assemblies.
	/// </summary>
	/// <param name="context">The AssemblyLoadContext instance to wrap.</param>
	/// <returns>An ICompilerContext that uses the specified AssemblyLoadContext.</returns>
	public static ICompilerContext ToContext(this AssemblyLoadContext context)
		=> new AssemblyLoadContextWrapper(context);
}