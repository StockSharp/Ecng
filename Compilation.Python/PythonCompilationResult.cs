namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using Microsoft.Scripting.Hosting;

using IronPython.Runtime.Types;

class PythonCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	private class AssemblyImpl(CompiledCode compiledCode) : IAssembly
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

			private ScriptEngine Engine => _code.Engine;
			private ObjectOperations Ops => Engine.Operations;

			bool IType.IsAbstract => false;
			bool IType.IsPublic => true;
			bool IType.IsGenericTypeDefinition => false;
			object IType.GetConstructor(IType[] value) => new object();

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

		private readonly CompiledCode _compiledCode = compiledCode ?? throw new ArgumentNullException(nameof(compiledCode));
		private readonly SynchronizedSet<ScriptScope> _execScopes = [];

		byte[] IAssembly.AsBytes => throw new NotSupportedException();

		IEnumerable<IType> IAssembly.GetExportTypes(object context)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			var scope = (ScriptScope)context;

			if (_execScopes.TryAdd(scope))
				_compiledCode.Execute(scope);

			return scope.GetTypes().Select(t => new TypeImpl(_compiledCode, t)).ToArray();
		}
	}

	public CompiledCode CompiledCode { get; set; }

	private IAssembly _assembly;
	public override IAssembly Assembly => CompiledCode is null ? null : _assembly ??= new AssemblyImpl(CompiledCode);
}