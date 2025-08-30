namespace Ecng.Tests.Compilation;

using System.Threading;

using Ecng.Compilation;
using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;

[TestClass]
public class CompilerTests
{
	private static readonly string _coreLibPath = typeof(object).Assembly.Location;

	[TestMethod]
	public async Task Compile()
	{
		ICompiler compiler = new CSharpCompiler();
		var res = await compiler.Compile("test", "class Class1 {}",
		[
			_coreLibPath,
		]);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Assert.Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task CompileError()
	{
		ICompiler compiler = new CSharpCompiler();
		var res = await compiler.Compile("test", "class Class1 {",
		[
			_coreLibPath,
		]);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public Task CompileCancel()
	{
		var cts = new CancellationTokenSource();
		cts.Cancel();

		ICompiler compiler = new CSharpCompiler();
		return Assert.ThrowsExactlyAsync<OperationCanceledException>(()
			=> compiler.Compile("test", "class Class1 {",
			[
				_coreLibPath,
			], cts.Token));
	}

	[TestMethod]
	public async Task CSharpCompileWithWarnings()
	{
		ICompiler compiler = new CSharpCompiler();
		// Unused variable 'x' should produce a warning
		var code = "class Class1 { void M() { int x = 1; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath]);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Assert.Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
		res.Errors.Any(e => e.Type == CompilationErrorTypes.Warning).AssertTrue();
	}

	[TestMethod]
	public async Task CSharpCompileMultipleErrors()
	{
		ICompiler compiler = new CSharpCompiler();
		// Two errors: missing semicolon, and undefined variable
		var code = "class Class1 { void M() { int x = ; y = 2; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath]);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
		res.Errors.Count().AssertGreater(1);
	}

	[TestMethod]
	public async Task CSharpCompilerSimpleSuccess()
	{
		ICompiler compiler = new CSharpCompiler();
		var code = "public class Foo { public int Bar() => 42; }";
		var res = await compiler.Compile("test", [code], [_coreLibPath]);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Assert.Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task CSharpCompilerWarning()
	{
		ICompiler compiler = new CSharpCompiler();
		// Unused variable 'x' should produce a warning
		var code = "public class Foo { public void Bar() { int x = 1; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath]);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Assert.Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
		res.Errors.Any(e => e.Type == CompilationErrorTypes.Warning).AssertTrue();
	}

	[TestMethod]
	public async Task PythonCompileSuccess()
	{
		ICompiler compiler = new PythonCompiler();
		var code = "def foo():\n    return 42";
		var res = await compiler.Compile("test", [code], []);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Assert.Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task PythonCompileError()
	{
		ICompiler compiler = new PythonCompiler();
		// Syntax error: missing colon
		var code = "def foo()\n    return 42";
		var res = await compiler.Compile("test", [code], []);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public async Task FSharpCompileSuccess()
	{
		// TODO: F# compiler is not available on non-Windows platforms.
		if (!OperatingSystemEx.IsWindows())
			return;

		ICompiler compiler = new FSharpCompiler();
		var code = "module Foo\nlet bar () = 42";
		var res = await compiler.Compile("test", [code], [_coreLibPath]);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Assert.Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task FSharpCompileError()
	{
		// TODO: F# compiler is not available on non-Windows platforms.
		if (!OperatingSystemEx.IsWindows())
			return;

		ICompiler compiler = new FSharpCompiler();
		// Syntax error: missing '='
		var code = "module Foo\nlet bar () 42";
		var res = await compiler.Compile("test", [code], [_coreLibPath]);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}
}