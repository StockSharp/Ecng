namespace Ecng.Compilation;

using System.Collections.Generic;
using System.Linq;

public abstract class CompilationResult(IEnumerable<CompilationError> errors)
{
	public IEnumerable<CompilationError> Errors { get; } = errors.ToArray();

	public abstract IAssembly Assembly { get; }
}

public class AssemblyCompilationResult(IEnumerable<CompilationError> errors, byte[] assemblyBody = null)
	: CompilationResult(errors)
{
	public byte[] AssemblyBody { get; } = assemblyBody;

	private IAssembly _assembly;
	public override IAssembly Assembly => AssemblyBody is null ? null : _assembly ??= new AssemblyImpl(AssemblyBody);
}