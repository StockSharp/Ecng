namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

using Ecng.Common;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using IronPython.Hosting;
using IronPython.Compiler;

public class PythonCompiler : ICompiler
{
	private class CustomErrorListener(IList<CompilationError> errors) : ErrorListener
	{
		private readonly IList<CompilationError> _errors = errors ?? throw new ArgumentNullException(nameof(errors));

		public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
			=> _errors.Add(new()
			{
				Id = errorCode.ToString(),
				Line = span.Start.Line,
				Character = span.Start.Column,
				Message = message,
				Type = severity.ToErrorType(),
			});
	}

	private readonly ScriptEngine _engine;

	public PythonCompiler()
		: this(Python.CreateEngine())
	{
	}

	public PythonCompiler(ScriptEngine engine)
	{
		_engine = engine ?? throw new ArgumentNullException(nameof(engine));
	}

	bool ICompiler.IsAssembly { get; } = false;
	string ICompiler.Extension { get; } = FileExts.Python;

	Task<CompilationError[]> ICompiler.Analyse(
		object analyzer,
		IEnumerable<object> analyzerSettings,
		string name,
		IEnumerable<string> sources,
		IEnumerable<(string name, byte[] body)> refs,
		CancellationToken cancellationToken)
	{
		if (sources is null)	throw new ArgumentNullException(nameof(sources));
		if (refs is null)		throw new ArgumentNullException(nameof(refs));

		return Array.Empty<CompilationError>().FromResult();
	}

	Task<CompilationResult> ICompiler.Compile(
		string name,
		IEnumerable<string> sources,
		IEnumerable<(string name, byte[] body)> refs,
		CancellationToken cancellationToken)
	{
		if (sources is null)	throw new ArgumentNullException(nameof(sources));
		if (refs is null)		throw new ArgumentNullException(nameof(refs));

		PythonCompilationResult result;

		try
		{
			var source = _engine.CreateScriptSourceFromString(sources.JoinN());
			var errors = new List<CompilationError>();
			var compiled = source.Compile(new PythonCompilerOptions
			{
				ModuleName = name,
				Optimized = true
			}, new CustomErrorListener(errors));

			// try to execute the code to catch runtime errors
			var scope = _engine.CreateScope();
			compiled.Execute(scope);

			result = new(errors) { CompiledCode = compiled };
		}
		catch (SyntaxErrorException ex)
		{
			result = new([new()
			{
				Type = ex.Severity.ToErrorType(),
				Line = ex.Line,
				Character = ex.Column,
				Message = ex.Message,
				Id = ex.ErrorCode.ToString(),
			}]);
		}
		catch (Exception ex)
		{
			result = new([new()
			{
				Type = CompilationErrorTypes.Error,
				Message = ex.Message,
			}]);
		}

		return ((CompilationResult)result).FromResult();
	}

	object ICompiler.CreateContext() => _engine.CreateScope();
}