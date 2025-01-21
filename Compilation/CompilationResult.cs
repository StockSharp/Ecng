namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public abstract class CompilationResult(IEnumerable<CompilationError> errors)
{
	public IEnumerable<CompilationError> Errors { get; } = errors.ToArray();

	public abstract Assembly GetAssembly(ICompilerContext context);
}

public class AssemblyCompilationResult(IEnumerable<CompilationError> errors, byte[] assemblyBody = null)
	: CompilationResult(errors)
{
	public byte[] AssemblyBody { get; } = assemblyBody;

	public override Assembly GetAssembly(ICompilerContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var asm = AssemblyBody;

		if (asm is null)
			return null;

		return context.LoadFromBinary(asm);
	}
}