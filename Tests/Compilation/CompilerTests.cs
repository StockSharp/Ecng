namespace Ecng.Tests.Compilation;

using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Loader;

using Ecng.Compilation;
using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;
using Ecng.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[TestClass]
public class CompilerTests : BaseTestClass
{
	private static readonly string _coreLibPath = typeof(object).Assembly.Location;
	private static readonly IFileSystem _fs = LocalFileSystem.Instance;

#pragma warning disable RS1001, RS2008
	private sealed class HiddenDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "ECNGTEST001";

		private static readonly DiagnosticDescriptor _descriptor = new(
			DiagnosticId,
			"Hidden diagnostic",
			"Hidden diagnostic",
			"Tests",
			DiagnosticSeverity.Hidden,
			true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_descriptor);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxTreeAction(static ctx =>
			{
				var root = ctx.Tree.GetRoot(ctx.CancellationToken);
				ctx.ReportDiagnostic(Diagnostic.Create(_descriptor, root.GetLocation()));
			});
		}
	}
#pragma warning restore RS1001, RS2008

	[TestMethod]
	public async Task Compile()
	{
		ICompiler compiler = new CSharpCompiler();
		var res = await compiler.Compile("test", "class Class1 {}",
		[
			_coreLibPath,
		], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
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
		], _fs, CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public Task CompileCancel()
	{
		var cts = new CancellationTokenSource();
		cts.Cancel();

		ICompiler compiler = new CSharpCompiler();
		return ThrowsExactlyAsync<OperationCanceledException>(()
			=> compiler.Compile("test", "class Class1 {",
			[
				_coreLibPath,
			], _fs, cts.Token));
	}

	[TestMethod]
	public void AssemblyLoadContextWrapper_Dispose_DefaultContextDoesNotThrow()
	{
		using var context = AssemblyLoadContext.Default.ToContext();
	}

	[TestMethod]
	public async Task Analyse_HiddenDiagnostic_ReturnsInfo()
	{
		ICompiler compiler = new CSharpCompiler();

		var errors = await compiler.Analyse(
			new HiddenDiagnosticAnalyzer(),
			[],
			"test",
			["public class Class1 { }"],
			[_coreLibPath.ToRef(_fs)],
			CancellationToken);

		errors.Any(e => e.Id == HiddenDiagnosticAnalyzer.DiagnosticId && e.Type == CompilationErrorTypes.Info).AssertTrue();
	}

	[TestMethod]
	public async Task CSharpCompileWithWarnings()
	{
		ICompiler compiler = new CSharpCompiler();
		// Unused variable 'x' should produce a warning
		var code = "class Class1 { void M() { int x = 1; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
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
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
		res.Errors.Count().AssertGreater(1);
	}

	[TestMethod]
	public async Task CSharpCompilerSimpleSuccess()
	{
		ICompiler compiler = new CSharpCompiler();
		var code = "public class Foo { public int Bar() => 42; }";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task CSharpCompilerWarning()
	{
		ICompiler compiler = new CSharpCompiler();
		// Unused variable 'x' should produce a warning
		var code = "public class Foo { public void Bar() { int x = 1; } }";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
		res.Errors.Any(e => e.Type == CompilationErrorTypes.Warning).AssertTrue();
	}

	[TestMethod]
	public async Task PythonCompileSuccess()
	{
		ICompiler compiler = new PythonCompiler();
		var code = "def foo():\n    return 42";
		var res = await compiler.Compile("test", [code], [], CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}
		res.GetAssembly(compiler.CreateContext()).AssertNotNull();
	}

	[TestMethod]
	public async Task PythonCompileError()
	{
		ICompiler compiler = new PythonCompiler();
		// Syntax error: missing colon
		var code = "def foo()\n    return 42";
		var res = await compiler.Compile("test", [code], [], CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public async Task PythonCompileParallel_SharedEngine_ShouldFail()
	{
		// This test reproduces threading issues with shared ScriptEngine
		// Scripts that each define and use their own helper function
		var userScripts = Enumerable.Range(1, 20).Select(i => $@"
def helper_func_{i}(x):
    return x * 2

class Script{i}:
    def run(self):
        return helper_func_{i}({i})
").ToArray();

		// Single compiler instance (shared ScriptEngine)
		ICompiler compiler = new PythonCompiler();

		var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
		var successCount = 0;

		// Compile all scripts in parallel
		await Task.WhenAll(userScripts.Select(async (script, idx) =>
		{
			try
			{
				var res = await compiler.Compile($"script{idx}", [script], [], CancellationToken);
				if (res.HasErrors())
				{
					foreach (var err in res.Errors)
						errors.Add($"Script{idx}: {err.Message}");
				}
				else
				{
					var asm = res.GetAssembly(compiler.CreateContext());
					if (asm != null)
						Interlocked.Increment(ref successCount);
					else
						errors.Add($"Script{idx}: Assembly is null");
				}
			}
			catch (Exception ex)
			{
				errors.Add($"Script{idx}: Exception - {ex.Message}");
			}
		}));

		// If there are any errors, the test exposes the threading issue
		if (errors.Any())
		{
			Console.WriteLine($"Threading issues detected ({errors.Count} errors, {successCount} successes):");
			foreach (var err in errors.Take(10))
				Console.WriteLine($"  {err}");
		}

		// This assertion will likely fail due to threading issues
		successCount.AssertEqual(userScripts.Length);
	}

	[TestMethod]
	public async Task PythonCompileParallel_SharedScope_ShouldFail()
	{
		// This test simulates the real scenario: utility script loaded first,
		// then user scripts executed in parallel that reference utility functions
		var utilityScript = @"
def utility_add(a, b):
    return a + b

def utility_multiply(a, b):
    return a * b
";

		// User scripts that use the utility functions
		var userScripts = Enumerable.Range(1, 20).Select(i => $@"
class UserScript{i}:
    def calculate(self):
        return utility_add({i}, utility_multiply({i}, 2))
").ToArray();

		// Single compiler instance (shared ScriptEngine)
		ICompiler compiler = new PythonCompiler();

		var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
		var successCount = 0;

		// First compile and load utility script
		var utilityRes = await compiler.Compile("utility", [utilityScript], [], CancellationToken);
		utilityRes.HasErrors().AssertFalse();

		// Now compile user scripts in parallel - they need to include utility code
		await Task.WhenAll(userScripts.Select(async (script, idx) =>
		{
			try
			{
				// Include utility script with each user script
				var fullScript = utilityScript + "\n" + script;
				var res = await compiler.Compile($"user{idx}", [fullScript], [], CancellationToken);
				if (res.HasErrors())
				{
					foreach (var err in res.Errors)
						errors.Add($"User{idx}: {err.Message}");
				}
				else
				{
					var asm = res.GetAssembly(compiler.CreateContext());
					if (asm != null)
						Interlocked.Increment(ref successCount);
					else
						errors.Add($"User{idx}: Assembly is null");
				}
			}
			catch (Exception ex)
			{
				errors.Add($"User{idx}: Exception - {ex.Message}");
			}
		}));

		if (errors.Any())
		{
			Console.WriteLine($"Threading issues detected ({errors.Count} errors, {successCount} successes):");
			foreach (var err in errors.Take(10))
				Console.WriteLine($"  {err}");
		}

		successCount.AssertEqual(userScripts.Length);
	}

	[TestMethod]
	public async Task PythonCompileParallel_SeparateEngines_ShouldSucceed()
	{
		// This test shows that separate engines work correctly
		var utilityScript = @"
def helper_func(x):
    return x * 2
";

		var userScripts = Enumerable.Range(1, 10).Select(i => $@"
{utilityScript}

class Script{i}:
    def run(self):
        return helper_func({i})
").ToArray();

		var successCount = 0;

		// Compile all scripts in parallel with SEPARATE compiler instances
		await Task.WhenAll(userScripts.Select(async (script, idx) =>
		{
			// Each task gets its own compiler (and ScriptEngine)
			ICompiler compiler = new PythonCompiler();
			var res = await compiler.Compile($"script{idx}", [script], [], CancellationToken);
			if (!res.HasErrors())
			{
				var asm = res.GetAssembly(compiler.CreateContext());
				if (asm != null)
					Interlocked.Increment(ref successCount);
			}
		}));

		successCount.AssertEqual(userScripts.Length);
	}

	[TestMethod]
	public async Task PythonCompileParallel_StressTest()
	{
		// Stress test with many parallel compilations
		const int scriptCount = 50;
		const int iterations = 3;

		ICompiler compiler = new PythonCompiler();

		for (var iter = 0; iter < iterations; iter++)
		{
			var scripts = Enumerable.Range(1, scriptCount).Select(i => $@"
def func_{iter}_{i}(x):
    result = x
    for j in range({i}):
        result = result + j
    return result

class Script_{iter}_{i}:
    value = {i}

    def process(self):
        return func_{iter}_{i}(self.value)
").ToArray();

			var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
			var successCount = 0;

			await Task.WhenAll(scripts.Select(async (script, idx) =>
			{
				try
				{
					var res = await compiler.Compile($"stress_{iter}_{idx}", [script], [], CancellationToken);
					if (res.HasErrors())
					{
						foreach (var err in res.Errors)
							errors.Add($"Iter{iter}_Script{idx}: {err.Message}");
					}
					else
					{
						var ctx = compiler.CreateContext();
						var asm = res.GetAssembly(ctx);
						if (asm != null)
							Interlocked.Increment(ref successCount);
						else
							errors.Add($"Iter{iter}_Script{idx}: Assembly is null");
					}
				}
				catch (Exception ex)
				{
					errors.Add($"Iter{iter}_Script{idx}: {ex.GetType().Name} - {ex.Message}");
				}
			}));

			if (errors.Any())
			{
				Console.WriteLine($"Iteration {iter}: {errors.Count} errors, {successCount} successes");
				foreach (var err in errors.Take(5))
					Console.WriteLine($"  {err}");
			}

			successCount.AssertEqual(scriptCount);
		}
	}

	[TestMethod]
	public async Task PythonCompileParallel_WithFileImports_ShouldFail()
	{
		// This test reproduces the real issue: scripts that import from external .py files
		// When compiled in parallel, module imports can interfere with each other

		// Create temp directory for Python modules
		var tempDir = Path.Combine(Path.GetTempPath(), $"python_test_{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempDir);

		try
		{
			// Write utility modules to temp directory
			await File.WriteAllTextAsync(Path.Combine(tempDir, "data_utils.py"), @"
def load_data(source, param):
    return [param * i for i in range(10)]

def process_data(data, factor):
    return [x * factor for x in data]
");

			await File.WriteAllTextAsync(Path.Combine(tempDir, "chart_utils.py"), @"
def draw_chart(panel, data, title):
    return f'Chart: {title} with {len(data)} points'

def create_series(name, values):
    return {'name': name, 'values': list(values)}
");

			await File.WriteAllTextAsync(Path.Combine(tempDir, "indicator_utils.py"), @"
def calculate_sma(data, period):
    if len(data) < period:
        return []
    return [sum(data[i:i+period])/period for i in range(len(data)-period+1)]

def calculate_ema(data, period):
    if not data:
        return []
    multiplier = 2 / (period + 1)
    ema = [data[0]]
    for price in data[1:]:
        ema.append((price - ema[-1]) * multiplier + ema[-1])
    return ema
");

			// Create engine with search paths (simulating real setup)
			var engine = IronPython.Hosting.Python.CreateEngine();
			var paths = engine.GetSearchPaths();
			paths.Add(tempDir);
			engine.SetSearchPaths(paths);

			// Single compiler with shared engine
			ICompiler compiler = new PythonCompiler(engine);

			// Scripts that import from the utility modules
			var userScripts = Enumerable.Range(1, 30).Select(i => $@"
from data_utils import load_data, process_data
from chart_utils import draw_chart, create_series
from indicator_utils import calculate_sma, calculate_ema

class AnalyticsScript_{i}:
    '''Analytics script {i}'''

    def run(self, panel, securities):
        results = []
        for sec in securities:
            data = load_data(sec, {i})
            processed = process_data(data, 2)
            sma = calculate_sma(processed, 3)
            ema = calculate_ema(processed, 3)
            series = create_series(f'SMA_{i}', sma)
            chart = draw_chart(panel, ema, f'Script{i}')
            results.append((series, chart))
        return results
").ToArray();

			var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
			var successCount = 0;

			// Compile all scripts in parallel
			await Task.WhenAll(userScripts.Select(async (script, idx) =>
			{
				try
				{
					var res = await compiler.Compile($"analytics_{idx}", [script], [], CancellationToken);
					if (res.HasErrors())
					{
						foreach (var err in res.Errors)
							errors.Add($"Analytics{idx}: {err.Message}");
					}
					else
					{
						var ctx = compiler.CreateContext();
						var asm = res.GetAssembly(ctx);
						if (asm != null)
							Interlocked.Increment(ref successCount);
						else
							errors.Add($"Analytics{idx}: Assembly is null");
					}
				}
				catch (Exception ex)
				{
					errors.Add($"Analytics{idx}: {ex.GetType().Name} - {ex.Message}");
				}
			}));

			if (errors.Any())
			{
				Console.WriteLine($"File import threading issues ({errors.Count} errors, {successCount} successes):");
				foreach (var err in errors.Take(10))
					Console.WriteLine($"  {err}");
			}

			successCount.AssertEqual(userScripts.Length);
		}
		finally
		{
			// Cleanup
			try { Directory.Delete(tempDir, true); } catch { }
		}
	}

	[TestMethod]
	public async Task FSharpCompileSuccess()
	{
		// TODO: F# compiler is not available on non-Windows platforms.
		if (!OperatingSystemEx.IsWindows())
			return;

		ICompiler compiler = new FSharpCompiler();
		var code = "module Foo\nlet bar () = 42";
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
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
		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);
		res.GetAssembly(compiler.CreateContext()).AssertNull();
		res.HasErrors().AssertTrue();
	}

	[TestMethod]
	public void AssemblyReference_Location_WithFullPath_ShouldPreservePath()
	{
		// Bug: When FileName is set to a full path, Location should return that path,
		// not just the filename. Currently Path.GetFileName() strips the directory.
		var fullPath = Path.Combine(Path.GetTempPath(), "CustomLibs", "MyLibrary.dll");

		var asmRef = new AssemblyReference
		{
			FileName = fullPath
		};

		// Location should preserve the full path when it's provided
		// Currently this fails because Location returns just "MyLibrary.dll"
		asmRef.Location.AssertEqual(fullPath);
	}

	[TestMethod]
	public void AssemblyReference_Location_WithRelativePath_ShouldPreservePath()
	{
		// Bug: When FileName is a relative path, Location should preserve it
		var relativePath = Path.Combine("libs", "subdir", "MyLibrary.dll");

		var asmRef = new AssemblyReference
		{
			FileName = relativePath
		};

		// Location should preserve the relative path
		// Currently this fails because Location returns just "MyLibrary.dll"
		asmRef.Location.AssertEqual(relativePath);
	}

	[TestMethod]
	public void AssemblyReference_Location_WithJustFileName_ShouldSearchRuntime()
	{
		// When FileName is just a filename (no path), Location should work as before:
		// look in RuntimePath first, otherwise return just the filename
		var fileName = "System.Runtime.dll";

		var asmRef = new AssemblyReference
		{
			FileName = fileName
		};

		// Since System.Runtime.dll exists in runtime path, it should find it there
		var location = asmRef.Location;
		location.IsEmpty().AssertFalse();
		Path.GetFileName(location).AssertEqual(fileName);
	}

	// The F# toolchain (FSharp.Compiler.Service) is only loadable on Windows in this repo,
	// mirroring the guard used by the existing FSharpCompileSuccess/FSharpCompileError tests.
	private static bool FSharpAvailable => OperatingSystemEx.IsWindows();

	/// <summary>
	/// BUG: FSharpCompiler.Analyse deconstructs ParseAndCheckFileInProject into (results, answer)
	/// but only collects results.Diagnostics (parse-stage only); the type-check answer carrying the
	/// semantic diagnostics is never inspected (FSharpCompiler.cs:145,147).
	/// Expected: source that parses cleanly but fails type checking (here an undefined identifier)
	/// reports at least one error, like RoslynCompiler.Analyse does.
	/// Actual: Analyse returns an empty error list, so broken F# is treated as valid.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Analyse_SemanticError_IsReported()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		// Parses fine, but 'undefinedIdentifier' is not defined => a type-check (semantic) error.
		var code = "module Foo\nlet bar () = undefinedIdentifier 42";

		var errors = await compiler.Analyse(
			null,
			[],
			"test",
			[code],
			[_coreLibPath.ToRef(_fs)],
			CancellationToken);

		errors.HasErrors().AssertTrue();
	}

	/// <summary>
	/// BUG: FSharpCompiler.Analyse builds its project options with an empty source list and an empty
	/// in-memory file system (CreateOptions(name, [], refs) and new FileSystemContext([], new()) at
	/// FSharpCompiler.cs:134,140), so reference bodies are never readable and the supplied sources are
	/// not registered. Any source consuming a type from a passed reference yields false
	/// "assembly not found" / "identifier not defined" diagnostics once the dropped type-check
	/// diagnostics (finding 1) are restored.
	/// Expected: valid F# that uses a type from a supplied reference analyses without errors.
	/// Actual: references cannot resolve, so the code is reported as broken (false positives).
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Analyse_ValidCodeUsingReference_HasNoFalseErrors()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		// Uses System.String from the supplied core reference; must resolve cleanly.
		var code = "module Foo\nlet bar () : string = System.String.Empty";

		var errors = await compiler.Analyse(
			null,
			[],
			"test",
			[code],
			[_coreLibPath.ToRef(_fs)],
			CancellationToken);

		errors.HasErrors().AssertFalse();
	}

	/// <summary>
	/// BUG: ToError maps the 1-based FSharpDiagnostic.StartLine straight into CompilationError.Line
	/// (FSharpCompiler.cs:201), whereas the Roslyn implementation fills Line from the 0-based
	/// Location line position. The same UI maps CompilationError.Line onto editor lines for every
	/// ICompiler, so F# errors are highlighted one line below the actual location.
	/// Expected: an error on the 3rd physical source line (0-based index 2) is reported with Line == 2.
	/// Actual: Line == 3 (FCS 1-based value passed through unchanged).
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Compile_ErrorLine_IsZeroBased()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		// Line 0: module Foo
		// Line 1: let ok () = 1
		// Line 2: let bad () = undefinedIdentifier 42   <- the error lives here (0-based line 2).
		var code = "module Foo\nlet ok () = 1\nlet bad () = undefinedIdentifier 42";

		var res = await compiler.Compile("test", [code], [_coreLibPath], _fs, CancellationToken);

		res.HasErrors().AssertTrue();

		var error = res.Errors.ErrorsOnly().First();
		error.Line.AssertEqual(2);
	}

	/// <summary>
	/// BUG: FSharpCompiler.Compile inserts reference bodies into a Dictionary via Add()
	/// (FSharpCompiler.cs:168-169); a refs sequence with a repeated file name (realistic when merging
	/// default and user references) throws ArgumentException "An item with the same key has already
	/// been added", crashing the whole compilation. RoslynCompiler tolerates duplicate references.
	/// Expected: Compile tolerates duplicate reference names and returns a CompilationResult.
	/// Actual: Add() throws ArgumentException and the call fails outright.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Compile_DuplicateReferenceNames_DoesNotThrow()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		ICompiler compiler = new FSharpCompiler();

		var coreRef = _coreLibPath.ToRef(_fs);

		// Same reference name supplied twice (e.g. resolved from two places).
		IEnumerable<(string name, byte[] body)> refs = [coreRef, coreRef];

		var code = "module Foo\nlet bar () = 42";

		var res = await compiler.Compile("test", [code], refs, CancellationToken);

		res.AssertNotNull();
	}

	/// <summary>
	/// BUG: every reference is presented to FCS under a constant synthetic path
	/// Path.Combine(Path.GetTempPath(), r.name) and InMemoryFileSystem overrides only
	/// FileExistsShim/OpenFileForReadShim/OpenFileForWriteShim, never GetLastWriteTimeShim
	/// (FSharpCompiler.cs:115). The FCS IL-module-reader cache keys assembly metadata by
	/// (path, last-write-time); the missing-on-disk synthetic path makes GetLastWriteTimeShim
	/// fall back to a constant timestamp, so a second Compile reusing the same reference name but a
	/// rebuilt body keeps the stale metadata of the first body.
	/// Expected: recompiling against a reference of the same name but new content sees the new types.
	/// Actual: the stale cached metadata is reused, so the freshly added type fails to resolve.
	/// </summary>
	[TestMethod]
	[TestCategory("Integration")]
	public async Task Compile_RebuiltReferenceSameName_UsesFreshMetadata()
	{
		if (!FSharpAvailable)
		{
			Inconclusive("F# compiler is only available on Windows.");
			return;
		}

		// Build two distinct assemblies that share the same simple name "MyLib" but expose
		// different public types: v1 has TypeA, v2 has TypeB. A single FSharpCompiler instance
		// (one FCS checker, one module-reader cache) then compiles against each in turn.
		var libV1 = await BuildReferenceAsync("namespace MyLib { public class TypeA { } }");
		var libV2 = await BuildReferenceAsync("namespace MyLib { public class TypeB { } }");

		const string refName = "MyLib.dll";
		var coreRef = _coreLibPath.ToRef(_fs);

		ICompiler compiler = new FSharpCompiler();

		// First compile resolves TypeA against the v1 body. This populates the FCS cache for
		// the synthetic reference path keyed by the (constant) write time.
		var resV1 = await compiler.Compile(
			"useA",
			["module UseA\nlet a = MyLib.TypeA()"],
			[coreRef, (refName, libV1)],
			CancellationToken);

		resV1.HasErrors().AssertFalse();

		// Second compile presents the SAME reference name but the rebuilt v2 body and uses the
		// newly added TypeB. The fresh metadata must be honoured.
		var resV2 = await compiler.Compile(
			"useB",
			["module UseB\nlet b = MyLib.TypeB()"],
			[coreRef, (refName, libV2)],
			CancellationToken);

		resV2.HasErrors().AssertFalse();
	}

	// Compiles a tiny C# library to raw assembly bytes used as an in-memory F# reference body.
	private async Task<byte[]> BuildReferenceAsync(string source)
	{
		ICompiler csharp = new CSharpCompiler();

		var res = await csharp.Compile("MyLib", source, [_coreLibPath], _fs, CancellationToken);

		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Reference build failed:\n{errors.JoinNL()}");
		}

		var body = ((AssemblyCompilationResult)res).AssemblyBody;
		IsNotNull(body, "Reference build produced no assembly body.");
		return body;
	}

	// Compiles a single-class Python module and returns the reflected type produced by the
	// IronPython reflection layer (TypeImpl) for the requested class name. This is the exact
	// public path real consumers take: Compile -> GetAssembly -> GetTypes.
	private Type CompilePythonType(string code, string typeName)
	{
		ICompiler compiler = new PythonCompiler();

		var res = compiler.Compile("audit", [code], [], CancellationToken).Result;

		if (res.HasErrors())
		{
			var errors = res.Errors.Select(e => $"{e.Type}: {e.Message}").ToArray();
			Fail($"Compilation errors:\n{errors.JoinNL()}");
		}

		var asm = res.GetAssembly(compiler.CreateContext());
		IsNotNull(asm);

		var type = asm.GetTypes().FirstOrDefault(t => t.Name == typeName);
		IsNotNull(type, $"Type '{typeName}' not found in compiled assembly.");
		return type;
	}

	/// <summary>
	/// BUG: GetMethodImpl evaluates m.GetParameters().Select(p =&gt; p.ParameterType).SequenceEqual(types)
	/// unconditionally, but Type.GetMethod(string) passes types == null, so SequenceEqual throws
	/// ArgumentNullException as soon as a name match is found (PythonContext.cs:675).
	/// Expected: type.GetMethod("run") resolves the Python method by name without throwing.
	/// Actual: ArgumentNullException ("Value cannot be null. (Parameter 'second')") is thrown.
	/// </summary>
	[TestMethod]
	public void GetMethodByName_DoesNotThrowAndResolves()
	{
		var type = CompilePythonType("class C1:\n    def run(self):\n        return 42\n", "C1");

		// Standard reflection lookup by name only (no parameter types).
		var method = type.GetMethod("run");

		IsNotNull(method, "GetMethod(\"run\") must resolve the Python method.");
		AreEqual("run", method.Name);
	}

	/// <summary>
	/// BUG: MethodImpl.Invoke spreads parameters via [obj, .. parameters], which throws
	/// NullReferenceException when parameters is null - yet MethodInfo.Invoke(obj, null) is the
	/// canonical way to call a parameterless method (PythonContext.cs:266).
	/// Expected: invoking a no-argument Python method with null parameters returns its value.
	/// Actual: NullReferenceException is thrown while spreading the null parameters array.
	/// </summary>
	[TestMethod]
	public void Invoke_NullParameters_DoesNotThrow()
	{
		var type = CompilePythonType("class C2:\n    def get_value(self):\n        return 42\n", "C2");

		var instance = ((ITypeConstructor)type).CreateInstance([]);
		IsNotNull(instance);

		var method = type
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
			.First(m => m.Name == "get_value");

		// Canonical reflection call of a parameterless method: pass null for the arguments.
		var result = method.Invoke(instance, null);

		AreEqual(42, result.To<int>());
	}

	/// <summary>
	/// BUG: GetEvents materializes a Select that yields null for an add_-prefixed function lacking a
	/// matching remove_ pair, caches those nulls in _events, then dereferences them via
	/// e.GetAddMethod(), so GetEvents/GetEvent/GetMembers throw NullReferenceException forever once the
	/// poisoned cache is built (PythonContext.cs:627,634,637).
	/// Expected: a class exposing an ordinary add_item method (no remove_item) reflects its events
	/// without throwing, and the add_item method stays reachable through GetMethods.
	/// Actual: GetEvents throws NullReferenceException because of the cached null event.
	/// </summary>
	[TestMethod]
	public void GetEvents_UnpairedAddMethod_DoesNotThrow()
	{
		var type = CompilePythonType("class C4:\n    def add_item(self, x):\n        return x\n", "C4");

		// Must not throw: the unpaired add_item is simply not an event.
		var events = type.GetEvents();
		IsNotNull(events);

		// The add_item function must remain reachable as a regular method.
		var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
		IsTrue(methods.Any(m => m.Name == "add_item"), "add_item must remain reachable as a method.");
	}

	/// <summary>
	/// BUG: GetInterfaces returns _dotNetBaseType.GetInterfaces(); _dotNetBaseType walks the BaseType
	/// chain to System.Object, so interfaces implemented directly by the Python class are lost, even
	/// though IsAssignableFrom (via UnderlyingSystemType) reports them as implemented
	/// (PythonContext.cs:555).
	/// Expected: a Python class declaring class C(System.IDisposable) lists IDisposable in
	/// GetInterfaces().
	/// Actual: GetInterfaces() returns an empty array and IDisposable is missing.
	/// </summary>
	[TestMethod]
	public void GetInterfaces_DirectlyImplementedDotNetInterface_IsListed()
	{
		var code =
			"import System\n" +
			"class C6(System.IDisposable):\n" +
			"    def Dispose(self):\n" +
			"        pass\n";

		var type = CompilePythonType(code, "C6");

		var interfaces = type.GetInterfaces();

		IsTrue(interfaces.Contains(typeof(IDisposable)),
			"GetInterfaces() must include the directly implemented .NET interface.");
	}

	/// <summary>
	/// BUG: the event name is extracted with StringHelper.Remove(_eventAddPrefix, true), which strips
	/// every occurrence of "add_" anywhere in the name rather than only the leading prefix; an
	/// add_pre_add_check function becomes event "pre_check", the remove_pre_check lookup fails, the
	/// event is silently dropped (and a null is cached, see finding 4) (PythonContext.cs:624).
	/// Expected: only the leading prefix is removed, yielding event "pre_add_check" resolvable via
	/// GetEvent with its matching remove_pre_add_check pair.
	/// Actual: the name collapses to "pre_check", the pair is not found and GetEvent throws/returns null.
	/// </summary>
	[TestMethod]
	public void EventName_StripsOnlyLeadingPrefix()
	{
		var code =
			"class C7:\n" +
			"    def add_pre_add_check(self, h):\n" +
			"        pass\n" +
			"    def remove_pre_add_check(self, h):\n" +
			"        pass\n";

		var type = CompilePythonType(code, "C7");

		var evt = type.GetEvent("pre_add_check");

		IsNotNull(evt, "Event name must keep the inner 'add_' and resolve as 'pre_add_check'.");
		AreEqual("pre_add_check", evt.Name);
	}

	/// <summary>
	/// BUG: in the typed GetCustomAttributes overload, the else-if branch matched for
	/// typeof(DisplayAttribute) returns nothing when display_name/__doc__ are absent and never falls
	/// through to the __dict__ scan, so a real DisplayAttribute stored in the class __dict__ is
	/// unreachable via GetCustomAttributes(typeof(DisplayAttribute)) even though the untyped overload
	/// returns it (PythonContext.cs:88).
	/// Expected: GetCustomAttributes(typeof(DisplayAttribute), false) returns the DisplayAttribute
	/// present in __dict__.
	/// Actual: an empty array is returned because the dictionary fallback is skipped.
	/// </summary>
	[TestMethod]
	public void TypedGetCustomAttributes_DisplayAttributeFromDict_IsFound()
	{
		// Reference the DisplayAttribute assembly by its simple name so IronPython reuses the
		// assembly already loaded in the host. Loading a fresh copy by path would give the attribute
		// a distinct Type identity, and attr.GetType().Is(typeof(DisplayAttribute)) would never match.
		var annotationsAsm = typeof(DisplayAttribute).Assembly.GetName().Name;

		var code =
			"import clr\n" +
			$"clr.AddReference('{annotationsAsm}')\n" +
			"from System.ComponentModel.DataAnnotations import DisplayAttribute\n" +
			"class C9:\n" +
			"    my_display = DisplayAttribute()\n";

		var type = CompilePythonType(code, "C9");

		// Sanity: the untyped overload sees the attribute living in __dict__.
		IsTrue(type.GetCustomAttributes(false).OfType<DisplayAttribute>().Any(),
			"Untyped GetCustomAttributes must see the DisplayAttribute in __dict__.");

		// The typed overload must find the same attribute.
		var typed = type.GetCustomAttributes(typeof(DisplayAttribute), false);

		IsTrue(typed.OfType<DisplayAttribute>().Any(),
			"Typed GetCustomAttributes(DisplayAttribute) must return the attribute from __dict__.");
	}

	/// <summary>
	/// BUG: TypeImpl.InvokeMember resolves a PythonFunction member and calls
	/// pythonFunction.__call__(DefaultContext.Default, target, args). Because __call__ is declared
	/// __call__(CodeContext, params object[] args), the args array is NOT spread - it is wrapped as a
	/// single positional argument, so the Python function receives (target, args[]) instead of
	/// (target, arg0, arg1, ...) (PythonContext.cs:575).
	/// Expected: invoking a member with one argument forwards that argument as a scalar value.
	/// Actual: the function receives the raw object[] array as the argument (e.g. an Object[] instead
	/// of the string "only").
	/// </summary>
	[TestMethod]
	public void InvokeMember_SpreadsArgsNotAsSingleArray()
	{
		// A @staticmethod accessed off an instance target is resolved as a raw PythonFunction, which
		// drives the buggy InvokeMember branch. Its first parameter receives the target, the rest
		// receive the (correctly spread) call arguments.
		var code =
			"class C3:\n" +
			"    @staticmethod\n" +
			"    def grab(recv, value):\n" +
			"        return value\n";

		var type = CompilePythonType(code, "C3");

		var instance = ((ITypeConstructor)type).CreateInstance([]);
		IsNotNull(instance);

		// Pass exactly one argument. With correct spreading the Python 'value' parameter must be the
		// scalar string itself, not an object[] wrapping it.
		var result = type.InvokeMember("grab", BindingFlags.InvokeMethod, null, instance, ["only"], null, null, null);

		AreEqual("only", result);
	}
}
