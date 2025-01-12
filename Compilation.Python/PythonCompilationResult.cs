namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Linq;

using IronPython.Runtime.Types;

using Microsoft.Scripting.Hosting;

public class PythonCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	private class TypeImpl(CompiledCode code, PythonType pythonType) : IType
	{
		private readonly CompiledCode _code = code ?? throw new ArgumentNullException(nameof(code));
		private readonly PythonType _pythonType = pythonType ?? throw new ArgumentNullException(nameof(pythonType));
		public object Native => _pythonType;
		public string Name => _pythonType.GetName();
		public Type DotNet => _pythonType.GetUnderlyingSystemType();

		public object CreateInstance(params object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			return _code.Engine.Operations.Invoke(_pythonType, args);
		}

		public bool Is(Type type) => _pythonType.Is(type);
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

		return scope.GetTypes().Select(t => new TypeImpl(code, t)).ToArray();
	}
}