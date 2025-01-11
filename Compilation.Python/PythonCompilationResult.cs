namespace Ecng.Compilation.Python;

using System.Collections.Generic;

using Microsoft.Scripting.Hosting;

public class PythonCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	public ScriptScope Scope { get; set; }
}