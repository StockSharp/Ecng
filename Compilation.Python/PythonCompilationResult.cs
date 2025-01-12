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

		string IType.Name => _pythonType.GetName();
		string IType.DisplayName => TryGetAttr("display_name");
		string IType.Description => TryGetAttr("description");
		string IType.DocUrl => TryGetAttr("__doc__");
		Uri IType.IconUri => TryGetAttr("icon") is string url ? new(url) : (Uri)default;

		//private dynamic AsDynamic => _pythonType;
		private ScriptEngine Engine => _code.Engine;
		private ObjectOperations Ops => Engine.Operations;

		private string TryGetAttr(string name)
			=> Ops.TryGetMember(_pythonType, name, out object value) ? value as string : null;

		object IType.CreateInstance(object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			return Ops.Invoke(_pythonType, args);
		}

		bool IType.Is(Type type) => _pythonType.Is(type);
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