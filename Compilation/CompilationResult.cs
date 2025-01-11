namespace Ecng.Compilation;

using System.Collections.Generic;
using System.Linq;

public abstract class CompilationResult(IEnumerable<CompilationError> errors)
{
	public IEnumerable<CompilationError> Errors { get; } = errors.ToArray();
}

public class AssemblyCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	public byte[] Assembly { get; set; }
}