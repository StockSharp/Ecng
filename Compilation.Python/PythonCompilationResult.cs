namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Scripting.Hosting;

class PythonCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	public CompiledCode CompiledCode { get; set; }

	public override Assembly GetAssembly(ICompilerContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var code = CompiledCode;

		if (code is null)
			return null;

		var pythonContext = (PythonContext)context;
		return pythonContext.LoadFromCode(code);
	}
}