namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Linq;

using IronPython.Runtime.Types;

using Microsoft.Scripting.Hosting;

public class PythonCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	private class TypeImpl(PythonType pythonType) : IType
	{
		private readonly PythonType _pythonType = pythonType ?? throw new ArgumentNullException(nameof(pythonType));
		public object Native => _pythonType;
		public string Name => _pythonType.GetName();
		public Type DotNet => _pythonType.GetUnderlyingSystemType();
	}

	private bool _executed;

	public CompiledCode CompiledCode { get; set; }

	public override IEnumerable<IType> GetExportTypes(object context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var code = CompiledCode ?? throw new InvalidOperationException("Compiled code is not set.");

		var scope = (ScriptScope)context;

		if (!_executed)
		{
			code.Execute(scope);
			_executed = true;
		}

		return scope.GetTypes().Select(t => new TypeImpl(t)).ToArray();
	}
}