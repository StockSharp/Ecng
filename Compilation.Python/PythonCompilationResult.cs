namespace Ecng.Compilation.Python;

class PythonCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	private Assembly _assembly;

	private CompiledCode _compiledCode;

	public CompiledCode CompiledCode
	{
		get => _compiledCode;
		set
		{
			if (_compiledCode == value)
				return;

			_compiledCode = value;
			_assembly = default;
		}
	}

	public override Assembly GetAssembly(ICompilerContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var code = CompiledCode;

		if (code is null)
			return null;

		if (_assembly is null)
		{
			var pythonContext = (PythonContext)context;

			// The engine is not thread-safe; serialise the one-time load under the context lock and
			// double-check so concurrent callers don't load the assembly twice.
			using (pythonContext.SyncRoot.EnterScope())
				_assembly ??= pythonContext.LoadFromCode(code);
		}

		return _assembly;
	}
}