namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ecng.Common;

public abstract class CompilationResult(IEnumerable<CompilationError> errors)
{
	public IEnumerable<CompilationError> Errors { get; } = errors.ToArray();

	public abstract Assembly GetAssembly(object context);
}

public class AssemblyCompilationResult(IEnumerable<CompilationError> errors, byte[] assemblyBody = null)
	: CompilationResult(errors)
{
	public byte[] AssemblyBody { get; } = assemblyBody;

	public override Assembly GetAssembly(object context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var asm = AssemblyBody;

		if (asm is null)
			return null;

		if (context is AssemblyLoadContextTracker tracker)
			return tracker.LoadFromStream(asm);
		else if (context is System.Runtime.Loader.AssemblyLoadContext alc)
			return alc.LoadFromStream(asm);
		else
			throw new NotSupportedException(context.To<string>());
	}
}