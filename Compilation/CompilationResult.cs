namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Represents the result of a compilation process including any compilation errors.
/// </summary>
/// <param name="errors">A collection of compilation errors.</param>
public abstract class CompilationResult(IEnumerable<CompilationError> errors)
{
	/// <summary>
	/// Gets the collection of compilation errors.
	/// </summary>
	public IEnumerable<CompilationError> Errors { get; } = [.. errors];

	/// <summary>
	/// Loads the compiled assembly using the provided compiler context.
	/// </summary>
	/// <param name="context">The compiler context used to load the assembly.</param>
	/// <returns>The loaded assembly, or null if the assembly body is not available.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
	public abstract Assembly GetAssembly(ICompilerContext context);
}

/// <summary>
/// Represents a compilation result that includes a binary representation of an assembly.
/// </summary>
/// <param name="errors">A collection of compilation errors.</param>
/// <param name="assemblyBody">The binary content of the compiled assembly.</param>
public class AssemblyCompilationResult(IEnumerable<CompilationError> errors, byte[] assemblyBody = null)
	: CompilationResult(errors)
{
	/// <summary>
	/// Gets the binary content of the compiled assembly.
	/// </summary>
	public byte[] AssemblyBody { get; } = assemblyBody;

	/// <inheritdoc />
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